using System.Text.RegularExpressions;

namespace Tweey.WorldData;

internal partial class World
{
    public ResourceTemplates Resources { get; }
    public PlantTemplates PlantTemplates { get; }
    public BuildingTemplates BuildingTemplates { get; }
    public ThoughtTemplates ThoughtTemplates { get; }
    public RoomTemplates RoomTemplates { get; }
    public Configuration Configuration { get; }
    public Biomes Biomes { get; }

    public List<Room> Rooms { get; } = new();
    public TerrainCell[,]? TerrainCells { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }

    internal Entity? SelectedEntity { get; set; }
    public CurrentWorldTemplate CurrentWorldTemplate { get; } = new();
    public Vector2i? CurrentTemplateStartPoint { get; set; }

    public double TimeSpeedUp { get; set; } = 1;

    public bool ShowDetails { get; private set; }
    public bool DebugShowLightAtMouse { get; private set; }

    /// <summary>
    /// The position of the mouse in screen coordinates.
    /// </summary>
    public Vector2i MouseScreenPosition { get; private set; }

    /// <summary>
    /// The position of the mouse in world coordinates.
    /// </summary>
    public Vector2 MouseWorldPosition { get; private set; }

    public Vector2 RawOffset { get; set; }
    public Vector2 Offset =>
        RawOffset - (dragStart is null ? Vector2.Zero : (MouseScreenPosition - dragStart.Value).ToNumericsVector2() / Zoom);
    public float Zoom { get; set; } = 35;

    Vector2 deltaOffsetNextFrame;
    static readonly Vector2 deltaOffsetPerSecond = new(10f);
    float deltaZoomNextFrame;

    public World(ILoader loader)
    {
        (Resources, Configuration, ThoughtTemplates) = (new(loader), new(loader), new(loader));
        (BuildingTemplates, PlantTemplates) = (new(loader, Resources), new(loader, Resources));
        Biomes = new(loader, PlantTemplates);
        RoomTemplates = new(loader, BuildingTemplates);
    }

    [MemberNotNull(nameof(TerrainCells))]
    public void GenerateMap(int width, int height, out Vector2i embarkmentLocation)
    {
        Width = width;
        Height = height;

        // generate the terrain
        var map = MapGeneration.Generate(width, height,
            new MapGenerationWave[]
            {
                new(Random.Shared.Next(), 0.004f, 1),
                new(Random.Shared.Next(), 0.02f, 0.5f),
            },
            new MapGenerationWave[]
            {
                new(Random.Shared.Next(), 0.02f, 1),
            },
            new MapGenerationWave[]
            {
                new(Random.Shared.Next(), 0.02f, 1),
                new(Random.Shared.Next(), 0.01f, 0.5f),
            },
            Biomes.Values.Select(b => new MapGenerationBiome(b.MinHeight, b.MinMoisture, b.MinHeat)).ToArray());

        var biomeTiles = DiskLoader.Instance.VFS.EnumerateFiles("Data/Biomes", SearchOption.AllDirectories)
            .Select(p => ExtractBiomeNameFromPathRegex().Match(p))
            .Where(m => m.Success)
            .GroupBy(w => w.Groups[1].Value)
            .ToDictionary(w => w.Key, w => w.Select(ww => ww.Groups[0].Value).ToList());

        TerrainCells = new TerrainCell[width, height];
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var biome = Biomes[map[x, y].BiomeIndex];
                ref var tile = ref TerrainCells[x, y];
                tile = new(biomeTiles[biome.TileName].RandomSubset(1).First(), (float)biome.MovementModifier);
                tile.BuildingEntity = Entity.Invalid;

                // plant a tree?
                foreach (var (template, chance) in biome.Plants)
                    if (Random.Shared.NextDouble() < chance)
                    {
                        AddPlantEntity(template, new(x, y), true, false);
                        break;
                    }
            }

        const int embarkmentRadius = 5;

        {
            retry:
            var (x, y) = (Random.Shared.Next(embarkmentRadius, width - embarkmentRadius), Random.Shared.Next(embarkmentRadius, height - embarkmentRadius));
            for (var ty = y - embarkmentRadius; ty < y + embarkmentRadius; ty++)
                for (var tx = x - embarkmentRadius; tx < x + embarkmentRadius; tx++)
                    if (TerrainCells[tx, ty].Impassable)
                        goto retry;
            embarkmentLocation = new(x, y);
        }
    }

    public Room? GetRoomAtWorldLocationAsNullable(Vector2i location)
    {
        foreach (ref var room in CollectionsMarshal.AsSpan(Rooms))
            if (room.Locations.Contains(location))
                return room;
        return null;
    }

    public ref Room GetRoomAtWorldLocation(Vector2i location)
    {
        foreach (ref var room in CollectionsMarshal.AsSpan(Rooms))
            if (room.Locations.Contains(location))
                return ref room;
        return ref Unsafe.NullRef<Room>();
    }

    #region AddEntities
    public Entity AddVillagerEntity(string name, Vector2 location)
    {
        var entity = EcsCoordinator.CreateEntity();
        entity.AddLocationComponent(Box2.FromCornerSize(location, new(1, 1)));
        entity.AddRenderableComponent("Data/Misc/villager.png",
            LightEmission: Colors4.White, LightRange: 12, LightAngleRadius: .1f);
        entity.AddHeadingComponent();
        entity.AddWorkerComponent()
            .SystemJobPriorities = EcsCoordinator.AISystem!.SystemJobs.Select(j => j.IsConfigurable ? 2 : -1).ToArray();
        entity.AddInventoryComponent();
        entity.AddIdentityComponent(name);
        entity.AddThoughtWhenInRangeComponent(ThoughtTemplates[ThoughtTemplates.FriendSeen], TimeSpan.FromDays(.4), 5);

        var villagerComponent = entity.AddVillagerComponent(Configuration.Data.BaseCarryWeight, Configuration.Data.BasePickupSpeed, Configuration.Data.BaseMovementSpeed,
            Configuration.Data.BaseWorkSpeed, Configuration.Data.BaseHarvestSpeed, Configuration.Data.BasePlantSpeed,
            Configuration.Data.BaseTiredMax, Configuration.Data.BaseTiredDecayPerWorldSecond, Configuration.Data.BasePoopMax, Configuration.Data.BasePoopDecayPerWorldSecond,
            Configuration.Data.BaseHungerMax, Configuration.Data.BaseHungerDecayPerWorldSecond);
        villagerComponent.AddThought(this, ThoughtTemplates[ThoughtTemplates.ExtremelyLowExpectations]);

        return entity;
    }

    public Entity AddPoopEntity(Vector2 location, CustomDateTime expirationDate)
    {
        var entity = EcsCoordinator.CreateEntity();
        entity.AddLocationComponent(Box2.FromCornerSize(location, new(1, 1)));
        entity.AddRenderableComponent("Data/Misc/poop.png");
        entity.AddPoopComponent();
        entity.AddIdentityComponent("Poop");
        entity.AddExpirationComponent(expirationDate);
        entity.AddThoughtWhenInRangeComponent(ThoughtTemplates[ThoughtTemplates.PoopSeen], TimeSpan.FromDays(1.0 / 6), 20);

        return entity;
    }

    public Entity AddPlantEntity(PlantTemplate plantTemplate, Vector2 location, bool isMature, bool isFarmed)
    {
        var entity = EcsCoordinator.CreateEntity();
        entity.AddLocationComponent(Box2.FromCornerSize(location, new(1, 1)));
        entity.AddRenderableComponent($"Data/Plants/{plantTemplate.FileName}.png",
            OcclusionCircle: true, OcclusionScale: plantTemplate.IsOccludingLight ? .3f : 0f);
        entity.AddWorkableComponent();
        entity.AddPlantComponent(plantTemplate, plantTemplate.HarvestWorkTicks, isMature ? CustomDateTime.Invalid : WorldTime);
        entity.AddIdentityComponent(plantTemplate.Name);
        entity.AddInventoryComponent().Inventory
            .Add(ResourceMarker.All, plantTemplate.Inventory, ResourceMarker.Unmarked);
        if (isFarmed)
            entity.AddPlantIsFarmedComponent();

        if (plantTemplate.IsTree)
            TerrainCells![(int)Math.Round(location.X), (int)Math.Round(location.Y)].AboveGroundMovementModifier =
                (float)Configuration.Data.TreeMovementModifier;

        return entity;
    }

    public IEnumerable<Entity> AddResourceEntities(ResourceMarker srcMarker, ResourceBucket srcRB, ResourceMarker dstMarker, Vector2 location)
    {
        // obey the maximum ground stack weight
        var result = new List<Entity>();
        using var availableNeighbors = CollectionPool<(Vector2i pt, Entity? entity, ResourceBucket? rb)>.Get();

        var availableNeighborsSearchRadius = -1;
        while (!srcRB.IsEmpty(srcMarker))
        {
            // take any spill-over and put it in a random direction, up to maximumGroundDropSpillOverRange range
            while (availableNeighbors.Count == 0)
            {
                ++availableNeighborsSearchRadius;

                foreach (var dv in GameUtilities.EnumerateNeighborLocations(location, radiusMin: availableNeighborsSearchRadius, radiusMax: availableNeighborsSearchRadius))
                {
                    var okay = true;
                    ResourceBucket? foundResourceBucket = default;
                    Entity? foundEntity = default;

                    EcsCoordinator.IteratePlacedResourceArchetype((in EcsCoordinator.PlacedResourceIterationResult w) =>
                    {
                        if (w.LocationComponent.Box.Contains(dv.ToNumericsVector2Center()))
                            if (w.InventoryComponent.Inventory.GetWeight(dstMarker) >= Configuration.Data.GroundStackMaximumWeight)
                            {
                                okay = false;
                                return false;
                            }
                            else
                                (foundEntity, foundResourceBucket) = (w.Entity, w.InventoryComponent.Inventory);
                        return true;
                    });

                    if (okay)
                        availableNeighbors.Add((dv, foundEntity, foundResourceBucket));
                }
            }

            var chosenNeighborIndex = Random.Shared.Next(availableNeighbors.Count);
            var chosenNeighbor = availableNeighbors[chosenNeighborIndex];
            availableNeighbors.Remove(chosenNeighbor);

            var newRB = chosenNeighbor.rb;
            var newEntity = chosenNeighbor.entity ?? Entity.Invalid;
            if (newRB is null)
            {
                newEntity = EcsCoordinator.CreateEntity();
                newEntity.AddRenderableComponent(null);
                newEntity.AddLocationComponent(Box2.FromCornerSize(chosenNeighbor.pt, new(1, 1)));
                newEntity.AddResourceComponent();
                newRB = newEntity.AddInventoryComponent().Inventory;
                newEntity.AddIdentityComponent();
            }
            result.Add(newEntity);
            var newRBWeight = newRB.GetWeight(dstMarker);

            foreach (var resQ in srcRB.GetResourceQuantities(srcMarker).Where(w => !w.IsEmpty))
                // only allow one resource kind on the ground
                if (!newRB.GetResourceQuantities(dstMarker).Any() || newRB.GetResourceQuantities(dstMarker).Any(rq => rq.Resource == resQ.Resource))
                {
                    var maxNewWeight = Configuration.Data.GroundStackMaximumWeight - newRBWeight;
                    var quantityToMove = (int)Math.Floor(Math.Min(maxNewWeight, resQ.Weight) / resQ.Resource.Weight);

                    var newResQ = new ResourceQuantity(resQ.Resource, quantityToMove);
                    newRB.Add(newResQ, dstMarker);
                    srcRB.Remove(newResQ);
                    if (!resQ.IsEmpty)
                        break;  // couldn't finish the stack
                    newRBWeight += resQ.Resource.Weight * quantityToMove;
                }

            if (newRB.GetResourceQuantities(dstMarker).FirstOrDefault() is { } newRQ)
            {
                // once we finished this resource clump, set its name and render image
                newEntity.GetIdentityComponent().Name = newRQ.Resource.Name;
                newEntity.GetRenderableComponent().AtlasEntryName = $"Data/Resources/{newRQ.Resource.FileName}.png";
            }
        }

        return result;
    }

    public Entity AddBuildingEntity(BuildingTemplate buildingTemplate, Vector2i location, bool isBuilt)
    {
        var box = Box2.FromCornerSize(location, buildingTemplate.Width, buildingTemplate.Height);

        // if there's buildings in the way already, we're replacing them all
        foreach (var testLocation in box)
        {
            if (TerrainCells![testLocation.X, testLocation.Y].BuildingEntity.HasBuildingComponent())
                DeleteEntity(TerrainCells![testLocation.X, testLocation.Y].BuildingEntity);
        }

        var entity = EcsCoordinator.CreateEntity();
        ref var locationComponent = ref entity.AddLocationComponent(box);
        entity.AddRenderableComponent($"Data/Buildings/{buildingTemplate.FileName}.png",
            OcclusionScale: buildingTemplate.EmitLight is null ? 1 : 0,
            LightEmission: buildingTemplate.EmitLight?.Color.ToVector4(1) ?? default, LightRange: buildingTemplate.EmitLight?.Range ?? 0f);
        ref var buildingComponent = ref entity.AddBuildingComponent(buildingTemplate, isBuilt ? 0 : buildingTemplate.BuildWorkTicks);
        entity.AddInventoryComponent();
        entity.AddWorkableComponent();
        entity.AddIdentityComponent(buildingTemplate.Name);

        if (buildingTemplate.Type is BuildingType.Rest)
            entity.AddRestableComponent();

        foreach (var loc in locationComponent.Box)
            TerrainCells![loc.X, loc.Y].BuildingEntity = entity;

        // when the building is finished trigger a recalculation of rooms and room templates in the world
        buildingComponent.IsBuiltChanged += RecalculateRooms;

        MarkAllPlantsForHarvest(locationComponent.Box);

        return entity;
    }

    public static Entity AddGrowZoneEntity(ZoneType zoneType, in Box2 box, PlantTemplate plantTemplate)
    {
        var entity = EcsCoordinator.CreateEntity();
        entity.AddLocationComponent(box);
        entity.AddRenderableComponent(null);
        entity.AddZoneComponent(zoneType, plantTemplate);

        return entity;
    }

    public static Entity AddStorageZoneEntity(ZoneType zoneType, in Box2 box)
    {
        var entity = EcsCoordinator.CreateEntity();
        entity.AddLocationComponent(box);
        entity.AddRenderableComponent(null);
        entity.AddZoneComponent(zoneType, null);

        return entity;
    }
    #endregion

    public void RecalculateRooms()
    {
        using var open = Box2.FromCornerSize(Vector2i.Zero, new(Width, Height))
            .Where(l => TerrainCells![l.X, l.Y].IsBuildingEntityBlockingOrDoor is not true)
            .ToPooledHashSet();
        using var floodQueue = QueuePool<Vector2i>.Get();

        Rooms.Clear();

        while (open.Count > 0)
        {
            var location = open.First();
            open.Remove(location);

            // flood fill setup
            floodQueue.Clear();
            floodQueue.Enqueue(location);

            using var roomLocations = CollectionPool<Vector2i>.Get();
            using var roomBuildingEntities = HashSetPool<Entity>.Get();
            var roomIndoors = true;

            // flood fill
            while (floodQueue.TryDequeue(out var currentLocation))
            {
                open.Remove(currentLocation);

                if (roomIndoors)
                {
                    roomLocations.Add(currentLocation);
                    if (TerrainCells![currentLocation.X, currentLocation.Y].BuildingEntity != Entity.Invalid)
                        roomBuildingEntities.Add(TerrainCells![currentLocation.X, currentLocation.Y].BuildingEntity);
                }

                // outdoors?
                if (roomIndoors && (currentLocation.X == 0 || currentLocation.X == Width - 1 || currentLocation.Y == 0 || currentLocation.Y == Height - 1))
                    roomIndoors = false;

                // neighbors
                static void enqueueNeighbor(Vector2i newLocation, Queue<Vector2i> floodQueue, HashSet<Vector2i> open)
                {
                    floodQueue.Enqueue(newLocation);
                    open.Remove(newLocation);
                }

                static bool isRoomBoundary(Entity entity) =>
                    entity.HasBuildingComponent() && entity.GetBuildingComponent().Template?.Type is BuildingType.Wall or BuildingType.Door;

                if (currentLocation.X > 0
                    && !isRoomBoundary(TerrainCells![currentLocation.X - 1, currentLocation.Y].BuildingEntity)
                    && open.Contains(new(currentLocation.X - 1, currentLocation.Y)))
                {
                    enqueueNeighbor(new(currentLocation.X - 1, currentLocation.Y), floodQueue, open);
                }

                if (currentLocation.Y > 0
                    && !isRoomBoundary(TerrainCells![currentLocation.X, currentLocation.Y - 1].BuildingEntity)
                    && open.Contains(new(currentLocation.X, currentLocation.Y - 1)))
                {
                    enqueueNeighbor(new(currentLocation.X, currentLocation.Y - 1), floodQueue, open);
                }

                if (currentLocation.X < Width - 1
                    && !isRoomBoundary(TerrainCells![currentLocation.X + 1, currentLocation.Y].BuildingEntity)
                    && open.Contains(new(currentLocation.X + 1, currentLocation.Y)))
                {
                    enqueueNeighbor(new(currentLocation.X + 1, currentLocation.Y), floodQueue, open);
                }

                if (currentLocation.Y < Height - 1
                    && !isRoomBoundary(TerrainCells![currentLocation.X, currentLocation.Y + 1].BuildingEntity)
                    && open.Contains(new(currentLocation.X, currentLocation.Y + 1)))
                {
                    enqueueNeighbor(new(currentLocation.X, currentLocation.Y + 1), floodQueue, open);
                }
            }

            if (roomIndoors)
                Rooms.Add(new(this, roomLocations, roomBuildingEntities));
        }
    }

    public static void MarkAllPlantsForHarvest(Box2 box)
    {
        EcsCoordinator.IteratePlantArchetype((in EcsCoordinator.PlantIterationResult w) =>
        {
            if (box.Intersects(w.LocationComponent.Box))
                w.Entity.AddMarkForHarvestComponent();
        });
    }

    internal bool DeleteEntity(Entity entity)
    {
        if (SelectedEntity == entity) SelectedEntity = null;
        if (entity.HasPlantComponent() && entity.GetPlantComponent().Template.IsTree
            && entity.GetLocationComponent().Box.TopLeft.ToVector2i() is var pos)
        {
            TerrainCells![pos.X, pos.Y].AboveGroundMovementModifier = 1;
        }

        if (entity.HasBuildingComponent() && entity.GetLocationComponent().Box is var box)
            foreach (var loc in box)
                TerrainCells![loc.X, loc.Y].BuildingEntity = Entity.Invalid;

        return entity.Delete();
    }

    public bool IsBoxFreeOfBuildings(Box2 box, bool allowUnbuilt = false)
    {
        box = box.WithClamp(Box2.FromCornerSize(Vector2i.Zero, new(Width - 1, Height - 1)));

        return allowUnbuilt
            ? box.All(l =>
            {
                var buildingEntity = TerrainCells![l.X, l.Y].BuildingEntity;
                if (buildingEntity == Entity.Invalid) return true;

                // building can be replaced if it hasn't been built yet and nobody's assigned to build it
                ref var buildingComponent = ref buildingEntity.GetBuildingComponent();
                ref var workableComponent = ref buildingEntity.GetWorkableComponent();
                return Unsafe.IsNullRef(ref buildingComponent) || Unsafe.IsNullRef(ref workableComponent)
                    || (!buildingComponent.IsBuilt && workableComponent.Entity == Entity.Invalid);
            })
            : box.All(l => TerrainCells![l.X, l.Y].BuildingEntity == Entity.Invalid);
    }

    public static bool IsBoxFreeOfPlants(Box2 box)
    {
        var okay = true;
        EcsCoordinator.IteratePlantArchetype((in EcsCoordinator.PlantIterationResult w) =>
        {
            if (w.LocationComponent.Box.Intersects(box))
            {
                okay = false;
                return false;
            }
            return true;
        });

        return okay;
    }

    public bool IsBoxFreeOfBlockingTerrain(Box2 box)
    {
        if (TerrainCells is not null)
        {
            var (ye, xe) = ((int)MathF.Round(MathF.Min(TerrainCells.GetLength(1), box.Bottom)), (int)MathF.Round(MathF.Min(TerrainCells.GetLength(0), box.Right)));
            for (int y = (int)MathF.Max(0, box.Top); y <= ye; ++y)
                for (int x = (int)MathF.Max(0, box.Left); x <= xe; ++x)
                {
                    ref var cell = ref TerrainCells[x, y];
                    if (cell.AboveGroundMovementModifier == 0 || cell.GroundMovementModifier == 0)
                        return false;
                }
        }

        return true;
    }

    public static PooledDictionary<Resource, int> GetAllResources(ResourceMarker marker, bool onlyStored)
    {
        var result = DictionaryPool<Resource, int>.Get();
        Func<Entity, bool> conditionTest = onlyStored ? entity => entity.HasPlacedResourceIsStoredComponent() : _ => true;

        EcsCoordinator.IteratePlacedResourceArchetype((in EcsCoordinator.PlacedResourceIterationResult rw) =>
        {
            if (conditionTest(rw.Entity))
                foreach (var rq in rw.InventoryComponent.Inventory.GetResourceQuantities(marker))
                    if (result.TryGetValue(rq.Resource, out var qty))
                        result[rq.Resource] = qty + rq.Quantity;
                    else
                        result[rq.Resource] = rq.Quantity;
        });

        return result;
    }

    public Vector2 GetWorldLocationFromScreenPoint(Vector2i screenPoint) =>
        new(screenPoint.X / Zoom + Offset.X, screenPoint.Y / Zoom + Offset.Y);

    public static void CalculateBuildingTiling(BuildingTemplate template, Vector2i? p0, Vector2i p1, Action<Vector2i, Vector2i> process)
    {
        if (template.TilingType is BuildingTilingType.None || p0 is null)
            process(p1, Vector2i.One);
        else if (p0 is not null)
        {
            var fullSize = (p1 - p0.Value).Abs() + Vector2i.One;
            var fullCount = fullSize / new Vector2i(template.Width, template.Height);
            if (template.TilingType is BuildingTilingType.Outline)
            {
                var start = new Vector2i(Math.Min(p0.Value.X, p1.X), Math.Min(p0.Value.Y, p1.Y));

                if (fullCount.X > 0)
                    process(start, new(fullCount.X, 1));
                if (fullCount.X > 0 && fullCount.Y > 1)
                    process(new(start.X, start.Y + (fullCount.Y - 1) * template!.Height), new(fullCount.X, 1));
                if (fullCount.X > 0 && fullCount.Y > 2)
                    process(new(start.X, start.Y + template!.Height), new(1, fullCount.Y - 2));
                if (fullCount.X > 1 && fullCount.Y > 2)
                    process(new(start.X + (fullCount.X - 1) * template!.Width, start.Y + template!.Height), new(1, fullCount.Y - 2));
            }
            else
                throw new NotImplementedException();
        }
    }

    Vector2i? dragStart;

    public void MouseEvent(GameWindow gameWindow, Vector2i screenPosition, Vector2 worldLocation, InputAction? inputAction = null, MouseButton? mouseButton = null, KeyModifiers? keyModifiers = null)
    {
        if (inputAction == InputAction.Press && mouseButton == MouseButton.Button1)
            if ((CurrentWorldTemplate.ZoneType is not null || CurrentWorldTemplate.BuildingTemplate?.TilingType is BuildingTilingType.OneAxis or BuildingTilingType.BothAxis or BuildingTilingType.Outline) && CurrentTemplateStartPoint is null)
                // first point
                CurrentTemplateStartPoint = MouseWorldPosition.ToVector2i();
            else if (CurrentWorldTemplate.ZoneType is not null)
            {
                // second point, add the zone entity
                var box = Box2.FromCornerSize(CurrentTemplateStartPoint!.Value,
                    (MouseWorldPosition - CurrentTemplateStartPoint.Value.ToNumericsVector2() + Vector2.One).ToVector2i());

                if (CurrentWorldTemplate.ZoneType is ZoneType.MarkHarvest)
                    MarkAllPlantsForHarvest(box);
                else if (IsBoxFreeOfBuildings(box) && IsBoxFreeOfBlockingTerrain(box))
                {
                    MarkAllPlantsForHarvest(box);
                    if (CurrentWorldTemplate.ZoneType is ZoneType.Grow)
                        AddGrowZoneEntity(CurrentWorldTemplate.ZoneType.Value, box, PlantTemplates["plant-rice"]);
                    else if (CurrentWorldTemplate.ZoneType is ZoneType.Storage)
                        AddStorageZoneEntity(CurrentWorldTemplate.ZoneType.Value, box);
                    else
                        throw new NotImplementedException();
                }
                if (keyModifiers?.HasFlagsFast(KeyModifiers.Shift) != true)
                    CurrentWorldTemplate.Clear();
            }
            else if (CurrentWorldTemplate.BuildingTemplate is { TilingType: BuildingTilingType.OneAxis or BuildingTilingType.BothAxis or BuildingTilingType.Outline } currentBuildingTemplate)
            {
                // tiled building
                CalculateBuildingTiling(currentBuildingTemplate, CurrentTemplateStartPoint, MouseWorldPosition.ToVector2i(), (location, count) =>
                {
                    var itemSize = new Vector2i(currentBuildingTemplate!.Width, currentBuildingTemplate!.Height);
                    for (int y = 0; y < count.Y; ++y)
                        for (int x = 0; x < count.X; ++x)
                        {
                            var itemBox = Box2.FromCornerSize(location + new Vector2i(x, y) * itemSize, itemSize);
                            if (IsBoxFreeOfBuildings(itemBox, true) && IsBoxFreeOfBlockingTerrain(itemBox))
                                AddBuildingEntity(currentBuildingTemplate, location + new Vector2i(x, y) * itemSize, false);
                        }
                });
                if (keyModifiers?.HasFlagsFast(KeyModifiers.Shift) != true)
                    CurrentWorldTemplate.Clear();
            }
            else if (CurrentWorldTemplate.BuildingTemplate is not null)
            {
                var box = Box2.FromCornerSize(worldLocation.ToVector2i(), CurrentWorldTemplate.BuildingTemplate.Width, CurrentWorldTemplate.BuildingTemplate.Height);
                if (IsBoxFreeOfBuildings(box, true) && IsBoxFreeOfBlockingTerrain(box))
                {
                    var building = AddBuildingEntity(CurrentWorldTemplate.BuildingTemplate, worldLocation.ToVector2i(), false);
                    if (keyModifiers?.HasFlagsFast(KeyModifiers.Shift) != true)
                        CurrentWorldTemplate.Clear();
                }
            }
            else
            {
                var foundNextEntity = false;
                Entity firstEntity = Entity.Invalid;

                // look for the valid entity after the selected one
                var foundSelectedEntity = false;
                EcsCoordinator.IterateRenderArchetype((in EcsCoordinator.RenderIterationResult w) =>
                {
                    if (w.LocationComponent.Box.Contains(worldLocation))
                    {
                        if (firstEntity == Entity.Invalid)
                            if (SelectedEntity is null)
                            {
                                foundNextEntity = true;
                                SelectedEntity = w.Entity;
                                return false;
                            }
                            else
                                firstEntity = w.Entity;

                        if (SelectedEntity == w.Entity)
                            foundSelectedEntity = true;
                        else if (foundSelectedEntity)
                        {
                            SelectedEntity = w.Entity;
                            foundNextEntity = true;
                            return false;
                        }
                    }

                    return true;
                });

                if (!foundNextEntity)
                    SelectedEntity = firstEntity == Entity.Invalid ? null : firstEntity;
            }
        else if (inputAction == InputAction.Press && mouseButton == MouseButton.Button2)
            CurrentWorldTemplate.Clear();

        // dragging
        if (inputAction is InputAction.Press && mouseButton is MouseButton.Button2)
        {
            dragStart = screenPosition;

            gameWindow.Cursor = OpenTK.Windowing.Common.Input.MouseCursor.Hand;
        }
        else if (inputAction is InputAction.Release && mouseButton is MouseButton.Button2 && dragStart is not null)
        {
            RawOffset -= (screenPosition - dragStart.Value).ToNumericsVector2() / Zoom;
            dragStart = null;

            gameWindow.Cursor = OpenTK.Windowing.Common.Input.MouseCursor.Default;
        }

        (MouseScreenPosition, MouseWorldPosition) = (screenPosition, worldLocation);
    }

    public void KeyEvent(InputAction inputAction, Keys key, int scanCode, KeyModifiers keyModifiers)
    {
        if (inputAction == InputAction.Press && key == Keys.Space)
            TimeSpeedUp = 0;
        else if (inputAction == InputAction.Press && key is Keys.LeftAlt or Keys.RightAlt)
            ShowDetails = true;
        else if (inputAction == InputAction.Release && key is Keys.LeftAlt or Keys.RightAlt)
            ShowDetails = false;
        else if (inputAction == InputAction.Press && key == Keys.F1)
            DebugShowLightAtMouse = !DebugShowLightAtMouse;
        else if (inputAction == InputAction.Press && key is Keys.W)
            deltaOffsetNextFrame.Y = -1;
        else if (inputAction == InputAction.Release && key is Keys.W)
            deltaOffsetNextFrame.Y = 0;
        else if (inputAction == InputAction.Press && key is Keys.S)
            deltaOffsetNextFrame.Y = 1;
        else if (inputAction == InputAction.Release && key is Keys.S)
            deltaOffsetNextFrame.Y = 0;
        else if (inputAction == InputAction.Press && key is Keys.A)
            deltaOffsetNextFrame.X = -1;
        else if (inputAction == InputAction.Release && key is Keys.A)
            deltaOffsetNextFrame.X = 0;
        else if (inputAction == InputAction.Press && key is Keys.D)
            deltaOffsetNextFrame.X = 1;
        else if (inputAction == InputAction.Release && key is Keys.D)
            deltaOffsetNextFrame.X = 0;
        else if (inputAction == InputAction.Press && key is Keys.F5)
            Save("quick");
    }

    #region Saving
    static readonly string SavesFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Tweey");
    const string SavesExtension = "sav";

    class SaveData
    {
        public required EcsDataDump EcsDataDump { get; set; }
        public Vector2 WorldOffset { get; set; }
        public float WorldZoom { get; set; }
    }

    private void Save(string name)
    {
        var saveData = new SaveData
        {
            EcsDataDump = EcsCoordinator.DumpAllData(),
            WorldOffset = Offset,
            WorldZoom = Zoom
        };

        Directory.CreateDirectory(SavesFolder);
        using var file = File.Create(Path.Combine(SavesFolder, $"{name}.{SavesExtension}"));
        using var compressStream = new GZipStream(file, CompressionLevel.SmallestSize, true);
        JsonSerializer.Serialize(compressStream, saveData, Loader.BuildJsonOptions());
    }
    #endregion

    public TimeSpan TotalRealTime { get; private set; }
    const double worldTimeMultiplier = 96 * 6;
    public TimeSpan RawWorldTime { get; private set; }
    public CustomDateTime WorldTime { get; private set; }
    public TimeSpan DeltaWorldTime { get; private set; }

    public static TimeSpan GetWorldTimeFromTicks(double ticks) =>
        TimeSpan.FromSeconds(ticks * worldTimeMultiplier);

    public void Update(double deltaSec)
    {
        TotalRealTime += TimeSpan.FromSeconds(deltaSec);
        RawWorldTime += DeltaWorldTime = TimeSpan.FromSeconds(deltaSec * worldTimeMultiplier * TimeSpeedUp);
        WorldTime = new(RawWorldTime);

        RawOffset += deltaOffsetNextFrame * (float)deltaSec * deltaOffsetPerSecond;
    }

    [GeneratedRegex("Data[/\\\\]Biomes[/\\\\](.*)[/\\\\].*\\.png", RegexOptions.IgnoreCase)]
    private static partial Regex ExtractBiomeNameFromPathRegex();
}

record struct TerrainCell(string? TileFileName, float GroundMovementModifier = 1, float AboveGroundMovementModifier = 1)
{
    public bool Impassable => GroundMovementModifier == 0 || AboveGroundMovementModifier == 0;
    public Entity BuildingEntity { get; set; }

    public bool IsBuildingEntityBlocking
    {
        get
        {
            if (BuildingEntity == Entity.Invalid) return false;

            ref var buildingComponent = ref BuildingEntity.GetBuildingComponent();
            return !Unsafe.IsNullRef(ref buildingComponent) && buildingComponent.IsBuilt && buildingComponent.Template.IsBlocking;
        }
    }

    public bool IsBuildingEntityBlockingOrDoor
    {
        get
        {
            if (BuildingEntity == Entity.Invalid) return false;

            ref var buildingComponent = ref BuildingEntity.GetBuildingComponent();
            return !Unsafe.IsNullRef(ref buildingComponent) && (buildingComponent.Template.IsBlocking || buildingComponent.Template.Type is BuildingType.Door);
        }
    }
}

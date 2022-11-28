using System.Text.RegularExpressions;

namespace Tweey.WorldData;

internal partial class World
{
    public ResourceTemplates Resources { get; }
    public PlantTemplates PlantTemplates { get; }
    public BuildingTemplates BuildingTemplates { get; }
    public Configuration Configuration { get; }
    public Biomes Biomes { get; }

    public string[,]? TerrainTileNames { get; private set; }

    internal Entity? SelectedEntity { get; set; }
    public CurrentWorldTemplate CurrentWorldTemplate { get; } = new();
    public Vector2i? CurrentZoneStartPoint { get; set; }

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

    public Vector2 Offset { get; set; }
    public float Zoom { get; set; } = 35;

    Vector2 deltaOffsetNextFrame;
    static readonly Vector2 deltaOffsetPerSecond = new(10f);
    float deltaZoomNextFrame;

    public World(ILoader loader)
    {
        (Resources, Configuration) = (new(loader), new(loader));
        (BuildingTemplates, PlantTemplates) = (new(loader, Resources), new(loader, Resources));
        Biomes = new(loader, PlantTemplates);
    }

    [MemberNotNull(nameof(TerrainTileNames))]
    public void GenerateMap(int width, int height)
    {
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

        TerrainTileNames = new string[width, height];
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var biome = Biomes[map[x, y].BiomeIndex];
                TerrainTileNames[x, y] = biomeTiles[biome.TileName].RandomSubset(1).First();

                // plant a tree?
                foreach (var (template, chance) in biome.Plants)
                    if (Random.Shared.NextDouble() < chance)
                    {
                        AddPlantEntity(template, new(x, y), true, false);
                        break;
                    }
            }
    }

    #region AddEntities
    internal Entity AddVillagerEntity(string name, Vector2 location)
    {
        var entity = EcsCoordinator.CreateEntity();
        entity.AddLocationComponent(Box2.FromCornerSize(location, new(1, 1)));
        entity.AddRenderableComponent("Data/Misc/villager.png",
            LightEmission: Colors4.White, LightRange: 12, LightAngleRadius: .1f);
        entity.AddHeadingComponent();
        entity.AddVillagerComponent(Configuration.Data.BaseCarryWeight, Configuration.Data.BasePickupSpeed, Configuration.Data.BaseMovementSpeed,
            Configuration.Data.BaseWorkSpeed, Configuration.Data.BaseHarvestSpeed, Configuration.Data.BasePlantSpeed);
        entity.AddWorkerComponent();
        entity.AddInventoryComponent();
        entity.AddIdentityComponent(name);

        return entity;
    }

    public Entity AddPlantEntity(PlantTemplate plantTemplate, Vector2 location, bool isMature, bool isFarmed)
    {
        var entity = EcsCoordinator.CreateEntity();
        entity.AddLocationComponent(Box2.FromCornerSize(location, new(1, 1)));
        entity.AddRenderableComponent($"Data/Plants/{plantTemplate.FileName}.png",
            OcclusionCircle: true, OcclusionScale: plantTemplate.OccludeLight ? .3f : 0f);
        entity.AddWorkableComponent();
        entity.AddPlantComponent(plantTemplate, plantTemplate.HarvestWorkTicks, isMature ? CustomDateTime.Invalid : WorldTime);
        entity.AddIdentityComponent(plantTemplate.Name);
        entity.AddInventoryComponent().Inventory
            .Add(ResourceMarker.All, plantTemplate.Inventory, ResourceMarker.Default);
        if (isFarmed)
            entity.AddPlantIsFarmedComponent();

        return entity;
    }

    public IEnumerable<Entity> AddResourceEntities(ResourceMarker srcMarker, ResourceBucket srcRB, ResourceMarker dstMarker, Vector2 location)
    {
        // obey the maximum ground stack weight
        var result = new List<Entity>();
        using var availableNeighbours = CollectionPool<(Vector2i pt, Entity? entity, ResourceBucket? rb)>.Get();

        var availableNeighboursSearchRadius = -1;
        while (!srcRB.IsEmpty(srcMarker))
        {
            // take any spill-over and put it in a random direction, up to maximumGroundDropSpillOverRange range
            while (availableNeighbours.Count == 0)
            {
                ++availableNeighboursSearchRadius;

                foreach (var dv in GameUtilities.EnumerateNeighbourLocations(location, radiusMin: availableNeighboursSearchRadius, radiusMax: availableNeighboursSearchRadius))
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
                        availableNeighbours.Add((dv, foundEntity, foundResourceBucket));
                }
            }

            var chosenNeighbourIndex = Random.Shared.Next(availableNeighbours.Count);
            var chosenNeighbour = availableNeighbours[chosenNeighbourIndex];
            availableNeighbours.Remove(chosenNeighbour);

            var newRB = chosenNeighbour.rb;
            var newEntity = chosenNeighbour.entity ?? Entity.Invalid;
            if (newRB is null)
            {
                newEntity = EcsCoordinator.CreateEntity();
                newEntity.AddRenderableComponent(null);
                newEntity.AddLocationComponent(Box2.FromCornerSize(chosenNeighbour.pt, new(1, 1)));
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

    public static Entity AddBuildingEntity(BuildingTemplate buildingTemplate, Vector2 location, bool isBuilt)
    {
        var entity = EcsCoordinator.CreateEntity();
        ref var locationComponent = ref entity.AddLocationComponent(Box2.FromCornerSize(location, buildingTemplate.Width, buildingTemplate.Height));
        entity.AddRenderableComponent($"Data/Buildings/{buildingTemplate.FileName}.png",
            OcclusionScale: buildingTemplate.EmitLight is null ? 1 : 0,
            LightEmission: buildingTemplate.EmitLight?.Color.ToVector4(1) ?? default, LightRange: buildingTemplate.EmitLight?.Range ?? 0f);
        entity.AddBuildingComponent(buildingTemplate, isBuilt ? 0 : buildingTemplate.BuildWorkTicks);
        entity.AddInventoryComponent();
        entity.AddWorkableComponent();
        entity.AddIdentityComponent(buildingTemplate.Name);

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
        return entity.Delete();
    }

    public static bool IsBoxFreeOfBuildings(Box2 box)
    {
        var okay = true;
        EcsCoordinator.IterateBuildingArchetype((in EcsCoordinator.BuildingIterationResult w) =>
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

    public Dictionary<Resource, int> GetStoredResources(ResourceMarker marker)
    {
        var result = new Dictionary<Resource, int>();

        EcsCoordinator.IteratePlacedResourceArchetype((in EcsCoordinator.PlacedResourceIterationResult rw) =>
        {
            if (rw.Entity.HasPlacedResourceIsStoredComponent())
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

    public void MouseEvent(Vector2i screenPosition, Vector2 worldLocation, InputAction? inputAction = null, MouseButton? mouseButton = null, KeyModifiers? keyModifiers = null)
    {
        if (inputAction == InputAction.Press && mouseButton == MouseButton.Button1)
            if (CurrentWorldTemplate.ZoneType is not null && CurrentZoneStartPoint is null)
                // first point
                CurrentZoneStartPoint = MouseWorldPosition.ToVector2i();
            else if (CurrentWorldTemplate.ZoneType is not null)
            {
                // second point, add the zone entity
                var box = Box2.FromCornerSize(CurrentZoneStartPoint!.Value,
                    (MouseWorldPosition - CurrentZoneStartPoint.Value.ToNumericsVector2() + Vector2.One).ToVector2i());

                if (CurrentWorldTemplate.ZoneType is ZoneType.MarkHarvest)
                    MarkAllPlantsForHarvest(box);
                else if (IsBoxFreeOfBuildings(box))
                {
                    MarkAllPlantsForHarvest(box);
                    if (CurrentWorldTemplate.ZoneType is ZoneType.Grow)
                        AddGrowZoneEntity(CurrentWorldTemplate.ZoneType.Value, box, PlantTemplates["plant-rice"]);
                    else if (CurrentWorldTemplate.ZoneType is ZoneType.Storage)
                        AddStorageZoneEntity(CurrentWorldTemplate.ZoneType.Value, box);
                    else
                        throw new NotImplementedException();
                }
                CurrentWorldTemplate.Clear();
            }
            else if (CurrentWorldTemplate.BuildingTemplate is not null)
            {
                if (IsBoxFreeOfBuildings(Box2.FromCornerSize(worldLocation.ToVector2i(), CurrentWorldTemplate.BuildingTemplate.Width, CurrentWorldTemplate.BuildingTemplate.Height)))
                {
                    var building = AddBuildingEntity(CurrentWorldTemplate.BuildingTemplate, worldLocation.Floor(), false);
                    if (keyModifiers?.HasFlag(KeyModifiers.Shift) != true)
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

        Offset += deltaOffsetNextFrame * (float)deltaSec * deltaOffsetPerSecond;
    }

    [GeneratedRegex("Data[/\\\\]Biomes[/\\\\](.*)[/\\\\].*\\.png", RegexOptions.IgnoreCase)]
    private static partial Regex ExtractBiomeNameFromPathRegex();
}
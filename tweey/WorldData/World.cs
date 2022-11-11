using System.IO.Compression;
using Tweey.Loaders;

namespace Tweey.WorldData;

internal class World
{
    public ResourceTemplates Resources { get; }
    public TreeTemplates TreeTemplates { get; }
    public BuildingTemplates BuildingTemplates { get; }
    public Configuration Configuration { get; }

    internal Entity? SelectedEntity { get; set; }
    public ZoneType? CurrentZoneType { get; set; }
    public Vector2i? CurrentZoneStartPoint { get; set; }
    public BuildingTemplate? CurrentBuildingTemplate { get; set; }

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
        (BuildingTemplates, TreeTemplates) = (new(loader, Resources), new(loader, Resources));
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
            Configuration.Data.BaseWorkSpeed);
        entity.AddWorkerComponent();
        entity.AddInventoryComponent();
        entity.AddIdentityComponent(name);

        return entity;
    }

    internal static Entity AddTreeEntity(TreeTemplate treeTemplate, Vector2 location)
    {
        var entity = EcsCoordinator.CreateEntity();
        entity.AddLocationComponent(Box2.FromCornerSize(location, new(1, 1)));
        entity.AddRenderableComponent($"Data/Trees/{treeTemplate.FileName}.png",
            OcclusionCircle: true, OcclusionScale: .3f);
        entity.AddWorkableComponent()
            .ResizeSlots(1);
        entity.AddTreeComponent();
        entity.AddIdentityComponent(treeTemplate.Name);

        return entity;
    }

    public void AddResourceEntity(ResourceBucket resourceBucket, Vector2 location)
    {
        // obey the maximum ground stack weight
        var availableNeighbours = ObjectPool<List<(Vector2i pt, Entity? entity, ResourceBucket? rb)>>.Shared.Get();

        try
        {
            int availableNeighboursSearchRadius = -1;
            while (resourceBucket.GetWeight(ResourceMarker.All) > 0)
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
                            if (w.LocationComponent.Box.Contains(location + dv.ToNumericsVector2()))
                            {
                                if (w.InventoryComponent.Inventory.GetWeight(ResourceMarker.All) >= Configuration.Data.GroundStackMaximumWeight)
                                {
                                    okay = false;
                                    return false;
                                }
                                else
                                    (foundEntity, foundResourceBucket) = (w.Entity, w.InventoryComponent.Inventory);
                            }
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
                var newRBWeight = newRB.GetWeight(ResourceMarker.Default);

                foreach (var resQ in resourceBucket.GetResourceQuantities(ResourceMarker.All).Where(w => w.Quantity != 0))
                {
                    // only allow one resource kind on the ground
                    if (!newRB.GetResourceQuantities(ResourceMarker.All).Any() || newRB.GetResourceQuantities(ResourceMarker.All).Any(rq => rq.Resource == resQ.Resource))
                    {
                        var maxNewWeight = Configuration.Data.GroundStackMaximumWeight - newRBWeight;
                        var quantityToMove = (int)Math.Floor(Math.Min(maxNewWeight, resQ.Weight) / resQ.Resource.Weight);

                        var newResQ = new ResourceQuantity(resQ.Resource, quantityToMove);
                        newRB.Add(newResQ, ResourceMarker.Default);
                        resourceBucket.Remove(newResQ);
                        if (resQ.Quantity > 0)
                            break;  // couldn't finish the stack
                        newRBWeight += resQ.Resource.Weight * quantityToMove;
                    }
                }

                if (newRB.GetResourceQuantities(ResourceMarker.All).FirstOrDefault() is { } newRQ)
                {
                    // once we finished this resource clump, set its name and render image
                    newEntity.GetIdentityComponent().Name = newRQ.Resource.Name;
                    newEntity.GetRenderableComponent().AtlasEntryName = $"Data/Resources/{newRQ.Resource.FileName}.png";
                }
            }
        }
        finally
        {
            ObjectPool<List<(Vector2i pt, Entity? entity, ResourceBucket? rb)>>.Shared.Return(availableNeighbours);
        }
    }

    public static Entity AddBuildingEntity(BuildingTemplate buildingTemplate, Vector2 location, bool isBuilt)
    {
        var entity = EcsCoordinator.CreateEntity();
        entity.AddLocationComponent(Box2.FromCornerSize(location, new(1, 1)));
        entity.AddRenderableComponent($"Data/Buildings/{buildingTemplate.FileName}.png", OcclusionScale: 1,
            LightEmission: buildingTemplate.EmitLight?.Color.ToVector4(1) ?? default, LightRange: buildingTemplate.EmitLight?.Range ?? 0f);
        entity.AddBuildingComponent(isBuilt, buildingTemplate.BuildCost, buildingTemplate.BuildWorkTicks);
        entity.AddInventoryComponent();
        entity.AddWorkableComponent()
            .ResizeSlots(isBuilt ? buildingTemplate.MaxWorkersAmount : 1);
        entity.AddIdentityComponent(buildingTemplate.Name);

        return entity;
    }

    public static Entity AddZoneEntity(ZoneType zoneType, Box2 box)
    {
        var entity = EcsCoordinator.CreateEntity();
        entity.AddLocationComponent(box);
        entity.AddRenderableComponent(null);
        entity.AddZoneComponent(zoneType);

        return entity;
    }
    #endregion

    public static void PlantForest(TreeTemplate treeTemplate, Vector2i center, float radius, float chanceCenter, float chanceEdge)
    {
        for (int y = (int)MathF.Ceiling(center.Y + radius); y >= MathF.Floor(center.Y - radius); --y)
            for (int x = (int)MathF.Ceiling(center.X + radius); x >= MathF.Floor(center.X - radius); --x)
            {
                var distanceFromCenter = new Vector2i(Math.Abs(y - center.Y), Math.Abs(y - center.Y)).EuclideanLength;
                var chance = chanceCenter * (radius - distanceFromCenter) / radius + chanceEdge * distanceFromCenter / radius;
                if (Random.Shared.NextDouble() < chance)
                    AddTreeEntity(treeTemplate, new(x, y));
            }
    }

    internal bool RemoveEntity(Entity entity)
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

    public Vector2 GetWorldLocationFromScreenPoint(Vector2i screenPoint) =>
        new((screenPoint.X) / Zoom + Offset.X, (screenPoint.Y) / Zoom + Offset.Y);

    event Action<Entity>? PlacedBuilding;
    public void MouseEvent(Vector2i screenPosition, Vector2 worldLocation, InputAction? inputAction = null, MouseButton? mouseButton = null, KeyModifiers? keyModifiers = null)
    {
        if (inputAction == InputAction.Press && mouseButton == MouseButton.Button1)
        {
            if (CurrentZoneType is not null && CurrentZoneStartPoint is null)
            {
                // first point
                CurrentZoneStartPoint = MouseWorldPosition.ToVector2i();
            }
            else if (CurrentZoneType is not null)
            {
                // second point, add the zone entity
                var box = Box2.FromCornerSize(CurrentZoneStartPoint!.Value,
                    (MouseWorldPosition - CurrentZoneStartPoint.Value.ToNumericsVector2() + Vector2.One).ToVector2i());
                if (IsBoxFreeOfBuildings(box))
                    AddZoneEntity(CurrentZoneType.Value, box);
                CurrentZoneType = null;
            }
            else if (CurrentBuildingTemplate is not null)
            {
                if (IsBoxFreeOfBuildings(Box2.FromCornerSize(worldLocation.ToVector2i(), CurrentBuildingTemplate.Width, CurrentBuildingTemplate.Height)))
                {
                    var building = AddBuildingEntity(CurrentBuildingTemplate, worldLocation, false);
                    PlacedBuilding?.Invoke(building);
                    if (keyModifiers?.HasFlag(KeyModifiers.Shift) != true)
                        CurrentBuildingTemplate = null;
                }
            }
            else
            {
                var foundAny = false;
                EcsCoordinator.IterateRenderArchetype((in EcsCoordinator.RenderIterationResult w) =>
                {
                    if (w.LocationComponent.Box.Contains(worldLocation))
                    {
                        SelectedEntity = w.Entity;
                        foundAny = true;
                        return false;
                    }

                    return true;
                });

                if (!foundAny)
                    SelectedEntity = null;
            }
        }
        else if (inputAction == InputAction.Press && mouseButton == MouseButton.Button2)
            CurrentBuildingTemplate = null;

        (MouseScreenPosition, MouseWorldPosition) = (screenPosition, worldLocation);
    }

    event Action<Entity /* workable */, Entity /* worker */>? StartedJob;
    public static void PlanWork(Entity workable, Entity worker)
    {
        ref var workableComponent = ref workable.GetWorkableComponent();
        ref var emptyWorkerSlot = ref workableComponent.GetEmptyWorkerSlot();

        if (emptyWorkerSlot.Entity != Entity.Invalid)
            emptyWorkerSlot.Entity = worker;
        else
            throw new InvalidOperationException($"Could not find an empty worker slot for {worker} to work on {workable}.");
    }

    public void StartWork(Entity workable, Entity worker)
    {
        StartedJob?.Invoke(workable, worker);

        ref var workableComponent = ref workable.GetWorkableComponent();
        ref var emptyWorkerSlot = ref workableComponent.GetAssignedWorkerSlot(worker);

        if (emptyWorkerSlot.Entity != Entity.Invalid)
            emptyWorkerSlot.EntityWorking = true;
        else
            throw new InvalidOperationException($"Could not find worker slot on {workable} supposedly worked by {worker}.");
    }

    event Action<Entity /* workable */, Entity /* worker */, bool /* last */>? EndedBuildingJob;
    public void EndWork(Entity workable, Entity worker)
    {
        ref var workableComponent = ref workable.GetWorkableComponent();
        ref var emptyWorkerSlot = ref workableComponent.GetAssignedWorkerSlot(worker);

        if (emptyWorkerSlot.Entity != Entity.Invalid)
        {
            EndedBuildingJob?.Invoke(workable, worker, !workableComponent.WorkerSlots.Any(w => w.EntityWorking));
            emptyWorkerSlot.Clear();
        }
        else
            throw new InvalidOperationException($"Could not find worker slot on {workable} supposedly worked by {worker}.");
    }

    event Action<BuildingTemplate?>? CurrentBuildingTemplateChanged;
    public void FireCurrentBuildingTemplateChanged() =>
        CurrentBuildingTemplateChanged?.Invoke(CurrentBuildingTemplate);

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
    public TimeSpan WorldTime { get; private set; }
    public string? WorldTimeString { get; private set; }
    public TimeSpan DeltaWorldTime { get; private set; }

    public static TimeSpan GetWorldTimeFromTicks(double ticks) =>
        TimeSpan.FromSeconds(ticks * worldTimeMultiplier);

    public void Update(double deltaSec)
    {
        TotalRealTime += TimeSpan.FromSeconds(deltaSec);
        WorldTime += DeltaWorldTime = TimeSpan.FromSeconds(deltaSec * worldTimeMultiplier * TimeSpeedUp);
        Offset += deltaOffsetNextFrame * (float)deltaSec * deltaOffsetPerSecond;

        var wt = WorldTime.TotalMinutes;
        var min = (int)(wt % 60); wt /= 60;
        var hour = (int)(wt % 24); wt /= 24;
        var day = (int)(wt % 30 + 1); wt /= 30;
        var month = (int)(wt % 12 + 1); wt /= 12;
        var year = (int)(wt + 1);
        WorldTimeString = $"{year:00}-{month:00}-{day:00} {hour:00}:{min:00}";
    }
}
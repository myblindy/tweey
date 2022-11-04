﻿using Tweey.Loaders;

namespace Tweey.WorldData;

internal class World
{
    public ResourceTemplates Resources { get; }
    public TreeTemplates TreeTemplates { get; }
    public BuildingTemplates BuildingTemplates { get; }
    public Configuration Configuration { get; }

    internal Entity? SelectedEntity { get; set; }
    public BuildingTemplate? CurrentBuildingTemplate { get; set; }

    public bool Paused { get; private set; }
    public bool ShowDetails { get; private set; }
    public bool DebugShowLightAtMouse { get; private set; }

    /// <summary>
    /// The position of the mouse in screen coordinates.
    /// </summary>
    public Vector2i MouseScreenPosition { get; private set; }

    /// <summary>
    /// The position of the mouse in world coordinates.
    /// </summary>
    public Vector2i MouseWorldPosition { get; private set; }

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
    internal static Entity AddVillagerEntity(string name, Vector2 location)
    {
        var entity = EcsCoordinator.CreateEntity();
        EcsCoordinator.AddLocationComponent(entity, Box2.FromCornerSize(location, new(1, 1)));
        EcsCoordinator.AddRenderableComponent(entity, "Data/Misc/villager.png",
            LightEmission: Colors4.White, LightRange: 12, LightAngleRadius: .1f);
        EcsCoordinator.AddHeadingComponent(entity);
        EcsCoordinator.AddVillagerComponent(entity, name);

        return entity;
    }

    internal static Entity AddTreeEntity(TreeTemplate treeTemplate, Vector2 location)
    {
        var entity = EcsCoordinator.CreateEntity();
        EcsCoordinator.AddLocationComponent(entity, Box2.FromCornerSize(location, new(1, 1)));
        EcsCoordinator.AddRenderableComponent(entity, $"Data/Trees/{treeTemplate.FileName}.png",
            OcclusionCircle: true, OcclusionScale: .3f);

        return entity;
    }

    public void AddResourceEntity(ResourceBucket resourceBucket, Vector2 location)
    {
        // obey the maximum ground stack weight
        List<(Vector2i pt, ResourceBucket? rb)> availableNeighbours = new();
        int availableNeighboursSearchRadius = -1;
        while (resourceBucket.AvailableWeight > 0)
        {
            // take any spill-over and put it in a random direction, up to maximumGroundDropSpillOverRange range
            while (availableNeighbours.Count == 0)
            {
                ++availableNeighboursSearchRadius;

                foreach (var dv in GameUtilities.EnumerateNeighbourLocations(location, radiusMin: availableNeighboursSearchRadius, radiusMax: availableNeighboursSearchRadius))
                {
                    var okay = true;
                    ResourceBucket? foundResourceBucket = default;

                    foreach (var entity in EcsCoordinator.Entities)
                    {
                        if (EcsCoordinator.GetLocationComponent(entity).Box.Contains(location + dv.ToNumericsVector2()))
                            if (EcsCoordinator.HasResourceComponent(entity))
                            {
                                ref var inventoryComponent = ref EcsCoordinator.GetInventoryComponent(entity);
                                if (inventoryComponent.Inventory.AvailableWeight < Configuration.Data.GroundStackMaximumWeight)
                                {
                                    okay = false;
                                    break;
                                }
                                else
                                    foundResourceBucket = inventoryComponent.Inventory;
                            }
                    }

                    if (okay)
                        availableNeighbours.Add((dv, foundResourceBucket));
                }
            }

            var chosenNeighbourIndex = Random.Shared.Next(availableNeighbours.Count);
            var chosenNeighbour = availableNeighbours[chosenNeighbourIndex];
            availableNeighbours.Remove(chosenNeighbour);

            var newRB = chosenNeighbour.rb;
            if (newRB is null)
            {
                var entity = EcsCoordinator.CreateEntity();
                EcsCoordinator.AddRenderableComponent(entity, null);
                EcsCoordinator.AddLocationComponent(entity, Box2.FromCornerSize(chosenNeighbour.pt, new(1, 1)));
                EcsCoordinator.AddResourceComponent(entity);
                newRB = EcsCoordinator.AddInventoryComponent(entity).Inventory;
            }
            var newRBWeight = newRB.AvailableWeight;
            int resourceIndex = 0;
            for (; resourceIndex < resourceBucket.ResourceQuantities.Count && resourceBucket.ResourceQuantities[resourceIndex].Quantity == 0; resourceIndex++) { }

            while (resourceIndex < resourceBucket.ResourceQuantities.Count)
            {
                var resQ = resourceBucket.ResourceQuantities[resourceIndex];

                // only allow one resource kind on the ground
                if (!newRB.ResourceQuantities.Any() || newRB.ResourceQuantities.Any(rq => rq.Resource == resQ.Resource))
                {
                    var maxNewWeight = Configuration.Data.GroundStackMaximumWeight - newRBWeight;
                    var quantityToMove = (int)Math.Floor(Math.Min(maxNewWeight, resQ.Weight) / resQ.Resource.Weight);

                    var newResQ = new ResourceQuantity(resQ.Resource, quantityToMove);
                    newRB.Add(newResQ);
                    resourceBucket.Remove(newResQ);
                    if (resQ.Quantity > 0)
                        break;  // couldn't finish the stack
                    newRBWeight += resQ.Resource.Weight * quantityToMove;
                }

                for (++resourceIndex; resourceIndex < resourceBucket.ResourceQuantities.Count && resourceBucket.ResourceQuantities[resourceIndex].Quantity == 0; resourceIndex++) { }
            }
        }
    }

    public static Entity AddBuildingEntity(BuildingTemplate buildingTemplate, Vector2 location, bool isBuilt)
    {
        var entity = EcsCoordinator.CreateEntity();
        EcsCoordinator.AddLocationComponent(entity, Box2.FromCornerSize(location, new(1, 1)));
        EcsCoordinator.AddRenderableComponent(entity, $"Data/Buildings/{buildingTemplate.FileName}.png", OcclusionScale: 1,
            LightEmission: buildingTemplate.EmitLight?.Color.ToVector4(1) ?? default, LightRange: buildingTemplate.EmitLight?.Range ?? 0f);
        EcsCoordinator.AddBuildingComponent(entity, isBuilt);

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
        return EcsCoordinator.DeleteEntity(entity);
    }

    public Vector2i GetWorldLocationFromScreenPoint(Vector2i screenPoint) =>
        new((int)MathF.Floor(((screenPoint.X) / Zoom + Offset.X)), (int)MathF.Floor(((screenPoint.Y) / Zoom + Offset.Y)));

    event Action<Entity>? PlacedBuilding;
    public void MouseEvent(Vector2i screenPosition, Vector2i worldLocation, InputAction? inputAction = null, MouseButton? mouseButton = null, KeyModifiers? keyModifiers = null)
    {
        if (inputAction == InputAction.Press && mouseButton == MouseButton.Button1)
        {
            if (CurrentBuildingTemplate is not null)
            {
                var okay = true;
                EcsCoordinator.IterateBuildingArchetype((in EcsCoordinator.BuildingIterationResult w) =>
                {
                    if (w.LocationComponent.Box.Intersects(Box2.FromCornerSize(worldLocation, new(CurrentBuildingTemplate.Width, CurrentBuildingTemplate.Height))))
                    {
                        okay = false;
                        return false;
                    }

                    return true;
                });

                if (okay)
                {
                    var building = AddBuildingEntity(CurrentBuildingTemplate, worldLocation.ToNumericsVector2(), false);
                    PlacedBuilding?.Invoke(building);
                    if (keyModifiers?.HasFlag(KeyModifiers.Shift) != true)
                        CurrentBuildingTemplate = null;
                }
            }
            else
                EcsCoordinator.IterateRenderArchetype((in EcsCoordinator.RenderIterationResult w) =>
                {
                    if (w.LocationComponent.Box.Contains(worldLocation))
                    {
                        SelectedEntity = w.Entity;
                        return false;
                    }

                    return true;
                });
        }
        else if (inputAction == InputAction.Press && mouseButton == MouseButton.Button2)
            CurrentBuildingTemplate = null;

        (MouseScreenPosition, MouseWorldPosition) = (screenPosition, worldLocation);
    }

    event Action<Entity /* workable */, Entity /* worker */>? StartedJob;
    public static void PlanWork(Entity workable, Entity worker)
    {
        ref var workableComponent = ref EcsCoordinator.GetWorkableComponent(workable);
        ref var emptyWorkerSlot = ref workableComponent.GetEmptyWorkerSlot();

        if (emptyWorkerSlot.Entity != Entity.Invalid)
            emptyWorkerSlot.Entity = worker;
        else
            throw new InvalidOperationException($"Could not find an empty worker slot for {worker} to work on {workable}.");
    }

    public void StartWork(Entity workable, Entity worker)
    {
        StartedJob?.Invoke(workable, worker);

        ref var workableComponent = ref EcsCoordinator.GetWorkableComponent(workable);
        ref var emptyWorkerSlot = ref workableComponent.GetAssignedWorkerSlot(worker);

        if (emptyWorkerSlot.Entity != Entity.Invalid)
            emptyWorkerSlot.EntityWorking = true;
        else
            throw new InvalidOperationException($"Could not find worker slot on {workable} supposedly worked by {worker}.");
    }

    event Action<Entity /* workable */, Entity /* worker */, bool /* last */>? EndedBuildingJob;
    public void EndWork(Entity workable, Entity worker)
    {
        ref var workableComponent = ref EcsCoordinator.GetWorkableComponent(workable);
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
            Paused = !Paused;
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
    }

    public TimeSpan TotalTime { get; private set; }
    public void Update(double deltaSec)
    {
        TotalTime += TimeSpan.FromSeconds(deltaSec);
        Offset += deltaOffsetNextFrame * (float)deltaSec * deltaOffsetPerSecond;
    }
}
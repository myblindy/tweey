﻿namespace Tweey.WorldData;

public class World
{
    public ResourceTemplates Resources { get; }
    public TreeTemplates TreeTemplates { get; }
    public BuildingTemplates BuildingTemplates { get; }
    public Configuration Configuration { get; }

    readonly AIManager aiManager;
    readonly SoundManager soundManager;

    List<PlaceableEntity> PlacedEntities { get; } = new();
    public PlaceableEntity? SelectedEntity { get; set; }
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
    static readonly Vector2 deltaOffsetPerSecond = new(10);
    float deltaZoomNextFrame;

    public World(ILoader loader)
    {
        (Resources, Configuration, aiManager) = (new(loader), new(loader), new(this));
        (BuildingTemplates, TreeTemplates) = (new(loader, Resources), new(loader, Resources));
        soundManager = new(this) { Volume = .1f };

        StartedJob += soundManager.OnStartedJob;
        EndedBuildingJob += soundManager.OnEndedJob;
        PlacedBuilding += soundManager.OnPlacedBuilding;
        CurrentBuildingTemplateChanged += soundManager.OnCurrentBuildingTemplateChanged;
    }

    public void PlaceEntity(PlaceableEntity entity)
    {
        if (entity is ResourceBucket resourceBucket)
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
                    availableNeighbours.AddRange(
                        GameUtilities.EnumerateNeighbourLocations(resourceBucket.Location, radiusMin: availableNeighboursSearchRadius, radiusMax: availableNeighboursSearchRadius)
                            .Select(l => (l, rb: GetEntities<ResourceBucket>().FirstOrDefault(e => e.Contains(l))))
                            .Where(l => l.rb is null || l.rb.AvailableWeight < Configuration.Data.GroundStackMaximumWeight));
                }

                var chosenNeighbourIndex = Random.Shared.Next(availableNeighbours.Count);
                var chosenNeighbour = availableNeighbours[chosenNeighbourIndex];
                availableNeighbours.Remove(chosenNeighbour);

                var newRB = chosenNeighbour.rb ?? new() { Location = chosenNeighbour.pt.ToNumericsVector2() };
                if (chosenNeighbour.rb is null) PlacedEntities.Add(newRB);
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
        else
            PlacedEntities.Add(entity);
    }

    public void PlantForest(TreeTemplate treeTemplate, Vector2i center, float radius, float chanceCenter, float chanceEdge)
    {
        for (int y = (int)MathF.Ceiling(center.Y + radius); y >= MathF.Floor(center.Y - radius); --y)
            for (int x = (int)MathF.Ceiling(center.X + radius); x >= MathF.Floor(center.X - radius); --x)
            {
                var distanceFromCenter = new Vector2i(Math.Abs(y - center.Y), Math.Abs(y - center.Y)).EuclideanLength;
                var chance = chanceCenter * (radius - distanceFromCenter) / radius + chanceEdge * distanceFromCenter / radius;
                if (Random.Shared.NextDouble() < chance)
                    PlaceEntity(Tree.FromTemplate(treeTemplate, new(x, y)));
            }
    }

    public bool RemoveEntity(PlaceableEntity entity)
    {
        if (SelectedEntity == entity) SelectedEntity = null;
        return PlacedEntities.Remove(entity);
    }

    event Action<Building>? PlacedBuilding;
    public void MouseEvent(Vector2i screenPosition, Vector2i worldLocation, InputAction? inputAction = null, MouseButton? mouseButton = null, KeyModifiers? keyModifiers = null)
    {
        if (inputAction == InputAction.Press && mouseButton == MouseButton.Button1)
        {
            if (CurrentBuildingTemplate is not null
                && !GetEntities<Building>().Any(b => b.Box.Intersects(Box2.FromCornerSize(worldLocation, new(CurrentBuildingTemplate.Width, CurrentBuildingTemplate.Height)))))
            {
                var building = Building.FromTemplate(CurrentBuildingTemplate, worldLocation.ToNumericsVector2(), false);
                PlaceEntity(building);
                PlacedBuilding?.Invoke(building);
                if (keyModifiers?.HasFlag(KeyModifiers.Shift) != true)
                    CurrentBuildingTemplate = null;
            }
            else
            {
                if (SelectedEntity is not null && SelectedEntity.Location.ToVector2i() == worldLocation)
                    SelectedEntity = GetEntities().SkipWhile(e => e != SelectedEntity).Skip(1).FirstOrDefault(e => e.Box.Contains(worldLocation));
                else
                    SelectedEntity = null;
                SelectedEntity ??= GetEntities().FirstOrDefault(e => e.Box.Contains(worldLocation));
            }
        }
        else if (inputAction == InputAction.Press && mouseButton == MouseButton.Button2)
            CurrentBuildingTemplate = null;

        (MouseScreenPosition, MouseWorldPosition) = (screenPosition, worldLocation);
    }

    event Action<PlaceableEntity, Villager>? StartedJob;
    public void PlanWork<T>(T entity, Villager villager) where T : PlaceableEntity
    {
        if (entity is Building building)
            if (building.GetEmptyAssignedWorkerSlot() is { } emptyAssignedWorker)
                emptyAssignedWorker.Villager = villager;
            else
                ThrowNoEmptyWorkerSlotException(entity, villager);
        else if (entity is Tree tree)
            if (tree.AssignedVillager is null)
                tree.AssignedVillager = villager;
            else
                ThrowNoEmptyWorkerSlotException(entity, villager);
        else
            throw new NotImplementedException();
    }

    [DoesNotReturn]
    static void ThrowNoEmptyWorkerSlotException<T>(T entity, Villager villager) where T : PlaceableEntity =>
        throw new InvalidOperationException($"Could not find an empty worker slot for {villager} to work on {entity}.");

    public void StartWork<T>(T entity, Villager villager) where T : PlaceableEntity
    {
        StartedJob?.Invoke(entity, villager);
        if (entity is Building building && building.GetAssignedWorkerSlot(villager) is { } assignedWorker)
            assignedWorker.VillagerWorking = true;
        else if (entity is Tree tree)
            tree.AssignedVillagerWorking = true;
        else
            throw new NotImplementedException();
    }

    event Action<PlaceableEntity, Villager, bool /* last */>? EndedBuildingJob;
    public void EndWork<T>(T entity, Villager villager) where T : PlaceableEntity
    {
        if (entity is Building building)
        {
            if (building.IsBuilt)
                if (building.GetAssignedWorkerSlot(villager) is { } assignedWorker)
                    assignedWorker.Villager = null;
                else
                    ThrowNoEmptyWorkerSlotException(entity, villager);
            else
                building.FinishBuilding();
            EndedBuildingJob?.Invoke(building, villager, !building.AssignedWorkers.Any(w => w.VillagerWorking));
        }
        else if (entity is Tree tree)
        {
            tree.AssignedVillager = null;
            EndedBuildingJob?.Invoke(tree, villager, true);
        }
        else
            throw new NotImplementedException();
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

        if (!Paused)
        {
            Offset += deltaOffsetNextFrame * (float)deltaSec * deltaOffsetPerSecond;
            aiManager.Update(deltaSec);
        }
        soundManager.Update(deltaSec);
        GetEntities<Villager>().ForEach(villager => villager.Update(deltaSec));
    }

    public IEnumerable<T> GetEntities<T>() where T : PlaceableEntity => PlacedEntities.OfType<T>();
    public IEnumerable<PlaceableEntity> GetEntities() => PlacedEntities;

    public double GetTotalResourceAmount(Resource resource)
    {
        double total = 0;
        foreach (var entity in GetEntities())
            if (entity is ResourceBucket rb)
                total += (rb.ResourceQuantities.FirstOrDefault(w => w.Resource == resource)?.Quantity ?? 0);
            else if (entity is Building { IsBuilt: true, Type: BuildingType.Storage, Inventory: { } buildingInventory })
                total += (buildingInventory.ResourceQuantities.FirstOrDefault(w => w.Resource == resource)?.Quantity ?? 0);
        return total;
    }
}
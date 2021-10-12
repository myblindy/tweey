namespace Tweey.WorldData;

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

    public Vector2i MouseScreenPosition { get; private set; }
    public Vector2i MouseWorldPosition { get; private set; }

    public World(ILoader loader)
    {
        (Resources, Configuration, aiManager, soundManager) = (new(loader), new(loader), new(this), new(this));
        (BuildingTemplates, TreeTemplates) = (new(loader, Resources), new(loader, Resources));

        StartedBuildingJob += soundManager.OnStartedBuildingJob;
        EndedBuildingJob += soundManager.OnEndedBuildingJob;
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
                            .Select(l => (l, rb: PlacedEntities.OfType<ResourceBucket>().FirstOrDefault(e => e.Contains(l))))
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

        if (entity is IResourceNeed resourceNeed)
        {

        }
    }

    public void PlantForest(Vector2i center, float radius, float chanceCenter, float chanceEdge)
    {
        for (int y = (int)MathF.Ceiling(center.Y + radius); y >= MathF.Floor(center.Y - radius); --y)
            for (int x = (int)MathF.Ceiling(center.X + radius); x >= MathF.Floor(center.X - radius); --x)
            {
                var distanceFromCenter = new Vector2i(Math.Abs(y - center.Y), Math.Abs(y - center.Y)).EuclideanLength;
                var chance = chanceCenter * (radius - distanceFromCenter) / radius + chanceEdge * distanceFromCenter / radius;
                if (Random.Shared.NextDouble() < chance)
                    PlaceEntity(Tree.FromTemplate(TreeTemplates["pine"], new(x, y)));
            }
    }

    public bool RemoveEntity(PlaceableEntity entity)
    {
        if (SelectedEntity == entity) SelectedEntity = null;
        return PlacedEntities.Remove(entity);
    }

    public event Action<Building>? PlacedBuilding;
    public void MouseEvent(Vector2i screenPosition, Vector2i worldLocation, InputAction? inputAction = null, MouseButton? mouseButton = null, KeyModifiers? keyModifiers = null)
    {
        if (inputAction == InputAction.Press && mouseButton == MouseButton.Button1)
        {
            if (CurrentBuildingTemplate is not null && !GetEntities<Building>().Any(b => b.Box.Intersects(Box2.FromCornerSize(worldLocation.ToNumericsVector2(), new(CurrentBuildingTemplate.Width, CurrentBuildingTemplate.Height)))))
            {
                var building = Building.FromTemplate(CurrentBuildingTemplate, worldLocation.ToNumericsVector2(), false);
                PlaceEntity(building);
                PlacedBuilding?.Invoke(building);
                if (keyModifiers?.HasFlag(OpenTK.Windowing.GraphicsLibraryFramework.KeyModifiers.Shift) != true)
                    CurrentBuildingTemplate = null;
            }
            else
            {
                if (SelectedEntity is not null && SelectedEntity.Location.ToVector2i() == worldLocation)
                    SelectedEntity = PlacedEntities.SkipWhile(e => e != SelectedEntity).Skip(1).FirstOrDefault(e => e.Box.Contains(worldLocation));
                else
                    SelectedEntity = null;
                SelectedEntity ??= PlacedEntities.FirstOrDefault(e => e.Box.Contains(worldLocation));
            }
        }
        else if (inputAction == InputAction.Press && mouseButton == MouseButton.Button2)
            CurrentBuildingTemplate = null;

        (MouseScreenPosition, MouseWorldPosition) = (screenPosition, worldLocation);
    }

    public event Action<Building, Villager>? StartedBuildingJob;
    public void StartWork(Building building, Villager villager)
    {
        StartedBuildingJob?.Invoke(building, villager);
        building.AssignedWorkersWorking[building.AssignedWorkers.FindIndex(v => v == villager)] = true;
    }

    public event Action<Building, Villager>? EndedBuildingJob;
    public void EndWork(Building building, Villager villager)
    {
        EndedBuildingJob?.Invoke(building, villager);
        building.AssignedWorkersWorking[building.AssignedWorkers.FindIndex(v => v == villager)] = false;
    }

    public event Action<BuildingTemplate?>? CurrentBuildingTemplateChanged;
    internal void FireCurrentBuildingTemplateChanged() =>
        CurrentBuildingTemplateChanged?.Invoke(CurrentBuildingTemplate);

    public void KeyEvent(InputAction inputAction, Keys key, int scanCode, KeyModifiers keyModifiers)
    {
        if (inputAction == InputAction.Press && key == Keys.Space)
            Paused = !Paused;
    }

    public void Update(double deltaSec)
    {
        if (!Paused)
            aiManager.Update(deltaSec);
        soundManager.Update(deltaSec);
    }

    public IEnumerable<T> GetEntities<T>() where T : PlaceableEntity => PlacedEntities.OfType<T>();

    public IEnumerable<PlaceableEntity> GetEntities() => PlacedEntities;
}
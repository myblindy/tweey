namespace Tweey.WorldData;

public class World
{
    public ResourceTemplates Resources { get; }
    public BuildingTemplates BuildingTemplates { get; }
    public Configuration Configuration { get; }

    AIManager AIManager { get; }
    List<PlaceableEntity> PlacedEntities { get; } = new();
    public PlaceableEntity? SelectedEntity { get; set; }

    public bool Paused { get; private set; }

    public World(ILoader loader)
    {
        (Resources, Configuration, AIManager) = (new(loader), new(loader), new(this));
        BuildingTemplates = new(loader, Resources);
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

    public bool RemoveEntity(PlaceableEntity entity)
    {
        if (SelectedEntity == entity) SelectedEntity = null;
        return PlacedEntities.Remove(entity);
    }
    //public void RemoveEntitiesWhere(Predicate<PlaceableEntity> entitiesMatch) => PlacedEntities.RemoveAll(entitiesMatch);
    //public void RemoveEntitiesWhere<T>(Predicate<T> entitiesMatch) where T : PlaceableEntity => PlacedEntities.RemoveAll(w => w is T t && entitiesMatch(t));

    public void MouseEvent(Vector2i worldLocation, InputAction inputAction, MouseButton mouseButton, KeyModifiers keyModifiers)
    {
        if (inputAction == InputAction.Press && mouseButton == MouseButton.Button1)
        {
            if (SelectedEntity is not null && SelectedEntity.Location.ToVector2i() == worldLocation)
                SelectedEntity = PlacedEntities.SkipWhile(e => e != SelectedEntity).Skip(1).FirstOrDefault(e => e.Box.Contains(worldLocation));
            else
                SelectedEntity = null;
            SelectedEntity ??= PlacedEntities.FirstOrDefault(e => e.Box.Contains(worldLocation));
        }
    }

    public void KeyEvent(InputAction inputAction, Keys key, int scanCode, KeyModifiers keyModifiers)
    {
        if (inputAction == InputAction.Press && key == Keys.Space)
            Paused = !Paused;
    }

    public void Update(double deltaSec)
    {
        if (!Paused)
            AIManager.Update(deltaSec);
    }

    public IEnumerable<T> GetEntities<T>() where T : PlaceableEntity => PlacedEntities.OfType<T>();

    public IEnumerable<PlaceableEntity> GetEntities() => PlacedEntities;
}

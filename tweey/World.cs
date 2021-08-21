namespace Tweey
{
    class World
    {
        public ResourceTemplates Resources { get; }
        public BuildingTemplates BuildingTemplates { get; }
        public Configuration Configuration { get; }

        public List<IPlaceableEntity> PlacedEntities { get; } = new();

        public World(ILoader loader) =>
            (Resources, BuildingTemplates, Configuration) = (new(loader), new(loader), new(loader));

        public void PlaceEntity(IPlaceableEntity entity)
        {
            if (entity is ResourceBucket resourceBucket)
            {
                // obey the maximum ground stack weight
                List<(Vector2i pt, ResourceBucket? rb)> availableNeighbours = new();
                int availableNeighboursSearchRadius = -1;
                while (resourceBucket.Weight > 0)
                {
                    // take any spill-over and put it in a random direction, up to maximumGroundDropSpillOverRange range
                    while (availableNeighbours.Count == 0)
                    {
                        ++availableNeighboursSearchRadius;
                        availableNeighbours.AddRange(
                            GameUtilities.EnumerateNeighbourLocations(resourceBucket.Location, radiusMin: availableNeighboursSearchRadius, radiusMax: availableNeighboursSearchRadius)
                                .Select(l => (l, rb: PlacedEntities.OfType<ResourceBucket>().FirstOrDefault(e => e.Contains(l))))
                                .Where(l => l.rb is null || l.rb.Weight < Configuration.Data.GroundStackMaximumWeight));
                    }

                    var chosenNeighbourIndex = Random.Shared.Next(availableNeighbours.Count);
                    var chosenNeighbour = availableNeighbours[chosenNeighbourIndex];
                    availableNeighbours.Remove(chosenNeighbour);

                    var newRB = chosenNeighbour.rb ?? new() { Location = chosenNeighbour.pt.ToNumericsVector2() };
                    if (chosenNeighbour.rb is null) PlacedEntities.Add(newRB);
                    var newRBWeight = newRB.Weight;
                    int resourceIndex = 0;
                    for (; resourceIndex < resourceBucket.Resources.Count && resourceBucket.Resources[resourceIndex].Quantity == 0; resourceIndex++) { }

                    while (resourceIndex < resourceBucket.Resources.Count)
                    {
                        var resQ = resourceBucket.Resources[resourceIndex];
                        var maxNewWeight = Configuration.Data.GroundStackMaximumWeight - newRBWeight;
                        var quantityToMove = (int)Math.Floor(Math.Min(maxNewWeight, resQ.Weight) / resQ.Resource.Weight);
                        newRB.Resources.Add(new(resQ.Resource) { Quantity = quantityToMove });
                        if ((resQ.Quantity -= quantityToMove) > 0)
                            break;  // couldn't finish the stack
                        newRBWeight += resQ.Resource.Weight * quantityToMove;

                        for (++resourceIndex; resourceIndex < resourceBucket.Resources.Count && resourceBucket.Resources[resourceIndex].Quantity == 0; resourceIndex++) { }
                    }
                }
            }
            else
                PlacedEntities.Add(entity);

            if (entity is IResourceNeed resourceNeed)
            {

            }
        }
    }
}

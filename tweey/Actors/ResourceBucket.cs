using System.Collections.ObjectModel;

namespace Tweey.Actors
{
    public class ResourceBucket : PlaceableEntity
    {
        public ResourceBucket(params ResourceQuantity[] rq)
        {
            (ResourceQuantities, AvailableResourceQuantities) = (new(resourceQuantities), new(availableResourceQuantities));
            (Width, Height) = (1, 1);
            AddRange(rq);
        }

        readonly List<ResourceQuantity> resourceQuantities = new(), availableResourceQuantities = new();

        public ReadOnlyCollection<ResourceQuantity> ResourceQuantities { get; }
        public ReadOnlyCollection<ResourceQuantity> AvailableResourceQuantities { get; }
        internal readonly Dictionary<ResourcePickupAIPlan, ResourceBucket> PlannedResourceBucket = new();

        public bool PlanResouces(ResourcePickupAIPlan plan, ref double availableCarryWeight)
        {
            ResourceBucket? plannedRB = null;

            foreach (var rq in resourceQuantities)
                if (rq.Resource.Weight <= availableCarryWeight)
                {
                    var usedQuantity = Math.Floor(availableCarryWeight / rq.Resource.Weight);
                    var usedRQ = new ResourceQuantity(rq.Resource, usedQuantity);
                    InternalAdd(usedRQ, resourceQuantities, negative: true);
                    InternalAdd(usedRQ, availableResourceQuantities, negative: true);

                    if (plannedRB is null && !PlannedResourceBucket.TryGetValue(plan, out plannedRB))
                        PlannedResourceBucket[plan] = plannedRB = new();
                    plannedRB.Add(usedRQ);

                    availableCarryWeight -= usedRQ.Weight;
                }

            return plannedRB is not null;
        }

        static void InternalAdd(ResourceQuantity rq, List<ResourceQuantity> rqs, bool negative = false)
        {
            var destResource = rqs.FirstOrDefault(w => w.Resource == rq.Resource);
            if (destResource is null && !negative)
                rqs.Add(destResource = new(rq.Resource, rq.Quantity));
            else if (destResource is null)
                throw new InvalidOperationException("Trying to remove more resources than available.");
            else if (negative)
                destResource.Quantity -= rq.Quantity;
            else
                destResource.Quantity += rq.Quantity;
        }

        public void Add(ResourceQuantity rq)
        {
            InternalAdd(rq, resourceQuantities);
            InternalAdd(rq, availableResourceQuantities);
        }

        public void AddRange(IEnumerable<ResourceQuantity> src)
        {
            foreach (var rq in src)
                Add(rq);
        }

        public double Weight => ResourceQuantities.Sum(rq => rq.Weight);
    }
}

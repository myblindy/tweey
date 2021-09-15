namespace Tweey.Actors;

public class ResourceBucket : PlaceableEntity
{
    public ResourceBucket(params ResourceQuantity[] rq) : this(rq.AsEnumerable()) { }
    public ResourceBucket(IEnumerable<ResourceQuantity> rq)
    {
        (ResourceQuantities, AvailableResourceQuantities) = (new(resourceQuantities), new(availableResourceQuantities));
        (Width, Height) = (1, 1);
        AddRange(rq);
    }

    public override string? Name { get => ResourceQuantities.FirstOrDefault()?.Resource.Name; set => throw new NotImplementedException(); }

    readonly List<ResourceQuantity> resourceQuantities = new(), availableResourceQuantities = new();

    public ReadOnlyCollection<ResourceQuantity> ResourceQuantities { get; }
    public ReadOnlyCollection<ResourceQuantity> AvailableResourceQuantities { get; }
    readonly Dictionary<ResourcePickupAIPlan, ResourceBucket> PlannedResourceBucket = new();

    public bool PlanResouces(ResourcePickupAIPlan plan, ref double availableCarryWeight, ResourceBucket? costs = null)
    {
        ResourceBucket? plannedRB = null;

        foreach (var rq in AvailableResourceQuantities.Where(rq => rq.Weight > 0))
            if (Math.Min(rq.Quantity, Math.Floor(availableCarryWeight / rq.Resource.Weight)) is { } usedQuantity)
            {
                if (costs is not null)
                {
                    var maxCostQuantity = costs.AvailableResourceQuantities.FirstOrDefault(arq => arq.Resource == rq.Resource)?.Quantity ?? 0;
                    usedQuantity = Math.Min(usedQuantity, maxCostQuantity);
                }
                if (usedQuantity <= 0) continue;

                var usedRQ = new ResourceQuantity(rq.Resource, usedQuantity);
                InternalAdd(usedRQ, availableResourceQuantities, negative: true);

                if (plannedRB is null && !PlannedResourceBucket.TryGetValue(plan, out plannedRB))
                    PlannedResourceBucket[plan] = plannedRB = new();
                plannedRB.Add(usedRQ);

                if (costs is not null)
                {
                    if (!costs.PlannedResourceBucket.TryGetValue(plan, out var plannedCostRB))
                        costs.PlannedResourceBucket[plan] = plannedCostRB = new();
                    plannedCostRB.Add(usedRQ);
                    InternalAdd(usedRQ, costs.availableResourceQuantities, negative: true);
                }

                availableCarryWeight -= usedRQ.Weight;
            }

        return plannedRB is not null;
    }

    public ResourceBucket GetPlannedResource(ResourcePickupAIPlan plan) => PlannedResourceBucket[plan];

    public ResourceBucket RemovePlannedResources(ResourcePickupAIPlan plan)
    {
        if (PlannedResourceBucket.Remove(plan, out var rb))
        {
            // remove it from the total resources as well now
            rb.ResourceQuantities.ForEach(rq => InternalAdd(rq, resourceQuantities, negative: true));
            return rb;
        }

        throw new InvalidOperationException();
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

    public void Add(ResourceBucket rb, bool removeFromSource = false)
    {
        rb.AvailableResourceQuantities.ForEach(rq =>
        {
            InternalAdd(rq, resourceQuantities);
            InternalAdd(rq, availableResourceQuantities);
        });

        if (removeFromSource)
            rb.Clear();
    }

    public void AddRange(IEnumerable<ResourceQuantity> src)
    {
        foreach (var rq in src)
            Add(rq);
    }

    public void Remove(ResourceQuantity rq)
    {
        InternalAdd(rq, resourceQuantities, true);
        InternalAdd(rq, availableResourceQuantities, true);
    }

    public void Clear()
    {
        AvailableResourceQuantities.ForEach(rq => InternalAdd(rq, resourceQuantities, true));
        availableResourceQuantities.Clear();
    }

    public bool IsAvailableEmpty => !AvailableResourceQuantities.Any(rq => rq.Quantity > 0);
    public double AvailableWeight => AvailableResourceQuantities.Sum(rq => rq.Weight);
    public bool IsAllEmpty => !ResourceQuantities.Any(rq => rq.Quantity > 0);
    public double FullWeight => ResourceQuantities.Sum(rq => rq.Weight);
    public double PickupSpeedMultiplier => AvailableResourceQuantities.Sum(rq => rq.PickupSpeedMultiplier / rq.Quantity);

    public override string ToString() =>
        ResourceQuantities.Any(rq => rq.Quantity > 0) ? string.Join(", ", ResourceQuantities.Where(rq => rq.Quantity > 0).Select(rq => $"{rq.Quantity}x {rq.Resource.Name}")) : "nothing";
}

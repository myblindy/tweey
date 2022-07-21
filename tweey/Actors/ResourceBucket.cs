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

    public override string Name { get => ResourceQuantities.FirstOrDefault()?.Resource.Name ?? "Empty Resource"; set => throw new NotImplementedException(); }

    readonly List<ResourceQuantity> resourceQuantities = new(), availableResourceQuantities = new();

    public ReadOnlyCollection<ResourceQuantity> ResourceQuantities { get; }
    public ReadOnlyCollection<ResourceQuantity> AvailableResourceQuantities { get; }
    readonly Dictionary<AIPlan, ResourceBucket> PlannedResourceBucket = new();

    public Building? Building { get; set; }

    public ResourceBucket Clone()
    {
        var newRb = new ResourceBucket { Building = Building };

        foreach (var rq in resourceQuantities)
            InternalAdd(rq, newRb.resourceQuantities);
        foreach (var rq in availableResourceQuantities)
            InternalAdd(rq, newRb.availableResourceQuantities);

        return newRb;
    }

    /// <summary>
    /// Set (some) resources from this <c>ResourceBucket</c> as planned, to later be removed (<see href="RemovePlannedResources"/>) or 
    /// canceled to be brought back into the pool of available resources (<see href="CanelPlannedResources"/>).
    /// </summary>
    /// <param name="plan">The plan under which these resources are planned.</param>
    /// <param name="availableCarryWeight">How much weight can be planned. This parameter will be updated with the weight left after the plan.</param>
    /// <param name="costs">Costs describe which resources to gather and how many. If provided, this function will only consider those resources.</param>
    /// <param name="globalFilter">Selector to (partially) filter out resources globally, before anything is actually planned. Return less than or the same as the quantity given.</param>
    /// <param name="incrementalFilter">
    /// Selector to (partially) filter out resources incrementally, as resources are planned. This is called once with the resources considered in sequence, 
    /// to allow aggregation. Return less than or the same as the quantity given.
    /// </param>
    /// <returns>Whether or not anything was planned.</returns>
    public bool PlanResouces(AIPlan plan, ref double availableCarryWeight, ResourceBucket? costs = null, Func<Resource, double, double>? globalFilter = null,
        Func<Resource, double, double>? incrementalFilter = null)
    {
        ResourceBucket? plannedRB = null;

        var filteredAvailableResourceQuantities = globalFilter is null ? AvailableResourceQuantities : AvailableResourceQuantities.Select(rq => new ResourceQuantity(rq.Resource, globalFilter(rq.Resource, rq.Quantity)));
        foreach (var rq in filteredAvailableResourceQuantities.Where(rq => rq.Weight > 0))
            if (Math.Min(rq.Quantity, Math.Floor(availableCarryWeight / rq.Resource.Weight)) is { } usedQuantity)
            {
                if (costs is not null)
                {
                    var maxCostQuantity = costs.AvailableResourceQuantities.FirstOrDefault(arq => arq.Resource == rq.Resource)?.Quantity ?? 0;
                    usedQuantity = Math.Min(usedQuantity, maxCostQuantity);
                }
                if (usedQuantity <= 0) continue;

                if (incrementalFilter is not null)
                    if ((usedQuantity = incrementalFilter(rq.Resource, usedQuantity)) <= 0)
                        continue;

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

    public ResourceBucket GetPlannedResource(AIPlan plan) => PlannedResourceBucket[plan];

    public ResourceBucket RemovePlannedResources(AIPlan plan)
    {
        if (PlannedResourceBucket.Remove(plan, out var rb))
        {
            // remove it from the total resources as well now
            rb.ResourceQuantities.ForEach(rq => InternalAdd(rq, resourceQuantities, negative: true));
            return rb;
        }

        throw new InvalidOperationException();
    }

    public ResourceBucket CancelPlannedResources(AIPlan plan)
    {
        if (PlannedResourceBucket.Remove(plan, out var rb))
        {
            // put it back in the pool of available resources
            rb.ResourceQuantities.ForEach(rq => InternalAdd(rq, availableResourceQuantities));
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

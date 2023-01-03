namespace Tweey.Support;

internal class ResourceBucket
{
    public ResourceBucket(params ResourceQuantity[] rqs) : this(rqs.AsEnumerable()) { }
    public ResourceBucket(IEnumerable<ResourceQuantity> rqs)
    {
        foreach (var rq in rqs)
            Add(new(rq.Resource, rq.Quantity), ResourceMarker.Unmarked);
    }
    public ResourceBucket(ResourceQuantity rq, ResourceMarker marker) =>
        Add(new(rq.Resource, rq.Quantity), marker);

    readonly List<(ResourceQuantity rq, ResourceMarker marker)> resources = new();

    public void Add(ResourceQuantity rq, ResourceMarker marker)
    {
        if (marker == ResourceMarker.All)
            marker = ResourceMarker.Unmarked;

        foreach (var dstRq in GetResourceQuantities(marker))
            if (dstRq.Resource == rq.Resource)
            {
                dstRq.Quantity += rq.Quantity;
                return;
            }
        resources.Add((rq, marker));
    }

    public void Add(ResourceMarker srcMarker, ResourceBucket srcRB, ResourceMarker dstMarker)
    {
        foreach (var srcRQ in srcRB.GetResourceQuantities(srcMarker))
            Add(srcRQ, dstMarker);
    }

    public static void MarkResources(ResourceMarker marker, IEnumerable<ResourceBucket> sourceRBs, ResourceMarker sourceMarker, double maxWeight, ResourceBucket? _desired,
        Action<ResourceQuantity>? selectedFeedbackAction, out double usedWeight)
    {
        var desired = _desired?.Clone();
        usedWeight = 0;

        foreach (var srcRB in sourceRBs)
        {
            changedRetry:
            foreach (var srcRQ in srcRB.GetResourceQuantities(sourceMarker))
                if (desired is null)
                {
                    var qtyUsed = Math.Min(srcRQ.Quantity, ((maxWeight - usedWeight) / srcRQ.Resource.Weight).Floor<int>());

                    if (qtyUsed > 0)
                    {
                        srcRQ.Quantity -= qtyUsed;

                        srcRB.Add(new(srcRQ.Resource, qtyUsed), marker);
                        usedWeight += qtyUsed * srcRQ.Resource.Weight;
                        selectedFeedbackAction?.Invoke(new(srcRQ.Resource, qtyUsed));

                        goto changedRetry;
                    }
                }
                else
                    foreach (var desiredRQ in desired.GetResourceQuantities(ResourceMarker.All))
                        if (desiredRQ.Resource == srcRQ.Resource)
                        {
                            var qtyUsed = Math.Min(Math.Min(srcRQ.Quantity, desiredRQ.Quantity),
                                ((maxWeight - usedWeight) / srcRQ.Resource.Weight).Floor<int>());

                            if (qtyUsed > 0)
                            {
                                desiredRQ.Quantity -= qtyUsed;
                                srcRQ.Quantity -= qtyUsed;

                                srcRB.Add(new(srcRQ.Resource, qtyUsed), marker);
                                usedWeight += qtyUsed * srcRQ.Resource.Weight;
                                selectedFeedbackAction?.Invoke(new(srcRQ.Resource, qtyUsed));

                                if (desired.IsEmpty(ResourceMarker.All))
                                    return;
                                else
                                    goto changedRetry;
                            }
                        }
        }
    }

    public static void MarkResources(World world, ResourceMarker marker, IEnumerable<ResourceBucket> sourceRBs, ResourceMarker sourceMarker, double maxWeight,
        IEnumerable<Box2> zoneBoxes, IEnumerable<(Entity entity, ResourceBucket rb, Vector2i location)> _storedResources,
        Func<ResourceQuantity, Entity?, ResourceBucket?, Vector2i?, (ResourceBucket newRB, Entity newEntity)>? selectedFeedbackAction, out double usedWeight)
    {
        usedWeight = 0;
        using var storedResources = _storedResources.ToPooledCollection();

        // first try to stack it on existing stacks
        fullRetry:
        if (storedResources.Any())
            foreach (var srcRB in sourceRBs)
            {
                changedRetry0:
                foreach (var srcRQ in srcRB.GetResourceQuantities(sourceMarker))
                    if (!srcRQ.IsEmpty)
                    {
                        foreach (ref var storedResource in storedResources.AsSpanUnsafe())
                            foreach (var dstRQ in storedResource.rb.GetResourceQuantities(ResourceMarker.All))
                                if (dstRQ.Resource == srcRQ.Resource && dstRQ.Weight < world.Configuration.Data.GroundStackMaximumWeight)
                                {
                                    // found some space to stack it in
                                    var qtyUsed = Math.Min((world.Configuration.Data.GroundStackMaximumWeight / srcRQ.Resource.Weight - storedResource.rb.GetWeight(ResourceMarker.All)).Floor<int>(),
                                        Math.Min(srcRQ.Quantity, ((maxWeight - usedWeight) / srcRQ.Resource.Weight).Floor<int>()));

                                    if (qtyUsed > 0)
                                    {
                                        srcRQ.Quantity -= qtyUsed;

                                        srcRB.Add(new(srcRQ.Resource, qtyUsed), marker);
                                        usedWeight += qtyUsed * srcRQ.Resource.Weight;
                                        selectedFeedbackAction?.Invoke(new(srcRQ.Resource, qtyUsed), storedResource.entity, storedResource.rb, null);

                                        goto changedRetry0;
                                    }
                                }
                    }
            }

        // next try to make new stacks
        foreach (var srcRB in sourceRBs)
        {
            foreach (var srcRQ in srcRB.GetResourceQuantities(sourceMarker))
                if (!srcRQ.IsEmpty)
                {
                    foreach (var zoneBox in zoneBoxes)
                        foreach (var zoneBoxLocation in zoneBox)
                            if (!storedResources.Any(rw => rw.location == zoneBoxLocation))
                            {
                                // found some space to create a new stack
                                var qtyUsed = Math.Min((world.Configuration.Data.GroundStackMaximumWeight / srcRQ.Resource.Weight).Floor<int>(),
                                    Math.Min(srcRQ.Quantity, ((maxWeight - usedWeight) / srcRQ.Resource.Weight).Floor<int>()));

                                if (qtyUsed > 0)
                                {
                                    srcRQ.Quantity -= qtyUsed;

                                    srcRB.Add(new(srcRQ.Resource, qtyUsed), marker);
                                    usedWeight += qtyUsed * srcRQ.Resource.Weight;
                                    if (selectedFeedbackAction?.Invoke(new(srcRQ.Resource, qtyUsed), null, null, zoneBoxLocation) is { } result)
                                        storedResources.Add((result.newEntity, result.newRB, zoneBoxLocation));

                                    goto fullRetry;
                                }
                            }
                }
        }
    }

    public static bool TryToMarkResources(Func<ResourceMarker> markerGen, IEnumerable<ResourceBucket> sourceRBs, ResourceMarker sourceMarker, double maxWeight,
        IEnumerable<BuildingResouceQuantityTemplate> _requiredResourceGroups, out double usedWeight)
    {
        usedWeight = 0;
        var actions = new List<Action>();
        using var requiredResourceGroups = _requiredResourceGroups.ToPooledCollection();
        ResourceMarker marker = default;

        foreach (var reqRQ in requiredResourceGroups)
            foreach (var srcRB in sourceRBs)
                foreach (var srcRQ in srcRB.GetResourceQuantities(sourceMarker))
                    if (!srcRQ.IsEmpty && (srcRQ.Resource.Name == reqRQ.Resource || srcRQ.Resource.Groups.Contains(reqRQ.Resource)))
                    {
                        var qtyUsed = Math.Min(reqRQ.Quantity, Math.Min(srcRQ.Quantity, ((maxWeight - usedWeight) / srcRQ.Resource.Weight).Floor<int>()));

                        if (qtyUsed > 0)
                        {
                            reqRQ.Quantity -= qtyUsed;
                            actions.Add(() =>
                            {
                                srcRQ.Quantity -= qtyUsed;
                                srcRB.Add(new(srcRQ.Resource, qtyUsed), marker);
                            });
                            usedWeight += qtyUsed * srcRQ.Resource.Weight;
                        }
                    }

        if (requiredResourceGroups.Any(w => w.Quantity > 0))
            return false;

        marker = markerGen();
        foreach (var action in actions)
            action();
        return true;
    }

    public bool IsEmpty(ResourceMarker marker) =>
        GetResourceQuantities(marker).All(w => w.IsEmpty);

    public void Remove(ResourceQuantity rq)
    {
        var qtyRemaining = rq.Quantity;
        foreach (var (dstRq, _) in resources)
            if (dstRq.Resource == rq.Resource)
            {
                var qtyUsed = Math.Min(qtyRemaining, dstRq.Quantity);
                qtyRemaining -= qtyUsed;
                dstRq.Quantity -= qtyUsed;

                if (qtyRemaining == 0)
                    return;
                else if (qtyRemaining < 0)
                    throw new InvalidOperationException($"ResourceBucket was asked to remove too much {rq.Resource}.");
            }
    }

    public ResourceBucket Clone()
    {
        var newRb = new ResourceBucket();
        foreach (var (rq, marker) in resources)
            newRb.resources.Add((new(rq.Resource, rq.Quantity), marker));

        return newRb;
    }

    public IEnumerable<ResourceQuantity> GetResourceQuantities(ResourceMarker marker) =>
        marker == ResourceMarker.All ? resources.Select(w => w.rq) : resources.Where(w => w.marker == marker).Select(w => w.rq);

    public IEnumerable<(ResourceQuantity rq, ResourceMarker marker)> GetResourceQuantitiesWithMarkers(ResourceMarker marker) =>
        marker == ResourceMarker.All ? resources : resources.Where(w => w.marker == marker);

    public bool Contains(ResourceMarker marker, ResourceBucket other, ResourceMarker otherMarker)
    {
        foreach (var otherRQ in other.GetResourceQuantities(otherMarker).GroupBy(w => w.Resource, w => w.Quantity))
            if (GetResourceQuantities(marker).Where(w => w.Resource == otherRQ.Key).Sum(w => w.Quantity) < otherRQ.Sum())
                return false;
        return true;
    }

    public ResourceBucket WithRemove(ResourceBucket other)
    {
        var newRB = Clone();
        foreach (var (rq, _) in other.resources)
        {
            var qty = rq.Quantity;
            foreach (var (newRQ, _) in newRB.resources)
                if (newRQ.Resource == rq.Resource)
                {
                    var qtyUsed = Math.Min(newRQ.Quantity, qty);
                    qty -= qtyUsed;
                    newRQ.Quantity -= qtyUsed;
                }
        }

        return newRB;
    }

    public ResourceBucket WithRemove(ResourceMarker keepMarker, ResourceBucket other, ResourceMarker otherMarker, ResourceMarker destMarker)
    {
        if (destMarker == ResourceMarker.All)
            destMarker = ResourceMarker.Unmarked;

        var newRB = new ResourceBucket(GetResourceQuantities(keepMarker));

        foreach (var rq in other.GetResourceQuantities(otherMarker))
        {
            var qty = rq.Quantity;
            foreach (var newRQ in newRB.GetResourceQuantities(destMarker))
                if (newRQ.Resource == rq.Resource)
                {
                    var qtyUsed = Math.Max(newRQ.Quantity, qty);
                    qty -= qtyUsed;
                    newRQ.Quantity -= qtyUsed;
                }
        }

        return newRB;
    }

    public bool Overlaps(ResourceBucket other)
    {
        foreach (var (rq, _) in other.resources)
            if (!rq.IsEmpty && resources.Any(r => r.rq.Resource == rq.Resource && !r.rq.IsEmpty))
                return true;
        return false;
    }

    public double GetWeight(ResourceMarker marker) =>
        GetResourceQuantities(marker).Sum(w => w.Weight);

    public void Clear() => resources.Clear();

    public bool HasMarker(ResourceMarker marker) =>
        GetResourceQuantities(marker).Any();

    public void MoveTo(ResourceMarker srcMarker, ResourceBucket dstRB, ResourceMarker dstMarker)
    {
        foreach (var srcRQ in GetResourceQuantities(srcMarker))
            dstRB.Add(new(srcRQ.Resource, srcRQ.Quantity), dstMarker);
        resources.RemoveAll(w => w.marker == srcMarker);
    }

    internal void Remove(ResourceMarker marker) =>
        resources.RemoveAll(w => w.marker == marker);

    public override string ToString() =>
        string.Join(", ", resources.Select(w => $"{w.rq}{(w.marker == ResourceMarker.Unmarked ? "" : $" [{w.marker}]")}"));
}

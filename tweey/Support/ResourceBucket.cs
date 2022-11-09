namespace Tweey.Support;

internal class ResourceBucket
{
    public ResourceBucket(params ResourceQuantity[] rqs) : this(rqs.AsEnumerable()) { }
    public ResourceBucket(IEnumerable<ResourceQuantity> rqs)
    {
        foreach (var rq in rqs)
            Add(rq, ResourceMarker.Default);
    }

    readonly List<(ResourceQuantity rq, ResourceMarker marker)> resources = new();

    public void Add(ResourceQuantity rq, ResourceMarker marker)
    {
        if (marker == ResourceMarker.All)
            marker = ResourceMarker.Default;

        foreach (var dstRq in GetResourceQuantities(marker))
            if (dstRq.Resource == rq.Resource)
            {
                dstRq.Quantity += rq.Quantity;
                return;
            }
        resources.Add((rq, marker));
    }

    public static void MarkResources(ResourceMarker marker, IEnumerable<ResourceBucket> sourceRBs, ResourceMarker sourceMarker, double maxWeight, ResourceBucket desired,
        Action<ResourceQuantity>? selectedFeedbackAction, out double usedWeight)
    {
        desired = desired.Clone();
        usedWeight = 0;

        foreach (var srcRB in sourceRBs)
        {
            changedRetry:
            foreach (var srcRQ in srcRB.GetResourceQuantities(sourceMarker))
                foreach (var desiredRQ in desired.GetResourceQuantities(ResourceMarker.All))
                    if (desiredRQ.Resource == srcRQ.Resource)
                    {
                        var qtyUsed = Math.Min(Math.Min(srcRQ.Quantity, desiredRQ.Quantity), (maxWeight - usedWeight) / srcRQ.Resource.Weight);

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

    public bool IsEmpty(ResourceMarker marker) =>
        GetResourceQuantities(marker).All(w => w.Quantity == 0);

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
            if (rq.Quantity > 0 && resources.Any(r => r.rq.Resource == rq.Resource && r.rq.Quantity > 0))
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
}

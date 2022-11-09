namespace Tweey.Support;

internal class ResourceBucket
{
    public ResourceBucket(params ResourceQuantity[] rqs) : this(rqs.AsEnumerable()) { }
    public ResourceBucket(IEnumerable<ResourceQuantity> rqs)
    {
        foreach (var rq in rqs)
            Add(rq);
    }

    readonly List<(ResourceQuantity rq, ResourceMarker marker)> resources = new();

    public void Add(ResourceQuantity rq)
    {
        foreach (var (dstRq, _) in resources)
            if (dstRq.Resource == rq.Resource)
            {
                dstRq.Quantity += rq.Quantity;
                return;
            }
        resources.Add((rq, ResourceMarker.Default));
    }

    public static void MarkResources(ResourceMarker marker, IEnumerable<ResourceBucket> source, ResourceMarker sourceMarker, double maxWeight, ResourceBucket desired)
    {

    }

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
        foreach (var thisRq in GetResourceQuantities(marker).GroupBy(w => w.Resource, w => w.Quantity))
            if (other.GetResourceQuantities(otherMarker).Where(w => w.Resource == thisRq.Key).Sum(w => w.Quantity) < thisRq.Sum())
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
}

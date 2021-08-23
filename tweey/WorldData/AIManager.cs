using MoreLinq;

namespace Tweey.WorldData;

public abstract class AIPlan
{
    protected readonly Villager villager;
    protected AIPlan(Villager villager) => this.villager = villager;

    public abstract void Update(double deltaSec);

    public abstract PlaceableEntity? FirstTarget { get; }
}

public class ResourcePickupAIPlan : AIPlan
{
    public ResourcePickupAIPlan(Villager villager) : base(villager) { }

    public List<ResourceBucket> WorldBuckets { get; } = new();
    public override PlaceableEntity? FirstTarget => WorldBuckets.FirstOrDefault();

    public override void Update(double deltaSec)
    {
        // move to the resource
        villager.MovementActionTime.AdvanceTime(deltaSec);

        for (int movements = villager.MovementActionTime.ConsumeActions(); movements > 0; --movements)
            villager.Location += (FirstTarget!.Location - villager.Location).Sign();
    }
}

class AIManager
{
    private readonly World world;

    public AIManager(World world) => this.world = world;

    bool TryToBuildHaulingPlan(Villager villager)
    {
        if (!world.GetEntities<ResourceBucket>().Any()) return false;

        var plan = new ResourcePickupAIPlan(villager);

        var availableCarryWeight = world.Configuration.Data.BaseCarryWeight - villager.Inventory.Weight;
        foreach (var rb in world.GetEntities<ResourceBucket>().OrderBy(rb => (rb.Center - villager.Center).LengthSquared()))
            if (availableCarryWeight > 0 && rb.PlanResouces(plan, ref availableCarryWeight))
                plan.WorldBuckets.Add(rb);

        var ok = plan.WorldBuckets.Any();
        if (ok) villager.AIPlan = plan;
        return ok;
    }

    public void Update(double deltaSec)
    {
        foreach (var villager in world.GetEntities<Villager>())
        {
            if (villager.AIPlan is null)
                TryToBuildHaulingPlan(villager);
            villager.AIPlan?.Update(deltaSec);
        }
    }
}

using MoreLinq;

namespace Tweey.WorldData;

public abstract class AIPlan
{
    protected readonly Villager villager;
    protected AIPlan(Villager villager) => this.villager = villager;

    public bool Done { get; protected set; }

    public abstract void Update(double deltaSec);

    public abstract PlaceableEntity? FirstTarget { get; }
}

public class ResourcePickupAIPlan : AIPlan
{
    readonly World world;
    public ResourcePickupAIPlan(World world, Villager villager) : base(villager) => this.world = world;

    public List<ResourceBucket> WorldBuckets { get; } = new();
    public override PlaceableEntity? FirstTarget => WorldBuckets.FirstOrDefault();

    enum State { MoveToResource, PickupResource }
    State state = State.MoveToResource;

    public override void Update(double deltaSec)
    {
        switch (state)
        {
            case State.MoveToResource:
                // check for when we're done
                if (!WorldBuckets.Any()) { Done = true; return; }

                villager.MovementActionTime.AdvanceTime(deltaSec);

                for (int movements = villager.MovementActionTime.ConsumeActions(); movements > 0; --movements)
                {
                    var locationDifference = FirstTarget!.Location - villager.Location;

                    // reached the resource?
                    if (locationDifference is ( >= -1 and <= 1, >= -1 and <= 1))
                    {
                        state = State.PickupResource;
                        villager.PickupActionTime.Reset(
                            villager.PickupActionsPerSecond / WorldBuckets[0].GetPlannedResource(this).PickupSpeedMultiplier);
                        return;
                    }

                    // if not, move towards it
                    villager.Location += locationDifference.Sign();
                }
                break;

            case State.PickupResource:
                villager.PickupActionTime.AdvanceTime(deltaSec);
                if (villager.PickupActionTime.ConsumeActions() > 0)
                {
                    // remove the resource from the world and from our list of targets to hit and place it in our inventory
                    var worldRB = WorldBuckets[0];
                    WorldBuckets.Remove(worldRB);
                    var plannedRB = worldRB.RemovePlannedResources(this);

                    if (worldRB.IsEmpty)
                        world.RemoveEntity(worldRB);

                    villager.Inventory.AddRange(plannedRB.ResourceQuantities);

                    state = State.MoveToResource;
                    return;
                }
                break;

            default: throw new NotImplementedException();
        }
    }
}

class AIManager
{
    private readonly World world;

    public AIManager(World world) => this.world = world;

    bool TryToBuildHaulingPlan(Villager villager)
    {
        if (!world.GetEntities<ResourceBucket>().Any()) return false;

        var plan = new ResourcePickupAIPlan(world, villager);

        var availableCarryWeight = world.Configuration.Data.BaseCarryWeight - villager.Inventory.Weight;
        foreach (var rb in world.GetEntities<ResourceBucket>().OrderBy(rb => (rb.Center - villager.Center).LengthSquared()))
            if (availableCarryWeight > 0 && rb.PlanResouces(plan, ref availableCarryWeight))
                plan.WorldBuckets.Add(rb);

        var ok = plan.WorldBuckets.Any();
        if (ok) villager.AIPlan = plan;
        return ok;
    }

    readonly List<Villager> updateVillagersList = new();
    public void Update(double deltaSec)
    {
        updateVillagersList.Clear();
        updateVillagersList.AddRange(world.GetEntities<Villager>());
        foreach (var villager in updateVillagersList)
        {
            if (villager.AIPlan is null)
                TryToBuildHaulingPlan(villager);
            villager.AIPlan?.Update(deltaSec);
        }

        world.GetEntities<Villager>().Where(v => v.AIPlan?.Done == true).ForEach(v => v.AIPlan = null);
    }
}

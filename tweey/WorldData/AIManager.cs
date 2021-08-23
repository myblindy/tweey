namespace Tweey.WorldData;

public abstract class AIPlan
{
    protected readonly Villager villager;
    protected readonly World world;
    protected AIPlan(World world, Villager villager) => (this.villager, this.world) = (villager, world);

    public AIPlan? NextPlan { get; set; }

    public bool Done { get; protected set; }

    public abstract void Update(double deltaSec);

    public abstract PlaceableEntity? FirstTarget { get; }

    protected void StepToPlaceable(PlaceableEntity entity, double deltaSec, Action targetReached)
    {
        villager.MovementActionTime.AdvanceTime(deltaSec);

        for (int movements = villager.MovementActionTime.ConsumeActions(); movements > 0; --movements)
        {
            // reached the resource?
            if (FirstTarget!.Box.Intersects(villager.Box.WithExpand(Vector2.One)))
            {
                targetReached();
                return;
            }

            // if not, move towards it
            villager.Location += (FirstTarget!.Location - villager.Location).Sign();
        }
    }
}

public class ResourcePickupAIPlan : AIPlan
{
    public ResourcePickupAIPlan(World world, Villager villager) : base(world, villager) { }

    public List<ResourceBucket> WorldBuckets { get; } = new();
    public override PlaceableEntity? FirstTarget => WorldBuckets.FirstOrDefault();

    enum State { MoveToResource, PickupResources }
    State state = State.MoveToResource;

    public override void Update(double deltaSec)
    {
        switch (state)
        {
            case State.MoveToResource:
                // check for when we're done
                if (!WorldBuckets.Any()) { Done = true; return; }

                StepToPlaceable(FirstTarget!, deltaSec, () =>
                {
                    state = State.PickupResources;
                    villager.PickupActionTime.Reset(
                        villager.PickupActionsPerSecond / WorldBuckets[0].GetPlannedResource(this).PickupSpeedMultiplier);
                });
                break;

            case State.PickupResources:
                villager.PickupActionTime.AdvanceTime(deltaSec);
                if (villager.PickupActionTime.ConsumeActions() > 0)
                {
                    // remove the resource from the world and from our list of targets to hit and place it in our inventory
                    var worldRB = WorldBuckets[0];
                    WorldBuckets.Remove(worldRB);
                    var plannedRB = worldRB.RemovePlannedResources(this);

                    if (worldRB.IsAllEmpty)
                        world.RemoveEntity(worldRB);

                    villager.Inventory.AddRange(plannedRB.ResourceQuantities);

                    // sort the events for distance to our new position
                    WorldBuckets.Sort((a, b) => (a.Center - villager.Center).LengthSquared().CompareTo((b.Center - villager.Center).LengthSquared()));

                    state = State.MoveToResource;
                    return;
                }
                break;

            default: throw new NotImplementedException();
        }
    }
}

public class StoreInventoryAIPlan : AIPlan
{
    readonly Building storage;

    public StoreInventoryAIPlan(World world, Villager villager, Building storage) : base(world, villager) => this.storage = storage;

    public override PlaceableEntity? FirstTarget => storage;

    enum State { MoveToStorage, StoreResources }
    State state = State.MoveToStorage;

    public override void Update(double deltaSec)
    {
        switch (state)
        {
            case State.MoveToStorage:
                StepToPlaceable(FirstTarget!, deltaSec, () =>
                {
                    state = State.StoreResources;
                    villager.PickupActionTime.Reset(
                        villager.PickupActionsPerSecond / villager.Inventory.PickupSpeedMultiplier);
                });
                break;
            case State.StoreResources:
                villager.PickupActionTime.AdvanceTime(deltaSec);
                if (villager.PickupActionTime.ConsumeActions() > 0)
                {
                    storage.Inventory.Add(villager.Inventory, true);
                    Done = true;
                }
                break;
        }
    }
}

class AIManager
{
    private readonly World world;

    public AIManager(World world) => this.world = world;

    bool TryToBuildHaulingPlan(Villager villager)
    {
        bool anyRB = false, anyStorage = false;
        world.GetEntities().ForEach(e => { anyRB = anyRB || e is ResourceBucket; anyStorage = anyStorage || (e is Building building && building.Type == BuildingType.Storage); });
        if (!world.GetEntities<ResourceBucket>().Any()) return false;

        var plan = new ResourcePickupAIPlan(world, villager);

        // plan the resource acquisition
        var availableCarryWeight = world.Configuration.Data.BaseCarryWeight - villager.Inventory.AvailableWeight;
        foreach (var rb in world.GetEntities<ResourceBucket>().OrderBy(rb => (rb.Center - villager.Center).LengthSquared()))
            if (availableCarryWeight > 0 && rb.PlanResouces(plan, ref availableCarryWeight))
                plan.WorldBuckets.Add(rb);

        var ok = plan.WorldBuckets.Any();
        if (ok)
        {
            // plan the storage
            var closestAvailableStorage = world.GetEntities<Building>().FirstOrDefault(b => b.Type == BuildingType.Storage);
            if (closestAvailableStorage is not null)
            {
                var storePlan = new StoreInventoryAIPlan(world, villager, closestAvailableStorage);
                plan.NextPlan = storePlan;

                villager.AIPlan = plan;
            }
        }
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

        world.GetEntities<Villager>().Where(v => v.AIPlan?.Done == true).ForEach(v => v.AIPlan = v.AIPlan?.NextPlan);
    }
}

namespace Tweey.WorldData;

public abstract class AIPlan
{
    protected Villager Villager { get; }
    protected World World { get; }
    public bool IsEmergency { get; }
    protected AIPlan(World world, Villager villager, bool isEmergency) => (Villager, World, IsEmergency) = (villager, world, isEmergency);

    public AIPlan? NextPlan { get; set; }

    public bool Done { get; protected set; }
    public Action? DoneAction { get; set; }

    public abstract void Update(double deltaSec);

    public abstract PlaceableEntity? FirstTarget { get; }

    public abstract string Description { get; }

    protected void StepToPlaceable(PlaceableEntity entity, double deltaSec, Action targetReached)
    {
        Villager.MovementActionTime.AdvanceTime(deltaSec);

        for (int movements = Villager.MovementActionTime.ConsumeActions(); movements > 0; --movements)
        {
            // reached the resource?
            if (entity!.Box.Intersects(Villager.Box.WithExpand(Vector2.One)))
            {
                targetReached();
                return;
            }

            // if not, move towards it
            Villager.Location += (entity!.Location - Villager.Location).Sign();
        }
    }

    public virtual void Cancel() { }
}

public class ResourcePickupAIPlan : AIPlan
{
    public ResourcePickupAIPlan(World world, Villager villager) : base(world, villager, false) { }

    public List<ResourceBucket> WorldBuckets { get; } = new();
    public override PlaceableEntity? FirstTarget => (PlaceableEntity?)WorldBuckets.FirstOrDefault()?.Building ?? WorldBuckets.FirstOrDefault();
    public override string Description => "Gathering resources to haul.";

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
                    Villager.PickupActionTime.Reset(
                        Villager.PickupActionsPerSecond / WorldBuckets[0].GetPlannedResource(this).PickupSpeedMultiplier);
                });
                break;

            case State.PickupResources:
                Villager.PickupActionTime.AdvanceTime(deltaSec);
                if (Villager.PickupActionTime.ConsumeActions() > 0)
                {
                    // remove the resource from the world and from our list of targets to hit and place it in our inventory
                    var worldRB = WorldBuckets[0];
                    WorldBuckets.Remove(worldRB);
                    var plannedRB = worldRB.RemovePlannedResources(this);

                    if (worldRB.IsAllEmpty)
                        World.RemoveEntity(worldRB);

                    Villager.Inventory.AddRange(plannedRB.ResourceQuantities);

                    // sort the events for distance to our new position
                    WorldBuckets.Sort((a, b) => (a.Center - Villager.Center).LengthSquared().CompareTo((b.Center - Villager.Center).LengthSquared()));

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

    public StoreInventoryAIPlan(World world, Villager villager, Building storage) : base(world, villager, false) => this.storage = storage;

    public override PlaceableEntity? FirstTarget => storage;
    public override string Description => $"Dropping resources off to {storage.Name}.";

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
                    Villager.PickupActionTime.Reset(Villager.PickupActionsPerSecond / Villager.Inventory.PickupSpeedMultiplier);
                });
                break;
            case State.StoreResources:
                Villager.PickupActionTime.AdvanceTime(deltaSec);
                if (Villager.PickupActionTime.ConsumeActions() > 0)
                {
                    storage.Inventory.Add(Villager.Inventory, true);
                    Done = true;
                }
                break;
        }
    }
}

public class BuildAIPlan : AIPlan
{
    readonly Building building;

    public BuildAIPlan(World world, Villager villager, Building building) : base(world, villager, false) => this.building = building;

    public override PlaceableEntity? FirstTarget => building;
    public override string Description => $"Building {building.Name}.";

    enum State { MoveToBuilding, Build }
    State state = State.MoveToBuilding;

    public override void Update(double deltaSec)
    {
        switch (state)
        {
            case State.MoveToBuilding:
                StepToPlaceable(FirstTarget!, deltaSec, () =>
                {
                    state = State.Build;
                    Villager.WorkActionTime.Reset(Villager.WorkActionsPerSecond);

                    World.StartWork(building, Villager);
                });
                break;
            case State.Build:
                Villager.WorkActionTime.AdvanceTime(deltaSec);
                if (Villager.WorkActionTime.ConsumeActions() > 0 && --building.BuildWorkTicks <= 0)
                {
                    (building.IsBuilt, Done) = (true, true);
                    World.EndWork(building, Villager);
                    building.Inventory.Clear();
                }

                break;
        }
    }
}

public class ChopTreeAIPlan : AIPlan
{
    readonly Tree tree;

    public ChopTreeAIPlan(World world, Villager villager, Tree tree) : base(world, villager, false) => this.tree = tree;

    public override PlaceableEntity? FirstTarget => tree;
    public override string Description => $"Chopping {tree.Name} tree.";

    enum State { MoveToTree, Build }
    State state = State.MoveToTree;

    public override void Update(double deltaSec)
    {
        switch (state)
        {
            case State.MoveToTree:
                StepToPlaceable(FirstTarget!, deltaSec, () =>
                {
                    state = State.Build;
                    Villager.WorkActionTime.Reset(Villager.WorkActionsPerSecond);

                    World.StartWork(tree, Villager);
                });
                break;
            case State.Build:
                Villager.WorkActionTime.AdvanceTime(deltaSec);
                if (Villager.WorkActionTime.ConsumeActions() > 0 && --tree.WorkTicks <= 0)
                {
                    Done = true;
                    World.EndWork(tree, Villager);
                    tree.Inventory.Location = tree.Location;
                    World.RemoveEntity(tree);
                    World.PlaceEntity(tree.Inventory);
                }

                break;
        }
    }
}

public class EatAIPlan : AIPlan
{
    public List<ResourceBucket> WorldBuckets { get; } = new();

    public EatAIPlan(World world, Villager villager) : base(world, villager, true) { }

    public override PlaceableEntity? FirstTarget => WorldBuckets.FirstOrDefault();
    public override string Description => $"Eating.";

    public override void Update(double deltaSec)
    {
        Villager.EatActionTime.AdvanceTime(deltaSec);
        if (Villager.EatActionTime.ConsumeActions() > 0)
        {
            Villager.Needs.UpdateWithChanges(new() { Hunger = -Villager.Inventory.AvailableResourceQuantities.Sum(rq => rq.Resource.Nourishment * rq.Quantity) });
            Villager.Inventory.Clear();
            Done = true;
        }
    }
}

class AIManager
{
    private readonly World world;

    public AIManager(World world) => this.world = world;

    bool TryBuildingPlan(Villager villager)
    {
        var availableBuildingSite = world.GetEntities<Building>().Where(b => !b.IsBuilt && b.BuildCost.IsAllEmpty && b.AssignedWorkers[0] is null)
            .OrderBy(b => (b.Center - villager.Center).LengthSquared())
            .FirstOrDefault();
        if (availableBuildingSite == null)
            return false;

        availableBuildingSite.AssignedWorkers[0] = villager;
        availableBuildingSite.AssignedWorkersWorking[0] = false;

        if (villager.AIPlan?.Done == false)
            villager.AIPlan.Cancel();
        villager.AIPlan = new BuildAIPlan(world, villager, availableBuildingSite);
        return true;
    }

    bool TryCuttingTreesPlan(Villager villager)
    {
        var availableTree = world.GetEntities<Tree>().Where(t => t.AssignedVillager is null).OrderBy(t => (t.Center - villager.Center).LengthSquared())
            .FirstOrDefault();
        if (availableTree == null)
            return false;

        availableTree.AssignedVillager = villager;
        availableTree.AssignedVillagerWorking = false;

        if (villager.AIPlan?.Done == false)
            villager.AIPlan.Cancel();
        villager.AIPlan = new ChopTreeAIPlan(world, villager, availableTree);
        return true;
    }

    bool TryBuildingSiteHaulingPlan(Villager villager)
    {
        var pickupPlan = new ResourcePickupAIPlan(world, villager);
        var availableCarryWeight = world.Configuration.Data.BaseCarryWeight - villager.Inventory.AvailableWeight;

        foreach (var buildingSite in world.GetEntities<Building>().Where(b => !b.IsBuilt && !b.BuildCost.IsAvailableEmpty).OrderBy(b => (b.Center - villager.Center).LengthSquared()))
        {
            foreach (var rb in world.GetEntities().Where(e => (e is ResourceBucket rb && !rb.IsAvailableEmpty) || (e is Building { Type: BuildingType.Storage, IsBuilt: true } building && !building.Inventory.IsAvailableEmpty))
                .Select(e => e is ResourceBucket rb ? rb : ((Building)e).Inventory)
                .OrderBy(rb => (rb.Center - villager.Center).LengthSquared()))
            {
                if (rb.PlanResouces(pickupPlan, ref availableCarryWeight, buildingSite.BuildCost))
                    pickupPlan.WorldBuckets.Add(rb);
            }

            if (pickupPlan.WorldBuckets.Any())
            {
                // plan the storage
                var storePlan = new StoreInventoryAIPlan(world, villager, buildingSite);
                pickupPlan.NextPlan = storePlan;
                storePlan.DoneAction = () => buildingSite.BuildCost.RemovePlannedResources(pickupPlan);

                if (villager.AIPlan?.Done == false)
                    villager.AIPlan.Cancel();
                villager.AIPlan = pickupPlan;
                return true;
            }
        }

        return false;
    }

    bool TryHaulingPlan(Villager villager)
    {
        bool anyRB = false, anyStorage = false, ok = false;
        world.GetEntities().ForEach(e => { anyRB = anyRB || e is ResourceBucket; anyStorage = anyStorage || (e is Building building && building.IsBuilt && building.Type == BuildingType.Storage); });
        if (!anyRB || !anyStorage) return false;

        var closestAvailableStorage = world.GetEntities<Building>().Where(b => b.IsBuilt).OrderBy(rb => (rb.Center - villager.Center).LengthSquared()).FirstOrDefault(b => b.Type == BuildingType.Storage);
        if (closestAvailableStorage is not null)
        {
            var plan = new ResourcePickupAIPlan(world, villager);

            // plan the resource acquisition
            var availableCarryWeight = world.Configuration.Data.BaseCarryWeight - villager.Inventory.AvailableWeight;
            foreach (var rb in world.GetEntities<ResourceBucket>().OrderBy(rb => (rb.Center - closestAvailableStorage.Center).LengthSquared()))
                if (availableCarryWeight > 0 && rb.PlanResouces(plan, ref availableCarryWeight))
                    plan.WorldBuckets.Add(rb);

            ok = plan.WorldBuckets.Any();
            if (ok)
            {
                // plan the storage
                var storePlan = new StoreInventoryAIPlan(world, villager, closestAvailableStorage);
                plan.NextPlan = storePlan;

                if (villager.AIPlan?.Done == false)
                    villager.AIPlan.Cancel();
                villager.AIPlan = plan;
            }
        }
        return ok;
    }

    bool TryEatingPlan(Villager villager)
    {
        bool anyRB = false, anyStorage = false;
        world.GetEntities().ForEach(e => { anyRB = anyRB || e is ResourceBucket; anyStorage = anyStorage || (e is Building building && building.IsBuilt && building.Type == BuildingType.Storage); });
        if (!anyRB && !anyStorage) return false;

        var pickupPlan = new ResourcePickupAIPlan(world, villager);
        var availableCarryWeight = world.Configuration.Data.BaseCarryWeight - villager.Inventory.AvailableWeight;
        var neededNourishment = villager.Needs.HungerMax - villager.Needs.Hunger - 0.05 * villager.Needs.HungerMax;

        foreach (var rb in world.GetEntities().Where(e => (e is ResourceBucket rb && !rb.IsAvailableEmpty) || (e is Building { Type: BuildingType.Storage, IsBuilt: true } building && !building.Inventory.IsAvailableEmpty))
            .Select(e => e is ResourceBucket rb ? rb : ((Building)e).Inventory)
            .OrderBy(rb => (rb.Center - villager.Center).LengthSquared()))
        {
            if (rb.PlanResouces(pickupPlan, ref availableCarryWeight, globalFilter: (r, q) => r.Nourishment > 0 ? q : 0,
                incrementalFilter: (r, q) =>
                 {
                     var usefulQuantity = Math.Min(q, Math.Ceiling(neededNourishment / r.Nourishment));
                     neededNourishment -= usefulQuantity * r.Nourishment;
                     return usefulQuantity;
                 }))
            {
                pickupPlan.WorldBuckets.Add(rb);
            }
        }

        if (pickupPlan.WorldBuckets.Any())
        {
            // plan the eating
            var eatPlan = new EatAIPlan(world, villager);
            pickupPlan.NextPlan = eatPlan;
            pickupPlan.DoneAction = () => villager.EatActionTime.Reset(villager.EatActionsPerSecond);

            if (villager.AIPlan?.Done == false)
                villager.AIPlan.Cancel();
            villager.AIPlan = pickupPlan;
            return true;
        }

        return false;
    }

    readonly List<Villager> updateVillagersList = new();
    public void Update(double deltaSec)
    {
        updateVillagersList.Clear();
        updateVillagersList.AddRange(world.GetEntities<Villager>());
        foreach (var villager in updateVillagersList)
        {
            // emergency plans interrupt other plans
            if (villager.AIPlan?.IsEmergency != true)
            {
                // hungry?
                _ = villager.Needs.HungerPercentage > .6 && TryEatingPlan(villager);
            }

            if (villager.AIPlan is null)
                _ = TryBuildingPlan(villager) || TryBuildingSiteHaulingPlan(villager) || TryHaulingPlan(villager) || TryCuttingTreesPlan(villager);

            villager.AIPlan?.Update(deltaSec);
        }

        world.GetEntities<Villager>().Where(v => v.AIPlan?.Done == true).ForEach(v => { v.AIPlan!.DoneAction?.Invoke(); v.AIPlan = v.AIPlan?.NextPlan; });
    }
}

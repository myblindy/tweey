namespace Tweey.WorldData;

public enum AIPlanStepResult { Stay, NextStep, End }
public record AIPlanStep(Func<double, AIPlanStepResult> Update, Action? DoneAction = null);

public abstract class AIPlan
{
    protected Villager Villager { get; }
    protected World World { get; }
    public bool IsEmergency { get; }
    protected AIPlan(World world, Villager villager, bool isEmergency, IEnumerable<PlaceableEntity>? targets = null)
    {
        (Villager, World, IsEmergency) = (villager, world, isEmergency);
        if (targets is not null)
            foreach (var target in targets)
                Targets.Enqueue(target);
    }

    public AIPlan? NextPlan { get; set; }

    public Queue<PlaceableEntity> Targets { get; } = new();
    public virtual PlaceableEntity? FirstTarget => Targets.TryPeek(out var value) ? value : null;

    public bool Done { get; private set; }
    public Action? DoneAction { get; set; }

    protected List<AIPlanStep> Steps { get; } = new();
    protected int CurrentStep { get; set; }

    public void Update(double deltaSec)
    {
        //if (Targets.Count == 0) { Done = true; return; }
        var step = Steps[CurrentStep];

        switch (step.Update(deltaSec))
        {
            case AIPlanStepResult.NextStep: step.DoneAction?.Invoke(); CurrentStep = (CurrentStep + 1) % Steps.Count; break;
            case AIPlanStepResult.End: step.DoneAction?.Invoke(); Done = true; break;
        }

        //if (Targets.Count == 0) Done = true;
    }

    public abstract string Description { get; }

    protected bool StepToPlaceable(PlaceableEntity entity, double deltaSec)
    {
        Villager.MovementActionTime.AdvanceTime(deltaSec);

        for (int movements = Villager.MovementActionTime.ConsumeActions(); movements > 0; --movements)
        {
            // reached the resource?
            if (entity.Box.Intersects(Villager.Box.WithExpand(Vector2.One)))
                return true;

            // if not, move towards it
            Villager.Location += (entity.Location - Villager.Location).Sign();
        }

        return false;
    }

    public virtual void Cancel() { }

    public AIPlanStep CreateMoveToPlaceableAIPlanStep(Action? stepDoneAction) => new(deltaSec =>
        FirstTarget is { } && StepToPlaceable(FirstTarget, deltaSec) ? AIPlanStepResult.NextStep : AIPlanStepResult.Stay, stepDoneAction);
}

public abstract class TypedAIPlan<T> : AIPlan where T : PlaceableEntity
{
    protected TypedAIPlan(World world, Villager villager, bool isEmergency, IEnumerable<T>? targets = null) : base(world, villager, isEmergency, targets)
    {
    }

    protected TypedAIPlan(World world, Villager villager, bool isEmergency, T target) : base(world, villager, isEmergency, Enumerable.Repeat(target, 1))
    {
    }

    public void EnqueueTarget(T target) => Targets.Enqueue(target);
    protected T? PeekTarget() => Targets.TryPeek(out var target) ? (T)target : null;
    protected T DequeueTarget() => (T)Targets.Dequeue();
}

public class ResourcePickupAIPlan : TypedAIPlan<ResourceBucket>
{
    public ResourcePickupAIPlan(World world, Villager villager)
        : base(world, villager, false)
    {
        Steps.Add(CreateMoveToPlaceableAIPlanStep(() => Villager.PickupActionTime.Reset(Villager.PickupActionsPerSecond / PeekTarget()!.GetPlannedResource(this).PickupSpeedMultiplier)));
        Steps.Add(new(deltaSec =>
        {
            if (Villager.PickupActionTime.AdvanceTimeAndConsumeActions(deltaSec) > 0)
            {
                // remove the resource from the world and from our list of targets to hit and place it in our inventory
                var worldRB = DequeueTarget();
                var plannedRB = worldRB.RemovePlannedResources(this);

                if (worldRB.IsAllEmpty)
                    World.RemoveEntity(worldRB);

                Villager.Inventory.AddRange(plannedRB.ResourceQuantities);

                return Targets.Count > 0 ? AIPlanStepResult.NextStep : AIPlanStepResult.End;
            }

            return AIPlanStepResult.Stay;
        }));
    }

    public override PlaceableEntity? FirstTarget => (PlaceableEntity?)PeekTarget()?.Building ?? PeekTarget();
    public override string Description => "Gathering resources to haul.";
}

public class StoreInventoryAIPlan : TypedAIPlan<Building>
{
    public StoreInventoryAIPlan(World world, Villager villager, Building storage) : base(world, villager, false, storage)
    {
        Steps.Add(CreateMoveToPlaceableAIPlanStep(() => Villager.PickupActionTime.Reset(Villager.PickupActionsPerSecond / Villager.Inventory.PickupSpeedMultiplier)));
        Steps.Add(new(deltaSec =>
        {
            if (Villager.PickupActionTime.AdvanceTimeAndConsumeActions(deltaSec) > 0)
            {
                PeekTarget()!.Inventory.Add(Villager.Inventory, true);
                return AIPlanStepResult.End;
            }
            return AIPlanStepResult.Stay;
        }));
    }

    public override string Description => $"Dropping resources off to {PeekTarget()!.Name}.";
}

public class BuildAIPlan : TypedAIPlan<Building>
{
    public BuildAIPlan(World world, Villager villager, Building building) : base(world, villager, false, building)
    {
        Steps.Add(CreateMoveToPlaceableAIPlanStep(() =>
        {
            Villager.WorkActionTime.Reset(Villager.WorkActionsPerSecond);
            World.StartWork(PeekTarget()!, Villager);
        }));
        Steps.Add(new(deltaSec =>
        {
            var building = PeekTarget()!;
            if (Villager.WorkActionTime.AdvanceTimeAndConsumeActions(deltaSec) > 0 && --building.BuildWorkTicks <= 0)
            {
                building.IsBuilt = true;
                World.EndWork(building, Villager);
                building.Inventory.Clear();
                return AIPlanStepResult.End;
            }
            return AIPlanStepResult.Stay;
        }));
    }

    public override string Description => $"Building {PeekTarget()?.Name}.";
}

public class ChopTreeAIPlan : TypedAIPlan<Tree>
{
    public ChopTreeAIPlan(World world, Villager villager, Tree tree) : base(world, villager, false, tree)
    {
        Steps.Add(CreateMoveToPlaceableAIPlanStep(() =>
        {
            Villager.WorkActionTime.Reset(Villager.WorkActionsPerSecond);
            World.StartWork(tree, Villager);
        }));
        Steps.Add(new(deltaSec =>
        {
            if (Villager.WorkActionTime.AdvanceTimeAndConsumeActions(deltaSec) > 0 && --tree.WorkTicks <= 0)
            {
                World.EndWork(tree, Villager);
                tree.Inventory.Location = tree.Location;
                World.RemoveEntity(tree);
                World.PlaceEntity(tree.Inventory);
                return AIPlanStepResult.End;
            }
            return AIPlanStepResult.Stay;
        }));
    }

    public override string Description => $"Chopping {PeekTarget()?.Name} tree.";
}

public class EatAIPlan : AIPlan
{
    public EatAIPlan(World world, Villager villager) : base(world, villager, true) =>
        Steps.Add(new(deltaSec =>
        {
            if (Villager.EatActionTime.AdvanceTimeAndConsumeActions(deltaSec) > 0)
            {
                Villager.Needs.UpdateWithChanges(new() { Hunger = -Villager.Inventory.AvailableResourceQuantities.Sum(rq => rq.Resource.Nourishment * rq.Quantity) });
                Villager.Inventory.Clear();
                return AIPlanStepResult.End;
            }
            return AIPlanStepResult.Stay;
        }));

    public override string Description => $"Eating.";
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
                    pickupPlan.EnqueueTarget(rb);
            }

            if (pickupPlan.Targets.Any())
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
                    plan.EnqueueTarget(rb);

            ok = plan.Targets.Any();
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
        world.GetEntities().ForEach(e => { anyRB = anyRB || e is ResourceBucket; anyStorage = anyStorage || (e is Building { Type: BuildingType.Storage, IsBuilt: true }); });
        if (!anyRB && !anyStorage) return false;

        var pickupPlan = new ResourcePickupAIPlan(world, villager);
        var availableCarryWeight = world.Configuration.Data.BaseCarryWeight - villager.Inventory.AvailableWeight;
        var neededNourishment = villager.Needs.Hunger - 0.05 * villager.Needs.HungerMax;

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
                pickupPlan.EnqueueTarget(rb);

                if (neededNourishment <= 0) break;
            }
        }

        if (pickupPlan.Targets.Any())
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

    static bool IsEmergencyPlan(AIPlan? aIPlan)
    {
        while (aIPlan is not null)
        {
            if (aIPlan.IsEmergency) return true;
            aIPlan = aIPlan.NextPlan;
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
            // priority, but non-emergency plans
            if (villager.AIPlan is null)
            {
                // hungry?
                _ = villager.Needs.HungerPercentage > villager.HungerThreshold && TryEatingPlan(villager);
            }

            // emergency plans interrupt other plans
            if (!IsEmergencyPlan(villager.AIPlan))
            {
                // hungry?
                _ = villager.Needs.HungerPercentage > villager.HungerEmergencyThreshold && TryEatingPlan(villager);
            }

            if (villager.AIPlan is null)
                _ = TryBuildingPlan(villager) || TryBuildingSiteHaulingPlan(villager) || TryHaulingPlan(villager) || TryCuttingTreesPlan(villager);

            villager.AIPlan?.Update(deltaSec);
        }

        world.GetEntities<Villager>().Where(v => v.AIPlan?.Done == true).ForEach(v => { v.AIPlan!.DoneAction?.Invoke(); v.AIPlan = v.AIPlan!.NextPlan; });
    }
}

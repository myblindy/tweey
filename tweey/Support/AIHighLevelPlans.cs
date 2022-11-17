namespace Tweey.Support;
abstract class AIHighLevelPlan
{
    public AIHighLevelPlan(World world, Entity entity)
    {
        World = world;
        MainEntity = entity;
    }

    protected World World { get; }
    protected Entity MainEntity { get; }

    public abstract IEnumerable<AILowLevelPlan> GetLowLevelPlans();
}

class GatherResourcesAIHighLevelPlan : AIHighLevelPlan
{
    private readonly ResourceMarker marker;

    public GatherResourcesAIHighLevelPlan(World world, Entity entity, ResourceMarker marker)
        : base(world, entity)
    {
        this.marker = marker;
    }

    public override IEnumerable<AILowLevelPlan> GetLowLevelPlans()
    {
        var targets = ObjectPool<List<(Entity entity, Vector2 location)>>.Shared.Get();
        targets.Clear();

        try
        {
            EcsCoordinator.IteratePlacedResourceArchetype((in EcsCoordinator.PlacedResourceIterationResult w) =>
            {
                if (w.InventoryComponent.Inventory.HasMarker(marker))
                    targets.Add((w.Entity, w.LocationComponent.Box.Center));
            });

            foreach (var targetEntity in targets.OrderByDistanceFrom(MainEntity.GetLocationComponent().Box.Center))
            {
                yield return new WalkToEntityLowLevelPlan(World, MainEntity, targetEntity);
                yield return new WaitLowLevelPlan(World, MainEntity, World.RawWorldTime + World.GetWorldTimeFromTicks(
                    MainEntity.GetVillagerComponent().PickupSpeedMultiplier * World.Configuration.Data.BasePickupSpeed
                        * targetEntity.GetInventoryComponent().Inventory.GetWeight(marker)));
                yield return new MoveInventoryLowLevelPlan(World, targetEntity, marker, MainEntity, marker);       // from the resource (marked) to the villager (marked)
            }
        }
        finally
        {
            ObjectPool<List<(Entity entity, Vector2 location)>>.Shared.Return(targets);
        }
    }
}

class DropResourcesToInventoryAIHighLevelPlan : AIHighLevelPlan
{
    private readonly Entity targetEntity;
    private readonly ResourceMarker marker;

    public DropResourcesToInventoryAIHighLevelPlan(World world, Entity mainEntity, Entity targetEntity, ResourceMarker marker)
        : base(world, mainEntity)
    {
        this.targetEntity = targetEntity;
        this.marker = marker;
    }

    public override IEnumerable<AILowLevelPlan> GetLowLevelPlans()
    {
        yield return new WalkToEntityLowLevelPlan(World, MainEntity, targetEntity);
        yield return new WaitLowLevelPlan(World, MainEntity, World.RawWorldTime + World.GetWorldTimeFromTicks(
            MainEntity.GetVillagerComponent().PickupSpeedMultiplier * MainEntity.GetInventoryComponent().Inventory.GetWeight(marker)));
        yield return new MoveInventoryLowLevelPlan(World, MainEntity, marker, targetEntity, ResourceMarker.Default, true);    // from the villager (marked) to the building (unmarked)
    }
}

class WorkAIHighLevelPlan : AIHighLevelPlan
{
    private readonly Entity workableEntity;
    private readonly Action<Entity /*worker*/, Entity /*workable*/>? doneAction;

    public WorkAIHighLevelPlan(World world, Entity workerEntity, Entity workableEntity, Action<Entity /*worker*/, Entity /*workable*/>? doneAction = null)
        : base(world, workerEntity)
    {
        this.workableEntity = workableEntity;
        this.doneAction = doneAction;
    }

    public override IEnumerable<AILowLevelPlan> GetLowLevelPlans()
    {
        yield return new WalkToEntityLowLevelPlan(World, MainEntity, workableEntity);

        workableEntity.GetWorkableComponent().GetAssignedWorkerSlot(MainEntity).EntityWorking = true;
        if (workableEntity.HasBuildingComponent())
        {
            while (workableEntity.GetBuildingComponent().BuildWorkTicks-- > 0)
                yield return new WaitLowLevelPlan(World, MainEntity, World.RawWorldTime
                    + World.GetWorldTimeFromTicks(MainEntity.GetVillagerComponent().WorkSpeedMultiplier));
            workableEntity.GetWorkableComponent().GetAssignedWorkerSlot(MainEntity).Clear();
        }
        else if (workableEntity.HasTreeComponent())
        {
            while (workableEntity.GetTreeComponent().WorkTicks-- > 0)
                yield return new WaitLowLevelPlan(World, MainEntity, World.RawWorldTime
                    + World.GetWorldTimeFromTicks(MainEntity.GetVillagerComponent().HarvestSpeedMultiplier));

            World.AddResourceEntity(workableEntity.GetInventoryComponent().Inventory.Clone(),
                workableEntity.GetLocationComponent().Box.Center.Floor());
            World.DeleteEntity(workableEntity);
        }

        doneAction?.Invoke(MainEntity, workableEntity);
    }
}
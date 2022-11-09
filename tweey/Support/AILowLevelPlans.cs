namespace Tweey.Support;

abstract class AILowLevelPlan
{
    protected World World { get; }
    protected Entity MainEntity { get; }

    public AILowLevelPlan(World world, Entity entity)
    {
        World = world;
        MainEntity = entity;
    }

    /// <summary>
    /// Runs the next AI step.
    /// </summary>
    /// <returns>If <see cref="false"/>, end the step.</returns>
    public abstract bool Run();
}

class WalkToEntityLowLevelPlan : AILowLevelPlan
{
    private readonly Entity targetEntity;

    public WalkToEntityLowLevelPlan(World world, Entity entity, Entity target)
        : base(world, entity)
    {
        this.targetEntity = target;
    }

    public override bool Run()
    {

        ref var entityLocationComponent = ref EcsCoordinator.GetLocationComponent(MainEntity);
        ref var targetLocationComponent = ref EcsCoordinator.GetLocationComponent(targetEntity);

        // already next to the resource?
        if (entityLocationComponent.Box.Intersects(targetLocationComponent.Box.WithExpand(Vector2.One)))
            return false;

        entityLocationComponent.Box = entityLocationComponent.Box.WithOffset(
            Vector2.Normalize((targetLocationComponent.Box.Center - entityLocationComponent.Box.Center))
                * (float)(EcsCoordinator.GetVillagerComponent(MainEntity).MovementRateMultiplier * World.DeltaWorldTime.TotalSeconds));
        return true;
    }
}

class WaitLowLevelPlan : AILowLevelPlan
{
    private readonly TimeSpan targetWorldTime;

    public WaitLowLevelPlan(World world, Entity entity, TimeSpan worldTime)
        : base(world, entity)
    {
        this.targetWorldTime = worldTime;
    }

    public override bool Run() => World.WorldTime < targetWorldTime;
}

class MoveInventoryLowLevelPlan : AILowLevelPlan
{
    private readonly Entity targetEntity;
    private readonly ResourceMarker sourceMarker, targetMarker;
    private readonly bool clearDestination;

    public MoveInventoryLowLevelPlan(World world, Entity sourceEntity, ResourceMarker sourceMarker, Entity targetEntity, ResourceMarker targetMarker, bool clearDestination = false)
        : base(world, sourceEntity)
    {
        this.targetEntity = targetEntity;
        this.targetMarker = targetMarker;
        this.clearDestination = clearDestination;
        this.sourceMarker = sourceMarker;
    }

    public override bool Run()
    {
        var targetRB = EcsCoordinator.GetInventoryComponent(targetEntity).Inventory;
        if (clearDestination)
            targetRB.Remove(sourceMarker);
        EcsCoordinator.GetInventoryComponent(MainEntity).Inventory.MoveTo(sourceMarker, targetRB, targetMarker);
        return false;
    }
}

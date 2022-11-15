namespace Tweey.Support;

abstract class AILowLevelPlan
{
    protected World World { get; }
    public Entity MainEntity { get; }

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

abstract class AILowLevelPlanWithTargetEntity : AILowLevelPlan
{
    public Entity TargetEntity { get; }

    public AILowLevelPlanWithTargetEntity(World world, Entity entity, Entity targetEntity)
        : base(world, entity)
    {
        TargetEntity = targetEntity;
    }
}

class WalkToEntityLowLevelPlan : AILowLevelPlanWithTargetEntity
{
    public WalkToEntityLowLevelPlan(World world, Entity entity, Entity target)
        : base(world, entity, target)
    {
    }

    public override bool Run()
    {

        ref var entityLocationComponent = ref MainEntity.GetLocationComponent();
        ref var targetLocationComponent = ref TargetEntity.GetLocationComponent();

        // already next to the resource?
        if (entityLocationComponent.Box.Intersects(targetLocationComponent.Box.WithExpand(Vector2.One)))
            return false;

        entityLocationComponent.Box = entityLocationComponent.Box.WithOffset(
            Vector2.Normalize((targetLocationComponent.Box.Center - entityLocationComponent.Box.Center))
                * (float)(MainEntity.GetVillagerComponent().MovementRateMultiplier * World.DeltaWorldTime.TotalSeconds));
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

    public override bool Run() => World.RawWorldTime < targetWorldTime;
}

class MoveInventoryLowLevelPlan : AILowLevelPlanWithTargetEntity
{
    private readonly ResourceMarker sourceMarker, targetMarker;
    private readonly bool clearDestination;

    public MoveInventoryLowLevelPlan(World world, Entity sourceEntity, ResourceMarker sourceMarker, Entity targetEntity, ResourceMarker targetMarker, bool clearDestination = false)
        : base(world, sourceEntity, targetEntity)
    {
        this.targetMarker = targetMarker;
        this.clearDestination = clearDestination;
        this.sourceMarker = sourceMarker;
    }

    public override bool Run()
    {
        var targetRB = TargetEntity.GetInventoryComponent().Inventory;
        if (clearDestination)
            targetRB.Remove(sourceMarker);
        MainEntity.GetInventoryComponent().Inventory.MoveTo(sourceMarker, targetRB, targetMarker);
        return false;
    }
}

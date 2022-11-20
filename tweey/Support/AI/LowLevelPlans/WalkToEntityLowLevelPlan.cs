namespace Tweey.Support.AI.LowLevelPlans;

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
            Vector2.Normalize(targetLocationComponent.Box.Center - entityLocationComponent.Box.Center)
                * (float)(MainEntity.GetVillagerComponent().MovementRateMultiplier * World.DeltaWorldTime.TotalSeconds));
        return true;
    }
}

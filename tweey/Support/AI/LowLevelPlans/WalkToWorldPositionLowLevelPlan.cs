namespace Tweey.Support.AI.LowLevelPlans;

class WalkToWorldPositionLowLevelPlan : AILowLevelPlan
{
    private readonly Vector2 targetLocation;
    private readonly float speedMultiplier;

    public WalkToWorldPositionLowLevelPlan(World world, Entity entity, Vector2 targetLocation, float speedMultiplier = 1f)
        : base(world, entity)
    {
        this.targetLocation = targetLocation;
        this.speedMultiplier = speedMultiplier;
    }

    public override bool Run()
    {
        ref var entityLocationComponent = ref MainEntity.GetLocationComponent();

        // already next to the resource?
        if (entityLocationComponent.Box.WithExpand(Vector2.One).Contains(targetLocation))
            return false;

        entityLocationComponent.Box = entityLocationComponent.Box.WithOffset(
            Vector2.Normalize(targetLocation - entityLocationComponent.Box.Center)
                * (float)(MainEntity.GetVillagerComponent().MovementRateMultiplier * World.DeltaWorldTime.TotalSeconds * speedMultiplier));
        return true;
    }
}

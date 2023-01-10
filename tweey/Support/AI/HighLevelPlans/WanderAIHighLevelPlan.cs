namespace Tweey.Support.AI.HighLevelPlans;

class WanderAIHighLevelPlan : AIHighLevelPlan
{
    private readonly Vector2 targetWorldPosition;
    private readonly float speedMultiplier;

    public WanderAIHighLevelPlan(World world, Entity mainEntity, Vector2 center, float range, float speedMultiplier)
        : base(world, mainEntity)
    {
        targetWorldPosition = center + new Vector2(Random.Shared.NextSingle() * (range * 2) - range, Random.Shared.NextSingle() * (range * 2) - range);
        this.speedMultiplier = speedMultiplier;
    }

    public override Task RunAsync(IFrameAwaiter frameAwaiter) =>
        new WanderAILowLevelPlan(World, MainEntity, targetWorldPosition, speedMultiplier).RunAsync(frameAwaiter);
}
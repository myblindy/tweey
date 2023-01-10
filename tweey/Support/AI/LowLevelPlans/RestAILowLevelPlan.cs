namespace Tweey.Support.AI.LowLevelPlans;

class RestAILowLevelPlan : AILowLevelPlan
{
    private readonly Entity bedEntity;

    public RestAILowLevelPlan(World world, Entity entity, Entity bedEntity) : base(world, entity, bedEntity)
    {
        this.bedEntity = bedEntity;
    }

    public override async Task RunAsync(IFrameAwaiter frameAwaiter)
    {
        var needs = MainEntity.GetVillagerComponent().Needs;
        var bedMultiplier = bedEntity != Entity.Invalid ? 1 : 0.4;

        while (needs.TiredPercentage < .95)
        {
            needs.Tired = Math.Clamp(needs.Tired + World.DeltaWorldTime.TotalSeconds * 0.005 * bedMultiplier, 0, needs.TiredMax);
            await frameAwaiter;
        }
    }
}

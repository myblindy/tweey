namespace Tweey.Support.AI.LowLevelPlans;

class WaitAILowLevelPlan : AILowLevelPlan
{
    private readonly TimeSpan targetWorldTime;

    public WaitAILowLevelPlan(World world, Entity entity, Entity targetEntity, TimeSpan worldTime)
        : base(world, entity, targetEntity)
    {
        targetWorldTime = worldTime;
    }

    public WaitAILowLevelPlan(World world, Entity entity, Entity targetEntity, CustomDateTime worldTime)
        : base(world, entity, targetEntity)
    {
        targetWorldTime = worldTime.TimeSpan;
    }

    public override async Task RunAsync(IFrameAwaiter frameAwaiter)
    {
        while (World.RawWorldTime > targetWorldTime)
            await frameAwaiter;
    }
}

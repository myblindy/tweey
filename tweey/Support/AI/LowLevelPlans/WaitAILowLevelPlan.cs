namespace Tweey.Support.AI.LowLevelPlans;

class WaitAILowLevelPlan : AILowLevelPlan
{
    private readonly TimeSpan targetWorldTime;

    public WaitAILowLevelPlan(World world, Entity entity, TimeSpan worldTime)
        : base(world, entity)
    {
        targetWorldTime = worldTime;
    }

    public override bool Run() => World.RawWorldTime < targetWorldTime;
}

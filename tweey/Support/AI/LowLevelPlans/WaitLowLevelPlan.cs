namespace Tweey.Support.AI.LowLevelPlans;

class WaitLowLevelPlan : AILowLevelPlan
{
    private readonly TimeSpan targetWorldTime;

    public WaitLowLevelPlan(World world, Entity entity, TimeSpan worldTime)
        : base(world, entity)
    {
        targetWorldTime = worldTime;
    }

    public override bool Run() => World.RawWorldTime < targetWorldTime;
}

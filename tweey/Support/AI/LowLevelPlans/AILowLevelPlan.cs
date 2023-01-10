namespace Tweey.Support.AI.LowLevelPlans;

abstract class AILowLevelPlan
{
    protected World World { get; }
    public Entity MainEntity { get; }
    public Entity TargetEntity { get; }

    public AILowLevelPlan(World world, Entity entity, Entity targetEntity)
    {
        World = world;
        MainEntity = entity;
        TargetEntity = targetEntity;
    }

    public abstract Task RunAsync(IFrameAwaiter frameAwaiter);
}

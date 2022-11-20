namespace Tweey.Support.AI.LowLevelPlans;

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

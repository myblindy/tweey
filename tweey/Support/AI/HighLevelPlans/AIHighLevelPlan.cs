namespace Tweey.Support.AI.HighLevelPlans;

abstract class AIHighLevelPlan
{
    public AIHighLevelPlan(World world, Entity mainEntity)
    {
        World = world;
        MainEntity = mainEntity;
    }

    protected World World { get; }
    protected Entity MainEntity { get; }

    public abstract IEnumerable<AILowLevelPlan> GetLowLevelPlans();
}

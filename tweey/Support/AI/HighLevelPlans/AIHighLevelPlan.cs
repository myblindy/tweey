using Tweey.Support.AI.LowLevelPlans;

namespace Tweey.Support.AI.HighLevelPlans;

abstract class AIHighLevelPlan
{
    public AIHighLevelPlan(World world, Entity entity)
    {
        World = world;
        MainEntity = entity;
    }

    protected World World { get; }
    protected Entity MainEntity { get; }

    public abstract IEnumerable<AILowLevelPlan> GetLowLevelPlans();
}

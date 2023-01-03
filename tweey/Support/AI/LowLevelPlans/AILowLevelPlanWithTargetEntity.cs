namespace Tweey.Support.AI.LowLevelPlans;

abstract class AILowLevelPlanWithTargetEntity : AILowLevelPlan
{
    public Entity? TargetEntity { get; }

    public AILowLevelPlanWithTargetEntity(World world, Entity entity, Entity? targetEntity)
        : base(world, entity)
    {
        TargetEntity = targetEntity;
    }
}

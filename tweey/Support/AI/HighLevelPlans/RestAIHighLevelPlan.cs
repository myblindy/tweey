namespace Tweey.Support.AI.HighLevelPlans;

class RestAIHighLevelPlan : AIHighLevelPlan
{
    private readonly Entity? bedEntity;

    public RestAIHighLevelPlan(World world, Entity mainEntity, Entity? bedEntity) : base(world, mainEntity)
    {
        this.bedEntity = bedEntity;
    }

    public override IEnumerable<AILowLevelPlan> GetLowLevelPlans()
    {
        if (bedEntity.HasValue)
        {
            yield return new WalkAILowLevelPlan(World, MainEntity, bedEntity.Value);
            bedEntity.Value.GetWorkableComponent().EntityWorking = true;
        }

        yield return new RestAILowLevelPlan(World, MainEntity, bedEntity);

        if (bedEntity.HasValue)
        {
            bedEntity.Value.GetWorkableComponent().Entity = Entity.Invalid;
            bedEntity.Value.GetWorkableComponent().EntityWorking = false;
        }
    }
}

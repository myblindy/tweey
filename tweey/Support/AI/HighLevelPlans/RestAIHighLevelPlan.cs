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
            yield return new WalkAILowLevelPlan(World, MainEntity, bedEntity.Value, true);
            bedEntity.Value.GetWorkableComponent().EntityWorking = true;
        }
        else
            MainEntity.GetVillagerComponent().AddThought(World, World.ThoughtTemplates[ThoughtTemplates.SleptOnGround]);

        yield return new RestAILowLevelPlan(World, MainEntity, bedEntity);

        if (bedEntity.HasValue)
        {
            bedEntity.Value.GetWorkableComponent().Entity = Entity.Invalid;
            bedEntity.Value.GetWorkableComponent().EntityWorking = false;
        }
    }
}

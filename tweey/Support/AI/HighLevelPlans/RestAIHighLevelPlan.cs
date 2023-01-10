namespace Tweey.Support.AI.HighLevelPlans;

class RestAIHighLevelPlan : AIHighLevelPlan
{
    private readonly Entity bedEntity;

    public RestAIHighLevelPlan(World world, Entity mainEntity, Entity bedEntity) : base(world, mainEntity)
    {
        this.bedEntity = bedEntity;
    }

    public override async Task RunAsync(IFrameAwaiter frameAwaiter)
    {
        if (bedEntity != Entity.Invalid)
        {
            await new WalkAILowLevelPlan(World, MainEntity, bedEntity, true).RunAsync(frameAwaiter);
            bedEntity.GetWorkableComponent().EntityWorking = true;
        }
        else
            MainEntity.GetVillagerComponent().AddThought(World, World.ThoughtTemplates[ThoughtTemplates.SleptOnGround]);

        await new RestAILowLevelPlan(World, MainEntity, bedEntity).RunAsync(frameAwaiter);

        if (bedEntity != Entity.Invalid)
        {
            bedEntity.GetWorkableComponent().Entity = Entity.Invalid;
            bedEntity.GetWorkableComponent().EntityWorking = false;
        }
    }
}

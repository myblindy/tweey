namespace Tweey.Support.AI.HighLevelPlans;

class PoopAIHighLevelPlan : AIHighLevelPlan
{
    private readonly Entity toiletEntity;

    public PoopAIHighLevelPlan(World world, Entity mainEntity, Entity toiletEntity) : base(world, mainEntity)
    {
        this.toiletEntity = toiletEntity;
    }

    public override async Task RunAsync(IFrameAwaiter frameAwaiter)
    {
        if (toiletEntity != Entity.Invalid)
        {
            await new WalkAILowLevelPlan(World, MainEntity, toiletEntity, true).RunAsync(frameAwaiter);
            toiletEntity.GetWorkableComponent().EntityWorking = true;
        }

        await new WaitAILowLevelPlan(World, MainEntity, toiletEntity, World.WorldTime + TimeSpan.FromSeconds(World.Configuration.Data.BasePoopDurationInWorldSeconds)).RunAsync(frameAwaiter);
        MainEntity.GetVillagerComponent().Needs.Poop = MainEntity.GetVillagerComponent().Needs.PoopMax;

        if (toiletEntity == Entity.Invalid)
        {
            MainEntity.GetVillagerComponent().AddThought(World, World.ThoughtTemplates[ThoughtTemplates.PoopedOnTheGround]);
            World.AddPoopEntity(MainEntity.GetLocationComponent().Box.TopLeft, World.WorldTime + TimeSpan.FromDays(World.Configuration.Data.BasePoopExpiryInWorldDays));
        }
        else
        {
            toiletEntity.GetWorkableComponent().Entity = Entity.Invalid;
            toiletEntity.GetWorkableComponent().EntityWorking = false;
        }
    }
}

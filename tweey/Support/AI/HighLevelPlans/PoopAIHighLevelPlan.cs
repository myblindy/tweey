namespace Tweey.Support.AI.HighLevelPlans;

class PoopAIHighLevelPlan : AIHighLevelPlan
{
    private readonly Entity toiletEntity;

    public PoopAIHighLevelPlan(World world, Entity mainEntity, Entity toiletEntity) : base(world, mainEntity)
    {
        this.toiletEntity = toiletEntity;
    }

    public override IEnumerable<AILowLevelPlan> GetLowLevelPlans()
    {
        if (toiletEntity != Entity.Invalid)
        {
            yield return new WalkAILowLevelPlan(World, MainEntity, toiletEntity, true);
            toiletEntity.GetWorkableComponent().EntityWorking = true;
        }

        yield return new WaitAILowLevelPlan(World, MainEntity, World.WorldTime + TimeSpan.FromSeconds(World.Configuration.Data.BasePoopDurationInWorldSeconds));
        MainEntity.GetVillagerComponent().Needs.Poop = MainEntity.GetVillagerComponent().Needs.PoopMax;

        if (toiletEntity == Entity.Invalid)
        {
            MainEntity.GetVillagerComponent().AddThought(World, World.ThoughtTemplates[ThoughtTemplates.PoopedOnTheGround]);
            World.AddPoopEntity(MainEntity.GetLocationComponent().Box.TopLeft, World.WorldTime + TimeSpan.FromDays(World.Configuration.Data.BasePoopExpiryInWorldDays));
            MainEntity.GetVillagerComponent().IsPooping = false;
        }
        else
        {
            toiletEntity.GetWorkableComponent().Entity = Entity.Invalid;
            toiletEntity.GetWorkableComponent().EntityWorking = false;
        }
    }
}

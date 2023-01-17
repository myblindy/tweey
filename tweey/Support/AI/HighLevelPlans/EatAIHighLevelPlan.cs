namespace Tweey.Support.AI.HighLevelPlans;

class EatAIHighLevelPlan : AIHighLevelPlan
{
    private readonly Entity chairEntity;
    private readonly ResourceMarker marker;

    public EatAIHighLevelPlan(World world, Entity mainEntity, Entity chairEntity, ResourceMarker marker)
        : base(world, mainEntity)
    {
        this.chairEntity = chairEntity;
        this.marker = marker;
    }

    public override async Task RunAsync(IFrameAwaiter frameAwaiter)
    {
        if (chairEntity != Entity.Invalid)
        {
            await new WalkAILowLevelPlan(World, MainEntity, chairEntity, true).RunAsync(frameAwaiter);
            chairEntity.GetWorkableComponent().EntityWorking = true;
        }
        else
            MainEntity.GetVillagerComponent().AddThought(World, World.ThoughtTemplates[ThoughtTemplates.AteOnGround]);

        using var foodResources = MainEntity.GetInventoryComponent().Inventory.GetResourceQuantities(marker).ToPooledCollection();
        await new WaitAILowLevelPlan(World, MainEntity, Entity.Invalid, World.RawWorldTime +
            foodResources.Sum(w => w.Quantity) * TimeSpan.FromSeconds(World.Configuration.Data.BaseEatSpeedPerWorldSeconds)).RunAsync(frameAwaiter);
        MainEntity.GetInventoryComponent().Inventory.Remove(marker);
        MainEntity.GetVillagerComponent().Needs.Hunger += foodResources.Sum(w => w.Resource.Nourishment);

        if (chairEntity != Entity.Invalid)
        {
            chairEntity.GetWorkableComponent().Entity = Entity.Invalid;
            chairEntity.GetWorkableComponent().EntityWorking = false;
        }
    }
}

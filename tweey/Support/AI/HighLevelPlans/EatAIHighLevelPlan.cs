namespace Tweey.Support.AI.HighLevelPlans;

class EatAIHighLevelPlan : AIHighLevelPlan
{
    private readonly ResourceMarker marker;

    public EatAIHighLevelPlan(World world, Entity mainEntity, ResourceMarker marker)
        : base(world, mainEntity)
    {
        this.marker = marker;
    }

    public override async Task RunAsync(IFrameAwaiter frameAwaiter)
    {
        using var foodResources = MainEntity.GetInventoryComponent().Inventory.GetResourceQuantities(marker).ToPooledCollection();
        await new WaitAILowLevelPlan(World, MainEntity, Entity.Invalid, World.RawWorldTime +
            foodResources.Sum(w => w.Quantity) * TimeSpan.FromSeconds(World.Configuration.Data.BaseEatSpeedPerWorldSeconds)).RunAsync(frameAwaiter);
        MainEntity.GetInventoryComponent().Inventory.Remove(marker);
        MainEntity.GetVillagerComponent().Needs.Hunger += foodResources.Sum(w => w.Resource.Nourishment);
    }
}

namespace Tweey.Support.AI.LowLevelPlans;

class RestAILowLevelPlan : AILowLevelPlan
{
    private readonly Entity? bedEntity;

    public RestAILowLevelPlan(World world, Entity entity, Entity? bedEntity) : base(world, entity)
    {
        this.bedEntity = bedEntity;
    }

    public override bool Run()
    {
        ref var villagerComponent = ref MainEntity.GetVillagerComponent();
        var bedMultiplier = bedEntity.HasValue ? 1 : 0.4;
        villagerComponent.Needs.Tired = Math.Clamp(villagerComponent.Needs.Tired + World.DeltaWorldTime.TotalSeconds * 0.005 * bedMultiplier, 0, villagerComponent.Needs.TiredMax);
        return villagerComponent.Needs.Tired != villagerComponent.Needs.TiredMax;
    }
}

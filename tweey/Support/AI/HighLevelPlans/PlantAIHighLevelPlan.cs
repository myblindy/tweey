namespace Tweey.Support.AI.HighLevelPlans;

class PlantAIHighLevelPlan : AIHighLevelPlan
{
    private readonly Entity workableEntity;
    private readonly Vector2i worldPositionOverride;
    private readonly PlantTemplate plantTemplate;

    public PlantAIHighLevelPlan(World world, Entity mainEntity, Entity workableEntity,
        Vector2i worldPositionOverride, PlantTemplate plantTemplate)
        : base(world, mainEntity)
    {
        this.workableEntity = workableEntity;
        this.worldPositionOverride = worldPositionOverride;
        this.plantTemplate = plantTemplate;
    }

    public override IEnumerable<AILowLevelPlan> GetLowLevelPlans()
    {
        yield return new WalkAILowLevelPlan(World, MainEntity, worldPositionOverride.ToNumericsVector2Center());
        yield return new WaitAILowLevelPlan(World, MainEntity, World.RawWorldTime
            + World.GetWorldTimeFromTicks(MainEntity.GetVillagerComponent().PlantSpeed));
        World.AddPlantEntity(plantTemplate, worldPositionOverride.ToNumericsVector2(), false, true);
        workableEntity.GetZoneComponent().WorkedTiles.Remove(worldPositionOverride);
    }
}

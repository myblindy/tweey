using Tweey.Support.AI.LowLevelPlans;

namespace Tweey.Support.AI.HighLevelPlans;

class WorkAIHighLevelPlan : AIHighLevelPlan
{
    private readonly Entity workableEntity;

    public WorkAIHighLevelPlan(World world, Entity workerEntity, Entity workableEntity)
        : base(world, workerEntity)
    {
        this.workableEntity = workableEntity;
    }

    public override IEnumerable<AILowLevelPlan> GetLowLevelPlans()
    {
        yield return new WalkToEntityLowLevelPlan(World, MainEntity, workableEntity);

        workableEntity.GetWorkableComponent().GetAssignedWorkerSlot(MainEntity).EntityWorking = true;
        if (workableEntity.HasBuildingComponent())
        {
            while (workableEntity.GetBuildingComponent().BuildWorkTicks-- > 0)
                yield return new WaitLowLevelPlan(World, MainEntity, World.RawWorldTime
                    + World.GetWorldTimeFromTicks(MainEntity.GetVillagerComponent().WorkSpeedMultiplier));
            workableEntity.GetWorkableComponent().GetAssignedWorkerSlot(MainEntity).Clear();
        }
        else if (workableEntity.HasPlantComponent())
        {
            while (workableEntity.GetPlantComponent().WorkTicks-- > 0)
                yield return new WaitLowLevelPlan(World, MainEntity, World.RawWorldTime
                    + World.GetWorldTimeFromTicks(MainEntity.GetVillagerComponent().HarvestSpeedMultiplier));

            World.AddResourceEntities(ResourceMarker.All, workableEntity.GetInventoryComponent().Inventory.Clone(), ResourceMarker.Default,
                workableEntity.GetLocationComponent().Box.Center.Floor());
            World.DeleteEntity(workableEntity);
        }
    }
}

namespace Tweey.Support.AI.HighLevelPlans;

class WorkAIHighLevelPlan : AIHighLevelPlan
{
    private readonly Entity workableEntity;
    private readonly ResourceMarker? billMarker;

    public WorkAIHighLevelPlan(World world, Entity workerEntity, Entity workableEntity, ResourceMarker? billMarker = null)
        : base(world, workerEntity)
    {
        this.workableEntity = workableEntity;
        this.billMarker = billMarker;
    }

    public override IEnumerable<AILowLevelPlan> GetLowLevelPlans()
    {
        yield return new WalkAILowLevelPlan(World, MainEntity, workableEntity);

        workableEntity.GetWorkableComponent().EntityWorking = true;
        if (workableEntity.HasBuildingComponent())
            if (workableEntity.GetBuildingComponent().IsBuilt)
            {
                while (workableEntity.GetWorkableComponent().ActiveBillTicks-- > 0)
                    yield return new WaitAILowLevelPlan(World, MainEntity, World.RawWorldTime
                        + World.GetWorldTimeFromTicks(MainEntity.GetVillagerComponent().WorkSpeedMultiplier));

                workableEntity.GetWorkableComponent().ClearWorkers();
                MainEntity.GetInventoryComponent().Inventory.Remove(billMarker!.Value);
                World.AddResourceEntities(ResourceMarker.All, workableEntity.GetWorkableComponent().ActiveBill!.ProductionLine.Outputs.Clone(), ResourceMarker.Unmarked,
                    workableEntity.GetLocationComponent().Box.Center.Floor());
                if (workableEntity.GetWorkableComponent().ActiveBill!.AmountType is BillAmountType.FixedValue)
                    --workableEntity.GetWorkableComponent().ActiveBill!.Amount;
            }
            else
            {
                while (workableEntity.GetBuildingComponent().BuildWorkTicks-- > 0)
                    yield return new WaitAILowLevelPlan(World, MainEntity, World.RawWorldTime
                        + World.GetWorldTimeFromTicks(MainEntity.GetVillagerComponent().WorkSpeedMultiplier));
                workableEntity.GetWorkableComponent().ClearWorkers();
            }
        else if (workableEntity.HasPlantComponent())
        {
            while (workableEntity.GetPlantComponent().WorkTicks-- > 0)
                yield return new WaitAILowLevelPlan(World, MainEntity, World.RawWorldTime
                    + World.GetWorldTimeFromTicks(MainEntity.GetVillagerComponent().HarvestSpeedMultiplier));

            World.AddResourceEntities(ResourceMarker.All, workableEntity.GetInventoryComponent().Inventory.Clone(), ResourceMarker.Unmarked,
                workableEntity.GetLocationComponent().Box.Center.Floor());
            World.DeleteEntity(workableEntity);
        }
    }
}

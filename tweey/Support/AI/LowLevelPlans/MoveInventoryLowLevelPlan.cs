namespace Tweey.Support.AI.LowLevelPlans;

class MoveInventoryLowLevelPlan : AILowLevelPlanWithTargetEntity
{
    private readonly ResourceMarker sourceMarker, targetMarker;
    private readonly bool clearDestination;

    public MoveInventoryLowLevelPlan(World world, Entity sourceEntity, ResourceMarker sourceMarker, Entity targetEntity, ResourceMarker targetMarker, bool clearDestination = false)
        : base(world, sourceEntity, targetEntity)
    {
        this.targetMarker = targetMarker;
        this.clearDestination = clearDestination;
        this.sourceMarker = sourceMarker;
    }

    public override bool Run()
    {
        var targetRB = TargetEntity!.Value.GetInventoryComponent().Inventory;
        if (clearDestination)
            targetRB.Remove(sourceMarker);
        MainEntity.GetInventoryComponent().Inventory.MoveTo(sourceMarker, targetRB, targetMarker);
        return false;
    }
}

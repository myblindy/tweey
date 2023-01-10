namespace Tweey.Support.AI.LowLevelPlans;

class MoveInventoryAILowLevelPlan : AILowLevelPlan
{
    private readonly ResourceMarker sourceMarker, targetMarker;
    private readonly bool clearDestination;

    public MoveInventoryAILowLevelPlan(World world, Entity sourceEntity, ResourceMarker sourceMarker, Entity targetEntity, ResourceMarker targetMarker, bool clearDestination = false)
        : base(world, sourceEntity, targetEntity)
    {
        this.targetMarker = targetMarker;
        this.clearDestination = clearDestination;
        this.sourceMarker = sourceMarker;
    }

    public override Task RunAsync(IFrameAwaiter frameAwaiter)
    {
        var targetRB = TargetEntity.GetInventoryComponent().Inventory;
        if (clearDestination)
            targetRB.Remove(sourceMarker);
        MainEntity.GetInventoryComponent().Inventory.MoveTo(sourceMarker, targetRB, targetMarker);

        return Task.CompletedTask;
    }
}

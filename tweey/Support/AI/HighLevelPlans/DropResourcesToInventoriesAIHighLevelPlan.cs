namespace Tweey.Support.AI.HighLevelPlans;

class DropResourcesToInventoriesAIHighLevelPlan : AIHighLevelPlan
{
    private readonly ResourceMarker marker;

    public DropResourcesToInventoriesAIHighLevelPlan(World world, Entity mainEntity, ResourceMarker marker)
        : base(world, mainEntity)
    {
        this.marker = marker;
    }

    public override IEnumerable<AILowLevelPlan> GetLowLevelPlans()
    {
        using var targets = CollectionPool<(Entity entity, Vector2 location)>.Get();

        EcsCoordinator.IteratePlacedResourceArchetype((in EcsCoordinator.PlacedResourceIterationResult w) =>
        {
            if (w.InventoryComponent.Inventory.HasMarker(marker))
                targets.Add((w.Entity, w.LocationComponent.Box.Center));
        });

        EcsCoordinator.IterateBuildingArchetype((in EcsCoordinator.BuildingIterationResult w) =>
        {
            if (w.InventoryComponent.Inventory.HasMarker(marker))
                targets.Add((w.Entity, w.LocationComponent.Box.Center));
        });

        foreach (var targetEntity in targets.OrderByDistanceFrom(MainEntity.GetLocationComponent().Box.Center, w => w.location, w => w.entity))
        {
            yield return new WalkLowLevelPlan(World, MainEntity, targetEntity);
            yield return new WaitLowLevelPlan(World, MainEntity, World.RawWorldTime + World.GetWorldTimeFromTicks(
                MainEntity.GetVillagerComponent().PickupSpeedMultiplier * MainEntity.GetInventoryComponent().Inventory.GetWeight(marker)));
            yield return new MoveInventoryLowLevelPlan(World, MainEntity, marker, targetEntity, ResourceMarker.Default, true);    // from the villager (marked) to the building (unmarked)
        }
    }
}

namespace Tweey.Support.AI.HighLevelPlans;

class GatherResourcesAIHighLevelPlan : AIHighLevelPlan
{
    private readonly ResourceMarker marker;
    private readonly Func<Entity, bool>? resourceEntityTest;

    public GatherResourcesAIHighLevelPlan(World world, Entity entity, ResourceMarker marker, Func<Entity, bool>? resourceEntityTest = null)
        : base(world, entity)
    {
        this.marker = marker;
        this.resourceEntityTest = resourceEntityTest;
    }

    public override async Task RunAsync(IFrameAwaiter frameAwaiter)
    {
        using var targets = CollectionPool<(Entity entity, Vector2 location)>.Get();

        EcsCoordinator.IteratePlacedResourceArchetype((in EcsCoordinator.PlacedResourceIterationResult w) =>
        {
            if (w.InventoryComponent.Inventory.HasMarker(marker) && (resourceEntityTest?.Invoke(w.Entity) ?? true))
                targets.Add((w.Entity, w.LocationComponent.Box.Center));
        });

        foreach (var targetEntity in targets.OrderByDistanceFrom(MainEntity.GetLocationComponent().Box.Center, w => w.location, w => w.entity))
        {
            await new WalkAILowLevelPlan(World, MainEntity, targetEntity).RunAsync(frameAwaiter);
            await new WaitAILowLevelPlan(World, MainEntity, targetEntity, World.RawWorldTime + World.GetWorldTimeFromTicks(
                MainEntity.GetVillagerComponent().PickupSpeedMultiplier * World.Configuration.Data.BasePickupSpeed
                    * targetEntity.GetInventoryComponent().Inventory.GetWeight(marker))).RunAsync(frameAwaiter);
            await new MoveInventoryAILowLevelPlan(World, targetEntity, marker, MainEntity, marker).RunAsync(frameAwaiter);       // from the resource (marked) to the villager (marked)

            if (targetEntity.HasResourceComponent() && targetEntity.GetInventoryComponent().Inventory.IsEmpty(ResourceMarker.All))
                targetEntity.Delete();
        }
    }
}

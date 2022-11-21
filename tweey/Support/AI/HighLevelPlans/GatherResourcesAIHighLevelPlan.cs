using Tweey.Support.AI.LowLevelPlans;

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

    public override IEnumerable<AILowLevelPlan> GetLowLevelPlans()
    {
        var targets = ObjectPool<List<(Entity entity, Vector2 location)>>.Shared.Get();
        targets.Clear();

        try
        {
            EcsCoordinator.IteratePlacedResourceArchetype((in EcsCoordinator.PlacedResourceIterationResult w) =>
            {
                if (w.InventoryComponent.Inventory.HasMarker(marker) && (resourceEntityTest?.Invoke(w.Entity) ?? true))
                    targets.Add((w.Entity, w.LocationComponent.Box.Center));
            });

            foreach (var targetEntity in targets.OrderByDistanceFrom(MainEntity.GetLocationComponent().Box.Center, w => w.location, w => w.entity))
            {
                yield return new WalkToEntityLowLevelPlan(World, MainEntity, targetEntity);
                yield return new WaitLowLevelPlan(World, MainEntity, World.RawWorldTime + World.GetWorldTimeFromTicks(
                    MainEntity.GetVillagerComponent().PickupSpeedMultiplier * World.Configuration.Data.BasePickupSpeed
                        * targetEntity.GetInventoryComponent().Inventory.GetWeight(marker)));
                yield return new MoveInventoryLowLevelPlan(World, targetEntity, marker, MainEntity, marker);       // from the resource (marked) to the villager (marked)

                if (targetEntity.HasResourceComponent() && targetEntity.GetInventoryComponent().Inventory.IsEmpty(ResourceMarker.All))
                    targetEntity.Delete();
            }
        }
        finally
        {
            ObjectPool<List<(Entity entity, Vector2 location)>>.Shared.Return(targets);
        }
    }
}

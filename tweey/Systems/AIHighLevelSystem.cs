using System.Buffers;

namespace Tweey.Systems;

[EcsSystem(Archetypes.Worker)]
partial class AIHighLevelSystem
{
    static bool TryToHaulToBuilingSite(in IterationResult w, out AIHighPlan? plan)
    {
        var entityLocation = w.LocationComponent.Box.Center;
        var villagerMaxWeight = w.VillagerComponent.MaxCarryWeight - w.InventoryComponent.Inventory.GetWeight(ResourceMarker.All);

        EcsCoordinator.IterateBuildingArchetype((in EcsCoordinator.BuildingIterationResult bw) =>
        {
            if (!bw.BuildingComponent.IsBuilt && !bw.InventoryComponent.Inventory.Contains(ResourceMarker.All, bw.BuildingComponent.Cost, ResourceMarker.Default))
            {
                var neededResources = bw.BuildingComponent.Cost.WithRemove(bw.InventoryComponent.Inventory);
                var foundResources = ObjectPool<List<(Entity entity, Vector2 location)>>.Shared.Get();

                EcsCoordinator.IteratePlacedResourceArchetype((in EcsCoordinator.PlacedResourceIterationResult rw) =>
                {
                    if (rw.InventoryComponent.Inventory.Overlaps(neededResources))
                        foundResources.Add((rw.Entity, rw.LocationComponent.Box.Center));
                });

                if (foundResources.Any())
                {
                    // we found some resources, order them by distance and plan them out
                    var planMarker = ResourceMarker.Create();
                    ResourceBucket.MarkResources(planMarker,
                        foundResources.OrderByDistanceFrom(entityLocation).Select(e => EcsCoordinator.GetInventoryComponent(e).Inventory),
                        ResourceMarker.Default, villagerMaxWeight, bw.BuildingComponent.Cost);
                }

                ObjectPool<List<(Entity entity, Vector2 location)>>.Shared.Return(foundResources);
            }
        });

        plan = null;
        return false;
    }

    static bool TryToBuild(in IterationResult w, out AIHighPlan? plan)
    {
        Entity targetEntity = default;
        double distanceToTargetEntity = double.MaxValue;

        EcsCoordinator.IterateBuildingArchetype((in EcsCoordinator.BuildingIterationResult w0) =>
        {
            //if(!w0.BuildingComponent.IsBuilt && w0.BuildingComponent.)
            return true;
        });

        plan = null;
        return false;
    }

    public partial void Run()
    {
        IterateComponents((in IterationResult w) =>
        {
            if (w.WorkerComponent.Plan is null)
            {
                AIHighPlan? plan;
                _ = TryToBuild(w, out plan) || TryToHaulToBuilingSite(w, out plan);
            }
        });
    }
}
namespace Tweey.Support.AI.SystemJobs;

class HaulToBuildingSiteSystemJob : BaseSystemJob
{
    public HaulToBuildingSiteSystemJob(World world) : base(world)
    {
    }

    public override string Name => "Building Materials";

    public override bool TryToRun(Entity workerEntity, [NotNullWhen(true)] out AIHighLevelPlan[]? plans)
    {
        var entityLocation = workerEntity.GetLocationComponent().Box.Center;
        var villagerAvailableWeight = workerEntity.GetVillagerComponent().MaxCarryWeight - workerEntity.GetInventoryComponent().Inventory.GetWeight(ResourceMarker.All);
        AIHighLevelPlan[]? selectedPlans = default;

        EcsCoordinator.IterateBuildingArchetype((in EcsCoordinator.BuildingIterationResult bw) =>
        {
            if (!bw.BuildingComponent.IsBuilt && !bw.InventoryComponent.Inventory.Contains(ResourceMarker.All, bw.BuildingComponent.Template.BuildCost, ResourceMarker.Unmarked))
            {
                var neededResources = bw.BuildingComponent.Template.BuildCost.WithRemove(bw.InventoryComponent.Inventory);
                var buildingInventory = bw.InventoryComponent.Inventory;
                using var foundResources = CollectionPool<(Entity entity, Vector2 location)>.Get();

                EcsCoordinator.IteratePlacedResourceArchetype((in EcsCoordinator.PlacedResourceIterationResult rw) =>
                {
                    if (rw.InventoryComponent.Inventory.Overlaps(neededResources))
                        foundResources.Add((rw.Entity, rw.LocationComponent.Box.Center));
                });

                if (foundResources.Count > 0)
                {
                    // we found some resources, order them by distance and plan them out
                    var planMarker = ResourceMarker.Create();
                    ResourceBucket.MarkResources(planMarker,
                        foundResources.OrderByDistanceFrom(entityLocation, w => w.location, w => w.entity).Select(e => e.GetInventoryComponent().Inventory),
                        ResourceMarker.Unmarked, villagerAvailableWeight, bw.BuildingComponent.Template.BuildCost, rq => buildingInventory.Add(rq, planMarker), out _);

                    selectedPlans = new AIHighLevelPlan[]
                    {
                        new GatherResourcesAIHighLevelPlan(World, workerEntity, planMarker),
                        new DropResourcesToInventoriesAIHighLevelPlan(World, workerEntity, planMarker)
                    };
                    return false;
                }
            }

            return true;
        });

        return (plans = selectedPlans) is not null;
    }
}

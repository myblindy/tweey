using Tweey.WorldData;

namespace Tweey.Support.AI.SystemJobs;

class EatSystemJob : BaseSystemJob
{
    public EatSystemJob(World world) : base(world)
    {
    }

    public override string Name => "Eating";

    public override bool TryToRun(Entity workerEntity, [NotNullWhen(true)] out AIHighLevelPlan[]? plans)
    {
        ref var workerVillagerComponent = ref workerEntity.GetVillagerComponent();
        var workerEntityLocation = workerEntity.GetLocationComponent().Box.Center;
        var workerAvailableWeight = workerEntity.GetVillagerComponent().MaxCarryWeight - workerEntity.GetInventoryComponent().Inventory.GetWeight(ResourceMarker.All);
        AIHighLevelPlan[]? selectedPlans = default;

        using var availableFood = CollectionPool<(Entity entity, Vector2i location)>.Get();
        var availableFoodSearched = false;

        bool SearchForAvailableFood()
        {
            if (availableFoodSearched) return availableFood!.Count > 0;
            EcsCoordinator.IteratePlacedResourceArchetype((in EcsCoordinator.PlacedResourceIterationResult rw) =>
            {
                if (rw.InventoryComponent.Inventory.ContainsGroup(ResourceMarker.Unmarked, "food"))
                    availableFood!.Add((rw.Entity, rw.LocationComponent.Box.Center.ToVector2i()));
            });
            availableFoodSearched = true;
            return availableFood.Count > 0;
        }

        ResourceMarker marker = default;

        if (workerVillagerComponent.Needs.HungerPercentage < 1.0 / 3)
        {
            if (SearchForAvailableFood())
            {
                // plan to use the closest ones
                if (ResourceBucket.TryToMarkResources(() => marker = ResourceMarker.Create(),
                    availableFood!.OrderByDistanceFrom(workerEntityLocation, static w => w.location.ToNumericsVector2Center(), static w => w.entity).Select(e => e.GetInventoryComponent().Inventory),
                    ResourceMarker.Unmarked, workerAvailableWeight, new[] { new BuildingResouceQuantityTemplate { Resource = "food", Quantity = 1 } }, out _))
                {

                    selectedPlans = new AIHighLevelPlan[]
                    {
                        new GatherResourcesAIHighLevelPlan(World, workerEntity, marker),
                        new EatAIHighLevelPlan(World, workerEntity, marker)
                    };
                }
            }
        }

        if (workerVillagerComponent.Needs.HungerPercentage <= 0)
            workerVillagerComponent.AddThought(World, World.ThoughtTemplates[ThoughtTemplates.Starving], false);

        return (plans = selectedPlans) is not null;
    }
}

using Microsoft.Win32.SafeHandles;
using Tweey.WorldData;

namespace Tweey.Support.AI.SystemJobs;

class EatSystemJob : BaseSystemJob
{
    public EatSystemJob(World world) : base(world)
    {
    }

    public override bool IsConfigurable => false;
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
                    // find all tables and chairs
                    using var tables = CollectionPool<(Entity entity, Box2 box)>.Get();
                    using var chairs = CollectionPool<(Entity entity, Box2 box)>.Get();
                    EcsCoordinator.IterateBuildingArchetype((in EcsCoordinator.BuildingIterationResult bw) =>
                    {
                        if (bw.BuildingComponent.IsBuilt)
                            if (bw.BuildingComponent.Template.Type is BuildingType.Table)
                                tables.Add((bw.Entity, bw.LocationComponent.Box));
                            else if (bw.WorkableComponent.Entity == Entity.Invalid && bw.BuildingComponent.Template.Type is BuildingType.Chair)
                                chairs.Add((bw.Entity, bw.LocationComponent.Box));
                    });

                    // find the closest chair with a table next to it
                    var (chair, table) = chairs.OrderByDistanceFrom(workerEntityLocation, w => w.box.Center, w => w.entity)
                        .Select(c => (chair: c, table: tables.FirstOrDefault(t => c.GetLocationComponent().Box.Intersects(t.box.WithExpand(new Vector2(1f))), (Entity.Invalid, default)).entity))
                        .FirstOrDefault(w => w is (var chair, var table) && chair != Entity.Invalid && table != Entity.Invalid, (Entity.Invalid, Entity.Invalid));

                    if (chair != Entity.Invalid && table != Entity.Invalid)
                        chair.GetWorkableComponent().Entity = workerEntity;

                    selectedPlans = new AIHighLevelPlan[]
                    {
                        new GatherResourcesAIHighLevelPlan(World, workerEntity, marker),
                        new EatAIHighLevelPlan(World, workerEntity, chair, table, marker)
                    };
                }
            }
        }

        if (workerVillagerComponent.Needs.HungerPercentage <= 0)
            workerVillagerComponent.AddThought(World, World.ThoughtTemplates[ThoughtTemplates.Starving], false);

        return (plans = selectedPlans) is not null;
    }
}

using Tweey.WorldData;

namespace Tweey.Support.AI.SystemJobs;

class PoopSystemJob : BaseSystemJob
{
    public PoopSystemJob(World world) : base(world)
    {
    }

    public override string Name => "Pooping";
    public override bool IsConfigurable => false;

    public override bool TryToRun(Entity workerEntity, [NotNullWhen(true)] out AIHighLevelPlan[]? plans)
    {
        ref var workerVillagerComponent = ref workerEntity.GetVillagerComponent();
        var workerEntityLocation = workerEntity.GetLocationComponent().Box.Center;
        AIHighLevelPlan[]? selectedPlans = default;

        using var availableToilets = CollectionPool<(Entity entity, Vector2i location)>.Get();
        var availableToiletsSearched = false;

        bool searchForAvailableToilets()
        {
            if (availableToiletsSearched) return availableToilets.Count > 0;
            EcsCoordinator.IterateBuildingArchetype((in EcsCoordinator.BuildingIterationResult bw) =>
            {
                if (bw.BuildingComponent.IsBuilt && bw.BuildingComponent.Template.Type is BuildingType.Toilet && bw.WorkableComponent.Entity == Entity.Invalid)
                    availableToilets!.Add((bw.Entity, bw.LocationComponent.Box.Center.ToVector2i()));
            });
            availableToiletsSearched = true;
            return availableToilets.Count > 0;
        }

        if (workerVillagerComponent.Needs.PoopPercentage < 1.0 / 3)
        {
            if (searchForAvailableToilets())
            {
                // plan to use the closest one
                var selectedToilet = availableToilets.OrderByDistanceFrom(workerEntityLocation, static w => w.location.ToNumericsVector2Center(), static w => w.entity).First();

                selectedToilet.GetWorkableComponent().Entity = workerEntity;
                selectedPlans = new AIHighLevelPlan[]
                {
                    new PoopAIHighLevelPlan(World, workerEntity, selectedToilet)
                };
            }
        }

        if (selectedPlans is null && workerVillagerComponent.Needs.PoopPercentage <= 0)
        {
            // uh oh, poop on the ground
            selectedPlans = new AIHighLevelPlan[]
            {
                new PoopAIHighLevelPlan(World, workerEntity, Entity.Invalid)
            };
        }

        return (plans = selectedPlans) is not null;
    }
}

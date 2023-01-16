namespace Tweey.Support.AI.SystemJobs;

class RestSystemJob : BaseSystemJob
{
    public RestSystemJob(World world) : base(world)
    {
    }

    public override string Name => "Resting";

    public override bool TryToRun(Entity workerEntity, [NotNullWhen(true)] out AIHighLevelPlan[]? plans)
    {
        AIHighLevelPlan[]? selectedPlans = default;
        var tiredRatio = workerEntity.GetVillagerComponent().Needs.TiredPercentage;

        if (tiredRatio < 1 / 3f)
        {
            // try to find available beds
            using var availableBeds = CollectionPool<(Entity entity, Vector2i location)>.Get();
            EcsCoordinator.IterateBuildingArchetype((in EcsCoordinator.BuildingIterationResult bw) =>
            {
                if (bw.BuildingComponent.IsBuilt && bw.BuildingComponent.Template.Type is BuildingType.Rest && bw.WorkableComponent.Entity == Entity.Invalid)
                    availableBeds.Add((bw.Entity, bw.LocationComponent.Box.Center.ToVector2i()));
            });

            // pick the closest bed and rest
            if (availableBeds.Count > 0)
            {
                var bed = availableBeds.OrderByDistanceFrom(workerEntity.GetLocationComponent().Box.Center, w => w.location.ToNumericsVector2Center(), w => w.entity).First();
                bed.GetWorkableComponent().Entity = workerEntity;
                selectedPlans = new AIHighLevelPlan[]
                {
                    new RestAIHighLevelPlan(World, workerEntity, bed)
                };
            }
        }

        // if no beds and it's an emergency, sleep on the floor
        if (tiredRatio < .1f && selectedPlans is null)
            selectedPlans = new AIHighLevelPlan[]
            {
                new RestAIHighLevelPlan(World, workerEntity, Entity.Invalid)
            };

        return (plans = selectedPlans) is not null;
    }
}

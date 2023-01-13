namespace Tweey.Support.AI.SystemJobs;

class HarvestSystemJob : BaseSystemJob
{
    public HarvestSystemJob(World world) : base(world)
    {
    }

    public override string Name => "Harvesting";

    public override bool TryToRun(Entity workerEntity, [NotNullWhen(true)] out AIHighLevelPlan[]? plans)
    {
        AIHighLevelPlan[]? selectedPlans = default;

        EcsCoordinator.IteratePlantArchetype((in EcsCoordinator.PlantIterationResult pw) =>
        {
            if (pw.Entity.HasMarkForHarvestComponent() && pw.WorkableComponent.Entity == Entity.Invalid)
            {
                pw.WorkableComponent.Entity = workerEntity;
                selectedPlans = new AIHighLevelPlan[]
                {
                    new WorkAIHighLevelPlan(World, workerEntity, pw.Entity)
                };
                return false;
            }

            return true;
        });

        return (plans = selectedPlans) is not null;
    }
}

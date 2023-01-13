namespace Tweey.Support.AI.SystemJobs;

class BuildSystemJob : BaseSystemJob
{
    public BuildSystemJob(World world) : base(world)
    {
    }

    public override string Name => "Building";

    public override bool TryToRun(Entity workerEntity, [NotNullWhen(true)] out AIHighLevelPlan[]? plans)
    {
        AIHighLevelPlan[]? selectedPlans = default;

        EcsCoordinator.IterateBuildingArchetype((in EcsCoordinator.BuildingIterationResult bw) =>
        {
            if (!bw.BuildingComponent.IsBuilt && bw.WorkableComponent.Entity == Entity.Invalid
                && bw.InventoryComponent.Inventory.Contains(ResourceMarker.Unmarked, bw.BuildingComponent.Template.BuildCost, ResourceMarker.All)
                && World.IsBoxFreeOfPlants(bw.LocationComponent.Box))
            {
                bw.WorkableComponent.Entity = workerEntity;
                selectedPlans = new AIHighLevelPlan[]
                {
                    new WorkAIHighLevelPlan(World, workerEntity, bw.Entity)
                };
                return false;
            }

            return true;
        });

        return (plans = selectedPlans) is not null;
    }
}

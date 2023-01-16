namespace Tweey.Support.AI.SystemJobs;

class PlantSystemJob : BaseSystemJob
{
    public PlantSystemJob(World world) : base(world)
    {
    }

    public override string Name => "Planting";

    public override bool TryToRun(Entity workerEntity, [NotNullWhen(true)] out AIHighLevelPlan[]? plans)
    {
        AIHighLevelPlan[]? selectedPlans = default;

        EcsCoordinator.IterateZoneArchetype((in EcsCoordinator.ZoneIterationResult zw) =>
        {
            // find any empty plant slots in the zone's box
            if (zw.ZoneComponent.PlantTemplate is not null)
                foreach (var position in zw.LocationComponent.Box)
                {
                    var tileBox = Box2.FromCornerSize(position, new(1));
                    if (!zw.ZoneComponent.WorkedTiles.Contains(position)
                        && !EcsCoordinator.PlantPartitionByLocationPartition!.GetEntities(tileBox).Any(pe => pe.GetLocationComponent().Box.Intersects(tileBox)))
                    {
                        zw.ZoneComponent.WorkedTiles.Add(position);
                        selectedPlans = new AIHighLevelPlan[]
                        {
                            new PlantAIHighLevelPlan(World, workerEntity, zw.Entity, position, zw.ZoneComponent.PlantTemplate)
                        };
                        return false;
                    }
                }
            return true;
        });

        return (plans = selectedPlans) is not null;
    }
}

using SuperLinq;
using Tweey.Support.AI.SystemJobs;

namespace Tweey.Systems;

[EcsSystem(Archetypes.Worker)]
partial class AISystem
{
    readonly World world;

    readonly FrameAwaiter frameAwaiter = new();
    readonly EntityDictionary<Task?> planRunners = new();
    readonly Dictionary<Entity, Vector2> wanderCenterLocations = new();

    public List<BaseSystemJob> SystemJobs { get; } = new();

    public AISystem(World world)
    {
        this.world = world;

        SystemJobs.Add(new EatSystemJob(world));
        SystemJobs.Add(new RestSystemJob(world));
        SystemJobs.Add(new PoopSystemJob(world));
        SystemJobs.Add(new PlantSystemJob(world));
        SystemJobs.Add(new BuildSystemJob(world));
        SystemJobs.Add(new HaulToBuildingSiteSystemJob(world));
        SystemJobs.Add(new HarvestSystemJob(world));
        SystemJobs.Add(new BillSystemJob(world));
        SystemJobs.Add(new HaulToStorageSystemJob(world));
    }

    public partial void Run()
    {
        IterateComponents((in IterationResult w) =>
        {
            if (w.WorkerComponent.Plans is null)
            {
                AIHighLevelPlan[]? plans = null;
                var priorities = w.WorkerComponent.SystemJobPriorities;
                foreach (var systemJob in SystemJobs.Index().OrderBy(jw => priorities[jw.index]).Select(jw => jw.item))
                    if (systemJob.TryToRun(w.Entity, out plans))
                        break;

                w.WorkerComponent.Plans = plans;

                if (w.WorkerComponent.Plans is not null && w.WorkerComponent.Plans is not [WanderAIHighLevelPlan])
                    wanderCenterLocations.Remove(w.Entity);
            }

            if (w.WorkerComponent.Plans is { } workerPlans)
            {
                if (planRunners[w.Entity] is null)
                {
                    static async Task runPlansAsync(IEnumerable<AIHighLevelPlan> workerPlans, IFrameAwaiter frameAwaiter)
                    {
                        foreach (var plan in workerPlans)
                            await plan.RunAsync(frameAwaiter);
                    }

                    planRunners[w.Entity] = runPlansAsync(workerPlans, frameAwaiter);
                }

                if (planRunners[w.Entity]?.IsCompleted == true)
                {
                    planRunners[w.Entity]!.Dispose();
                    planRunners[w.Entity] = null;
                    w.WorkerComponent.Plans = null;
                }
            }
            else
            {
                if (!wanderCenterLocations.TryGetValue(w.Entity, out var location))
                    wanderCenterLocations[w.Entity] = location = w.LocationComponent.Box.Center;

                // if idle, wander around
                w.WorkerComponent.Plans = new AIHighLevelPlan[]
                {
                    new WanderAIHighLevelPlan(world, w.Entity, location, 5f, .3f),
                };
            }
        });

        frameAwaiter.Run();

        IterateComponents((in IterationResult w) => w.Entity.UpdateRenderPartitions());
    }
}
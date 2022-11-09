using System.Buffers;

namespace Tweey.Systems;

[EcsSystem(Archetypes.Worker)]
partial class AIHighLevelSystem
{
    private readonly World world;

    public AIHighLevelSystem(World world)
    {
        this.world = world;
    }

    bool TryToHaulToBuilingSite(in IterationResult w, out AIHighLevelPlan[]? plans)
    {
        var entity = w.Entity;
        var entityLocation = w.LocationComponent.Box.Center;
        var villagerAvailableWeight = w.VillagerComponent.MaxCarryWeight - w.InventoryComponent.Inventory.GetWeight(ResourceMarker.All);
        AIHighLevelPlan[]? selectedPlans = default;

        EcsCoordinator.IterateBuildingArchetype((in EcsCoordinator.BuildingIterationResult bw) =>
        {
            if (!bw.BuildingComponent.IsBuilt && !bw.InventoryComponent.Inventory.Contains(ResourceMarker.All, bw.BuildingComponent.BuildCost, ResourceMarker.Default))
            {
                var neededResources = bw.BuildingComponent.BuildCost.WithRemove(bw.InventoryComponent.Inventory);
                var buildingInventory = bw.InventoryComponent.Inventory;
                var foundResources = ObjectPool<List<(Entity entity, Vector2 location)>>.Shared.Get();

                try
                {
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
                            ResourceMarker.Default, villagerAvailableWeight, bw.BuildingComponent.BuildCost, rq => buildingInventory.Add(rq, planMarker), out _);

                        selectedPlans = new AIHighLevelPlan[]
                        {
                            new GatherResourcesAIHighLevelPlan(world, entity, planMarker),
                            new DropResourcesToInventoryAIHighLevelPlan(world, entity, bw.Entity, planMarker)
                        };
                        return false;
                    }
                }
                finally
                {
                    ObjectPool<List<(Entity entity, Vector2 location)>>.Shared.Return(foundResources);
                }
            }

            return true;
        });

        return (plans = selectedPlans) is not null;
    }

    bool TryToBuild(in IterationResult w, out AIHighLevelPlan[]? plans)
    {
        var workerEntity = w.Entity;
        AIHighLevelPlan[]? selectedPlans = default;

        EcsCoordinator.IterateBuildingArchetype((in EcsCoordinator.BuildingIterationResult bw) =>
        {
            if (!bw.BuildingComponent.IsBuilt && bw.WorkableComponent.WorkerSlots.Any(s => s.Entity == Entity.Invalid)
                && bw.InventoryComponent.Inventory.Contains(ResourceMarker.Default, bw.BuildingComponent.BuildCost, ResourceMarker.All))
            {
                bw.WorkableComponent.GetEmptyWorkerSlot().Entity = workerEntity;
                selectedPlans = new AIHighLevelPlan[]
                {
                    new WorkAIHighLevelPlan(world, workerEntity, bw.Entity,
                        static (worker, workable) => EcsCoordinator.GetBuildingComponent(workable).IsBuilt = true)
                };
                return false;
            }

            return true;
        });

        return (plans = selectedPlans) is not null;
    }

    record PlanRunner(World World, Entity Entity) : IDisposable
    {
        private bool disposedValue;
        IEnumerator<AIHighLevelPlan>? highLevelEnumerator;
        IEnumerator<AILowLevelPlan>? lowLevelEnumerator;

        /// <summary>
        /// Runs the next step for the configured AI steps.
        /// </summary>
        /// <returns><see cref="false"/> if done, otherwise <see cref="true"/>.</returns>
        public bool Run()
        {
            if (highLevelEnumerator is null)
            {
                highLevelEnumerator = EcsCoordinator.GetWorkerComponent(Entity).Plans!.Select(w => w).GetEnumerator();
                if (!highLevelEnumerator.MoveNext())
                {
                    highLevelEnumerator.Dispose();
                    return false;
                }
            }

            if (lowLevelEnumerator is null)
            {
                lowLevelEnumerator = highLevelEnumerator.Current.GetLowLevelPlans().GetEnumerator();

                retry0:
                if (!lowLevelEnumerator.MoveNext())
                {
                    lowLevelEnumerator.Dispose();
                    if (!highLevelEnumerator.MoveNext())
                    {
                        highLevelEnumerator.Dispose();
                        return false;
                    }
                    lowLevelEnumerator = highLevelEnumerator.Current.GetLowLevelPlans().GetEnumerator();
                    goto retry0;
                }
            }

            if (!lowLevelEnumerator.Current.Run())
            {
                retry1:
                if (!lowLevelEnumerator.MoveNext())
                {
                    lowLevelEnumerator.Dispose();
                    if (!highLevelEnumerator.MoveNext())
                    {
                        highLevelEnumerator.Dispose();
                        return false;
                    }
                    lowLevelEnumerator = highLevelEnumerator.Current.GetLowLevelPlans().GetEnumerator();
                    goto retry1;
                }
            }

            return true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // managed
                    lowLevelEnumerator?.Dispose();
                    highLevelEnumerator?.Dispose();
                }

                // TODO: unmanaged
                disposedValue = true;
            }
        }

        ~PlanRunner()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
    readonly List<PlanRunner?> planRunners = new();

    public partial void Run()
    {
        IterateComponents((in IterationResult w) =>
        {
            if (w.WorkerComponent.Plans is null)
            {
                _ = TryToBuild(w, out var plans) || TryToHaulToBuilingSite(w, out plans);
                w.WorkerComponent.Plans = plans;
            }

            if (w.WorkerComponent.Plans is not null)
            {
                while (planRunners.Count <= w.Entity)
                    planRunners.Add(null);

                if (planRunners[w.Entity] is null)
                    planRunners[w.Entity] = new(world, w.Entity);

                if (!planRunners[w.Entity]!.Run())
                {
                    planRunners[w.Entity]!.Dispose();
                    planRunners[w.Entity] = null;
                    w.WorkerComponent.Plans = null;
                }
            }
        });
    }
}
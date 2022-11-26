using Twee.Core.Ecs;
using Tweey.Support.AI.HighLevelPlans;
using Tweey.Support.AI.LowLevelPlans;

namespace Tweey.Systems;

[EcsSystem(Archetypes.Worker)]
partial class AISystem
{
    private readonly World world;

    public AISystem(World world)
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
            if (!bw.BuildingComponent.IsBuilt && !bw.InventoryComponent.Inventory.Contains(ResourceMarker.All, bw.BuildingComponent.Template.BuildCost, ResourceMarker.Default))
            {
                var neededResources = bw.BuildingComponent.Template.BuildCost.WithRemove(bw.InventoryComponent.Inventory);
                var buildingInventory = bw.InventoryComponent.Inventory;
                var foundResources = ObjectPool<List<(Entity entity, Vector2 location)>>.Shared.Get();
                foundResources.Clear();

                try
                {
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
                            ResourceMarker.Default, villagerAvailableWeight, bw.BuildingComponent.Template.BuildCost, rq => buildingInventory.Add(rq, planMarker), out _);

                        selectedPlans = new AIHighLevelPlan[]
                        {
                            new GatherResourcesAIHighLevelPlan(world, entity, planMarker),
                            new DropResourcesToInventoriesAIHighLevelPlan(world, entity, planMarker)
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
            if (!bw.BuildingComponent.IsBuilt && bw.WorkableComponent.Entity == Entity.Invalid
                && bw.InventoryComponent.Inventory.Contains(ResourceMarker.Default, bw.BuildingComponent.Template.BuildCost, ResourceMarker.All)
                && World.IsBoxFreeOfPlants(bw.LocationComponent.Box))
            {
                bw.WorkableComponent.Entity = workerEntity;
                selectedPlans = new AIHighLevelPlan[]
                {
                    new WorkAIHighLevelPlan(world, workerEntity, bw.Entity)
                };
                return false;
            }

            return true;
        });

        return (plans = selectedPlans) is not null;
    }

    bool TryToPlant(in IterationResult w, out AIHighLevelPlan[]? plans)
    {
        var workerEntity = w.Entity;
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
                            new PlantAIHighLevelPlan(world, workerEntity, zw.Entity, position, zw.ZoneComponent.PlantTemplate)
                        };
                        return false;
                    }
                }
            return true;
        });

        return (plans = selectedPlans) is not null;
    }

    bool TryToHarvest(in IterationResult w, out AIHighLevelPlan[]? plans)
    {
        var workerEntity = w.Entity;
        AIHighLevelPlan[]? selectedPlans = default;

        EcsCoordinator.IteratePlantArchetype((in EcsCoordinator.PlantIterationResult pw) =>
        {
            if (pw.Entity.HasMarkForHarvestComponent())
            {
                if (pw.WorkableComponent.Entity == Entity.Invalid)
                {
                    pw.WorkableComponent.Entity = workerEntity;
                    selectedPlans = new AIHighLevelPlan[]
                    {
                        new WorkAIHighLevelPlan(world, workerEntity, pw.Entity)
                    };
                    return false;
                }
            }

            return true;
        });

        return (plans = selectedPlans) is not null;
    }

    bool TryToHaulToStorage(in IterationResult w, out AIHighLevelPlan[]? plans)
    {
        var workerEntity = w.Entity;
        var workerEntityLocation = w.LocationComponent.Box.Center;
        var workerAvailableWeight = w.VillagerComponent.MaxCarryWeight - w.InventoryComponent.Inventory.GetWeight(ResourceMarker.All);
        AIHighLevelPlan[]? selectedPlans = default;

        var foundFreeResources = ObjectPool<List<(Entity entity, ResourceBucket inventory, Vector2i location)>>.Shared.Get();
        foundFreeResources.Clear();
        var foundStoredResources = ObjectPool<List<(Entity entity, ResourceBucket inventory, Vector2i location)>>.Shared.Get();
        foundStoredResources.Clear();

        try
        {
            EcsCoordinator.IteratePlacedResourceArchetype((in EcsCoordinator.PlacedResourceIterationResult rw) =>
            {
                if (!rw.InventoryComponent.Inventory.IsEmpty(ResourceMarker.Default))
                    if (!rw.Entity.HasPlacedResourceIsStoredComponent())
                        foundFreeResources.Add((rw.Entity, rw.InventoryComponent.Inventory, rw.LocationComponent.Box.TopLeft.ToVector2i()));
                    else
                        foundStoredResources.Add((rw.Entity, rw.InventoryComponent.Inventory, rw.LocationComponent.Box.TopLeft.ToVector2i()));
            });

            if (foundFreeResources.Count > 0)
            {
                ResourceMarker? planMarker = default;
                var foundZones = ObjectPool<List<(Entity entity, Box2 box)>>.Shared.Get();
                foundZones.Clear();

                try
                {
                    EcsCoordinator.IterateZoneArchetype((in EcsCoordinator.ZoneIterationResult zw) =>
                    {
                        if (zw.ZoneComponent.Type is ZoneType.Storage)
                            foundZones.Add((zw.Entity, zw.LocationComponent.Box));
                    });

                    if (foundZones.Count > 0)
                    {
                        // we found some resources and zones, order them by distance and plan them out
                        planMarker ??= ResourceMarker.Create();
                        ResourceBucket.MarkResources(world, planMarker.Value,
                            foundFreeResources.OrderByDistanceFrom(workerEntityLocation, w => w.location.ToNumericsVector2Center(), w => w.entity).Select(e => e.GetInventoryComponent().Inventory),
                            ResourceMarker.Default, workerAvailableWeight, foundZones.Select(w => w.box), foundStoredResources,
                            (srcRQ, dstEntity, dstRB, dstLoc) =>
                            {
                                if (dstEntity is not null && dstRB is not null)
                                {
                                    dstRB.Add(srcRQ, planMarker.Value);
                                    return (dstRB, dstEntity.Value);
                                }
                                else if (dstLoc is not null)
                                {
                                    var newEntity = world.AddResourceEntities(ResourceMarker.All, new(srcRQ), planMarker.Value, dstLoc.Value.ToNumericsVector2()).Single();
                                    newEntity.AddPlacedResourceIsStoredComponent();

                                    return (newEntity.GetInventoryComponent().Inventory, newEntity);
                                }
                                else
                                    throw new NotImplementedException();
                            }, out _);

                        selectedPlans = new AIHighLevelPlan[]
                        {
                            new GatherResourcesAIHighLevelPlan(world, workerEntity, planMarker.Value, r => !r.HasPlacedResourceIsStoredComponent()),
                            new DropResourcesToInventoriesAIHighLevelPlan(world, workerEntity, planMarker.Value)
                        };
                    }
                }
                finally
                {
                    ObjectPool<List<(Entity entity, Box2 box)>>.Shared.Return(foundZones);
                }
            }

            return (plans = selectedPlans) is not null;
        }
        finally
        {
            ObjectPool<List<(Entity entity, ResourceBucket inventory, Vector2i location)>>.Shared.Return(foundFreeResources);
        }
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
                highLevelEnumerator = Entity.GetWorkerComponent().Plans!.Select(w => w).GetEnumerator();
                if (!highLevelEnumerator.MoveNext())
                {
                    Entity.GetWorkerComponent().CurrentHighLevelPlan = null;
                    Entity.GetWorkerComponent().CurrentLowLevelPlan = null;
                    highLevelEnumerator.Dispose();
                    return false;
                }
                Entity.GetWorkerComponent().CurrentHighLevelPlan = highLevelEnumerator.Current;
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
                    Entity.GetWorkerComponent().CurrentHighLevelPlan = highLevelEnumerator.Current;
                    lowLevelEnumerator = highLevelEnumerator.Current.GetLowLevelPlans().GetEnumerator();
                    goto retry0;
                }
                Entity.GetWorkerComponent().CurrentLowLevelPlan = lowLevelEnumerator.Current;
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
                    Entity.GetWorkerComponent().CurrentHighLevelPlan = highLevelEnumerator.Current;
                    lowLevelEnumerator = highLevelEnumerator.Current.GetLowLevelPlans().GetEnumerator();
                    goto retry1;
                }
                Entity.GetWorkerComponent().CurrentLowLevelPlan = lowLevelEnumerator.Current;
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
    readonly Dictionary<Entity, Vector2> wanderCenterLocations = new();

    public partial void Run()
    {
        IterateComponents((in IterationResult w) =>
        {
            if (w.WorkerComponent.Plans is null)
            {
                _ = TryToPlant(w, out var plans) || TryToBuild(w, out plans) || TryToHaulToBuilingSite(w, out plans) || TryToHarvest(w, out plans)
                    || TryToHaulToStorage(w, out plans);
                w.WorkerComponent.Plans = plans;

                if (w.WorkerComponent.Plans is not null && w.WorkerComponent.Plans is not [WanderAIHighLevelPlan])
                    wanderCenterLocations.Remove(w.Entity);
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

                w.Entity.UpdateRenderPartitions();
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
    }
}
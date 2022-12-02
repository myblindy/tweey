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
                using var foundResources = CollectionPool<(Entity entity, Vector2 location)>.Get();

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

        using var foundFreeResources = CollectionPool<(Entity entity, ResourceBucket inventory, Vector2i location)>.Get();
        using var foundStoredResources = CollectionPool<(Entity entity, ResourceBucket inventory, Vector2i location)>.Get();

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
            using var foundZones = CollectionPool<(Entity entity, Box2 box)>.Get();

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

        return (plans = selectedPlans) is not null;
    }

    bool TryToWorkBills(in IterationResult w, out AIHighLevelPlan[]? plans)
    {
        var workerEntity = w.Entity;
        var workerEntityLocation = w.LocationComponent.Box.Center;
        var workerAvailableWeight = w.VillagerComponent.MaxCarryWeight - w.InventoryComponent.Inventory.GetWeight(ResourceMarker.All);
        AIHighLevelPlan[]? selectedPlans = default;

        using var foundWorkables = CollectionPool<(Entity entity, Vector2i location)>.Get();
        EcsCoordinator.IterateWorkableArchetype((in EcsCoordinator.WorkableIterationResult ww) =>
        {
            if (ww.WorkableComponent.Entity == Entity.Invalid && ww.WorkableComponent.Bills.Count > 0)
                foundWorkables.Add((ww.Entity, ww.LocationComponent.Box.Center.ToVector2i()));
        });

        using var foundPlacedResources = CollectionPool<(Entity entity, Vector2i location)>.Get();
        EcsCoordinator.IteratePlacedResourceArchetype((in EcsCoordinator.PlacedResourceIterationResult rw) =>
            foundPlacedResources.Add((rw.Entity, rw.LocationComponent.Box.Center.ToVector2i())));
        foundPlacedResources.Sort((a, b) => (int)((a.location.ToNumericsVector2Center() - workerEntityLocation).LengthSquared()
            - (b.location.ToNumericsVector2Center() - workerEntityLocation).LengthSquared()));

        var storedResources = world.GetStoredResources(ResourceMarker.Default);
        int getStoredResourceAmount(Resource res) =>
            storedResources!.TryGetValue(res, out var val) ? val : 0;

        if (foundWorkables.Count > 0)
        {
            ResourceMarker marker = default;
            foreach (var workable in foundWorkables.OrderByDistanceFrom(w.LocationComponent.Box.Center, w => w.location.ToNumericsVector2(), w => w.entity))
            {
                ref var workableComponent = ref workable.GetWorkableComponent();
                foreach (var bill in workableComponent.Bills)
                    if (((bill.AmountType is BillAmountType.FixedValue && bill.Amount > 0)
                            || (bill.AmountType is BillAmountType.UntilInStock
                                && bill.ProductionLine.Outputs.GetResourceQuantities(ResourceMarker.All).First() is { } reqRq
                                && getStoredResourceAmount(reqRq.Resource) < bill.Amount))
                        && ResourceBucket.TryToMarkResources(() => marker = ResourceMarker.Create(), foundPlacedResources.Select(r => r.entity.GetInventoryComponent().Inventory),
                            ResourceMarker.Default, workerAvailableWeight, bill.ProductionLine.PossibleInputs, out _))
                    {
                        workableComponent.Entity = workerEntity;
                        workableComponent.ActiveBill = bill;
                        workableComponent.ActiveBillTicks = bill.ProductionLine.WorkTicks;

                        selectedPlans = new AIHighLevelPlan[]
                        {
                            new GatherResourcesAIHighLevelPlan(world, workerEntity, marker),
                            new WorkAIHighLevelPlan(world, workerEntity, workable, marker)
                        };
                        goto done;
                    }
            }
        }

        done:
        return (plans = selectedPlans) is not null;
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
                    || TryToWorkBills(w, out plans) || TryToHaulToStorage(w, out plans);
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
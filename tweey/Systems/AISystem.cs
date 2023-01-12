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
            if (!bw.BuildingComponent.IsBuilt && !bw.InventoryComponent.Inventory.Contains(ResourceMarker.All, bw.BuildingComponent.Template.BuildCost, ResourceMarker.Unmarked))
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
                        ResourceMarker.Unmarked, villagerAvailableWeight, bw.BuildingComponent.Template.BuildCost, rq => buildingInventory.Add(rq, planMarker), out _);

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
                && bw.InventoryComponent.Inventory.Contains(ResourceMarker.Unmarked, bw.BuildingComponent.Template.BuildCost, ResourceMarker.All)
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
            if (pw.Entity.HasMarkForHarvestComponent() && pw.WorkableComponent.Entity == Entity.Invalid)
            {
                pw.WorkableComponent.Entity = workerEntity;
                selectedPlans = new AIHighLevelPlan[]
                {
                    new WorkAIHighLevelPlan(world, workerEntity, pw.Entity)
                };
                return false;
            }

            return true;
        });

        return (plans = selectedPlans) is not null;
    }

    bool TryToRest(in IterationResult w, out AIHighLevelPlan[]? plans)
    {
        AIHighLevelPlan[]? selectedPlans = default;
        var workerEntity = w.Entity;
        var tiredRatio = w.VillagerComponent.Needs.Tired / w.VillagerComponent.Needs.TiredMax;

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

                var bed = availableBeds.OrderByDistanceFrom(w.LocationComponent.Box.Center, w => w.location.ToNumericsVector2Center(), w => w.entity).First();
                bed.GetWorkableComponent().Entity = workerEntity;
                selectedPlans = new AIHighLevelPlan[]
                {
                    new RestAIHighLevelPlan(world, workerEntity, bed)
                };
            }
        }

        // if no beds and it's an emergency, sleep on the floor
        if (tiredRatio < .1f && selectedPlans is null)
            selectedPlans = new AIHighLevelPlan[]
            {
                new RestAIHighLevelPlan(world, workerEntity, Entity.Invalid)
            };

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
            if (!rw.InventoryComponent.Inventory.IsEmpty(ResourceMarker.Unmarked))
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
                    ResourceMarker.Unmarked, workerAvailableWeight, foundZones.Select(w => w.box), foundStoredResources,
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

        using var allResources = World.GetAllResources(ResourceMarker.Unmarked, false);
        int getAvailableResourceAmount(Resource res) =>
            allResources.TryGetValue(res, out var val) ? val : 0;

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
                                && getAvailableResourceAmount(reqRq.Resource) < bill.Amount))
                        && ResourceBucket.TryToMarkResources(() => marker = ResourceMarker.Create(), foundPlacedResources.Select(r => r.entity.GetInventoryComponent().Inventory),
                            ResourceMarker.Unmarked, workerAvailableWeight, bill.ProductionLine.PossibleInputs, out _))
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

    bool TryToPoop(in IterationResult w, out AIHighLevelPlan[]? plans)
    {
        var workerEntity = w.Entity;
        ref var workerVillagerComponent = ref workerEntity.GetVillagerComponent();
        var workerEntityLocation = w.LocationComponent.Box.Center;
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
                    new PoopAIHighLevelPlan(world, workerEntity, selectedToilet)
                };
            }
        }

        if (selectedPlans is null && !workerVillagerComponent.IsPooping && workerVillagerComponent.Needs.PoopPercentage <= 0)
        {
            // uh oh, poop on the ground
            workerVillagerComponent.IsPooping = true;
            selectedPlans = new AIHighLevelPlan[]
            {
                new PoopAIHighLevelPlan(world, workerEntity, Entity.Invalid)
            };
        }

        return (plans = selectedPlans) is not null;
    }

    bool TryToEat(in IterationResult w, out AIHighLevelPlan[]? plans)
    {
        var workerEntity = w.Entity;
        ref var workerVillagerComponent = ref workerEntity.GetVillagerComponent();
        var workerEntityLocation = w.LocationComponent.Box.Center;
        var workerAvailableWeight = w.VillagerComponent.MaxCarryWeight - w.InventoryComponent.Inventory.GetWeight(ResourceMarker.All);
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

                    selectedPlans = new AIHighLevelPlan[]
                    {
                        new GatherResourcesAIHighLevelPlan(world, workerEntity, marker),
                        new EatAIHighLevelPlan(world, workerEntity, marker)
                    };
                }
            }
        }

        if (workerVillagerComponent.Needs.HungerPercentage <= 0)
            workerVillagerComponent.AddThought(world, world.ThoughtTemplates[ThoughtTemplates.Starving], false);

        return (plans = selectedPlans) is not null;
    }

    readonly FrameAwaiter frameAwaiter = new();
    readonly List<Task?> planRunners = new();
    readonly Dictionary<Entity, Vector2> wanderCenterLocations = new();

    public partial void Run()
    {
        IterateComponents((in IterationResult w) =>
        {
            if (w.WorkerComponent.Plans is null)
            {
                _ = TryToEat(w, out var plans) || TryToRest(w, out plans) || TryToPoop(w, out plans) || TryToPlant(w, out plans)
                    || TryToBuild(w, out plans) || TryToHaulToBuilingSite(w, out plans) || TryToHarvest(w, out plans)
                    || TryToWorkBills(w, out plans) || TryToHaulToStorage(w, out plans);
                w.WorkerComponent.Plans = plans;

                if (w.WorkerComponent.Plans is not null && w.WorkerComponent.Plans is not [WanderAIHighLevelPlan])
                    wanderCenterLocations.Remove(w.Entity);
            }

            if (w.WorkerComponent.Plans is { } workerPlans)
            {
                while (planRunners.Count <= w.Entity)
                    planRunners.Add(null);

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
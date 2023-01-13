namespace Tweey.Support.AI.SystemJobs;

class BillSystemJob : BaseSystemJob
{
    public BillSystemJob(World world) : base(world)
    {
    }

    public override string Name => "Crafting";

    public override bool TryToRun(Entity workerEntity, [NotNullWhen(true)] out AIHighLevelPlan[]? plans)
    {
        var workerEntityLocation = workerEntity.GetLocationComponent().Box.Center;
        var workerAvailableWeight = workerEntity.GetVillagerComponent().MaxCarryWeight
            - workerEntity.GetInventoryComponent().Inventory.GetWeight(ResourceMarker.All);
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
            foreach (var workable in foundWorkables.OrderByDistanceFrom(workerEntity.GetLocationComponent().Box.Center, w => w.location.ToNumericsVector2(), w => w.entity))
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
                            new GatherResourcesAIHighLevelPlan(World, workerEntity, marker),
                            new WorkAIHighLevelPlan(World, workerEntity, workable, marker)
                        };
                        goto done;
                    }
            }
        }

        done:
        return (plans = selectedPlans) is not null;
    }
}

namespace Tweey.Support.AI.SystemJobs;

class HaulToStorageSystemJob : BaseSystemJob
{
    public HaulToStorageSystemJob(World world) : base(world)
    {
    }

    public override string Name => "Hauling";

    public override bool TryToRun(Entity workerEntity, [NotNullWhen(true)] out AIHighLevelPlan[]? plans)
    {
        var workerEntityLocation = workerEntity.GetLocationComponent().Box.Center;
        var workerAvailableWeight = workerEntity.GetVillagerComponent().MaxCarryWeight
            - workerEntity.GetInventoryComponent().Inventory.GetWeight(ResourceMarker.All);
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
                ResourceBucket.MarkResources(World, planMarker.Value,
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
                            var newEntity = World.AddResourceEntities(ResourceMarker.All, new(srcRQ), planMarker.Value, dstLoc.Value.ToNumericsVector2()).Single();
                            newEntity.AddPlacedResourceIsStoredComponent();

                            return (newEntity.GetInventoryComponent().Inventory, newEntity);
                        }
                        else
                            throw new NotImplementedException();
                    }, out _);

                selectedPlans = new AIHighLevelPlan[]
                {
                    new GatherResourcesAIHighLevelPlan(World, workerEntity, planMarker.Value, r => !r.HasPlacedResourceIsStoredComponent()),
                    new DropResourcesToInventoriesAIHighLevelPlan(World, workerEntity, planMarker.Value)
                };
            }
        }

        return (plans = selectedPlans) is not null;
    }
}

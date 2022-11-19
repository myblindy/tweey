namespace Tweey.Support;
abstract class AIHighLevelPlan
{
    public AIHighLevelPlan(World world, Entity entity)
    {
        World = world;
        MainEntity = entity;
    }

    protected World World { get; }
    protected Entity MainEntity { get; }

    public abstract IEnumerable<AILowLevelPlan> GetLowLevelPlans();
}

class GatherResourcesAIHighLevelPlan : AIHighLevelPlan
{
    private readonly ResourceMarker marker;

    public GatherResourcesAIHighLevelPlan(World world, Entity entity, ResourceMarker marker)
        : base(world, entity)
    {
        this.marker = marker;
    }

    public override IEnumerable<AILowLevelPlan> GetLowLevelPlans()
    {
        var targets = ObjectPool<List<(Entity entity, Vector2 location)>>.Shared.Get();
        targets.Clear();

        try
        {
            EcsCoordinator.IteratePlacedResourceArchetype((in EcsCoordinator.PlacedResourceIterationResult w) =>
            {
                if (w.InventoryComponent.Inventory.HasMarker(marker))
                    targets.Add((w.Entity, w.LocationComponent.Box.Center));
            });

            foreach (var targetEntity in targets.OrderByDistanceFrom(MainEntity.GetLocationComponent().Box.Center))
            {
                yield return new WalkToEntityLowLevelPlan(World, MainEntity, targetEntity);
                yield return new WaitLowLevelPlan(World, MainEntity, World.RawWorldTime + World.GetWorldTimeFromTicks(
                    MainEntity.GetVillagerComponent().PickupSpeedMultiplier * World.Configuration.Data.BasePickupSpeed
                        * targetEntity.GetInventoryComponent().Inventory.GetWeight(marker)));
                yield return new MoveInventoryLowLevelPlan(World, targetEntity, marker, MainEntity, marker);       // from the resource (marked) to the villager (marked)
            }
        }
        finally
        {
            ObjectPool<List<(Entity entity, Vector2 location)>>.Shared.Return(targets);
        }
    }
}

class DropResourcesToInventoryAIHighLevelPlan : AIHighLevelPlan
{
    private readonly Entity targetEntity;
    private readonly ResourceMarker marker;

    public DropResourcesToInventoryAIHighLevelPlan(World world, Entity mainEntity, Entity targetEntity, ResourceMarker marker)
        : base(world, mainEntity)
    {
        this.targetEntity = targetEntity;
        this.marker = marker;
    }

    public override IEnumerable<AILowLevelPlan> GetLowLevelPlans()
    {
        yield return new WalkToEntityLowLevelPlan(World, MainEntity, targetEntity);
        yield return new WaitLowLevelPlan(World, MainEntity, World.RawWorldTime + World.GetWorldTimeFromTicks(
            MainEntity.GetVillagerComponent().PickupSpeedMultiplier * MainEntity.GetInventoryComponent().Inventory.GetWeight(marker)));
        yield return new MoveInventoryLowLevelPlan(World, MainEntity, marker, targetEntity, ResourceMarker.Default, true);    // from the villager (marked) to the building (unmarked)
    }
}

class WorkAIHighLevelPlan : AIHighLevelPlan
{
    private readonly Entity workableEntity;

    public WorkAIHighLevelPlan(World world, Entity workerEntity, Entity workableEntity)
        : base(world, workerEntity)
    {
        this.workableEntity = workableEntity;
    }

    public override IEnumerable<AILowLevelPlan> GetLowLevelPlans()
    {
        yield return new WalkToEntityLowLevelPlan(World, MainEntity, workableEntity);

        workableEntity.GetWorkableComponent().GetAssignedWorkerSlot(MainEntity).EntityWorking = true;
        if (workableEntity.HasBuildingComponent())
        {
            while (workableEntity.GetBuildingComponent().BuildWorkTicks-- > 0)
                yield return new WaitLowLevelPlan(World, MainEntity, World.RawWorldTime
                    + World.GetWorldTimeFromTicks(MainEntity.GetVillagerComponent().WorkSpeedMultiplier));
            workableEntity.GetWorkableComponent().GetAssignedWorkerSlot(MainEntity).Clear();
        }
        else if (workableEntity.HasPlantComponent())
        {
            while (workableEntity.GetPlantComponent().WorkTicks-- > 0)
                yield return new WaitLowLevelPlan(World, MainEntity, World.RawWorldTime
                    + World.GetWorldTimeFromTicks(MainEntity.GetVillagerComponent().HarvestSpeedMultiplier));

            World.AddResourceEntity(workableEntity.GetInventoryComponent().Inventory.Clone(),
                workableEntity.GetLocationComponent().Box.Center.Floor());
            World.DeleteEntity(workableEntity);
        }
    }
}

class PlantAIHighLevelPlan : AIHighLevelPlan
{
    private readonly Entity workableEntity;
    private readonly Vector2i worldPositionOverride;
    private readonly PlantTemplate plantTemplate;

    public PlantAIHighLevelPlan(World world, Entity mainEntity, Entity workableEntity,
        Vector2i worldPositionOverride, PlantTemplate plantTemplate)
        : base(world, mainEntity)
    {
        this.workableEntity = workableEntity;
        this.worldPositionOverride = worldPositionOverride;
        this.plantTemplate = plantTemplate;
    }

    public override IEnumerable<AILowLevelPlan> GetLowLevelPlans()
    {
        yield return new WalkToWorldPositionLowLevelPlan(World, MainEntity, worldPositionOverride.ToNumericsVector2Center());
        yield return new WaitLowLevelPlan(World, MainEntity, World.RawWorldTime
            + World.GetWorldTimeFromTicks(MainEntity.GetVillagerComponent().PlantSpeed));
        World.AddPlantEntity(plantTemplate, worldPositionOverride.ToNumericsVector2(), false, true);
        workableEntity.GetZoneComponent().WorkedTiles.Remove(worldPositionOverride);
    }
}

class WanderAIHighLevelPlan : AIHighLevelPlan
{
    private readonly Vector2 targetWorldPosition;
    private readonly float speedMultiplier;

    public WanderAIHighLevelPlan(World world, Entity mainEntity, Vector2 center, float range, float speedMultiplier)
        : base(world, mainEntity)
    {
        targetWorldPosition = center + new Vector2(Random.Shared.NextSingle() * (range * 2) - range, Random.Shared.NextSingle() * (range * 2) - range);
        this.speedMultiplier = speedMultiplier;
    }

    public override IEnumerable<AILowLevelPlan> GetLowLevelPlans()
    {
        yield return new WalkToWorldPositionLowLevelPlan(World, MainEntity, targetWorldPosition, speedMultiplier);
    }
}
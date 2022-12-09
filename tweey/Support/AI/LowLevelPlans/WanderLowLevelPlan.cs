namespace Tweey.Support.AI.LowLevelPlans;

class WanderLowLevelPlan : AILowLevelPlan
{
    private readonly Vector2 targetLocation;
    private readonly float speedMultiplier;

    public WanderLowLevelPlan(World world, Entity entity, Vector2 targetLocation, float speedMultiplier = 1f)
        : base(world, entity)
    {
        this.targetLocation = targetLocation;
        this.speedMultiplier = speedMultiplier;
    }

    public override bool Run()
    {
        ref var entityLocationComponent = ref MainEntity.GetLocationComponent();

        // already next to the resource?
        if (entityLocationComponent.Box.WithExpand(Vector2.One).Contains(targetLocation))
            return false;

        var currentPosition = entityLocationComponent.Box.Center;
        ref var currentTile = ref World.TerrainCells![(int)Math.Round(currentPosition.X), (int)Math.Round(currentPosition.Y)];
        var deltaPosition = Vector2.Normalize(targetLocation - currentPosition)
            * (float)(MainEntity.GetVillagerComponent().MovementRateMultiplier * World.DeltaWorldTime.TotalSeconds * speedMultiplier
                * MathF.Max(0.5f, MathF.Min(currentTile.GroundMovementModifier, currentTile.AboveGroundMovementModifier)));

        // would we run into unwalkable terrain?
        if (World.TerrainCells![(int)Math.Round(currentPosition.X + deltaPosition.X), (int)Math.Round(currentPosition.Y + deltaPosition.Y)] is { } tile
            && (tile.AboveGroundMovementModifier == 0 || tile.GroundMovementModifier == 0))
        {
            return false;
        }

        entityLocationComponent.Box = entityLocationComponent.Box.WithOffset(deltaPosition);

        return true;
    }
}

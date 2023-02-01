namespace Tweey.Support.AI.LowLevelPlans;

class WanderAILowLevelPlan : AILowLevelPlan
{
    private readonly Vector2 targetLocation;
    private readonly float speedMultiplier;

    public WanderAILowLevelPlan(World world, Entity entity, Vector2 targetLocation, float speedMultiplier = 1f)
        : base(world, entity, Entity.Invalid)
    {
        this.targetLocation = targetLocation;
        this.speedMultiplier = speedMultiplier;
    }

    public override async Task RunAsync(IFrameAwaiter frameAwaiter)
    {
        while (true)
        {
            // already next to the resource?
            if (MainEntity.GetLocationComponent().Box.WithExpand(Vector2.One).Contains(targetLocation))
                return;

            var currentPosition = MainEntity.GetLocationComponent().Box.Center;
            var currentTile = World.TerrainCells![(int)Math.Round(currentPosition.X), (int)Math.Round(currentPosition.Y)];
            var deltaPosition = Vector2.Normalize(targetLocation - currentPosition)
                * (float)(MainEntity.GetVillagerComponent().MovementRateMultiplier * World.DeltaWorldTime.TotalSeconds * speedMultiplier
                    * MathF.Max(0.5f, MathF.Min(currentTile.GroundMovementModifier, currentTile.AboveGroundMovementModifier)));

            // would we run into unwalkable terrain?
            if (World.TerrainCells![(int)Math.Round(currentPosition.X + deltaPosition.X), (int)Math.Round(currentPosition.Y + deltaPosition.Y)] is { } tile
                && (tile.AboveGroundMovementModifier == 0 || tile.GroundMovementModifier == 0 || tile.IsBuildingEntityBlocking))
            {
                return;
            }

            MainEntity.GetLocationComponent().Box = MainEntity.GetLocationComponent().Box.WithOffset(deltaPosition);

            await frameAwaiter;
        }
    }
}

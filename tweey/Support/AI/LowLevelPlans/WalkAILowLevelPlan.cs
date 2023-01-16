using System.Diagnostics;

namespace Tweey.Support.AI.LowLevelPlans;

class WalkAILowLevelPlan : AILowLevelPlan
{
    readonly PathFindingResult pathFindingResult;
    int nextPathIndex = 0;
    private readonly bool moveToCenter;
    private readonly float speedMultiplier;

    public WalkAILowLevelPlan(World world, Entity entity, Vector2 targetLocation, bool moveToCenter = false, float speedMultiplier = 1f)
        : base(world, entity, Entity.Invalid)
    {
        pathFindingResult = PathFindingService.Calculate(world,
            MainEntity.GetLocationComponent().Box.Center.ToVector2i(), targetLocation.ToVector2i());
        this.moveToCenter = moveToCenter;
        this.speedMultiplier = speedMultiplier;

        Debug.Assert(pathFindingResult.IsValid);
    }

    public WalkAILowLevelPlan(World world, Entity entity, Entity target, bool moveToCenter = false, float speedMultiplier = 1f)
        : base(world, entity, target)
    {
        pathFindingResult = PathFindingService.Calculate(world,
            MainEntity.GetLocationComponent().Box.Center.ToVector2i(),
            target.GetLocationComponent().Box.Center.ToVector2i());
        this.moveToCenter = moveToCenter;
        this.speedMultiplier = speedMultiplier;

        Debug.Assert(pathFindingResult.IsValid);
    }

    public override async Task RunAsync(IFrameAwaiter frameAwaiter)
    {
        void snapToTarget(int delta)
        {
            if (nextPathIndex + delta < pathFindingResult.Positions!.Count)
                MainEntity.GetLocationComponent().Box = Box2.FromCornerSize(pathFindingResult.Positions[nextPathIndex + delta].ToNumericsVector2(), Vector2.One);
        }

        while (!pathFindingResult.IsComplete)
            await frameAwaiter;

        if (!pathFindingResult.IsValid)
            throw new InvalidOperationException();

        var adjustedPositionsCount = pathFindingResult.Positions!.Count - (moveToCenter ? 0 : 1);
        if (nextPathIndex >= adjustedPositionsCount)
        {
            snapToTarget(0);
            return;
        }

        bool first = true;
        while (true)
        {
            if (first)
                first = false;
            else
                await frameAwaiter;

            var currentPosition = MainEntity.GetLocationComponent().Box.Center;
            var targetPosition = pathFindingResult.Positions[nextPathIndex].ToNumericsVector2Center();
            var vectorDifference = targetPosition - currentPosition;

            var currentTile = World.TerrainCells![(int)Math.Round(currentPosition.X), (int)Math.Round(currentPosition.Y)];
            var positionDelta = Vector2.Normalize(vectorDifference) * (float)(MainEntity.GetVillagerComponent().MovementRateMultiplier * World.DeltaWorldTime.TotalSeconds * speedMultiplier
                * MathF.Max(0.5f, MathF.Min(currentTile.GroundMovementModifier, currentTile.AboveGroundMovementModifier)));

            if (vectorDifference == default || vectorDifference.LengthSquared() <= positionDelta.LengthSquared())
                if (++nextPathIndex >= adjustedPositionsCount)
                {
                    snapToTarget(-1);
                    break;
                }

            if (vectorDifference != default)
                MainEntity.GetLocationComponent().Box = MainEntity.GetLocationComponent().Box.WithOffset(positionDelta);
        }
    }
}

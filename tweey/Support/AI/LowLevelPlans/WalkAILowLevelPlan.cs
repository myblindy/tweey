using System.Diagnostics;

namespace Tweey.Support.AI.LowLevelPlans;

class WalkAILowLevelPlan : AILowLevelPlanWithTargetEntity
{
    readonly PathFindingResult pathFindingResult;
    int nextPathIndex = 0;
    private readonly bool moveToCenter;
    private readonly float speedMultiplier;

    public WalkAILowLevelPlan(World world, Entity entity, Vector2 targetLocation, bool moveToCenter = false, float speedMultiplier = 1f)
        : base(world, entity, null)
    {
        pathFindingResult = PathFindingService.Calculate(world,
            MainEntity.GetLocationComponent().Box.Center.ToVector2i(), targetLocation.ToVector2i());
        this.moveToCenter = moveToCenter;
        this.speedMultiplier = speedMultiplier;

        Debug.Assert(pathFindingResult.IsValid);
    }

    public WalkAILowLevelPlan(World world, Entity entity, Entity target, float speedMultiplier = 1f)
        : base(world, entity, target)
    {
        pathFindingResult = PathFindingService.Calculate(world,
            MainEntity.GetLocationComponent().Box.Center.ToVector2i(),
            target.GetLocationComponent().Box.Center.ToVector2i());
        this.speedMultiplier = speedMultiplier;

        Debug.Assert(pathFindingResult.IsValid);
    }

    public override bool Run()
    {
        if (!pathFindingResult.IsComplete) return true;
        if (!pathFindingResult.IsValid) throw new InvalidOperationException();
        if (nextPathIndex >= pathFindingResult.Positions!.Count - 1) return false;

        ref var entityLocationComponent = ref MainEntity.GetLocationComponent();
        var currentPosition = entityLocationComponent.Box.Center;

        retry:
        var targetPosition = pathFindingResult.Positions[nextPathIndex].ToNumericsVector2Center();
        var vectorDifference = targetPosition - currentPosition;
        if (vectorDifference.LengthSquared() < .15f)
            if (++nextPathIndex >= pathFindingResult.Positions.Count - 1)
                return false;
            else
                goto retry;

        ref var currentTile = ref World.TerrainCells![(int)Math.Round(currentPosition.X), (int)Math.Round(currentPosition.Y)];
        entityLocationComponent.Box = entityLocationComponent.Box.WithOffset(Vector2.Normalize(vectorDifference)
            * (float)(MainEntity.GetVillagerComponent().MovementRateMultiplier * World.DeltaWorldTime.TotalSeconds * speedMultiplier
                * MathF.Max(0.5f, MathF.Min(currentTile.GroundMovementModifier, currentTile.AboveGroundMovementModifier))));

        return true;
    }
}

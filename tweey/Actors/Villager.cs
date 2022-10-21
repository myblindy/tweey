namespace Tweey.Actors;

public class Villager : PlaceableEntity
{
    public Villager(string name, Vector2 location, ConfigurationData configurationData)
    {
        (Name, Width, Height, MovementActionTime, PickupActionsPerSecond, WorkActionsPerSecond, EatActionsPerSecond, HungerThreshold, HungerEmergencyThreshold) =
            (name, 1, 1, new(configurationData.BaseMovementSpeed), configurationData.BasePickupSpeed, configurationData.BaseWorkSpeed, configurationData.BaseEatSpeed,
                configurationData.BaseHungerPercentage, configurationData.BaseHungerEmergencyPercentage);

        Needs = new()
        {
            HungerMax = configurationData.BaseHungerMax,
            HungerPerSecond = configurationData.BaseHungerPerRealTimeSecond
        };

        Location = InterpolatedLocation = location;
    }

    public override Vector2 InterpolatedLocation { get; set; }
    public override Box2 InterpolatedBox => Box2.FromCornerSize(InterpolatedLocation, Width, Height);
    WeakReference<PlaceableEntity>? headingTarget;
    public void InterpolateToTarget(PlaceableEntity target, double fraction)
    {
        InterpolatedLocation = Location + (target.Location - Location).Sign() * (float)fraction;
        headingTarget = new(target);
    }

    /// <summary>
    /// The villager's heading, based on its target, between <c>0</c> and <c>1</c> (east), with north being <c>0.25</c>, west being <c>0.5</c> and south being <c>0.75</c>.
    /// </summary>
    public double Heading
    {
        get
        {
            if (headingTarget?.TryGetTarget(out var target) != true)
                return 0;

            // P1 = Location
            // P2 = Target
            // P3 = Location + (1, 0)
            var rawAngle = /*Math.Atan2(0, 1)*/ 0 - Math.Atan2(target!.Center.Y - InterpolatedCenter.Y, target!.Center.X - InterpolatedCenter.X);
            return ((rawAngle + Math.PI * 2) % (Math.PI * 2)) / (Math.PI * 2);
        }
    }

    public void Update(double deltaSec) => Needs.Update(deltaSec);

    public override string Name { get; set; }

    public Needs Needs { get; }

    public AIPlan? AIPlan { get; set; }

    public ActionTime MovementActionTime { get; }

    public double PickupActionsPerSecond { get; }
    public ActionTime PickupActionTime { get; } = new();

    public double EatActionsPerSecond { get; }
    public double HungerThreshold { get; }
    public double HungerEmergencyThreshold { get; }
    public ActionTime EatActionTime { get; } = new();

    public double WorkActionsPerSecond { get; }
    public ActionTime WorkActionTime { get; } = new();

    public ResourceBucket Inventory { get; } = new();
}

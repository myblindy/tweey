namespace Tweey.Actors;

public class Villager : PlaceableEntity
{
    public Villager(ConfigurationData configurationData)
    {
        (Width, Height, MovementActionTime, PickupActionsPerSecond, WorkActionsPerSecond) =
            (1, 1, new(configurationData.BaseMovementSpeed), configurationData.BasePickupSpeed, configurationData.BaseWorkSpeed);

        Needs = new()
        {
            HungerMax = configurationData.BaseHungerMax,
            HungerPerSecond = configurationData.BaseHungerPerRealTimeSecond
        };
    }

    public void Update(double deltaSec) => Needs.Update(deltaSec);

    public override string? Name { get; set; }

    public Needs Needs { get; }

    public AIPlan? AIPlan { get; set; }

    public ActionTime MovementActionTime { get; }

    public double PickupActionsPerSecond { get; }
    public ActionTime PickupActionTime { get; } = new();

    public double WorkActionsPerSecond { get; }
    public ActionTime WorkActionTime { get; } = new();

    public ResourceBucket Inventory { get; } = new();
}

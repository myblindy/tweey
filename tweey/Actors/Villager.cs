namespace Tweey.Actors;

public class Villager : PlaceableEntity
{
    public Villager(ConfigurationData configurationData) =>
        (Width, Height, MovementActionTime, PickupActionsPerSecond, WorkActionsPerSecond) =
        (1, 1, new(configurationData.BaseMovementSpeed), configurationData.BasePickupSpeed, configurationData.BaseWorkSpeed);

    public override string? Name { get; set; }

    public AIPlan? AIPlan { get; set; }

    public ActionTime MovementActionTime { get; }

    public double PickupActionsPerSecond { get; }
    public ActionTime PickupActionTime { get; } = new();

    public double WorkActionsPerSecond { get; }
    public ActionTime WorkActionTime { get; } = new();

    public ResourceBucket Inventory { get; } = new();
}

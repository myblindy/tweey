namespace Tweey.Actors;

public class Villager : PlaceableEntity
{
    public Villager(ConfigurationData configurationData) =>
        (Width, Height, MovementActionTime, PickupActionsPerSecond) =
        (1, 1, new(configurationData.BaseMovementSpeed), configurationData.BasePickupSpeed);

    public AIPlan? AIPlan { get; set; }

    public ActionTime MovementActionTime { get; }

    public double PickupActionsPerSecond { get; }
    public ActionTime PickupActionTime { get; } = new();

    public ResourceBucket Inventory { get; } = new();
}

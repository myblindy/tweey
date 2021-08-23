namespace Tweey.Actors
{
    public class Villager : PlaceableEntity
    {
        public Villager(double baseMovementPerSecond) => 
            (Width, Height, MovementActionTime) = (1, 1, new(baseMovementPerSecond));

        public AIPlan? AIPlan { get; set; }

        public ActionTime MovementActionTime { get; }
        public ResourceBucket Inventory { get; } = new();
    }
}

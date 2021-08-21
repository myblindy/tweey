namespace Tweey.Actors
{
    public class ResourceBucket : IPlaceableEntity
    {
        public ResourceBucket(params ResourceQuantity[] rq) => Resources.AddRange(rq);
        public ResourceBucket(Vector2 location, params ResourceQuantity[] rq) : this(rq) => Resources.AddRange(rq);

        public List<ResourceQuantity> Resources { get; } = new();
        public Vector2 Location { get; set; }
        public int Width => 1;
        public int Height => 1;

        public bool Contains(Vector2i location) =>
            Location.X <= location.X && location.X < Location.X + Width && Location.Y <= location.Y && location.Y < Location.Y + Height;

        public double Weight => Resources.Sum(rq => rq.Weight);
    }
}

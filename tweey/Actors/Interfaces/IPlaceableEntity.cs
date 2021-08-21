namespace Tweey.Actors.Interfaces
{
    interface IPlaceableEntity
    {
        public Vector2 Location { get; set; }
        public int Width { get; }
        public int Height { get; }

        public bool Contains(Vector2i location);
    }
}

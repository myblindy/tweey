namespace Tweey.Actors
{
    class Villager : IPlaceableEntity
    {
        public Vector2 Location { get; set; }
        public int Width => 1;
        public int Height => 1;

        public bool Contains(Vector2i pt) => Location.X <= pt.X && pt.X < Location.X + Width && Location.Y <= pt.Y && pt.Y < Location.Y + Height;
    }
}

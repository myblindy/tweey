namespace Tweey.Actors.Interfaces;

public abstract class PlaceableEntity
{
    public Vector2 Location { get; set; }
    public abstract Vector2 InterpolatedLocation { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public abstract string Name { get; set; }

    public Box2 Box => Box2.FromCornerSize(Location, Width, Height);
    public abstract Box2 InterpolatedBox { get; }
    public Box2 GetBoxAtLocation(Vector2 location) => Box2.FromCornerSize(location, Width, Height);
    public Box2 GetBoxAtLocation(Vector2i location) => Box2.FromCornerSize(location, Width, Height);
    public Vector2 Center => Location + new Vector2((Width - 1) / 2, (Height - 1) / 2);
    public Vector2 InterpolatedCenter => InterpolatedLocation + new Vector2((Width - 1) / 2, (Height - 1) / 2);

    public bool Contains(Vector2i location) =>
        Location.X <= location.X && location.X < Location.X + Width && Location.Y <= location.Y && location.Y < Location.Y + Height;
}

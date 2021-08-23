﻿namespace Tweey.Actors.Interfaces;

public abstract class PlaceableEntity
{
    public Vector2 Location { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public Box2 Box => Box2.FromCornerSize(Location, Width, Height);
    public Vector2 Center => Location + new Vector2((Width - 1) / 2, (Height - 1) / 2);

    public bool Contains(Vector2i location) =>
        Location.X <= location.X && location.X < Location.X + Width && Location.Y <= location.Y && location.Y < Location.Y + Height;
}
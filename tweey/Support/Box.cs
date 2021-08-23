namespace Tweey.Support;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Box2
{
    public Vector2 TopLeft, BottomRight;

    public float Left => TopLeft.X;
    public float Top => TopLeft.Y;
    public float Right => BottomRight.X;
    public float Bottom => BottomRight.Y;

    public Vector2 Center => TopLeft + new Vector2((Right - Left) / 2, (Bottom - Top) / 2);

    public Vector2 Size => BottomRight - TopLeft + Vector2.One;

    public static Box2 FromCornerSize(Vector2 topLeft, Vector2 size) =>
        new() { TopLeft = topLeft, BottomRight = topLeft + size - Vector2.One };

    public static Box2 FromCornerSize(Vector2 topLeft, float width, float height) =>
        new() { TopLeft = topLeft, BottomRight = topLeft + new Vector2(width - 1, height - 1) };

    public Box2 WithExpand(Vector2 offset) => new() { TopLeft = TopLeft - offset, BottomRight = BottomRight + offset };

    public bool Intersects(Box2 other) => Left <= other.Right && Right >= other.Left && Top <= other.Bottom && Bottom >= other.Top;
}

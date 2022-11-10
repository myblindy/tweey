using Twee.Core.Support;

namespace Twee.Core;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct Box2 : IEquatable<Box2>
{
    public readonly Vector2 TopLeft, BottomRight;

    public Box2(Vector2 tl, Vector2 br)
    {
        var (x0, x1) = tl.X <= br.X ? (tl.X, br.X) : (br.X, tl.X);
        var (y0, y1) = tl.Y <= br.Y ? (tl.Y, br.Y) : (br.Y, tl.Y);
        (TopLeft, BottomRight) = (new(x0, y0), new(x1, y1));
    }

    public float Left => TopLeft.X;
    public float Top => TopLeft.Y;
    public float Right => BottomRight.X;
    public float Bottom => BottomRight.Y;

    public Vector2 TopRight => new(Right, Top);
    public Vector2 BottomLeft => new(Left, Bottom);

    public Vector2 Center => TopLeft + new Vector2((Right - Left) / 2, (Bottom - Top) / 2);

    public Vector2 Size => BottomRight - TopLeft + Vector2.One;

    public static Box2 FromCornerSize(Vector2 topLeft, Vector2 size) =>
        new(topLeft, topLeft + size - Vector2.One);
    public static Box2 FromCornerSize(Vector2i topLeft, Vector2i size) =>
        FromCornerSize(topLeft.ToNumericsVector2(), size.ToNumericsVector2());

    public static Box2 FromCornerSize(float left, float top, float width, float height) =>
        FromCornerSize(new Vector2(left, top), new Vector2(width, height));
    public static Box2 FromCornerSize(int left, int top, int width, int height) =>
        FromCornerSize(new Vector2i(left, top), new Vector2i(width, height));

    public static Box2 FromCornerSize(Vector2 topLeft, float width, float height) =>
        new(topLeft, topLeft + new Vector2(width - 1, height - 1));
    public static Box2 FromCornerSize(Vector2i topLeft, float width, float height) =>
        FromCornerSize(topLeft.ToNumericsVector2(), width, height);

    public Box2 WithExpand(Vector2 offset) => new(TopLeft - offset, BottomRight + offset);
    public Box2 WithExpand(Vector2i offset) => WithExpand(offset.ToNumericsVector2());

    public Box2 WithOffset(Vector2 offset) => new(TopLeft + offset, BottomRight + offset);
    public Box2 WithOffset(Vector2i offset) => WithOffset(offset.ToNumericsVector2());

    public bool Intersects(Box2 other) => Left <= other.Right && Right >= other.Left && Top <= other.Bottom && Bottom >= other.Top;
    public bool Contains(Vector2 location) => Left <= location.X && Right + 1 >= location.X && Top <= location.Y && Bottom + 1 >= location.Y;
    public bool Contains(Vector2i location) => Contains(location.ToNumericsVector2());

    public bool Equals(Box2 other) => other.TopLeft == TopLeft && other.BottomRight == BottomRight;
    public override bool Equals(object? obj) => obj is Box2 box && Equals(box);
    public override int GetHashCode() => HashCode.Combine(TopLeft, BottomRight);

    public static bool operator ==(Box2 left, Box2 right) => left.Equals(right);
    public static bool operator !=(Box2 left, Box2 right) => !(left == right);
}

namespace Tweey.Support;

public static class Extensions
{
    public static Vector2 ToNumericsVector2(this Vector2i v) => new(v.X, v.Y);
    public static Vector2 ToNumericsVector2(this OpenTK.Mathematics.Vector2 v) => new(v.X, v.Y);
    public static Vector2i ToVector2i(this Vector2 v) => new((int)v.X, (int)v.Y);
    public static Vector2i ToVector2i(this OpenTK.Mathematics.Vector2 v) => new((int)v.X, (int)v.Y);

    public static Vector2 Sign(this Vector2 v) => new(Math.Sign(v.X), Math.Sign(v.Y));

    public static void Deconstruct(this Vector2 v, out float x, out float y) => (x, y) = (v.X, v.Y);
}

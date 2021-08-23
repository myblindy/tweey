namespace Tweey.Support
{
    public static class Extensions
    {
        public static Vector2 ToNumericsVector2(this Vector2i v) => new(v.X, v.Y);

        public static Vector2 Sign(this Vector2 v) => new(Math.Sign(v.X), Math.Sign(v.Y));
    }
}

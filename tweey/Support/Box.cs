namespace Tweey.Support
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Box2
    {
        public Vector2 TopLeft, BottomRight;

        public float Left => TopLeft.X;
        public float Top => TopLeft.Y;
        public float Right => BottomRight.X;
        public float Bottom => BottomRight.Y;

        public Vector2 Center => TopLeft + new Vector2((Right - Left) / 2, (Bottom - Top) / 2);

        public Vector2 Size => BottomRight - TopLeft;

        public static Box2 FromCornerSize(Vector2 topLeft, Vector2 size) =>
            new() { TopLeft = topLeft, BottomRight = topLeft + size };

        public static Box2 FromCornerSize(Vector2 topLeft, float width, float height) =>
            new() { TopLeft = topLeft, BottomRight = topLeft + new Vector2(width, height) };

        public static Box2 FromCenterSize(Vector2 center, Vector2 size)
        {
            var halfSize = size / 2;
            return new() { TopLeft = center - halfSize, BottomRight = center + halfSize };
        }

        public static Box2 FromCenterSize(Vector2 center, float width, float height)
        {
            var halfSize = new Vector2(width / 2, height / 2);
            return new() { TopLeft = center - halfSize, BottomRight = center + halfSize };
        }
    }
}

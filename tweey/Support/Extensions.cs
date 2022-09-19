namespace Tweey.Support;

static class Extensions
{
    public static Box2 WithExpand(this Box2 box, Thickness offset) =>
        new()
        {
            TopLeft = box.TopLeft - new Vector2(offset.Left, offset.Top),
            BottomRight = box.BottomRight + new Vector2(offset.Right, offset.Bottom)
        };
}

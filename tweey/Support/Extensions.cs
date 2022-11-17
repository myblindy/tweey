namespace Tweey.Support;

static class Extensions
{
    public static Box2 WithExpand(this Box2 box, Thickness offset) =>
        new(box.TopLeft - new Vector2(offset.Left, offset.Top), box.BottomRight + new Vector2(offset.Right, offset.Bottom));
}

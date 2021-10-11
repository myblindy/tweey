namespace Tweey.Gui
{
    class EmptyView : View
    {
        public static View Default { get; } = new EmptyView();
        public EmptyView() : base(new()) => Visible = () => false;
    }
}

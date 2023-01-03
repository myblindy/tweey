namespace Tweey.Gui;

class WindowView : View, ISingleChildContainerView
{
    public WindowView() : base(new())
    {
    }

    public Func<string?>? Title { get; set; }
    public View? Child { get; set; }
}

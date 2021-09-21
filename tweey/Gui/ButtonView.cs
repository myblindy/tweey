namespace Tweey.Gui
{
    class ButtonView : View, IClickable, ISingleChildContainerView
    {
        public Action? Clicked { get; set; }
        public View? Child { get; set; }
    }
}

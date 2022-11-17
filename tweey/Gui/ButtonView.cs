namespace Tweey.Gui
{
    class ButtonView : View, IClickable, ISingleChildContainerView
    {
        public ButtonView() : base(new()) { }

        public Func<bool>? IsChecked { get; set; }
        public Action? Clicked { get; set; }
        public View? Child { get; set; }
    }
}

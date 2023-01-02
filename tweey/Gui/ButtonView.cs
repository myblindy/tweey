namespace Tweey.Gui
{
    class ButtonView : View, IClickable, ISingleChildContainerView
    {
        public ButtonView() : base(new()) { }

        public ButtonView(string text) : this() =>
            Child = new LabelView(text);
        public ButtonView(string text, Func<float> fontSize) : this() =>
            Child = new LabelView(text) { FontSize = fontSize };

        public ButtonView(Func<string> text) : this() =>
            Child = new LabelView(text);
        public ButtonView(Func<string> text, Func<float> fontSize) : this() =>
            Child = new LabelView(text) { FontSize = fontSize };

        public Func<bool>? IsChecked { get; set; }
        public Action? Clicked { get; set; }
        public View? Child { get; set; }
    }
}

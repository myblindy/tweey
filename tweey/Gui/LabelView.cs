namespace Tweey.Gui;

public class LabelView : View, IContentView
{
    public LabelView() : base(new()) { }

    [SetsRequiredMembers]
    public LabelView(string text) : this() => Text = () => text;

    [SetsRequiredMembers]
    public LabelView(Func<string?> text) : this() => Text = text;

    public Func<float>? FontSize { get; set; }
    public HorizontalAlignment HorizontalTextAlignment { get; set; } = HorizontalAlignment.Left;
    public required Func<string?> Text { get; set; }
    public Func<Vector4> ForegroundColor { get; set; } = () => Colors4.White;
}

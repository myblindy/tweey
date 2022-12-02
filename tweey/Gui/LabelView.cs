namespace Tweey.Gui;

public class LabelView : View, IContentView
{
    public LabelView() : base(new()) { }

    public Func<float> FontSize { get; set; } = () => 12;
    public HorizontalAlignment HorizontalTextAlignment { get; set; } = HorizontalAlignment.Left;
    public required Func<string?> Text { get; set; }
    public Func<Vector4> ForegroundColor { get; set; } = () => Colors4.White;
}

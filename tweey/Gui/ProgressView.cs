namespace Tweey.Gui;

public class ProgressView : View, IContentView
{
    public ProgressView() : base(new()) { }

    public required Func<double> Maximum { get; set; }
    public required Func<double> Value { get; set; }

    public Func<float> FontSize { get; set; } = () => 12;
    public HorizontalAlignment HorizontalTextAlignment { get; set; } = HorizontalAlignment.Left;
    public required Func<string?> StringFormat { get; set; }
    public Vector4 TextColor { get; set; } = Colors4.White;
    public Func<Vector4> ForegroundColor { get; set; } = () => Colors4.Gray;
    public Vector4 BorderColor { get; set; } = Colors4.Black;
}

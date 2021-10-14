namespace Tweey.Gui;

public class ProgressView : View, IContentView
{
    public ProgressView() : base(new()) { }

    [RequiredProperty]
    public Func<double>? Maximum { get; set; }
    [RequiredProperty]
    public Func<double>? Value { get; set; }

    public float FontSize { get; set; } = 12;
    public HorizontalAlignment HorizontalTextAlignment { get; set; } = HorizontalAlignment.Left;
    [RequiredProperty]
    public Func<string?>? StringFormat { get; set; }
    public Vector4 TextColor { get; set; } = Colors.White;
    public Vector4 ForegroundColor { get; set; } = Colors.Gray;
    public Vector4 BorderColor { get; set; } = Colors.Black;
}

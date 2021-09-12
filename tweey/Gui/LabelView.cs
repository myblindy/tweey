namespace Tweey.Gui;

public class LabelView : ContentView
{
    public float FontSize { get; set; } = 12;
    public HorizontalAlignment HorizontalTextAlignment { get; set; } = HorizontalAlignment.Left;
    public Func<string?>? Text { get; set; }
}

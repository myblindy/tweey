namespace Tweey.Gui;

public class LabelView : View
{
    public float FontSize { get; set; } = 12;
    public Func<string>? Text { get; set; }
}

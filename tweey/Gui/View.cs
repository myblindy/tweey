namespace Tweey.Gui;

public abstract class View
{
    public Func<int>? MinWidth { get; set; }
    public Func<int>? MinHeight { get; set; }
    public Vector4 BackgroundColor { get; set; } = Colors.Transparent;
}

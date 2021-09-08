namespace Tweey.Gui;

public abstract class View
{
    public int MinWidth { get; set; }
    public int MinHeight { get; set; }
    public Vector4 BackgroundColor { get; set; } = Colors.Transparent;
}

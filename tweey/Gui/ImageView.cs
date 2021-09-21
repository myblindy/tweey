namespace Tweey.Gui;

public class ImageView : View, IContentView
{
    public Func<string?>? Source { get; set; }
    public Vector4 ForegroundColor { get; set; } = Colors.White;
}

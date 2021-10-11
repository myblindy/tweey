namespace Tweey.Gui;

public class ImageView : View, IContentView
{
    public ImageView() : base(new()) { }

    public Func<string?>? Source { get; set; }
    public Vector4 ForegroundColor { get; set; } = Colors.White;
}

namespace Tweey.Gui;

public class ImageView : View, IContentView
{
    public ImageView() : base(new()) { }

    public Func<string?>? Source { get; set; }
    public Func<Vector4> ForegroundColor { get; set; } = () => Colors.White;
}

namespace Tweey.Gui.Base;

public record struct Thickness(int Left, int Top, int Right, int Bottom)
{
    public Thickness(int all) : this(all, all, all, all) { }
    public Thickness(int x, int y) : this(x, y, x, y) { }

    public static Thickness operator -(Thickness src) =>
        new(-src.Left, -src.Top, -src.Right, -src.Bottom);
}

public abstract class View
{
    public ViewData ViewData { get; }
    protected View(ViewData viewData) => ViewData = viewData;

    public View? Parent { get; internal set; }
    public Func<bool>? IsVisible { get; set; }
    public bool InheritParentSize { get; set; }
    public Thickness Padding { get; set; }
    public Func<Thickness> Margin { get; set; } = () => new();
    public Func<int>? MinWidth { get; set; }
    public Func<int>? MinHeight { get; set; }
    public Vector4 BackgroundColor { get; set; } = Colors4.Transparent;
}

public class ViewData
{
    public Box2 Box { get; set; }
    public Box2 BaseBox { get; set; }
    public View? TemplatedView { get; set; }
}
namespace Tweey.Gui;

public enum Anchor
{
    TopLeft, Top, TopRight,
    Left, Center, Right,
    BottomLeft, Bottom, BottomRight,
}

public record RootViewDescription(View View, Vector2i Location, Anchor Anchor = Anchor.TopLeft);

class GuiSpace
{
    public List<RootViewDescription> RootViewDescriptions { get; } = new();
}

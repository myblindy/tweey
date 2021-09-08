namespace Tweey.Gui;

public abstract class ContainerView : View
{
    public List<View> Children { get; } = new();
}

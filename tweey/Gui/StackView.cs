namespace Tweey.Gui;

public enum StackType { Vertical, Horizontal }

public class StackView : ContainerView
{
    public StackView(StackType stackType) =>
        Type = stackType;

    public StackType Type { get; set; }
}

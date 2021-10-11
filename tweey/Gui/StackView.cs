namespace Tweey.Gui;

public enum StackType { Vertical, Horizontal }

public class StackView : View, IContainerView
{
    public StackView(StackType stackType) : base(new()) =>
        (Type, Children) = (stackType, new(this));

    public StackType Type { get; set; }

    public ViewsCollection Children { get; }
}

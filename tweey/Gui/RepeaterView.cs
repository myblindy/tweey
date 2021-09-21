namespace Tweey.Gui;

interface IRepeaterView
{
    View CreateView();
}

public class RepeaterView<T> : View, IRepeaterView
{
    [RequiredProperty]
    public Func<IEnumerable<T>?>? Source { get; set; }
    public IContainerView? ContainerView { get; set; }
    [RequiredProperty]
    public Func<T, View>? ItemView { get; set; }
    public View? EmptyView { get; set; }

    public View CreateView()
    {
        IContainerView? result = null;

        if (Source is not null && Source() is { } items)
            foreach (var item in items)
            {
                if (result is null)
                {
                    result = ContainerView ?? new StackView(StackType.Vertical);
                    result.Children.Clear();
                }

                result.Children.Add(ItemView!(item));
            }

        return ((View?)result) ?? EmptyView ?? Gui.EmptyView.Default;
    }
}

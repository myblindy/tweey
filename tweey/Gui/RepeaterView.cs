namespace Tweey.Gui;

interface IRepeaterView
{
    View CreateView();
}

public class RepeaterView<T> : View, IRepeaterView
{
    public /*required*/ Func<IEnumerable<T>?>? Source { get; set; }
    public ContainerView? ContainerView { get; set; }
    public /*required*/ Func<T, View>? ItemView { get; set; }
    public /*required*/ View? EmptyView { get; set; }

    public View CreateView()
    {
        ContainerView? result = null;

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

        return result ?? EmptyView!;
    }
}

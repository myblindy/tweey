namespace Tweey.Gui;

interface IRepeaterView
{
    View CreateView();
}

public class RepeaterView<T> : View, IRepeaterView
{
    public RepeaterView() : base(new()) { }

    public required Func<IEnumerable<T>?> Source { get; set; }
    public bool DisposeSource = true;
    public IContainerView? ContainerView { get; set; }
    public required Func<T, View> ItemView { get; set; }
    public View? EmptyView { get; set; }

    public View CreateView()
    {
        IContainerView? result = null;

        if (Source() is { } items)
        {
            foreach (var item in items)
            {
                if (result is null)
                {
                    result = ContainerView ?? new StackView(StackType.Vertical);
                    result.Children.Clear();
                }

                result.Children.Add(ItemView!(item));
            }

            if (DisposeSource)
                (items as IDisposable)?.Dispose();
        }

        return ((View?)result) ?? EmptyView ?? Gui.EmptyView.Default;
    }
}

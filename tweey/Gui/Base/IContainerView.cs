namespace Tweey.Gui.Base;

public class ViewsCollection : Collection<View>
{
    readonly View parent;
    public ViewsCollection(View parent) => this.parent = parent;

    protected override void InsertItem(int index, View item)
    {
        base.InsertItem(index, item);
        item.Parent = parent;
    }

    protected override void ClearItems()
    {
        Items.ForEach(v => v.Parent = null);
        base.ClearItems();
    }

    protected override void RemoveItem(int index)
    {
        Items[index].Parent = null;
        base.RemoveItem(index);
    }

    protected override void SetItem(int index, View item)
    {
        item.Parent = parent;
        base.SetItem(index, item);
    }
}

public interface IContainerView
{
    public ViewsCollection Children { get; }
}
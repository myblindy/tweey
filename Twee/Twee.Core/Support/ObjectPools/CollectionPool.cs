using System.Collections.ObjectModel;

namespace Twee.Core.Support.ObjectPools;

public static class CollectionPool<T>
{
    static readonly Queue<PooledCollection<T>> pool = new();

    public static PooledCollection<T> Get()
    {
        if (pool.Count == 0)
            return new();

        var list = pool.Dequeue();
        list.Clear();
        return list;
    }

    public static void Return(PooledCollection<T> item) =>
        pool.Enqueue(item);
}

public class PooledCollection<T> : Collection<T>, IDisposable
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "Specialized use to free pooled collection")]
    public void Dispose() => CollectionPool<T>.Return(this);

    public void Sort(Comparison<T> comparison) => ((List<T>)Items).Sort(comparison);

    public void AddRange(IEnumerable<T> source) => ((List<T>)Items).AddRange(source);

    public Span<T> AsSpanUnsafe() => CollectionsMarshal.AsSpan((List<T>)Items);
}
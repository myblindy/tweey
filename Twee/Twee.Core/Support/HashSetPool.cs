namespace Twee.Core.Support;

public static class HashSetPool<T>
{
    static readonly Queue<PooledHashSet<T>> pool = new();

    public static PooledHashSet<T> Get()
    {
        if (pool.Count == 0)
            return new();

        var set = pool.Dequeue();
        set.Clear();
        return set;
    }

    public static void Return(PooledHashSet<T> item) =>
        pool.Enqueue(item);
}

public class PooledHashSet<T> : HashSet<T>, IDisposable
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "Specialized use to free pooled collection")]
    public void Dispose() => HashSetPool<T>.Return(this);
}
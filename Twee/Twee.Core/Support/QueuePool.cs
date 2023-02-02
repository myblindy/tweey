namespace Twee.Core.Support;

public static class QueuePool<T>
{
    static readonly Queue<PooledQueue<T>> pool = new();

    public static PooledQueue<T> Get()
    {
        if (pool.Count == 0)
            return new();

        var list = pool.Dequeue();
        list.Clear();
        return list;
    }

    public static void Return(PooledQueue<T> item) =>
        pool.Enqueue(item);
}

public class PooledQueue<T> : Queue<T>, IDisposable
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "Specialized use to free pooled collection")]
    public void Dispose() => QueuePool<T>.Return(this);

    public void EnqueueRange(IEnumerable<T> source)
    {
        foreach (var item in source)
            Enqueue(item);
    }
}
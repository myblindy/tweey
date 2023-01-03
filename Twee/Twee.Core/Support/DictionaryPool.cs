namespace Twee.Core.Support;

public static class DictionaryPool<TKey, TVal> where TKey : notnull
{
    static readonly Queue<PooledDictionary<TKey, TVal>> pool = new();

    public static PooledDictionary<TKey, TVal> Get()
    {
        if (pool.Count == 0)
            return new();

        var list = pool.Dequeue();
        list.Clear();
        return list;
    }

    public static void Return(PooledDictionary<TKey, TVal> item) =>
        pool.Enqueue(item);
}

public class PooledDictionary<TKey, TVal> : Dictionary<TKey, TVal>, IDisposable where TKey : notnull
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "Specialized use to free pooled collection")]
    public void Dispose() => DictionaryPool<TKey, TVal>.Return(this);
}
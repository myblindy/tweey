using Microsoft.Extensions.ObjectPool;

namespace Twee.Core.Support;

public static class ObjectPool<T> where T : class, new()
{
    public static Microsoft.Extensions.ObjectPool.ObjectPool<T> Shared { get; } = new DefaultObjectPool<T>(new DefaultPooledObjectPolicy<T>());
}

using System.Runtime.CompilerServices;

namespace Twee.Core.Support;

public static class Extensions
{
    public static Vector2 ToNumericsVector2(this Vector2i v) => new(v.X, v.Y);
    public static Vector2 ToNumericsVector2Center(this Vector2i v) => new(v.X + .5f, v.Y + .5f);
    public static Vector2 ToNumericsVector2(this OpenTK.Mathematics.Vector2 v) => new(v.X, v.Y);
    public static Vector2i ToVector2i(this Vector2 v) => new((int)v.X, (int)v.Y);
    public static Vector2i ToVector2i(this OpenTK.Mathematics.Vector2 v) => new((int)v.X, (int)v.Y);

    public static Vector4 ToVector4(this Vector3 v, float w = 0) => new(v.X, v.Y, v.Z, w);

    public static Vector3 GetXYZ(this Vector4 v) => new(v.X, v.Y, v.Z);

    public static Vector2 Sign(this Vector2 v) => new(Math.Sign(v.X), Math.Sign(v.Y));

    public static void Deconstruct(this Vector2 v, out float x, out float y) => (x, y) = (v.X, v.Y);

    public static T[] ToArray<T>(this IEnumerable<T> e, int capacity)
    {
        var arr = new T[capacity];
        var i = 0;
        foreach (var element in e)
            arr[i++] = element;
        return arr;
    }

    public static int FindIndex<T>(this IEnumerable<T> src, Func<T, bool> test)
    {
        var i = 0;
        foreach (var element in src)
            if (test(element))
                return i;
            else
                ++i;

        return -1;
    }

    public static void AddRange<T>(this ISet<T> set, IEnumerable<T> items)
    {
        foreach (var item in items)
            set.Add(item);
    }

    public static ulong Sum<T>(this IEnumerable<T> source, Func<T, ulong> transform)
    {
        ulong sum = 0;
        foreach (var item in source)
            sum += transform(item);
        return sum;
    }

    public static TimeSpan Sum<T>(this IEnumerable<T> source, Func<T, TimeSpan> transform)
    {
        TimeSpan sum = default;
        foreach (var item in source)
            sum += transform(item);
        return sum;
    }

    public static IEnumerable<string> GetDirectoryParts(this string path, string? pathIsRelativeTo = null)
    {
        var dirRelativeToFullPath = pathIsRelativeTo is null ? null : Path.GetFullPath(pathIsRelativeTo);
        var dirPath = new DirectoryInfo(path);
        while (dirPath is { })
        {
            yield return dirPath.Name;
            dirPath = dirPath.Parent;
            if (dirPath?.FullName == dirRelativeToFullPath)
                break;
        }
    }

    public static Stream CopyToMemoryStream(this Stream sourceStream)
    {
        var memoryStream = new MemoryStream();
        sourceStream.CopyTo(memoryStream);
        return memoryStream;
    }

    public static void Resize<T>(this List<T> lst, int size, T def = default!)
    {
        if (lst.Count <= size)
        {
            lst.Capacity = Math.Max(size + 50, (int)(size * 1.3));
            lst.AddRange(Enumerable.Repeat(def, size - lst.Count + 1));
        }
        else if (lst.Count > size)
            lst.RemoveRange(size, lst.Count - size);
    }

    public static void Resize<T>(this FastList<T> lst, int size, T def = default!)
    {
        if (lst.Count <= size)
        {
            lst.Capacity = Math.Max(size + 50, (int)(size * 1.3));
            lst.AddRange(Enumerable.Repeat(def, size - lst.Count + 1));
        }
        else if (lst.Count > size)
            lst.RemoveRange(size, lst.Count - size);
    }

    public static Vector2 Ceiling(this Vector2 vector) =>
        new(MathF.Ceiling(vector.X), MathF.Ceiling(vector.Y));

    public static Vector2 Floor(this Vector2 vector) =>
        new(MathF.Floor(vector.X), MathF.Floor(vector.Y));

    public static Vector2 MaximumValueOnly(this Vector2 vector) =>
        vector.X > vector.Y ? new(vector.X, 0) : new(0, vector.Y);

    public static TRes Floor<TRes>(this double val)
        where TRes : INumber<TRes>
    {
        return TRes.CreateTruncating(Math.Floor(val));
    }

    public static TRes Ceiling<TRes>(this double val)
        where TRes : INumber<TRes>
    {
        return TRes.CreateTruncating(Math.Ceiling(val));
    }

    public static PooledCollection<T> ToPooledCollection<T>(this IEnumerable<T> source)
    {
        var pooledCollection = CollectionPool<T>.Get();
        pooledCollection.AddRange(source);
        return pooledCollection;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static unsafe bool HasFlagsFast<TEnum>(this TEnum @enum, TEnum flags) where TEnum : unmanaged
    {
        if (sizeof(TEnum) == 1)
            return (Unsafe.As<TEnum, byte>(ref @enum) & Unsafe.As<TEnum, byte>(ref flags)) == Unsafe.As<TEnum, byte>(ref flags);
        if (sizeof(TEnum) == 2)
            return (Unsafe.As<TEnum, ushort>(ref @enum) & Unsafe.As<TEnum, ushort>(ref flags)) == Unsafe.As<TEnum, ushort>(ref flags);
        if (sizeof(TEnum) == 4)
            return (Unsafe.As<TEnum, uint>(ref @enum) & Unsafe.As<TEnum, uint>(ref flags)) == Unsafe.As<TEnum, uint>(ref flags);
        if (sizeof(TEnum) == 8)
            return (Unsafe.As<TEnum, ulong>(ref @enum) & Unsafe.As<TEnum, ulong>(ref flags)) == Unsafe.As<TEnum, ulong>(ref flags);
        if (sizeof(TEnum) == 16)
            return (Unsafe.As<TEnum, UInt128>(ref @enum) & Unsafe.As<TEnum, UInt128>(ref flags)) == Unsafe.As<TEnum, UInt128>(ref flags);

        throw new NotImplementedException();
    }
}

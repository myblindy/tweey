namespace Twee.Core;

public static class Extensions
{
    public static Vector2 ToNumericsVector2(this Vector2i v) => new(v.X, v.Y);
    public static Vector2 ToNumericsVector2(this OpenTK.Mathematics.Vector2 v) => new(v.X, v.Y);
    public static Vector2i ToVector2i(this Vector2 v) => new((int)v.X, (int)v.Y);
    public static Vector2i ToVector2i(this OpenTK.Mathematics.Vector2 v) => new((int)v.X, (int)v.Y);

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
}

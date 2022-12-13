using CommunityToolkit.Diagnostics;
using System;
using System.Collections;
using System.Runtime.CompilerServices;

namespace Twee.Core.Support;

public class FastList<T> : IList<T>, IReadOnlyList<T>
{
    const int DefaultCapacity = 16;

    T[] items;
    int count;

    public FastList() : this(0) { }
    public FastList(int capacity) => items = capacity == 0 ? Array.Empty<T>() : new T[capacity];

    public int Capacity
    {
        get => items.Length;
        set
        {
            if (value < count)
                throw new ArgumentOutOfRangeException(nameof(value));

            if (value != items.Length)
                if (value > 0)
                {
                    var newItems = new T[value];
                    if (count > 0)
                        Array.Copy(items, newItems, count);
                    items = newItems;
                }
                else
                    items = Array.Empty<T>();
        }
    }

    public T this[int index] { get => items[index]; set => items[index] = value; }

    public int Count => count;

    public bool IsReadOnly => false;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void EnsureCapacity(int minCapacity)
    {
        if (items.Length <= minCapacity)
        {
            var newCapacity = items.Length == 0 ? DefaultCapacity : items.Length * 2;
            if ((uint)newCapacity > int.MaxValue) newCapacity = int.MaxValue;
            if (newCapacity < minCapacity) newCapacity = minCapacity;

            Capacity = newCapacity;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        EnsureCapacity(count + 1);
        items[count++] = item;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(IEnumerable<T> collection) => InsertRange(count, collection);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InsertRange(int index, IEnumerable<T> collection)
    {
        if (collection is ICollection<T> c)
        {
            int count = c.Count;
            if (count > 0)
            {
                EnsureCapacity(this.count + count);
                if (index < this.count)
                    Array.Copy(items, index, items, index + count, this.count - index);

                // If we're inserting a List into itself, we want to be able to deal with that.
                if (this == c)
                {
                    // Copy first part of _items to insert location
                    Array.Copy(items, 0, items, index, index);
                    // Copy last part of _items back to inserted location
                    Array.Copy(items, index + count, items, index * 2, this.count - index);
                }
                else
                    c.CopyTo(items, index);
                this.count += count;
            }
        }
        else
        {
            using IEnumerator<T> en = collection.GetEnumerator();
            while (en.MoveNext())
                Insert(index++, en.Current);
        }
    }

    public void RemoveRange(int index, int count)
    {
        if (this.count - index < count)
            ThrowHelper.ThrowArgumentException(nameof(index));

        if (count > 0)
        {
            this.count -= count;
            if (index < this.count)
                Array.Copy(items, index + count, items, index, this.count - index);

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                Array.Clear(items, this.count, count);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            Array.Clear(items, 0, count);                   // let the GC reclaim references
        count = 0;

    }

    public bool Contains(T item) => count > 0 && IndexOf(item) >= 0;

    public void CopyTo(T[] array, int arrayIndex) => items.AsSpan().CopyTo(array.AsSpan(arrayIndex));

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < count; ++i)
            yield return items[i];
    }

    public int IndexOf(T item) => Array.IndexOf(items, item, 0, count);

    public void Insert(int index, T item)
    {
        if (count == items.Length) EnsureCapacity(count + 1);
        if (index < count)
            Array.Copy(items, index, items, index + 1, count - index);
        items[index] = item;
        ++count;
    }

    public bool Remove(T item)
    {
        var index = IndexOf(item);
        if (index >= 0)
        {
            RemoveAt(index);
            return true;
        }

        return false;
    }

    public void RemoveAt(int index)
    {
        count--;
        if (index < count)
            Array.Copy(items, index + 1, items, index, count - index);

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            items[count] = default!;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Reverse() => Reverse(0, Count);

    public void Reverse(int index, int count)
    {
        if (this.count - index < count)
            ThrowHelper.ThrowArgumentException(nameof(count));

        if (count > 1)
            Array.Reverse(items, index, count);
    }

    public void Sort() => Sort(0, Count, null);

    public void Sort(IComparer<T>? comparer) => Sort(0, Count, comparer);

    public void Sort(int index, int count, IComparer<T>? comparer)
    {
        if (this.count - index < count)
            ThrowHelper.ThrowArgumentException(nameof(count));

        if (count > 1)
            Array.Sort(items, index, count, comparer);
    }

    class ComparerForComparison<TVal> : IComparer<TVal>
    {
        private readonly Comparison<TVal> comparison;

        public ComparerForComparison(Comparison<TVal> comparison)
        {
            this.comparison = comparison;
        }

        public int Compare(TVal? x, TVal? y) => comparison(x!, y!);
    }

    public void Sort(Comparison<T> comparison)
    {
        if (count > 1)
            Array.Sort(items, 0, count, new ComparerForComparison<T>(comparison));
    }

    public T[] ToArray()
    {
        if (count == 0)
            return Array.Empty<T>();

        var array = new T[count];
        Array.Copy(items, array, count);
        return array;
    }

    public Span<T> AsSpanUnsafe() => items.AsSpan(0, count);
}

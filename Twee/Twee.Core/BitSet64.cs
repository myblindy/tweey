namespace Twee.Core;

public class BitSet<T> where T : struct, IUnsignedNumber<T>, IShiftOperators<T, T, T>, INumberBase<T>, IBitwiseOperators<T, T, T>
{
    T bits;

    static readonly int BitSize =
        typeof(T) == typeof(uint) ? 32 : typeof(T) == typeof(ushort) ? 16 : typeof(T) == typeof(ulong) ? 64
        : typeof(T) == typeof(UInt128) ? 128 : throw new NotImplementedException();

    public bool this[int idx]
    {
        get => (bits & (T.One << T.CreateTruncating(idx))) != T.Zero;
        set => bits |= (T.One << T.CreateTruncating(idx));
    }

    public void Clear() => bits = default;
}

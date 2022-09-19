namespace Twee.Renderer;

unsafe struct RefArray<T> where T : unmanaged
{
    readonly T* memory;
    readonly int elementCapacity;

    public int Length { get; set; }
    public int Offset { get; set; }

    public RefArray(T* memory, int elementCapacity)
    {
        this.memory = memory;
        this.elementCapacity = elementCapacity;
        Offset = 0;
        Length = 0;
    }

    public ref T this[int idx] => ref memory[idx + Offset];

    public void Clear() => Length = 0;

    public void Add(T value) => memory[Offset + Length++] = value;
}

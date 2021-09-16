namespace Tweey.Renderer
{
    unsafe struct RefGrowableArray<T> where T : unmanaged
    {
        T[] array;

        public int Length { get; set; }

        public RefGrowableArray(int capacity)
        {
            array = new T[capacity];
            Length = 0;
        }

        public ref T this[int idx] => ref array[idx];

        public void Clear() => Length = 0;

        public void Add(T value)
        {
            if (Length >= array.Length)
            {
                var newArray = new T[array.Length * 2];
                System.Buffer.BlockCopy(array, 0, newArray, 0, array.Length * Unsafe.SizeOf<T>());
                array = newArray;
            }
            array[Length++] = value;
        }
    }
}

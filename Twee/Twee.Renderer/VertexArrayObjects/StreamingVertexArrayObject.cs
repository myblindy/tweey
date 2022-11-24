namespace Twee.Renderer.VertexArrayObjects;

public class StreamingVertexArrayObject<TVertex> : BaseVertexArrayObject where TVertex : unmanaged
{
    public unsafe StreamingVertexArrayObject(int layerCount = 1, int initialVertexCapacity = ushort.MaxValue)
    {
        newVertexCapacity = vertexCapacity = initialVertexCapacity;

        vertexBufferHandle = GL.CreateBuffer();
        OrphanVertexBuffer();

        vertexArrayHandle = GL.CreateVertexArray();
        GL.VertexArrayVertexBuffer(vertexArrayHandle, 0, vertexBufferHandle, IntPtr.Zero, Unsafe.SizeOf<TVertex>());

        VertexDefinitionSetup?.Invoke(typeof(TVertex), vertexArrayHandle);

        LayerVertices = new List<TVertex>[layerCount];
        foreach (ref var vertexList in LayerVertices.AsSpan())
            vertexList = new(initialVertexCapacity);
    }

    unsafe void OrphanVertexBuffer() =>
        GL.NamedBufferData(vertexBufferHandle, Unsafe.SizeOf<TVertex>() * vertexCapacity, null, VertexBufferObjectUsage.StreamDraw);

    public unsafe void UploadNewData(Range layerRange)
    {
        // TODO: instead upload everything and store the indices of each layer to only render
        // selective layers later
        var totalVertexCount = 0;
        foreach (ref var list in LayerVertices.AsSpan()[layerRange])
            totalVertexCount += list.Count;

        if (totalVertexCount == 0)
            return;

        const int minBufferMultiplier = 10, maxBufferMultiplier = 15;
        if (totalVertexCount * minBufferMultiplier > newVertexCapacity)
            newVertexCapacity = totalVertexCount * maxBufferMultiplier;

        if (vertexCurrentOffset + totalVertexCount >= vertexCapacity)
        {
            // orphan the buffer
            vertexCapacity = newVertexCapacity;
            OrphanVertexBuffer();
            vertexCurrentOffset = 0;
        }

        // copy the temp vertex data to the driver
        var dest = new Span<TVertex>(GL.MapNamedBufferRange(vertexBufferHandle, vertexCurrentOffset * Unsafe.SizeOf<TVertex>(), totalVertexCount * Unsafe.SizeOf<TVertex>(),
            MapBufferAccessMask.MapWriteBit | MapBufferAccessMask.MapUnsynchronizedBit | MapBufferAccessMask.MapInvalidateBufferBit | MapBufferAccessMask.MapInvalidateRangeBit), totalVertexCount);

        foreach (ref var vertexList in LayerVertices.AsSpan()[layerRange])
        {
            CollectionsMarshal.AsSpan(vertexList).CopyTo(dest);
            dest = dest[vertexList.Count..];
            vertexList.Clear();
        }

        GL.UnmapNamedBuffer(vertexBufferHandle);

        vertexCurrentOffset += lastUploadedVertexLength = totalVertexCount;
    }

    /// <summary>
    /// Draws the bound vertex (and index, if available) array(s), optionally starting at an arbitrary point and/or with an arbitrary length. By default, it draws the entire array. 
    /// </summary>
    /// <param name="primitiveType">The primitive type of the draw call.</param>
    /// <param name="vertexOrIndexOffset">The offset into the vertex or index buffers, in element units. Ie, <c>1</c> is the second element, regardless of vertex or index size. By default, it starts at the beginning of the buffer.</param>
    /// <param name="vertexOrIndexCount">The element count to draw. <c>1</c> is one element, regardless of vertex or index size. By default, it draws the entire array, taking into account the offset parameter.</param>
    public override void Draw(PrimitiveType primitiveType, int vertexOrIndexOffset = 0, int vertexOrIndexCount = -1)
    {
        var vertexCount = vertexOrIndexCount >= 0 ? vertexOrIndexCount : lastUploadedVertexLength - vertexOrIndexOffset;
        if (vertexCount <= 0) return;

        if (LastBoundVertexArray != this)
        {
            GL.BindVertexArray(vertexArrayHandle);
            LastBoundVertexArray = this;
        }

        GL.DrawArrays(primitiveType, vertexCurrentOffset - lastUploadedVertexLength + vertexOrIndexOffset, vertexCount);
        AddFrameData(primitiveType, (ulong)vertexCount);
    }

    readonly BufferHandle vertexBufferHandle;
    readonly VertexArrayHandle vertexArrayHandle;
    int vertexCapacity, newVertexCapacity, vertexCurrentOffset, lastUploadedVertexLength;

    public List<TVertex>[] LayerVertices { get; }
    public int CurrentVertexCount => LayerVertices.Sum(x => x.Count);
}

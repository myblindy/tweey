namespace Tweey.Renderer.VertexArrayObjects;

class StreamingVertexArrayObject<TVertex> : BaseVertexArrayObject where TVertex : unmanaged
{
    public unsafe StreamingVertexArrayObject(int initialVertexCapacity = ushort.MaxValue)
    {
        newVertexCapacity = vertexCapacity = initialVertexCapacity;

        vertexBufferHandle = GL.CreateBuffer();
        OrphanVertexBuffer();

        vertexArrayHandle = GL.CreateVertexArray();
        GL.VertexArrayVertexBuffer(vertexArrayHandle, 0, vertexBufferHandle, IntPtr.Zero, Unsafe.SizeOf<TVertex>());

        VertexDefinitionSetup.Setup(typeof(TVertex), vertexArrayHandle);

        Vertices = new(initialVertexCapacity);
    }

    unsafe void OrphanVertexBuffer() =>
        GL.NamedBufferData(vertexBufferHandle, Unsafe.SizeOf<TVertex>() * vertexCapacity, null, VertexBufferObjectUsage.StreamDraw);

    public unsafe void UploadNewData()
    {
        const int minBufferMultiplier = 10, maxBufferMultiplier = 15;
        if (Vertices.Count * minBufferMultiplier > newVertexCapacity)
            newVertexCapacity = Vertices.Count * maxBufferMultiplier;

        if (vertexCurrentOffset + Vertices.Count >= vertexCapacity)
        {
            // orphan the buffer
            vertexCapacity = newVertexCapacity;
            OrphanVertexBuffer();
            vertexCurrentOffset = 0;
        }

        // copy the temp vertex data to the driver
        // TODO: perhaps implement the Add() directly to this mapped memory
        var dest = GL.MapNamedBufferRange(vertexBufferHandle, vertexCurrentOffset * Unsafe.SizeOf<TVertex>(), Vertices.Count * Unsafe.SizeOf<TVertex>(),
            MapBufferAccessMask.MapWriteBit | MapBufferAccessMask.MapUnsynchronizedBit | MapBufferAccessMask.MapInvalidateBufferBit | MapBufferAccessMask.MapInvalidateRangeBit);
        CollectionsMarshal.AsSpan(Vertices).CopyTo(new Span<TVertex>(dest, Vertices.Count));
        GL.UnmapNamedBuffer(vertexBufferHandle);

        vertexCurrentOffset += lastUploadedVertexLength = Vertices.Count;
        Vertices.Clear();
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

        if (lastBoundVertexArray != this)
        {
            GL.BindVertexArray(vertexArrayHandle);
            lastBoundVertexArray = this;
        }

        GL.DrawArrays(primitiveType, vertexCurrentOffset - lastUploadedVertexLength + vertexOrIndexOffset, vertexCount);
        AddFrameData(primitiveType, (ulong)vertexCount);
    }

    readonly BufferHandle vertexBufferHandle;
    readonly VertexArrayHandle vertexArrayHandle;
    int vertexCapacity, newVertexCapacity, vertexCurrentOffset, lastUploadedVertexLength;

    public List<TVertex> Vertices { get; }
}

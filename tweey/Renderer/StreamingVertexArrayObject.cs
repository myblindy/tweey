namespace Tweey.Renderer;

interface IVertexArrayObject { }

class StreamingVertexArrayObject<TVertex> : IVertexArrayObject where TVertex : unmanaged
{
    public unsafe StreamingVertexArrayObject(int initialVertexCapacity = ushort.MaxValue)
    {
        newVertexCapacity = vertexCapacity = initialVertexCapacity;

        vertexBufferHandle = GL.GenBuffer();
        OrphanVertexBuffer();

        vertexArrayHandle = GL.CreateVertexArray();
        GL.VertexArrayVertexBuffer(vertexArrayHandle, 0, vertexBufferHandle, IntPtr.Zero, Unsafe.SizeOf<TVertex>());

        VertexDefinitionSetup.Setup(typeof(TVertex), vertexArrayHandle);

        tempVertices = new(initialVertexCapacity);
    }

    static IVertexArrayObject? lastBoundVertexArray;

    void OrphanVertexBuffer()
    {
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, vertexBufferHandle);
        GL.BufferData(BufferTargetARB.ArrayBuffer, Unsafe.SizeOf<TVertex>() * vertexCapacity, IntPtr.Zero, BufferUsageARB.StreamDraw);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, BufferHandle.Zero);
    }

    public unsafe void UploadNewData()
    {
        const int minBufferMultiplier = 10, maxBufferMultiplier = 15;
        if (tempVertices.Length * minBufferMultiplier > newVertexCapacity)
            newVertexCapacity = tempVertices.Length * maxBufferMultiplier;

        if (vertexCurrentOffset + tempVertices.Length >= vertexCapacity)
        {
            // orphan the buffer
            vertexCapacity = newVertexCapacity;
            OrphanVertexBuffer();
            vertexCurrentOffset = 0;
        }

        // copy the temp vertex data to the driver
        var destLength = vertexCapacity * Unsafe.SizeOf<TVertex>();
        var dest = GL.MapNamedBufferRange(vertexBufferHandle, new(vertexCurrentOffset * Unsafe.SizeOf<TVertex>()), tempVertices.Length * Unsafe.SizeOf<TVertex>(),
            MapBufferAccessMask.MapWriteBit | MapBufferAccessMask.MapUnsynchronizedBit | MapBufferAccessMask.MapInvalidateBufferBit | MapBufferAccessMask.MapInvalidateRangeBit);
        fixed (TVertex* src = &tempVertices[0])
            System.Buffer.MemoryCopy(src, dest, destLength, tempVertices.Length * Unsafe.SizeOf<TVertex>());
        GL.UnmapNamedBuffer(vertexBufferHandle);

        vertexCurrentOffset += lastUploadedVertexLength = tempVertices.Length;
        tempVertices.Clear();
    }

    /// <summary>
    /// Draws the bound vertex (and index, if available) array(s), optionally starting at an arbitrary point and/or with an arbitrary length. By default, it draws the entire array. 
    /// </summary>
    /// <param name="primitiveType">The primitive type of the draw call.</param>
    /// <param name="vertexOrIndexOffset">The offset into the vertex or index buffers, in element units. Ie, <c>1</c> is the second element, regardless of vertex or index size. By default, it starts at the beginning of the buffer.</param>
    /// <param name="vertexOrIndexCount">The element count to draw. <c>1</c> is one element, regardless of vertex or index size. By default, it draws the entire array, taking into account the offset parameter.</param>
    public void Draw(PrimitiveType primitiveType, int vertexOrIndexOffset = 0, int vertexOrIndexCount = -1)
    {
        var vertexCount = vertexOrIndexCount >= 0 ? vertexOrIndexCount : lastUploadedVertexLength - vertexOrIndexOffset;
        if (vertexCount <= 0) return;

        if (lastBoundVertexArray != this)
        {
            GL.BindVertexArray(vertexArrayHandle);
            lastBoundVertexArray = this;
        }

        GL.DrawArrays(primitiveType, vertexCurrentOffset - lastUploadedVertexLength + vertexOrIndexOffset, vertexCount);
    }

    readonly BufferHandle vertexBufferHandle;
    readonly VertexArrayHandle vertexArrayHandle;
    int vertexCapacity, newVertexCapacity, vertexCurrentOffset, lastUploadedVertexLength;

    RefGrowableArray<TVertex> tempVertices;
    public ref RefGrowableArray<TVertex> Vertices => ref tempVertices;
}

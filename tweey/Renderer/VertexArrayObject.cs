﻿namespace Tweey.Renderer;

struct Nothing { }

interface IVertexArrayObject { }

class VertexArrayObject<TVertex, TIndex> : IVertexArrayObject where TVertex : unmanaged where TIndex : unmanaged
{
    public unsafe VertexArrayObject(bool hasIndexBuffer, int vertexCapacity, int indexCapacity, int multiBufferCount = 3)
    {
        HasIndexBuffer = hasIndexBuffer;
        VertexCapacity = vertexCapacity;
        IndexCapacity = indexCapacity;

        vertexBufferHandle = GL.CreateBuffer();
        GL.NamedBufferStorage(vertexBufferHandle, Unsafe.SizeOf<TVertex>() * vertexCapacity * multiBufferCount, IntPtr.Zero,
            BufferStorageMask.MapWriteBit | BufferStorageMask.MapPersistentBit | BufferStorageMask.MapCoherentBit);
        Vertices = new((TVertex*)GL.MapNamedBufferRange(vertexBufferHandle, IntPtr.Zero, Unsafe.SizeOf<TVertex>() * vertexCapacity * multiBufferCount,
            MapBufferAccessMask.MapWriteBit | MapBufferAccessMask.MapPersistentBit | MapBufferAccessMask.MapCoherentBit), vertexCapacity);

        if (hasIndexBuffer)
        {
            indexBufferHandle = GL.CreateBuffer();
            GL.NamedBufferStorage(indexBufferHandle, Unsafe.SizeOf<TIndex>() * indexCapacity * multiBufferCount, IntPtr.Zero,
                BufferStorageMask.MapWriteBit | BufferStorageMask.MapPersistentBit | BufferStorageMask.MapCoherentBit);
            Indices = new((TIndex*)GL.MapNamedBufferRange(indexBufferHandle, IntPtr.Zero, Unsafe.SizeOf<TIndex>() * indexCapacity * multiBufferCount,
                MapBufferAccessMask.MapWriteBit | MapBufferAccessMask.MapPersistentBit | MapBufferAccessMask.MapCoherentBit), indexCapacity);

            drawElementsType =
                typeof(TIndex) == typeof(byte) ? DrawElementsType.UnsignedByte :
                typeof(TIndex) == typeof(uint) ? DrawElementsType.UnsignedInt :
                typeof(TIndex) == typeof(ushort) ? DrawElementsType.UnsignedShort :
                typeof(TIndex) == typeof(sbyte) ? DrawElementsType.UnsignedByte :
                typeof(TIndex) == typeof(int) ? DrawElementsType.UnsignedInt :
                typeof(TIndex) == typeof(short) ? DrawElementsType.UnsignedShort :
                throw new InvalidOperationException();
        }

        vertexArrayHandle = GL.CreateVertexArray();
        GL.VertexArrayVertexBuffer(vertexArrayHandle, 0, vertexBufferHandle, IntPtr.Zero, Unsafe.SizeOf<TVertex>());

        uint idx = 0, offset = 0;
        foreach (var fi in typeof(TVertex).GetFields())
        {
            GL.EnableVertexArrayAttrib(vertexArrayHandle, idx);
            GL.VertexArrayAttribFormat(vertexArrayHandle, idx, fieldCounts[fi.FieldType], fieldTypes[fi.FieldType], false, offset);
            offset += fieldSizes[fi.FieldType];
            GL.VertexArrayAttribBinding(vertexArrayHandle, idx, 0);
            ++idx;
        }

        syncObjects = new GLSync[multiBufferCount];
    }

    static readonly Dictionary<Type, int> fieldCounts = new()
    {
        [typeof(float)] = 1,
        [typeof(Vector2)] = 2,
        [typeof(Vector3)] = 3,
        [typeof(Vector4)] = 4,
    };

    static readonly Dictionary<Type, VertexAttribType> fieldTypes = new()
    {
        [typeof(float)] = VertexAttribType.Float,
        [typeof(Vector2)] = VertexAttribType.Float,
        [typeof(Vector3)] = VertexAttribType.Float,
        [typeof(Vector4)] = VertexAttribType.Float,
    };

    static readonly Dictionary<Type, uint> fieldSizes = new()
    {
        [typeof(float)] = (uint)Unsafe.SizeOf<float>(),
        [typeof(Vector2)] = (uint)Unsafe.SizeOf<Vector2>(),
        [typeof(Vector3)] = (uint)Unsafe.SizeOf<Vector3>(),
        [typeof(Vector4)] = (uint)Unsafe.SizeOf<Vector4>(),
    };

    int currentMultiBuffer;
    readonly GLSync[] syncObjects;
    void LockBuffer(int idx)
    {
        ref var sync = ref syncObjects[idx];
        if (sync.Value != IntPtr.Zero)
            GL.DeleteSync(sync);
        sync = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, SyncBehaviorFlags.None);
    }
    void WaitBuffer(int idx)
    {
        ref var sync = ref syncObjects[idx];
        if (sync.Value != IntPtr.Zero)
            while (true)
                if (GL.ClientWaitSync(sync, SyncObjectMask.SyncFlushCommandsBit, 1) is SyncStatus.AlreadySignaled or SyncStatus.ConditionSatisfied)
                    return;
    }

    /// <summary>
    /// Call at the beginning of a new frame, as close to the beginning of the draw calls as possible.
    /// </summary>
    public void BeginDraws() =>
        WaitBuffer(currentMultiBuffer);

    /// <summary>
    /// Call at the end of a new frame, as close to the end of the draw calls as possible.
    /// </summary>
    public void EndDraws()
    {
        LockBuffer(currentMultiBuffer);

        currentMultiBuffer = (currentMultiBuffer + 1) % syncObjects.Length;
        vertices.Offset = vertices.Length * currentMultiBuffer;
        vertices.Clear();
        indices.Offset = indices.Length * currentMultiBuffer;
        indices.Clear();
    }

    static IVertexArrayObject? lastBoundVertexArray;

    /// <summary>
    /// Draws the bound vertex (and index, if available) array(s), optionally starting at an arbitrary point and/or with an arbitrary length. By default, it draws the entire array. 
    /// </summary>
    /// <param name="primitiveType">The primitive type of the draw call.</param>
    /// <param name="vertexOrIndexOffset">The offset into the vertex or index buffers, in element units. Ie, <c>1</c> is the second element, regardless of vertex or index size. By default, it starts at the beginning of the buffer.</param>
    /// <param name="vertexOrIndexCount">The element count to draw. <c>1</c> is one element, regardless of vertex or index size. By default, it draws the entire array, taking into account the offset parameter.</param>
    internal void Draw(PrimitiveType primitiveType, int vertexOrIndexOffset = 0, int vertexOrIndexCount = -1)
    {
        if (lastBoundVertexArray != this)
        {
            GL.BindVertexArray(vertexArrayHandle);
            lastBoundVertexArray = this;
        }

        if (HasIndexBuffer)
            GL.DrawElements(primitiveType, vertexOrIndexCount >= 0 ? vertexOrIndexCount : Indices.Length - vertexOrIndexOffset,
                drawElementsType, (vertexOrIndexOffset + indices.Length * currentMultiBuffer) * Unsafe.SizeOf<TIndex>());
        else
            GL.DrawArrays(primitiveType, vertexOrIndexOffset + vertices.Length * currentMultiBuffer,
                vertexOrIndexCount >= 0 ? vertexOrIndexCount : Vertices.Length - vertexOrIndexOffset);
    }

    readonly BufferHandle vertexBufferHandle, indexBufferHandle;
    readonly VertexArrayHandle vertexArrayHandle;
    readonly DrawElementsType drawElementsType;

    RefArray<TVertex> vertices;
    public ref RefArray<TVertex> Vertices => ref vertices;

    RefArray<TIndex> indices;
    public ref RefArray<TIndex> Indices => ref indices;

    public bool HasIndexBuffer { get; }
    public int VertexCapacity { get; private set; }
    public int IndexCapacity { get; private set; }
}

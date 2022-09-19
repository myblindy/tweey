namespace Twee.Renderer.VertexArrayObjects;

public class StaticVertexArrayObject<TVertex> : BaseVertexArrayObject where TVertex : unmanaged
{
    readonly BufferHandle bufferHandle;
    readonly VertexArrayHandle vaHandle;
    readonly int vertexCount;

    public unsafe StaticVertexArrayObject(TVertex[] vertices)
    {
        vertexCount = vertices.Length;

        bufferHandle = GL.CreateBuffer();

        fixed (void* p = vertices)
            GL.NamedBufferData(bufferHandle, Unsafe.SizeOf<TVertex>() * vertices.Length, p, VertexBufferObjectUsage.StaticDraw);

        vaHandle = GL.CreateVertexArray();
        GL.VertexArrayVertexBuffer(vaHandle, 0, bufferHandle, IntPtr.Zero, Unsafe.SizeOf<TVertex>());

        VertexDefinitionSetup?.Invoke(typeof(TVertex), vaHandle);
    }

    public override void Draw(PrimitiveType primitiveType, int vertexOrIndexOffset = 0, int vertexOrIndexCount = -1)
    {
        if (LastBoundVertexArray != this)
        {
            GL.BindVertexArray(vaHandle);
            LastBoundVertexArray = this;
        }

        var count = vertexCount - vertexOrIndexOffset;
        GL.DrawArrays(primitiveType, vertexOrIndexOffset, count);
        AddFrameData(primitiveType, (ulong)count);
    }
}

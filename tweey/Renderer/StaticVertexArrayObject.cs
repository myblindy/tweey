namespace Tweey.Renderer;

class StaticVertexArrayObject<TVertex> : BaseVertexArrayObject where TVertex : unmanaged
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

        VertexDefinitionSetup.Setup(typeof(TVertex), vaHandle);
    }

    public override void Draw(PrimitiveType primitiveType, int vertexOrIndexOffset = 0, int vertexOrIndexCount = -1)
    {
        if (lastBoundVertexArray != this)
        {
            GL.BindVertexArray(vaHandle);
            lastBoundVertexArray = this;
        }

        GL.DrawArrays(primitiveType, vertexOrIndexOffset, vertexCount);
    }

}

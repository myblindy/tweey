namespace Twee.Renderer;

public class UniformBufferObject<T> where T : unmanaged
{
    public BufferHandle Handle { get; }

    T data;
    public ref T Data => ref data;

    public UniformBufferObject()
    {
        Handle = GL.CreateBuffer();
        GL.NamedBufferData(Handle, Unsafe.SizeOf<T>(), IntPtr.Zero, VertexBufferObjectUsage.DynamicDraw);
    }

    public void UploadData() =>
        GL.NamedBufferSubData(Handle, IntPtr.Zero, Unsafe.SizeOf<T>(), data);

    public void Bind(uint bindingPoint) =>
        GL.BindBufferBase(BufferTargetARB.UniformBuffer, bindingPoint, Handle);
}

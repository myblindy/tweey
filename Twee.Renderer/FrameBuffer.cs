using Twee.Renderer.Textures;

namespace Twee.Renderer;

public class FrameBuffer : IDisposable
{
    readonly FramebufferHandle handle;

    public FrameBuffer(IEnumerable<Texture2D> colorAttachments)
    {
        handle = GL.CreateFramebuffer();

        var fba = FramebufferAttachment.ColorAttachment0;
        foreach (var colorAttachment in colorAttachments)
            GL.NamedFramebufferTexture(handle, fba++, colorAttachment.Handle, 0);

        if (GL.CheckNamedFramebufferStatus(handle, FramebufferTarget.Framebuffer) is { } status && status is not FramebufferStatus.FramebufferComplete)
            throw new InvalidOperationException($"Invalid framebuffer status: {status}");
    }

    public void Bind(FramebufferTarget framebufferTarget) =>
        GL.BindFramebuffer(framebufferTarget, handle);

    private bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // managed
            }

            // unmanaged
            GL.DeleteFramebuffer(handle);

            disposedValue = true;
        }
    }

    ~FrameBuffer()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

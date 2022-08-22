namespace Tweey.Renderer;

public class Texture2D : IDisposable
{
    public TextureHandle Handle { get; }

    public Texture2D(int width, int height, SizedInternalFormat sizedInternalFormat)
    {
        Handle = GL.CreateTexture(TextureTarget.Texture2d);
        GL.TextureStorage2D(Handle, 1, sizedInternalFormat, width, height);

        GL.TextureParameteri(Handle, TextureParameterName.TextureWrapS, (int)All.ClampToEdge);
        GL.TextureParameteri(Handle, TextureParameterName.TextureWrapT, (int)All.ClampToEdge);

        GL.TextureParameteri(Handle, TextureParameterName.TextureMinFilter, (int)All.Linear);
        GL.TextureParameteri(Handle, TextureParameterName.TextureMagFilter, (int)All.Linear);
    }

    public void Bind() =>
        GL.BindTexture(TextureTarget.Texture2d, Handle);

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
            GL.DeleteTexture(Handle);

            disposedValue = true;
        }
    }

    ~Texture2D()
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

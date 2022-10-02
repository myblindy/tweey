namespace Twee.Renderer.Textures;

public class Texture2D : BaseTexture, IDisposable
{
    public TextureHandle Handle { get; private set; }

    public Texture2D(int width, int height, SizedInternalFormat sizedInternalFormat,
        TextureWrapMode wrapS = TextureWrapMode.ClampToEdge, TextureWrapMode wrapT = TextureWrapMode.ClampToEdge,
        TextureMinFilter minFilter = TextureMinFilter.Linear, TextureMagFilter magFilter = TextureMagFilter.Linear)
    {
        InitializeTextureData(width, height, sizedInternalFormat, wrapS, wrapT, minFilter, magFilter);
    }

    private void InitializeTextureData(int width, int height, SizedInternalFormat sizedInternalFormat,
        TextureWrapMode wrapS = TextureWrapMode.ClampToEdge, TextureWrapMode wrapT = TextureWrapMode.ClampToEdge,
        TextureMinFilter minFilter = TextureMinFilter.Linear, TextureMagFilter magFilter = TextureMagFilter.Linear)
    {
        Handle = GL.CreateTexture(TextureTarget.Texture2d);
        GL.TextureStorage2D(Handle, 1, sizedInternalFormat, width, height);

        GL.TextureParameteri(Handle, TextureParameterName.TextureWrapS, (int)wrapS);
        GL.TextureParameteri(Handle, TextureParameterName.TextureWrapT, (int)wrapT);

        GL.TextureParameteri(Handle, TextureParameterName.TextureMinFilter, (int)minFilter);
        GL.TextureParameteri(Handle, TextureParameterName.TextureMagFilter, (int)magFilter);
    }

    public Texture2D(Stream stream, SizedInternalFormat sizedInternalFormat,
        TextureWrapMode wrapS = TextureWrapMode.ClampToEdge, TextureWrapMode wrapT = TextureWrapMode.ClampToEdge,
        TextureMinFilter minFilter = TextureMinFilter.Linear, TextureMagFilter magFilter = TextureMagFilter.Linear,
        bool generateMipMaps = false)
    {
        using var bmp = new Bitmap(stream);

        var bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        try
        {
            InitializeTextureData(bmpData.Width, bmpData.Height, sizedInternalFormat, wrapS, wrapT, minFilter, magFilter);

            GL.PixelStorei(PixelStoreParameter.UnpackAlignment, param: sizedInternalFormat switch
            {
                SizedInternalFormat.R8 => 8,
                _ => 1
            });
            GL.PixelStorei(PixelStoreParameter.UnpackRowLength, 0);
            GL.TextureSubImage2D(Handle, 0, 0, 0, bmpData.Width, bmpData.Height,
                PixelFormat.Rgba, PixelType.UnsignedByte, bmpData.Scan0);
            if (generateMipMaps)
                GL.GenerateTextureMipmap(Handle);
        }
        finally { bmp.UnlockBits(bmpData); }
    }

    public override void Bind(int unit = 0)
    {
        if (LastBoundTexture[unit] != this)
        {
            GL.ActiveTexture((TextureUnit)((int)TextureUnit.Texture0 + unit));
            GL.BindTexture(TextureTarget.Texture2d, Handle);
            LastBoundTexture[unit] = this;
        }
    }

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

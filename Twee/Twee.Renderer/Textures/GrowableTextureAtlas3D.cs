﻿namespace Twee.Renderer.Textures;

public record AtlasEntry(Vector3 TextureCoordinate0, Vector3 TextureCoordinate1, Vector2i PixelSize);

public class GrowableTextureAtlas3D : BaseTexture
{
    readonly TextureHandle handle;

    public const string BlankName = "::blank";

    public GrowableTextureAtlas3D(int width, int height, int initialPages)
    {
        handle = GL.CreateTexture(TextureTarget.Texture2dArray);
        GL.TextureStorage3D(handle, 1, SizedInternalFormat.Rgba8, width, height, pages = initialPages);

        GL.TextureParameteri(handle, TextureParameterName.TextureWrapS, (int)All.ClampToEdge);
        GL.TextureParameteri(handle, TextureParameterName.TextureWrapT, (int)All.ClampToEdge);
        GL.TextureParameteri(handle, TextureParameterName.TextureWrapR, (int)All.ClampToEdge);

        GL.TextureParameteri(handle, TextureParameterName.TextureMinFilter, (int)All.Linear);
        GL.TextureParameteri(handle, TextureParameterName.TextureMagFilter, (int)All.Linear);

        size = new(width, height);
        used.Add(new(width * height));
    }

    public override void Bind(int unit = 0)
    {
        if (LastBoundTexture[unit] != this)
        {
            GL.ActiveTexture((TextureUnit)((int)TextureUnit.Texture0 + unit));
            GL.BindTexture(TextureTarget.Texture2dArray, handle);
            LastBoundTexture[unit] = this;
        }
    }

    (int x, int y, int z) FindAndMarkSpace(Vector2i entrySize)
    {
        bool found;
        var page = 0;
        foreach (var usedPage in used)
        {
            for (var y = 0; y < size.Y - entrySize.Y + 1; ++y)
                for (var x = 0; x < size.X - entrySize.X + 1; ++x)
                {
                    found = true;
                    for (var dy = 0; dy < entrySize.Y; ++dy)
                        for (var dx = 0; dx < entrySize.X; ++dx)
                            if (usedPage[(y + dy) * size.Y + dx + x])
                            {
                                found = false;
                                goto doneWithSubCheck;
                            }

                    doneWithSubCheck:
                    if (found)
                    {
                        // mark the space as used
                        for (var dy = 0; dy < entrySize.Y; ++dy)
                            for (var dx = 0; dx < entrySize.X; ++dx)
                                usedPage[(y + dy) * size.Y + dx + x] = true;

                        return new(x, y, page);
                    }
                }
            ++page;
        }

        // expand the texture, we ran out of space
        throw new NotImplementedException();
    }

    public unsafe AtlasEntry AddFromImage(Bitmap image, int width, int height, Action<Vector2i> writeAction)
    {
        var (x, y, page) = FindAndMarkSpace(new(width, height));
        writeAction(new(x, y));

        BitmapData? bmpData = null;
        try
        {
            bmpData = image.LockBits(new(0, 0, width, height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.PixelStorei(PixelStoreParameter.UnpackAlignment, 1);
            GL.PixelStorei(PixelStoreParameter.UnpackRowLength, image.Width);
            GL.TextureSubImage3D(handle, 0, x, y, page, width, height, 1, PixelFormat.Bgra, PixelType.UnsignedByte, bmpData.Scan0);

            var max = new Vector3(size.X - 1, size.Y - 1, pages - 1);
            return new(new Vector3(x, y, page) / max, new Vector3(x + width - 1, y + height - 1, page) / max, new(width, height));
        }
        finally
        {
            if (bmpData is { })
                image.UnlockBits(bmpData);
        }
    }

    public unsafe AtlasEntry this[string path]
    {
        get
        {
            if (map.TryGetValue(path, out var entry))
                return entry;

            int width, height;
            int x, y, page;
            if (path is BlankName)
            {
                var white = uint.MaxValue;
                (width, height) = (1, 1);

                (x, y, page) = FindAndMarkSpace(new(3, 3));
                GL.PixelStorei(PixelStoreParameter.UnpackAlignment, 1);
                GL.PixelStorei(PixelStoreParameter.UnpackRowLength, 0);
                fixed (uint* p = new[] { white, white, white, white, white, white, white, white, white })
                    GL.TextureSubImage3D(handle, 0, x, y, page, 3, 3, 1, PixelFormat.Bgra, PixelType.UnsignedByte, p);
                x += 1;
                y += 1;
            }
            else
            {
                using var image = new Bitmap(path);
                (width, height) = (image.Width, image.Height);

                BitmapData? bmpData = null;
                try
                {
                    bmpData = image.LockBits(new(0, 0, width, height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                    (x, y, page) = FindAndMarkSpace(new(width, height));
                    GL.PixelStorei(PixelStoreParameter.UnpackAlignment, 1);
                    GL.PixelStorei(PixelStoreParameter.UnpackRowLength, 0);
                    GL.TextureSubImage3D(handle, 0, x, y, page, width, height, 1, PixelFormat.Bgra, PixelType.UnsignedByte, bmpData.Scan0);
                }
                finally
                {
                    if (bmpData is { })
                        image.UnlockBits(bmpData);
                }
            }

            var max = new Vector3(size.X - 1, size.Y - 1, pages - 1);
            return map[path] = new(new Vector3(x, y, page) / max, new Vector3(x + width - 1, y + height - 1, page) / max, new(width, height));
        }
    }

    readonly List<BitArray> used = new();
    readonly Dictionary<string, AtlasEntry> map = new();
    readonly Vector2i size;
    int pages;
}
namespace Tweey.Renderer;

public record AtlasEntry(Vector3 TextureCoordinate0, Vector3 TextureCoordinate1);

public class GrowableTextureAtlas3D
{
    readonly uint name;

    public const string BlankName = "::blank";

    public GrowableTextureAtlas3D(int width, int height, int initialPages)
    {
        GL.CreateTextures(TextureTarget.Texture2DArray, pages = initialPages, out name);
        GL.TextureStorage3D(name, 1, SizedInternalFormat.Rgba8, width, height, initialPages);

        int value = (int)All.ClampToEdge;
        GL.TextureParameterI(name, TextureParameterName.TextureWrapS, ref value);
        GL.TextureParameterI(name, TextureParameterName.TextureWrapT, ref value);
        GL.TextureParameterI(name, TextureParameterName.TextureWrapR, ref value);

        value = (int)All.Linear;
        GL.TextureParameterI(name, TextureParameterName.TextureMinFilter, ref value);
        GL.TextureParameterI(name, TextureParameterName.TextureMagFilter, ref value);

        size = new(width, height);
        used.Add(new(width * height));
    }

    static GrowableTextureAtlas3D? LastBoundTexture;
    public void Bind()
    {
        if (LastBoundTexture != this)
        {
            GL.BindTexture(TextureTarget.Texture2DArray, name);
            LastBoundTexture = this;
        }
    }

    (int x, int y, int z) FindAndMarkSpace(Vector2i entrySize)
    {
        bool found;
        int page = 0;
        foreach (var usedPage in used)
        {
            for (int y = 0; y < size.Y - entrySize.Y + 1; ++y)
                for (int x = 0; x < size.X - entrySize.X + 1; ++x)
                {
                    found = true;
                    for (int dy = 0; dy < entrySize.Y; ++dy)
                        for (int dx = 0; dx < entrySize.X; ++dx)
                            if (usedPage[(y + dy) * size.Y + (dx + x)])
                            {
                                found = false;
                                goto doneWithSubCheck;
                            }

                    doneWithSubCheck:
                    if (found)
                    {
                        // mark the space as used
                        for (int dy = 0; dy < entrySize.Y; ++dy)
                            for (int dx = 0; dx < entrySize.X; ++dx)
                                usedPage[(y + dy) * size.Y + (dx + x)] = true;

                        return new(x, y, page);
                    }
                }
            ++page;
        }

        // expand the texture, we ran out of space
        throw new NotImplementedException();
    }

    public unsafe AtlasEntry AddFromImage(Image<Bgra32> image, int width, int height, Action<Vector2i> writeAction)
    {
        var (x, y, page) = FindAndMarkSpace(new(width, height));
        writeAction(new(x, y));
        if (!image.TryGetSinglePixelSpan(out var imageBytes)) throw new NotImplementedException();

        GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
        GL.PixelStore(PixelStoreParameter.UnpackRowLength, image.Width);
        GL.TextureSubImage3D(name, 0, x, y, page, width, height, 1, PixelFormat.Bgra, PixelType.UnsignedByte, ref imageBytes[0]);

        var max = new Vector3(size.X - 1, size.Y - 1, pages - 1);
        return new(new Vector3(x, y, page) / max, new Vector3(x + width - 1, y + height - 1, page) / max);
    }

    public unsafe AtlasEntry this[string path]
    {
        get
        {
            if (map.TryGetValue(path, out var entry))
                return entry;

            Span<Bgra32> imageBytes;
            int width, height;

            if (path is BlankName)
            {
                imageBytes = new(new Bgra32[] { new(255, 255, 255) });
                (width, height) = (1, 1);
            }
            else
            {
                var image = Image.Load<Bgra32>(path);
                (width, height) = (image.Width, image.Height);
                if (!image.TryGetSinglePixelSpan(out imageBytes)) throw new NotImplementedException();
            }

            var (x, y, page) = FindAndMarkSpace(new(width, height));
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
            GL.PixelStore(PixelStoreParameter.UnpackRowLength, 0);
            GL.TextureSubImage3D(name, 0, x, y, page, width, height, 1, PixelFormat.Bgra, PixelType.UnsignedByte, ref imageBytes[0]);

            var max = new Vector3(size.X - 1, size.Y - 1, pages - 1);
            return map[path] = new(new Vector3(x, y, page) / max, new Vector3(x + width - 1, y + height - 1, page) / max);
        }
    }

    readonly List<BitArray> used = new();
    readonly Dictionary<string, AtlasEntry> map = new();
    readonly Vector2i size;
    int pages;
}

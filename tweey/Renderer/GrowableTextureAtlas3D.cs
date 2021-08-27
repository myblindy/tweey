using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace Tweey.Renderer
{
    record AtlasEntry(Vector3 TextureCoordinate0, Vector3 TextureCoordinate1);

    class GrowableTextureAtlas3D: ITexture
    {
        public GrowableTextureAtlas3D(int width, int height, int initialPages)
        {
            GL.CreateTextures(TextureTarget.Texture3D, pages = initialPages, out name);
            GL.TextureStorage2D(name, 1, SizedInternalFormat.Rgba8, width, height);

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
                GL.BindTexture(TextureTarget.Texture3D, name);
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
                                if (usedPage[dy * size.Y + dx])
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
                                    usedPage[dy * size.Y + dx] = true;

                            return new(x, y, page);
                        }
                    }
                ++page;
            }

            // expand the texture, we ran out of space
            throw new NotImplementedException();
        }

        public unsafe AtlasEntry AddImage(string path)
        {
            var image = Image.Load<Bgra32>(path);
            var (x, y, page) = FindAndMarkSpace(new(image.Width, image.Height));
            if (!image.TryGetSinglePixelSpan(out var imageBytes)) throw new NotImplementedException();
            GL.TextureSubImage3D(name, 0, x, y, page, image.Width, image.Height, 1, PixelFormat.Rgba, PixelType.UnsignedByte, ref imageBytes[0]);

            var max = new Vector3(size.X - 1, size.Y - 1, pages - 1);
            return new(new Vector3(x, y, page) / max, new Vector3(x + image.Width, y + image.Height, page) / max);
        }

        readonly List<BitArray> used = new();
        readonly Vector2i size;
        int pages;
        readonly int name;
    }
}

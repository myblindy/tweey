using Tweey.Renderer.Textures;

namespace Tweey.Renderer;

public struct FontDescription : IEquatable<FontDescription>
{
    public float Size { get; init; }
    public bool Bold { get; init; }
    public bool Italic { get; init; }

    public bool Equals(FontDescription other) => Size == other.Size && Bold == other.Bold && Italic == other.Italic;
    public override bool Equals(object? obj) => obj is FontDescription description && Equals(description);
    public override int GetHashCode() => HashCode.Combine(Size, Bold, Italic);

    public static bool operator ==(FontDescription left, FontDescription right) => left.Equals(right);
    public static bool operator !=(FontDescription left, FontDescription right) => !(left == right);
}

public enum HorizontalAlignment { Left, Center, Right }
public enum VerticalAlignment { Top, Center, Bottom }

public class FontRenderer : IDisposable
{
    readonly GrowableTextureAtlas3D backingTextureAtlas;
    readonly PrivateFontCollection fontCollection;
    readonly FontFamily regularFontFamily;

    readonly Dictionary<FontDescription, Font> fonts = new();
    readonly Dictionary<(FontDescription fontDescription, char ch), (AtlasEntry entry, Vector2i pixelSize)> fontAtlasEntries = new();

    Bitmap tempImage = new(60, 60, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
    static readonly StringFormat stringFormat;
    bool disposedValue;

    [SuppressMessage("Performance", "CA1810:Initialize reference type static fields inline", Justification = "Complex initialization")]
    static FontRenderer()
    {
        stringFormat = StringFormat.GenericTypographic;
        stringFormat.FormatFlags &= ~StringFormatFlags.LineLimit;
    }

    public FontRenderer(GrowableTextureAtlas3D backingTextureAtlas)
    {
        this.backingTextureAtlas = backingTextureAtlas;
        fontCollection = new();
        fontCollection.AddFontFile("Data/Fonts/OpenSans-Regular.ttf");
        regularFontFamily = fontCollection.Families[0];
    }

    (FontFamily, FontStyle) GetFontFamily(FontDescription fontDescription) => (fontDescription.Bold, fontDescription.Italic) switch
    {
        (false, false) => (regularFontFamily, FontStyle.Regular),
        _ => throw new NotImplementedException()
    };

    [MemberNotNull(nameof(tempImage))]
    void EnsureTempImage(int width, int height)
    {
        if (tempImage is null || tempImage.Width < width || tempImage.Height < height)
        {
            tempImage?.Dispose();
            tempImage = new Bitmap(width + 20, height + 20, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        }
    }

    public Vector2 Measure(char ch, FontDescription fontDescription) =>
        GetFullAtlasEntry(ch, fontDescription).pixelSize.ToNumericsVector2();

    private (AtlasEntry entry, Vector2i pixelSize) GetFullAtlasEntry(char ch, FontDescription fontDescription)
    {
        if (!fontAtlasEntries.TryGetValue((fontDescription, ch), out var fullAtlasEntry))
        {
            // get the font
            if (!fonts.TryGetValue(fontDescription, out var font))
            {
                var (fontfamily, fontStyle) = GetFontFamily(fontDescription);
                fonts[fontDescription] = font = new(fontfamily, fontDescription.Size, GraphicsUnit.Pixel);
            }

            // measure the character
            var s = ch.ToString();

            Box2 chBox;
            using (var g = Graphics.FromImage(tempImage))
            {
                g.PageUnit = GraphicsUnit.Pixel;
                SizeF chSize;

                if (s is " ")
                {
                    // gdi+ can't measure spaces, so I'm going to use a work around
                    var tmpSize = g.MeasureString("a a", font, 0, stringFormat);
                    chSize = g.MeasureString("a", font, 0, stringFormat);
                    chSize = new(tmpSize.Width - chSize.Width * 2, chSize.Height);

                }
                else
                    chSize = g.MeasureString(s, font, 0, stringFormat);
                chBox = Box2.FromCornerSize(new Vector2(), new(chSize.Width, chSize.Height + 3));
            }

            // draw the character
            var chSizei = new Vector2i((int)MathF.Ceiling(chBox.Size.X), (int)MathF.Ceiling(chBox.Size.Y));
            EnsureTempImage(chSizei.X, chSizei.Y);
            fontAtlasEntries[(fontDescription, ch)] = fullAtlasEntry = (backingTextureAtlas.AddFromImage(tempImage, chSizei.X, chSizei.Y, atlasPosition =>
            {
                using var g = Graphics.FromImage(tempImage);
                g.Clear(Color.Transparent);
                g.TextRenderingHint = fontDescription.Size <= 13 ? TextRenderingHint.SingleBitPerPixelGridFit : TextRenderingHint.ClearTypeGridFit;
                g.PageUnit = GraphicsUnit.Pixel;
                g.DrawString(s, font, Brushes.White, chBox.Left, chBox.Top, stringFormat);
            }), chBox.Size.ToVector2i());
        }

        return fullAtlasEntry;
    }

    public Vector2 Measure(ReadOnlySpan<char> s, FontDescription fontDescription)
    {
        Vector2 lastLineSize = default, totalSize = default;

        foreach (var ch in s)
            if (ch is '\n')
            {
                totalSize = new(Math.Max(totalSize.X, lastLineSize.X), totalSize.Y + lastLineSize.Y);
                lastLineSize = default;
            }
            else if (ch is not '\r')
            {
                var charSize = Measure(ch, fontDescription);
                lastLineSize.X += charSize.X;
                lastLineSize.Y = Math.Max(lastLineSize.Y, charSize.Y);
            }

        return new(Math.Max(totalSize.X, lastLineSize.X), totalSize.Y + lastLineSize.Y);
    }

    static readonly List<(char ch, AtlasEntry? entry, Vector2i pixelSize)> renderCache = new();
    public void Render(ReadOnlySpan<char> s, FontDescription fontDescription, Vector2i position, Action<Box2>? measure, Action<Box2, AtlasEntry> write,
        HorizontalAlignment horizontalAlignment = HorizontalAlignment.Center, VerticalAlignment verticalAlignment = VerticalAlignment.Center)
    {
        renderCache.Clear();
        Vector2i lastLineSize = default, totalSize = default;

        foreach (var ch in s)
            if (ch is '\n')
            {
                totalSize = new(Math.Max(totalSize.X, lastLineSize.X), totalSize.Y + lastLineSize.Y);
                lastLineSize = default;
                renderCache.Add((ch, default, default));
            }
            else if (ch is not '\r')
            {
                var (entry, pixelSize) = GetFullAtlasEntry(ch, fontDescription);
                lastLineSize.X += pixelSize.X;
                lastLineSize.Y = Math.Max(lastLineSize.Y, pixelSize.Y);
                renderCache.Add((ch, entry, pixelSize));
            }
        totalSize = new(Math.Max(totalSize.X, lastLineSize.X), totalSize.Y + lastLineSize.Y);

        if (horizontalAlignment is HorizontalAlignment.Right)
            position = new(position.X - totalSize.X, position.Y);
        else if (horizontalAlignment is HorizontalAlignment.Center)
            position = new(position.X - totalSize.X / 2, position.Y);

        measure?.Invoke(Box2.FromCornerSize(position, totalSize));

        var startX = position.X;
        var maxLineHeight = 0;
        foreach (var (ch, entry, pixelSize) in renderCache)
            if (ch is '\n')
            {
                position = new(startX, position.Y + maxLineHeight);
                maxLineHeight = 0;
            }
            else if (ch is not '\r')
            {
                var chBox = Box2.FromCornerSize(position, pixelSize);
                write(chBox, entry!);
                position.X += pixelSize.X;
                maxLineHeight = Math.Max(maxLineHeight, pixelSize.Y);
            }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // managed state
            }

            // unmanaged state
            tempImage?.Dispose();
            fontCollection.Dispose();

            disposedValue = true;
        }
    }

    ~FontRenderer()
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

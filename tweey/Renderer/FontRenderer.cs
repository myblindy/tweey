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

public class FontRenderer : IDisposable
{
    readonly GrowableTextureAtlas3D backingTextureAtlas;
    readonly FontCollection fontCollection;
    readonly FontFamily regularFontFamily;

    readonly Dictionary<FontDescription, TextOptions> fonts = new();
    readonly Dictionary<(FontDescription fontDescription, char ch), (AtlasEntry entry, Vector2i pixelSize)> fontAtlasEntries = new();

    Image<Bgra32>? tempImage;
    bool disposedValue;

    public FontRenderer(GrowableTextureAtlas3D backingTextureAtlas)
    {
        this.backingTextureAtlas = backingTextureAtlas;
        fontCollection = new();
        regularFontFamily = fontCollection.Add("Data/Fonts/OpenSans-Regular.woff2");
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
            tempImage = new Image<Bgra32>(width + 20, height + 20);
        }
    }

    public Vector2 Measure(char ch, FontDescription fontDescription) =>
        GetFullAtlasEntry(ch, fontDescription).pixelSize.ToNumericsVector2();

    private (AtlasEntry entry, Vector2i pixelSize) GetFullAtlasEntry(char ch, FontDescription fontDescription)
    {
        if (!fontAtlasEntries.TryGetValue((fontDescription, ch), out var fullAtlasEntry))
        {
            // get the font
            if (!fonts.TryGetValue(fontDescription, out var textOptions))
            {
                var (fontfamily, fontStyle) = GetFontFamily(fontDescription);
                fonts[fontDescription] = textOptions = new(fontfamily.CreateFont(fontDescription.Size, fontStyle))
                {
                    HintingMode = HintingMode.None,
                    KerningMode = KerningMode.Normal,
                    ColorFontSupport = ColorFontSupport.MicrosoftColrFormat
                };
            }

            // measure the character
            var s = ch.ToString();
            var fontRect = TextMeasurer.Measure(s, textOptions);
            var (width, height) = ((int)MathF.Ceiling(fontRect.Right), (int)MathF.Ceiling(fontRect.Bottom) + 3);

            // draw the character
            textOptions.Origin = new(fontRect.Left, fontRect.Top);
            EnsureTempImage(width + (int)MathF.Floor(fontRect.Left), height + (int)MathF.Floor(fontRect.Top));
            fontAtlasEntries[(fontDescription, ch)] = fullAtlasEntry = (backingTextureAtlas.AddFromImage(tempImage, width, height, atlasPosition =>
                tempImage.Mutate(ctx => ctx
                    .Clear(Color.Transparent)
                    .DrawText(textOptions, s, Color.White))), new(width, height));
        }

        return fullAtlasEntry;
    }

    public Vector2 Measure(ReadOnlySpan<char> s, FontDescription fontDescription)
    {
        Vector2 result = default;
        foreach (var ch in s)
        {
            var charSize = Measure(ch, fontDescription);
            result = new(result.X + charSize.X, Math.Max(result.Y, charSize.Y));
        }

        return result;
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
            tempImage = null;

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

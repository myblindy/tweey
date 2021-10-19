﻿namespace Tweey.Renderer;

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

public class FontRenderer
{
    readonly GrowableTextureAtlas3D backingTextureAtlas;
    readonly FontCollection fontCollection;
    readonly FontFamily regularFontFamily;

    readonly Dictionary<FontDescription, Font> fonts = new();
    readonly Dictionary<(FontDescription fontDescription, char ch), (AtlasEntry entry, Vector2i pixelSize)> fontAtlasEntries = new();

    Image<Bgra32>? tempImage;

    public FontRenderer(GrowableTextureAtlas3D backingTextureAtlas)
    {
        this.backingTextureAtlas = backingTextureAtlas;
        fontCollection = new();
        regularFontFamily = fontCollection.Install("Data/Fonts/OpenSans-Regular.ttf");
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

    static readonly DrawingOptions drawingOptions = new() { TextOptions = { ApplyKerning = true, RenderColorFonts = true } };

    public Vector2 Measure(char ch, FontDescription fontDescription) =>
        GetFullAtlasEntry(ch, fontDescription).pixelSize.ToNumericsVector2();

    public void Render(char ch, FontDescription fontDescription, Vector2i position, Action<Box2, AtlasEntry> write,
        HorizontalAlignment horizontalAlignment = HorizontalAlignment.Center, VerticalAlignment verticalAlignment = VerticalAlignment.Center)
    {
        var (entry, pixelSize) = GetFullAtlasEntry(ch, fontDescription);
        write(horizontalAlignment switch
        {
            HorizontalAlignment.Left => Box2.FromCornerSize(position.ToNumericsVector2(), pixelSize.ToNumericsVector2()),
            HorizontalAlignment.Right => Box2.FromCornerSize(new(position.X - pixelSize.X, position.Y), pixelSize.ToNumericsVector2()),
            _ => throw new NotImplementedException()
        }, entry);
    }

    private (AtlasEntry entry, Vector2i pixelSize) GetFullAtlasEntry(char ch, FontDescription fontDescription)
    {
        if (!fontAtlasEntries.TryGetValue((fontDescription, ch), out var fullAtlasEntry))
        {
            // get the font
            if (!fonts.TryGetValue(fontDescription, out var font))
            {
                var (fontfamily, fontStyle) = GetFontFamily(fontDescription);
                fonts[fontDescription] = font = fontfamily.CreateFont(fontDescription.Size, fontStyle);
            }

            // measure the character
            var s = ch.ToString();
            var renderOptions = new RendererOptions(font) { ApplyKerning = true, ColorFontSupport = ColorFontSupport.MicrosoftColrFormat };
            var fontRect = TextMeasurer.Measure(s, renderOptions);
            var (width, height) = ((int)MathF.Ceiling(fontRect.Right), (int)MathF.Ceiling(fontRect.Bottom) + 3);

            // draw the character
            EnsureTempImage(width + (int)MathF.Floor(fontRect.Left), height + (int)MathF.Floor(fontRect.Top));
            fontAtlasEntries[(fontDescription, ch)] = fullAtlasEntry = (backingTextureAtlas.AddFromImage(tempImage, width, height, atlasPosition =>
                tempImage.Mutate(ctx => ctx
                    .Clear(Color.Transparent)
                    .DrawText(drawingOptions, s, font, Color.White, new(fontRect.Left, fontRect.Top)))), new(width, height));
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

    public void Render(ReadOnlySpan<char> s, FontDescription fontDescription, Vector2i position, Action<Box2, AtlasEntry> write,
        HorizontalAlignment horizontalAlignment = HorizontalAlignment.Center, VerticalAlignment verticalAlignment = VerticalAlignment.Center)
    {
        switch (horizontalAlignment)
        {
            case HorizontalAlignment.Left:
                foreach (var ch in s)
                    Render(ch, fontDescription, position, (box, atlasEntry) =>
                    {
                        write(box, atlasEntry);
                        position += new Vector2i((int)MathF.Ceiling(box.Size.X), 0);
                    }, horizontalAlignment, verticalAlignment);
                break;
            case HorizontalAlignment.Right:
                for (int idx = s.Length - 1; idx >= 0; --idx)
                    Render(s[idx], fontDescription, position, (box, atlasEntry) =>
                    {
                        write(box, atlasEntry);
                        position -= new Vector2i((int)MathF.Ceiling(box.Size.X), 0);
                    }, horizontalAlignment, verticalAlignment);
                break;
        }
    }
}

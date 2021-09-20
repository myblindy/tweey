﻿namespace Tweey.Renderer;

partial class WorldRenderer
{
    int WidthPercentage(float p) => (int)(windowUbo.Data.WindowSize.X * p / 100f);
    int HeightPercentage(float p) => (int)(windowUbo.Data.WindowSize.Y * p / 100f);

    void ScreenFillQuad(Box2 box, Vector4 color, AtlasEntry entry, bool useScale = true)
    {
        var zoom = useScale ? pixelZoom : 1;
        var br = box.BottomRight + Vector2.One;
        vaoGui.Vertices.Add(new(box.TopLeft * zoom, color, entry.TextureCoordinate0));
        vaoGui.Vertices.Add(new(br * zoom, color, entry.TextureCoordinate1));
        vaoGui.Vertices.Add(new(new((box.Right + 1) * zoom, box.Top * zoom), color, new(entry.TextureCoordinate1.X, entry.TextureCoordinate0.Y, entry.TextureCoordinate0.Z)));

        vaoGui.Vertices.Add(new(new(box.Left * zoom, (box.Bottom + 1) * zoom), color, new(entry.TextureCoordinate0.X, entry.TextureCoordinate1.Y, entry.TextureCoordinate0.Z)));
        vaoGui.Vertices.Add(new(br * zoom, color, entry.TextureCoordinate1));
        vaoGui.Vertices.Add(new(box.TopLeft * zoom, color, entry.TextureCoordinate0));
    }

    void ScreenString(string s, FontDescription fontDescription, Vector2 location, Vector4 color, HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left) =>
        fontRenderer.Render(s, fontDescription, location.ToVector2i(), (box, atlasEntry) => ScreenFillQuad(box, color, atlasEntry, false),
            horizontalAlignment);

    void ScreenLine(Box2 box1, Box2 box2, Vector4 color)
    {
        var blankEntry = atlas[GrowableTextureAtlas3D.BlankName];

        vaoGui.Vertices.Add(new((box1.Center + new Vector2(.5f, .5f)) * pixelZoom, color, blankEntry.TextureCoordinate0));
        vaoGui.Vertices.Add(new((box2.Center + new Vector2(.5f, .5f)) * pixelZoom, color, blankEntry.TextureCoordinate1));
    }

    void ScreenLineQuad(Box2 box, Vector4 color, bool useScale = true)
    {
        var blankEntry = atlas[GrowableTextureAtlas3D.BlankName];
        var zoom = useScale ? pixelZoom : 1;
        var br = box.BottomRight + Vector2.One;

        vaoGui.Vertices.Add(new(box.TopLeft * zoom, color, blankEntry.TextureCoordinate0));
        vaoGui.Vertices.Add(new(new((box.Right + 1) * zoom, box.Top * zoom), color, new(blankEntry.TextureCoordinate1.X, blankEntry.TextureCoordinate0.Y, blankEntry.TextureCoordinate0.Z)));
        vaoGui.Vertices.Add(new(new(box.Left * zoom, (box.Bottom + 1) * zoom), color, new(blankEntry.TextureCoordinate0.X, blankEntry.TextureCoordinate1.Y, blankEntry.TextureCoordinate0.Z)));
        vaoGui.Vertices.Add(new(br * zoom, color, blankEntry.TextureCoordinate1));
        vaoGui.Vertices.Add(new(box.TopLeft * zoom, color, blankEntry.TextureCoordinate0));
        vaoGui.Vertices.Add(new(new(box.Left * zoom, (box.Bottom + 1) * zoom), color, new(blankEntry.TextureCoordinate0.X, blankEntry.TextureCoordinate1.Y, blankEntry.TextureCoordinate0.Z)));
        vaoGui.Vertices.Add(new(new((box.Right + 1) * zoom, box.Top * zoom), color, new(blankEntry.TextureCoordinate1.X, blankEntry.TextureCoordinate0.Y, blankEntry.TextureCoordinate0.Z)));
        vaoGui.Vertices.Add(new(br * zoom, color, blankEntry.TextureCoordinate1));
    }

    readonly Dictionary<View, Box2> viewBoxes = new();
    readonly Dictionary<View, View> viewTemplates = new();
    View GetRealView(View view) =>
        viewTemplates.TryGetValue(view, out var templateView) ? templateView : view;

    void TemplateView(View view)
    {
        if (view.Visible is not null && !view.Visible()) return;

        switch (view)
        {
            case IRepeaterView repeaterView:
                TemplateView(viewTemplates[view] = repeaterView.CreateView());
                break;

            case ContainerView containerView:
                containerView.Children.ForEach(TemplateView);
                break;
        }
    }

    Vector2 MeasureView(View view)
    {
        if (view.Visible is not null && !view.Visible()) return new();

        Vector2 size = new(view.Padding.Left + view.Padding.Right + view.Margin.Left + view.Margin.Right,
            view.Padding.Top + view.Padding.Bottom + view.Margin.Top + view.Margin.Bottom);
        switch (view)
        {
            case LabelView labelView:
                if (labelView.Text is not null && labelView.Text() is var text && !string.IsNullOrWhiteSpace(text))
                    size += fontRenderer.Measure(text, new FontDescription { Size = labelView.FontSize });
                break;

            case ImageView imageView:
                if (!imageView.InheritParentSize && imageView.Source is not null && imageView.Source() is var src && !string.IsNullOrWhiteSpace(src))
                {
                    var entry = atlas[src];
                    size = entry.PixelSize.ToNumericsVector2();
                }
                break;

            case StackView stackView:
                foreach (var child in stackView.Children.Select(GetRealView))
                {
                    var childSize = MeasureView(child);

                    size = stackView.Type switch
                    {
                        StackType.Horizontal => new(size.X + childSize.X, Math.Max(size.Y, childSize.Y)),
                        StackType.Vertical => new(Math.Max(size.X, childSize.X), size.Y + childSize.Y),
                        _ => throw new NotImplementedException(),
                    };
                }
                break;

            default:
                throw new NotImplementedException();
        }

        if (view.MinWidth is not null && view.MinWidth() is var minWidth && minWidth > size.X)
            size = new(minWidth, size.Y);
        if (view.MinHeight is not null && view.MinHeight() is var minHeight && minHeight > size.Y)
            size = new(size.X, minHeight);
        return (viewBoxes[view] = Box2.FromCornerSize(new(), size)).Size;
    }

    void LayoutView(View view, Vector2 offset, Vector2 multiplier)
    {
        if (view.Visible is not null && !view.Visible()) return;
        view = viewTemplates.TryGetValue(view, out var templateView) ? templateView : view;

        var boxSize = viewBoxes[view].Size;
        var boxStart = offset + new Vector2(Math.Min(multiplier.X, 0), Math.Min(multiplier.Y, 0)) * boxSize
            + new Vector2(view.Padding.Left + view.Margin.Left, view.Padding.Top + view.Margin.Top);
        viewBoxes[view] = Box2.FromCornerSize(boxStart, boxSize);

        switch (view)
        {
            case StackView stackView:
                // finish the layout for views that inherit their size from us
                foreach (var child in stackView.Children)
                    switch (child)
                    {
                        case ImageView { InheritParentSize: true } imageView when imageView.Source is not null && imageView.Source() is var src && !string.IsNullOrWhiteSpace(src):
                            // need the aspect ratio
                            var entry = atlas[src];
                            var aspect = (float)entry.PixelSize.X / entry.PixelSize.Y;
                            Vector2 newSize = stackView.Type != StackType.Horizontal
                                ? new(boxSize.X, boxSize.X / aspect)
                                : new(boxSize.Y * aspect, boxSize.Y);
                            viewBoxes[child] = Box2.FromCornerSize(new(), newSize);
                            break;
                    }

                var start = boxStart;
                foreach (var child in stackView.Children.Where(v => v.Visible is null || v.Visible()).Select(GetRealView))
                {
                    LayoutView(child, start, new(1, 1));
                    start = stackView.Type == StackType.Horizontal
                        ? new(start.X + viewBoxes[child].Size.X, start.Y)
                        : new(start.X, start.Y + viewBoxes[child].Size.Y);
                }
                break;

            default:
                if (view is ContainerView) throw new NotImplementedException();
                break;
        }
    }

    void RenderView(View view)
    {
        if (view.Visible is not null && !view.Visible()) return;
        view = viewTemplates.TryGetValue(view, out var templateView) ? templateView : view;

        var box = viewBoxes[view];
        if (view.BackgroundColor.W > 0)
            ScreenFillQuad(box.WithExpand(view.Padding), view.BackgroundColor, atlas[GrowableTextureAtlas3D.BlankName], false);

        switch (view)
        {
            case ContainerView containerView:
                foreach (var child in containerView.Children.Select(GetRealView))
                    RenderView(child);
                break;
            case LabelView labelView:
                if (labelView.Text is not null && labelView.Text() is { } text && !string.IsNullOrWhiteSpace(text))
                    ScreenString(text, new() { Size = labelView.FontSize }, labelView.HorizontalTextAlignment switch
                    {
                        HorizontalAlignment.Left => box.TopLeft + new Vector2(view.Margin.Left, view.Margin.Top),
                        HorizontalAlignment.Right => new(box.Right - view.Margin.Right, box.Top - view.Margin.Top),
                        _ => throw new NotImplementedException()
                    }, labelView.ForegroundColor, labelView.HorizontalTextAlignment);
                break;
            case ImageView imageView:
                if (imageView.Source is not null && imageView.Source() is { } src && !string.IsNullOrWhiteSpace(src))
                    ScreenFillQuad(box.WithExpand(-view.Margin), imageView.ForegroundColor, atlas[src], false);
                break;
        }
    }

    void RenderGui()
    {
        viewBoxes.Clear();

        foreach (var rootViewDescription in gui.RootViewDescriptions)
        {
            var view = GetRealView(rootViewDescription.View);
            TemplateView(view);
            MeasureView(view);
            LayoutView(view,
                rootViewDescription.Anchor switch
                {
                    Anchor.TopLeft => new(),
                    Anchor.BottomLeft => new(0, windowUbo.Data.WindowSize.Y),
                    _ => throw new NotImplementedException()
                },
                rootViewDescription.Anchor switch
                {
                    Anchor.TopLeft => new(1, 1),
                    Anchor.BottomLeft => new(1, -1),
                    _ => throw new NotImplementedException()
                });
            RenderView(view);
        }
    }
}
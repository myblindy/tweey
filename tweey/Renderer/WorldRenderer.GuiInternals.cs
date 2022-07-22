using Tweey.Gui;

namespace Tweey.Renderer;

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

    View GetTemplatedView(View view) => view.ViewData.TemplatedView ?? view;

    bool MouseEvent(View view, Vector2i position, InputAction inputAction, MouseButton mouseButton, KeyModifiers keyModifiers)
    {
        if ((view.Visible?.Invoke() ?? true) && GetTemplatedView(view) is { } realView && realView.ViewData.Box.Contains(position))
            switch (realView)
            {
                case IClickable clickable:
                    if (inputAction == InputAction.Press && mouseButton == MouseButton.Left)
                    {
                        clickable.Clicked?.Invoke();
                        return true;
                    }
                    break;
                case IContainerView containerView:
                    foreach (var childView in containerView.Children)
                        if (MouseEvent(childView, position, inputAction, mouseButton, keyModifiers))
                            return true;
                    break;
                case ISingleChildContainerView singleChildContainerView:
                    if (singleChildContainerView.Child is not null && MouseEvent(singleChildContainerView.Child, position, inputAction, mouseButton, keyModifiers))
                        return true;
                    break;
            }

        return false;
    }

    public bool MouseEvent(Vector2i position, InputAction inputAction, MouseButton mouseButton, KeyModifiers keyModifiers)
    {
        // use the old view boxes to calculate hit testing
        foreach (var rootViewDescription in gui.RootViewDescriptions)
            if (MouseEvent(rootViewDescription.View, position, inputAction, mouseButton, keyModifiers))
                return true;

        return false;
    }

    void TemplateView(View view)
    {
        if (view.Visible is not null && !view.Visible()) return;

        switch (view)
        {
            case IRepeaterView repeaterView:
                TemplateView(view.ViewData.TemplatedView = repeaterView.CreateView());
                break;

            case IContainerView containerView:
                containerView.Children.ForEach(TemplateView);
                break;

            case ISingleChildContainerView { Child: not null } singleChildContainerView:
                TemplateView(singleChildContainerView.Child);
                break;
        }
    }

    static Vector2 ConstrainSize(View view, Vector2 size)
    {
        if (view.MinWidth is not null && view.MinWidth() is var minWidth && minWidth > size.X)
            size = new(minWidth, size.Y);
        if (view.MinHeight is not null && view.MinHeight() is var minHeight && minHeight > size.Y)
            size = new(size.X, minHeight);
        return size;
    }

    static Box2 ConstrainSize(View view, Box2 box) => Box2.FromCornerSize(box.TopLeft, ConstrainSize(view, box.Size));

    Vector2 MeasureView(View view)
    {
        if (view.Visible is not null && !view.Visible()) return new();

        Vector2 size = new(view.Padding.Left + view.Padding.Right + view.Margin.Left + view.Margin.Right,
            view.Padding.Top + view.Padding.Bottom + view.Margin.Top + view.Margin.Bottom);
        switch (view)
        {
            case LabelView labelView:
                if (labelView.Text?.Invoke() is var text && !string.IsNullOrWhiteSpace(text))
                    size += fontRenderer.Measure(text, new FontDescription { Size = labelView.FontSize });
                break;

            case ImageView imageView:
                if (!imageView.InheritParentSize && imageView.Source?.Invoke() is var src && !string.IsNullOrWhiteSpace(src))
                {
                    var entry = atlas[src];
                    size = entry.PixelSize.ToNumericsVector2();
                }
                break;

            case ButtonView buttonView:
                if (buttonView.Child is not null)
                {
                    var childSize = MeasureView(buttonView.Child);
                    size += childSize;
                }
                break;

            case ProgressView progressView:
                if (progressView.StringFormat?.Invoke() is { } stringFormat && !string.IsNullOrWhiteSpace(stringFormat)
                    && progressView.Maximum?.Invoke() is var maximum && progressView.Value?.Invoke() is var value)
                {
                    size += fontRenderer.Measure(string.Format(stringFormat, value / maximum * 100), new FontDescription { Size = progressView.FontSize });
                    if (progressView.BorderColor.W != 0) size += new Vector2(2, 2);
                }
                break;

            case StackView stackView:
                foreach (var child in stackView.Children.Select(GetTemplatedView))
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

        view.ViewData.Box = Box2.FromCornerSize(new(), ConstrainSize(view, size));
        view.ViewData.BaseBox = Box2.FromCornerSize(new(), size);
        return view.ViewData.Box.Size;
    }

    Vector2 LayoutView(View view, Vector2 offset, Vector2 multiplier)
    {
        if (view.Visible is not null && !view.Visible()) return default;
        view = GetTemplatedView(view);

        var boxSize = view.ViewData.Box.Size;
        var boxStart = offset + new Vector2(Math.Min(multiplier.X, 0), Math.Min(multiplier.Y, 0)) * boxSize
            + new Vector2(view.Padding.Left + view.Margin.Left, view.Padding.Top + view.Margin.Top);
        view.ViewData.Box = Box2.FromCornerSize(boxStart, boxSize);
        view.ViewData.BaseBox = Box2.FromCornerSize(boxStart, view.ViewData.BaseBox.Size);

        switch (view)
        {
            case StackView stackView:
                {
                    Vector2 extraSize = default;

                    // finish the layout for views that inherit their size from us
                    foreach (var child in stackView.Children)
                        switch (child)
                        {
                            case ImageView { InheritParentSize: true } imageView when imageView.Source?.Invoke() is var src && !string.IsNullOrWhiteSpace(src):
                                // need the aspect ratio
                                var entry = atlas[src];
                                var aspect = (float)entry.PixelSize.X / entry.PixelSize.Y;
                                Vector2 newSize = stackView.Type != StackType.Horizontal ? new(boxSize.X, boxSize.X / aspect) : new(boxSize.Y * aspect, boxSize.Y);

                                child.ViewData.BaseBox = Box2.FromCornerSize(new(), newSize);
                                child.ViewData.Box = ConstrainSize(child, child.ViewData.BaseBox);
                                extraSize += stackView.Type != StackType.Horizontal ? new Vector2(0, child.ViewData.BaseBox.Size.Y) : new Vector2(child.ViewData.BaseBox.Size.X, 0);
                                break;
                        }

                    var start = boxStart;
                    foreach (var child in stackView.Children.Where(v => v.Visible?.Invoke() ?? true).Select(GetTemplatedView))
                    {
                        var childExtraSize = LayoutView(child, start, new(1, 1));
                        extraSize = stackView.Type != StackType.Horizontal ? new Vector2(Math.Max(childExtraSize.X, extraSize.X), extraSize.Y + childExtraSize.Y)
                            : new(extraSize.X + childExtraSize.X, Math.Max(childExtraSize.Y, extraSize.Y));
                        start = stackView.Type == StackType.Horizontal
                            ? new(start.X + child.ViewData.Box.Size.X, start.Y)
                            : new(start.X, start.Y + child.ViewData.Box.Size.Y);
                    }

                    if (extraSize != default)
                    {
                        view.ViewData.BaseBox = Box2.FromCornerSize(boxStart, view.ViewData.BaseBox.Size + extraSize);
                        view.ViewData.Box = ConstrainSize(view, view.ViewData.BaseBox);
                    }

                    return extraSize;
                }

            case ISingleChildContainerView { Child: not null } singleChildContainerView:
                {
                    var extraSize = LayoutView(singleChildContainerView.Child, boxStart, new(1, 1));

                    if (extraSize != default)
                    {
                        view.ViewData.BaseBox = Box2.FromCornerSize(boxStart, view.ViewData.BaseBox.Size + extraSize);
                        view.ViewData.Box = ConstrainSize(view, view.ViewData.BaseBox);
                    }

                    return extraSize;
                }

            default:
                if (view is IContainerView) throw new NotImplementedException();
                return default;
        }
    }

    void RenderView(View view)
    {
        if (view.Visible is not null && !view.Visible()) return;
        view = GetTemplatedView(view);

        var box = view.ViewData.Box;
        if (view.BackgroundColor.W > 0)
            ScreenFillQuad(box.WithExpand(view.Padding), view.BackgroundColor, atlas[GrowableTextureAtlas3D.BlankName], false);

        switch (view)
        {
            case IContainerView containerView:
                foreach (var child in containerView.Children)
                    RenderView(child);
                break;
            case LabelView labelView:
                if (labelView.Text?.Invoke() is { } text && !string.IsNullOrEmpty(text) && labelView.ForegroundColor.Invoke() is { } labelForegroundColor)
                    ScreenString(text, new() { Size = labelView.FontSize }, labelView.HorizontalTextAlignment switch
                    {
                        HorizontalAlignment.Left => box.TopLeft + new Vector2(view.Margin.Left, view.Margin.Top),
                        HorizontalAlignment.Right => new(box.Right - view.Margin.Right, box.Top - view.Margin.Top),
                        _ => throw new NotImplementedException()
                    }, labelForegroundColor, labelView.HorizontalTextAlignment);
                break;
            case ProgressView progressView:
                if (progressView.StringFormat?.Invoke() is { } stringFormat && !string.IsNullOrEmpty(stringFormat)
                    && progressView.Maximum?.Invoke() is { } maximum && progressView.Value?.Invoke() is { } value
                    && progressView.ForegroundColor?.Invoke() is { } progressForegroundColor)
                {
                    var borderOffset = progressView.BorderColor.W > 0;
                    if (borderOffset)
                        ScreenFillQuad(box, progressView.BorderColor, atlas[GrowableTextureAtlas3D.BlankName], false);

                    box = box.WithExpand(new Thickness(-1));
                    if (progressView.BackgroundColor.W > 0)
                        ScreenFillQuad(box, progressView.BackgroundColor, atlas[GrowableTextureAtlas3D.BlankName], false);
                    ScreenFillQuad(Box2.FromCornerSize(box.TopLeft, (float)(box.Size.X * value / maximum), box.Size.Y), progressForegroundColor, atlas[GrowableTextureAtlas3D.BlankName], false);
                    ScreenString(string.Format(stringFormat, value / maximum * 100), new() { Size = progressView.FontSize }, progressView.HorizontalTextAlignment switch
                    {
                        HorizontalAlignment.Left => box.TopLeft + new Vector2(view.Margin.Left, view.Margin.Top),
                        HorizontalAlignment.Right => new(box.Right - view.Margin.Right, box.Top - view.Margin.Top),
                        _ => throw new NotImplementedException()
                    }, progressView.TextColor, progressView.HorizontalTextAlignment);
                }
                break;
            case ImageView imageView:
                if (imageView.Source?.Invoke() is { } src && !string.IsNullOrWhiteSpace(src)  && imageView.ForegroundColor?.Invoke() is { } imageForegroundColor)
                    ScreenFillQuad(box.WithExpand(-view.Margin), imageForegroundColor, atlas[src], false);
                break;
            case ButtonView buttonView:
                if (buttonView.Child is not null)
                    RenderView(buttonView.Child);
                break;
        }
    }

    void RenderGui()
    {
        foreach (var rootViewDescription in gui.RootViewDescriptions.Where(rvd => rvd.View.Visible?.Invoke() != false))
        {
            var view = GetTemplatedView(rootViewDescription.View);
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

namespace Tweey.Systems;

partial class RenderSystem
{
    int WidthPercentage(float p) => (int)(windowUbo.Data.WindowSize.X * p / 100f);
    int HeightPercentage(float p) => (int)(windowUbo.Data.WindowSize.Y * p / 100f);

    const float ButtonBorderTextureWidth = 10;

    enum GuiTransformType { None, Rotate90, Rotate180, Rotate270, MirrorH, MirrorV }

    bool IsWorldViewBoxInView(Box2 box) =>
        Box2.FromCornerSize(world.Offset, windowUbo.Data.WindowSize / world.Zoom).Intersects(box);

    void ScreenFillQuad(in Box2 box, in Vector4 color, in AtlasEntry entry, bool asWorldCoords = true, GuiTransformType transform = GuiTransformType.None)
    {
        if (color.W == 0)
            return;

        var uv0 = entry.TextureCoordinate0;
        var uv1 = entry.TextureCoordinate1;
        var uv2 = new Vector3(uv1.X, uv0.Y, uv0.Z);
        var uv3 = new Vector3(uv0.X, uv1.Y, uv0.Z);

        if (transform is GuiTransformType.Rotate90)
            (uv0, uv1, uv2, uv3) = (uv3, uv2, uv0, uv1);
        else if (transform is GuiTransformType.Rotate270)
            (uv0, uv1, uv2, uv3) = (uv2, uv3, uv1, uv0);
        else if (transform is GuiTransformType.Rotate180)
            (uv0, uv1, uv2, uv3) = (uv1, uv0, uv3, uv2);

        var zoom = asWorldCoords ? world.Zoom : 1;
        var offset = asWorldCoords ? world.Offset : default;
        var br = box.BottomRight + Vector2.One;
        guiVAO.Vertices.Add(new((box.TopLeft - offset) * zoom, color, uv0));
        guiVAO.Vertices.Add(new((br - offset) * zoom, color, uv1));
        guiVAO.Vertices.Add(new(new((br.X - offset.X) * zoom, (box.Top - offset.Y) * zoom), color, uv2));

        guiVAO.Vertices.Add(new(new((box.Left - offset.X) * zoom, (br.Y - offset.Y) * zoom), color, uv3));
        guiVAO.Vertices.Add(new((br - offset) * zoom, color, uv1));
        guiVAO.Vertices.Add(new((box.TopLeft - offset) * zoom, color, uv0));
    }

    void ScreenStrokeQuad(in Box2 box, float strokeWidth, in Vector4 color, in AtlasEntry entry)
    {
        ScreenFillQuad(Box2.FromCornerSize(box.TopLeft, new(box.Size.X, strokeWidth)), color, entry, false);
        ScreenFillQuad(Box2.FromCornerSize(box.TopRight - new Vector2(strokeWidth - 1, 0), new(strokeWidth, box.Size.Y)), color, entry, false);
        ScreenFillQuad(Box2.FromCornerSize(box.BottomLeft - new Vector2(0, strokeWidth - 1), new(box.Size.X, strokeWidth)), color, entry, false);
        ScreenFillQuad(Box2.FromCornerSize(box.TopLeft, new(strokeWidth, box.Size.Y)), color, entry, false);
    }

    [Flags]
    enum FrameType { Normal = 0, Hover = 1 << 0, Checked = 1 << 2, NoEdges = 1 << 3, NoCorners = 1 << 4, NoBackground = 1 << 5 }

    /// <summary>
    /// Fills a quad with a frame made by two textures, the corner and the edge textures respectively. 
    /// <para>Naming conventions:</para>
    /// <list type="bullet">
    /// <item>Name starts with /Data/Frames/<c>name</c>/tex-</item>
    /// <item><see cref="FrameType.Hover"/> appends <c>-hover</c> to the file name.</item>
    /// <item>Corner textures append <c>-corner</c> to the file name.</item>
    /// <item>Edge textures append <c>-edge</c> to the file name.</item>
    /// </list>
    /// Some examples: <c>/Data/Frames/button/tex-corner.png</c>, <c>/Data/Frames/button/tex-hover-edge.png</c>, etc.
    /// </summary>
    /// <param name="elementWidth">The width of the corner &amp; edge textures, in pixels. Looks best with the pixel width, but it scales as needed.</param>
    void ScreenFillFrame(in Box2 box, string baseTextureName, float elementWidth, FrameType frameType = FrameType.Normal)
    {
        var hoverPart = frameType.HasFlag(FrameType.Hover) ? "-hover" : null;
        var checkedPart = frameType.HasFlag(FrameType.Checked) ? "-checked" : null;

        Vector4 edgeColor = default;
        if (!frameType.HasFlag(FrameType.NoCorners))
        {
            var cornerTexture = atlas[$"Data/Frames/{baseTextureName}/tex{hoverPart}{checkedPart}-corner.png"];
            ScreenFillQuad(Box2.FromCornerSize(box.TopLeft, new(elementWidth)),
                Colors4.White, cornerTexture, false);
            ScreenFillQuad(Box2.FromCornerSize(box.TopRight - new Vector2(elementWidth + 1, -1), new(elementWidth)),
                Colors4.White, cornerTexture, false, GuiTransformType.Rotate90);
            ScreenFillQuad(Box2.FromCornerSize(box.BottomRight - new Vector2(elementWidth + 2), new(elementWidth)),
                Colors4.White, cornerTexture, false, GuiTransformType.Rotate180);
            ScreenFillQuad(Box2.FromCornerSize(box.BottomLeft - new Vector2(1, elementWidth + 3), new(elementWidth)),
                Colors4.White, cornerTexture, false, GuiTransformType.Rotate270);

            if (edgeColor == default) edgeColor = cornerTexture.EdgeColor;
        }

        if (!frameType.HasFlag(FrameType.NoEdges))
        {
            var edgeTexture = atlas[$"Data/Frames/{baseTextureName}/tex{hoverPart}{checkedPart}-edge.png"];
            ScreenFillQuad(Box2.FromCornerSize(box.TopLeft + new Vector2(elementWidth - 1, 0), new(box.Size.X - 2 * elementWidth + 2, elementWidth)),
                Colors4.White, edgeTexture, false);
            ScreenFillQuad(Box2.FromCornerSize(box.TopRight - new Vector2(elementWidth + 1, -elementWidth), new(elementWidth, box.Size.Y - 2 * elementWidth + 2)),
                Colors4.White, edgeTexture, false, GuiTransformType.Rotate90);
            ScreenFillQuad(Box2.FromCornerSize(box.BottomLeft + new Vector2(elementWidth - 4, -elementWidth - 2), new(box.Size.X - 2 * elementWidth + 2, elementWidth)),
                Colors4.White, edgeTexture, false, GuiTransformType.Rotate180);
            ScreenFillQuad(Box2.FromCornerSize(box.TopLeft + new Vector2(-1, elementWidth - 3), new(elementWidth, box.Size.Y - 2 * elementWidth)),
                Colors4.White, edgeTexture, false, GuiTransformType.Rotate270);

            if (edgeColor == default) edgeColor = edgeTexture.EdgeColor;
        }

        if (!frameType.HasFlag(FrameType.NoBackground))
        {
            ScreenFillQuad(Box2.FromCornerSize(box.TopLeft + new Vector2(elementWidth - 1), new(box.Size.X - 2 * elementWidth, box.Size.Y - 2 * elementWidth)),
                edgeColor, blankAtlasEntry, false);
        }
    }

    void ScreenString(string? s, FontDescription fontDescription, Box2 box, Vector4 fgColor, Vector4 bgColor, HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left) =>
        ScreenString(s, fontDescription, horizontalAlignment switch
        {
            HorizontalAlignment.Left => box.TopLeft,
            HorizontalAlignment.Right => box.TopRight,
            HorizontalAlignment.Center => new(box.Left + box.Size.X / 2, box.Top),
            _ => throw new NotImplementedException()
        }, fgColor, bgColor, horizontalAlignment);

    void ScreenString(string? s, FontDescription fontDescription, Vector2 location, Vector4 fgColor, Vector4 bgColor, HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left)
    {
        if (s is { })
            fontRenderer.Render(s, fontDescription, location.ToVector2i(),
                box =>
                {
                    if (bgColor.W > 0)
                        ScreenFillQuad(box, bgColor, blankAtlasEntry, false);
                },
                (box, atlasEntry) => ScreenFillQuad(box, fgColor, atlasEntry, false), horizontalAlignment);
    }

    void ScreenLine(Box2 box1, Box2 box2, Vector4 color)
    {
        if (color.W == 0) return;

        guiVAO.Vertices.Add(new((box1.Center + new Vector2(.5f, .5f) - world.Offset) * world.Zoom, color, blankAtlasEntry.TextureCoordinate0));
        guiVAO.Vertices.Add(new((box2.Center + new Vector2(.5f, .5f) - world.Offset) * world.Zoom, color, blankAtlasEntry.TextureCoordinate1));
    }

    void ScreenLineQuad(Box2 box, Vector4 color, bool asWorldCoords = true)
    {
        if (color.W == 0) return;

        var zoom = asWorldCoords ? world.Zoom : 1;
        var offset = asWorldCoords ? world.Offset : default;
        var br = box.BottomRight + Vector2.One;

        guiVAO.Vertices.Add(new((box.TopLeft - offset) * zoom, color, blankAtlasEntry.TextureCoordinate0));
        guiVAO.Vertices.Add(new(new((box.Right + 1 - offset.X) * zoom, (box.Top - offset.Y) * zoom), color, new(blankAtlasEntry.TextureCoordinate1.X, blankAtlasEntry.TextureCoordinate0.Y, blankAtlasEntry.TextureCoordinate0.Z)));
        guiVAO.Vertices.Add(new(new((box.Left - offset.X) * zoom, (box.Bottom + 1 - offset.Y) * zoom), color, new(blankAtlasEntry.TextureCoordinate0.X, blankAtlasEntry.TextureCoordinate1.Y, blankAtlasEntry.TextureCoordinate0.Z)));
        guiVAO.Vertices.Add(new((br - offset) * zoom, color, blankAtlasEntry.TextureCoordinate1));
        guiVAO.Vertices.Add(new((box.TopLeft - offset) * zoom, color, blankAtlasEntry.TextureCoordinate0));
        guiVAO.Vertices.Add(new(new((box.Left - offset.X) * zoom, (box.Bottom + 1 - offset.Y) * zoom), color, new(blankAtlasEntry.TextureCoordinate0.X, blankAtlasEntry.TextureCoordinate1.Y, blankAtlasEntry.TextureCoordinate0.Z)));
        guiVAO.Vertices.Add(new(new((box.Right + 1 - offset.X) * zoom, (box.Top - offset.Y) * zoom), color, new(blankAtlasEntry.TextureCoordinate1.X, blankAtlasEntry.TextureCoordinate0.Y, blankAtlasEntry.TextureCoordinate0.Z)));
        guiVAO.Vertices.Add(new((br - offset) * zoom, color, blankAtlasEntry.TextureCoordinate1));
    }

    View GetTemplatedView(View view) => view.ViewData.TemplatedView ?? view;

    bool MouseEvent(View view, Vector2i position, InputAction inputAction, MouseButton mouseButton, KeyModifiers keyModifiers)
    {
        if ((view.IsVisible?.Invoke() ?? true) && GetTemplatedView(view) is { } realView && realView.ViewData.Box.Contains(position))
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

    [Message]
    public bool MouseEventMessage(Vector2i position, InputAction inputAction, MouseButton mouseButton, KeyModifiers keyModifiers)
    {
        // use the old view boxes to calculate hit testing
        foreach (var rootViewDescription in gui.RootViewDescriptions)
            if (MouseEvent(rootViewDescription.View, position, inputAction, mouseButton, keyModifiers))
                return true;

        return false;
    }

    void TemplateView(View view)
    {
        if (view.IsVisible is not null && !view.IsVisible()) return;

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
        if (view.IsVisible is not null && !view.IsVisible()) return new();

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
                size.X += 2 * ButtonBorderTextureWidth;
                size.Y += 2 * ButtonBorderTextureWidth;
                break;

            case ProgressView progressView:
                if (progressView.StringFormat() is { } stringFormat && !string.IsNullOrWhiteSpace(stringFormat)
                    && progressView.Maximum() is var maximum && progressView.Value() is var value)
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
        if (view.IsVisible is not null && !view.IsVisible()) return default;
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
                    foreach (var child in stackView.Children.Where(v => v.IsVisible?.Invoke() ?? true).Select(GetTemplatedView))
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
                    if (singleChildContainerView is ButtonView)
                        boxStart += new Vector2(ButtonBorderTextureWidth);

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
        if (view.IsVisible is not null && !view.IsVisible()) return;
        view = GetTemplatedView(view);

        var box = view.ViewData.Box;
        if (view is not LabelView && view.BackgroundColor.W > 0)
            ScreenFillQuad(box.WithExpand(view.Padding), view.BackgroundColor, blankAtlasEntry, false);

        switch (view)
        {
            case IContainerView containerView:
                foreach (var child in containerView.Children)
                    RenderView(child);
                break;
            case LabelView labelView:
                if (labelView.Text?.Invoke() is { } text && !string.IsNullOrEmpty(text) && labelView.ForegroundColor.Invoke() is { } labelForegroundColor)
                    ScreenString(text, new() { Size = labelView.FontSize },
                        new Box2(box.TopLeft + new Vector2(view.Margin.Left, view.Margin.Top), box.BottomRight - new Vector2(view.Margin.Right, view.Margin.Bottom)),
                        labelForegroundColor, labelView.BackgroundColor, labelView.HorizontalTextAlignment);
                break;
            case ProgressView progressView:
                if (progressView.StringFormat() is { } stringFormat && !string.IsNullOrEmpty(stringFormat)
                    && progressView.Maximum() is { } maximum && progressView.Value() is { } value
                    && progressView.ForegroundColor?.Invoke() is { } progressForegroundColor)
                {
                    var borderOffset = progressView.BorderColor.W > 0;
                    if (borderOffset)
                        ScreenFillQuad(box, progressView.BorderColor, blankAtlasEntry, false);

                    box = box.WithExpand(new Thickness(-1));
                    if (progressView.BackgroundColor.W > 0)
                        ScreenFillQuad(box, progressView.BackgroundColor, blankAtlasEntry, false);
                    ScreenFillQuad(Box2.FromCornerSize(box.TopLeft, (float)(box.Size.X * value / maximum), box.Size.Y), progressForegroundColor, blankAtlasEntry, false);
                    ScreenString(string.Format(stringFormat, value / maximum * 100), new() { Size = progressView.FontSize },
                        new Box2(box.TopLeft + new Vector2(view.Margin.Left, view.Margin.Top), box.BottomRight - new Vector2(view.Margin.Right, view.Margin.Bottom)),
                        progressView.TextColor, Colors4.Transparent, progressView.HorizontalTextAlignment);
                }
                break;
            case ImageView imageView:
                if (imageView.Source?.Invoke() is { } src && !string.IsNullOrWhiteSpace(src) && imageView.ForegroundColor?.Invoke() is { } imageForegroundColor)
                    ScreenFillQuad(box.WithExpand(-view.Margin), imageForegroundColor, atlas[src], false);
                break;
            case ButtonView buttonView:
                ScreenFillFrame(box, "button", ButtonBorderTextureWidth,
                    (box.Contains(world.MouseScreenPosition) ? FrameType.Hover : FrameType.Normal)
                    | (buttonView.IsChecked?.Invoke() is true ? FrameType.Checked : FrameType.Normal));

                if (buttonView.Child is not null)
                    RenderView(buttonView.Child);
                break;
        }
    }

    void RenderGui()
    {
        foreach (var rootViewDescription in gui.RootViewDescriptions.Where(rvd => rvd.View.IsVisible?.Invoke() != false))
        {
            var view = GetTemplatedView(rootViewDescription.View);
            TemplateView(view);
            MeasureView(view);
            LayoutView(view,
                rootViewDescription.Anchor switch
                {
                    Anchor.TopLeft => new(),
                    Anchor.BottomLeft => new(0, windowUbo.Data.WindowSize.Y),
                    Anchor.BottomRight => new(windowUbo.Data.WindowSize.X, windowUbo.Data.WindowSize.Y),
                    _ => throw new NotImplementedException()
                },
                rootViewDescription.Anchor switch
                {
                    Anchor.TopLeft => new(1, 1),
                    Anchor.BottomLeft => new(1, -1),
                    Anchor.BottomRight => new(-1, -1),
                    _ => throw new NotImplementedException()
                });
            RenderView(view);
        }
    }
}

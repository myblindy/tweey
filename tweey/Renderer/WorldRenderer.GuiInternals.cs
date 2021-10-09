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

    struct ViewBoxData
    {
        public Box2 Box, BaseBox;

        public ViewBoxData(Box2 box, Box2 baseBox) => (Box, BaseBox) = (box, baseBox);

        public ViewBoxData(Box2 box) => (Box, BaseBox) = (box, box);
    }

    readonly Dictionary<View, ViewBoxData> viewBoxes = new();

    readonly Dictionary<View, View> viewTemplates = new();
    View GetRealView(View view) =>
        viewTemplates.TryGetValue(view, out var templateView) ? templateView : view;

    bool MouseEvent(View view, Vector2i position, InputAction inputAction, MouseButton mouseButton, KeyModifiers keyModifiers)
    {
        if ((view.Visible?.Invoke() ?? true) && GetRealView(view) is { } realView && viewBoxes.TryGetValue(realView, out var boxData) && boxData.Box.Contains(position))
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
                TemplateView(viewTemplates[view] = repeaterView.CreateView());
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

        return (viewBoxes[view] = new(Box2.FromCornerSize(new(), ConstrainSize(view, size)), Box2.FromCornerSize(new(), size))).Box.Size;
    }

    Vector2 LayoutView(View view, Vector2 offset, Vector2 multiplier)
    {
        if (view.Visible is not null && !view.Visible()) return default;
        view = viewTemplates.TryGetValue(view, out var templateView) ? templateView : view;

        var boxSize = viewBoxes[view].Box.Size;
        var boxStart = offset + new Vector2(Math.Min(multiplier.X, 0), Math.Min(multiplier.Y, 0)) * boxSize
            + new Vector2(view.Padding.Left + view.Margin.Left, view.Padding.Top + view.Margin.Top);
        viewBoxes[view] = new(Box2.FromCornerSize(boxStart, boxSize), Box2.FromCornerSize(boxStart, viewBoxes[view].BaseBox.Size));

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

                                var newChildBaseBox = Box2.FromCornerSize(new(), newSize);
                                var newChildBox = ConstrainSize(child, newChildBaseBox);
                                viewBoxes[child] = new(newChildBox, newChildBaseBox);
                                extraSize += stackView.Type != StackType.Horizontal ? new Vector2(0, newChildBox.Size.Y) : new Vector2(newChildBox.Size.X, 0);
                                break;
                        }

                    var start = boxStart;
                    foreach (var child in stackView.Children.Where(v => v.Visible?.Invoke() ?? true).Select(GetRealView))
                    {
                        var childExtraSize = LayoutView(child, start, new(1, 1));
                        extraSize = stackView.Type != StackType.Horizontal ? new Vector2(Math.Max(childExtraSize.X, extraSize.X), extraSize.Y + childExtraSize.Y)
                            : new(extraSize.X + childExtraSize.X, Math.Max(childExtraSize.Y, extraSize.Y));
                        start = stackView.Type == StackType.Horizontal
                            ? new(start.X + viewBoxes[child].Box.Size.X, start.Y)
                            : new(start.X, start.Y + viewBoxes[child].Box.Size.Y);
                    }

                    if (extraSize != default)
                    {
                        var newViewBaseBox = Box2.FromCornerSize(boxStart, boxSize + extraSize);
                        viewBoxes[view] = new(ConstrainSize(view, newViewBaseBox), newViewBaseBox);
                    }

                    return extraSize;
                }

            case ISingleChildContainerView { Child: not null } singleChildContainerView:
                {
                    var extraSize = LayoutView(singleChildContainerView.Child, boxStart, new(1, 1));

                    if (extraSize != default)
                    {
                        var newViewBaseBox = Box2.FromCornerSize(boxStart, boxSize + extraSize);
                        viewBoxes[view] = new(ConstrainSize(view, newViewBaseBox), newViewBaseBox);
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
        view = viewTemplates.TryGetValue(view, out var templateView) ? templateView : view;

        var box = viewBoxes[view].Box;
        if (view.BackgroundColor.W > 0)
            ScreenFillQuad(box.WithExpand(view.Padding), view.BackgroundColor, atlas[GrowableTextureAtlas3D.BlankName], false);

        switch (view)
        {
            case IContainerView containerView:
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
            case ButtonView buttonView:
                if (buttonView.Child is not null)
                    RenderView(buttonView.Child);
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

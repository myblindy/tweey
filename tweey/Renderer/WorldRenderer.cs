namespace Tweey.Renderer;

class WorldRenderer
{
    readonly World world;
    readonly GrowableTextureAtlas3D atlas = new(2048, 2048, 5);
    readonly FontRenderer fontRenderer;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct WindowUbo
    {
        public Vector2 WindowSize;
    }

    readonly UniformBufferObject<WindowUbo> windowUbo = new();
    const int windowUboBindingPoint = 1;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct GuiVertex
    {
        public Vector2 Location;
        public Vector4 Color;
        public Vector3 Tex0;

        public GuiVertex(Vector2 location, Vector4 color, Vector3 tex0) =>
            (Location, Color, Tex0) = (location, color, tex0);
    }

    readonly VertexArrayObject<GuiVertex, Nothing> vaoGui = new(false, 1024, 0);
    readonly ShaderProgram shaderProgram = new("gui");
    readonly GuiSpace gui = new();

    public WorldRenderer(World world)
    {
        this.world = world;
        fontRenderer = new(atlas);
        shaderProgram.UniformBlockBind("ubo_window", windowUboBindingPoint);
        shaderProgram.Uniform("atlasSampler", 0);

        gui.RootViewDescriptions.Add(new(new StackView(StackType.Vertical)
        {
            Children =
            {
                new LabelView
                {
                    Text = () => "moopsies",
                }
            }
        }, new(), Anchor.BottomLeft));
    }

    public void Resize(int width, int height)
    {
        windowUbo.Data.WindowSize = new Vector2(width, height);
        windowUbo.Update();
    }

    float pixelZoom = 45;

    FrameData frameData;

    void ScreenQuad(Box2 box, Vector4 color, AtlasEntry entry, bool useScale = true)
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

    void ScreenString(string s, FontDescription fontDescription, Vector2 location, Vector4 color) =>
        fontRenderer.Render(s, fontDescription, location.ToVector2i(), (box, atlasEntry) => ScreenQuad(box, color, atlasEntry, false));

    void ScreenLine(Box2 box1, Box2 box2, Vector4 color)
    {
        var blankEntry = atlas[GrowableTextureAtlas3D.BlankName];
        vaoGui.Vertices.Add(new((box1.Center + new Vector2(.5f, .5f)) * pixelZoom, color, blankEntry.TextureCoordinate0));
        vaoGui.Vertices.Add(new((box2.Center + new Vector2(.5f, .5f)) * pixelZoom, color, blankEntry.TextureCoordinate1));
    }

    readonly Dictionary<View, Box2> viewBoxes = new();
    Vector2 MeasureView(View view)
    {
        switch (view)
        {
            case LabelView labelView:
                return (viewBoxes[view] = labelView.Text is null ? new() : Box2.FromCornerSize(new(), fontRenderer.Measure(labelView.Text(),
                    new FontDescription { Size = labelView.FontSize }))).Size;

            case StackView stackView:
                Vector2 size = default;
                foreach (var child in stackView.Children)
                {
                    var childSize = MeasureView(child);

                    size = stackView.Type switch
                    {
                        StackType.Horizontal => new(size.X + childSize.X, Math.Max(size.Y, childSize.Y)),
                        StackType.Vertical => new(Math.Max(size.X, childSize.X), size.Y + childSize.Y),
                        _ => throw new NotImplementedException(),
                    };
                }
                return size;

            default:
                throw new NotImplementedException();
        }
    }

    void LayoutView(View view, Vector2 offset, Vector2 multiplier)
    {
        var boxSize = viewBoxes[view].Size;
        var boxStart = offset + multiplier * boxSize;

        switch (view)
        {
            case StackView stackView:
                var start = boxStart;
                foreach (var child in stackView.Children)
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

    void RenderGui()
    {
        viewBoxes.Clear();

        foreach (var rootViewDescription in gui.RootViewDescriptions)
        {
            MeasureView(rootViewDescription.View);
            LayoutView(rootViewDescription.View, rootViewDescription.Location.ToNumericsVector2(), rootViewDescription.Anchor switch
            {
                Anchor.BottomLeft => new(1, -1),
                _ => throw new NotImplementedException()
            });
        }
    }

    public void Render(double deltaSec, double deltaUpdateTimeSec, double deltaRenderTimeSec)
    {
        vaoGui.Vertices.Clear();

        // store the actual entities' vertices
        foreach (var entity in world.GetEntities())
            switch (entity)
            {
                case Building building:
                    ScreenQuad(Box2.FromCornerSize(building.Location, building.Width, building.Height), building.Color, atlas[$"Data/Buildings/{building.FileName}.png"]);
                    break;
                case ResourceBucket resourceBucket:
                    var resQ = resourceBucket.ResourceQuantities.Single(r => r.Quantity > 0);
                    ScreenQuad(Box2.FromCornerSize(resourceBucket.Location, 1, 1), Colors.White, atlas[$"Data/Resources/{resQ.Resource.FileName}.png"]);
                    break;
                case Villager villager:
                    ScreenQuad(Box2.FromCornerSize(villager.Location, 1, 1), Colors.White, atlas["Data/Misc/villager.png"]);
                    break;
            }

        // render gui
        RenderGui();

        // frame data
        frameData.NewFrame(TimeSpan.FromSeconds(deltaSec), TimeSpan.FromSeconds(deltaUpdateTimeSec), TimeSpan.FromSeconds(deltaRenderTimeSec));
        ScreenString($"FPS: {frameData.Rate:0.0}, update: {frameData.UpdateTimePercentage * 100:0.00}%, render: {frameData.RenderTimePercentage * 100:0.00}%",
            new() { Size = 22 }, new(2, 2), Colors.Lime);

        var entityVertexCount = vaoGui.Vertices.Length;

        // store the ai plan targets' vertices
        foreach (var villager in world.GetEntities<Villager>())
            if (villager.AIPlan?.FirstTarget is { } firstAiTarget)
                ScreenLine(villager.Box, firstAiTarget.Box, Colors.Yellow);
        var aiPlanVertexCount = vaoGui.Vertices.Length - entityVertexCount;

        shaderProgram.Use();
        atlas.Bind();
        windowUbo.Bind(windowUboBindingPoint);
        vaoGui.Draw(PrimitiveType.Triangles, vertexOrIndexCount: entityVertexCount);
        vaoGui.Draw(PrimitiveType.Lines, entityVertexCount, aiPlanVertexCount);
    }
}

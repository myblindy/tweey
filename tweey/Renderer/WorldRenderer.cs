namespace Tweey.Renderer;

partial class WorldRenderer
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

        gui.RootViewDescriptions.Add(new(
            new StackView(StackType.Vertical)
            {
                BackgroundColor = new(.1f, .1f, .1f, 1),
                MinWidth = () => WidthPercentage(50),
                MinHeight = () => HeightPercentage(20),
                Children =
                {
                    new StackView(StackType.Horizontal)
                    {
                        Children =
                        {
                            new LabelView
                            {
                                Text = () => world.SelectedEntity is null ? null : $"{world.SelectedEntity.GetType().Name}: ",
                                FontSize = 30,
                            },
                            new LabelView
                            {
                                Text = () => world.SelectedEntity?.Name,
                                FontSize = 30,
                                ForegroundColor = Colors.Aqua
                            },
                        }
                    },
                    new LabelView
                    {
                        Text = () => world.SelectedEntity is Villager villager ? villager.AIPlan is { } aiPlan ? aiPlan.Description : "Idle." : null,
                        FontSize = 14,
                    }
                }
            }, Anchor.BottomLeft));
    }

    public void Resize(int width, int height)
    {
        windowUbo.Data.WindowSize = new Vector2(width, height);
        windowUbo.Update();
    }

    float pixelZoom = 45;

    FrameData frameData;

    public void Render(double deltaSec, double deltaUpdateTimeSec, double deltaRenderTimeSec)
    {
        vaoGui.Vertices.Clear();

        // store the actual entities' vertices
        foreach (var entity in world.GetEntities())
            switch (entity)
            {
                case Building building:
                    ScreenFillQuad(Box2.FromCornerSize(building.Location, building.Width, building.Height), building.Color, atlas[$"Data/Buildings/{building.FileName}.png"]);
                    break;
                case ResourceBucket resourceBucket:
                    var resQ = resourceBucket.ResourceQuantities.Single(r => r.Quantity > 0);
                    ScreenFillQuad(Box2.FromCornerSize(resourceBucket.Location, 1, 1), Colors.White, atlas[$"Data/Resources/{resQ.Resource.FileName}.png"]);
                    break;
                case Villager villager:
                    ScreenFillQuad(Box2.FromCornerSize(villager.Location, 1, 1), Colors.White, atlas["Data/Misc/villager.png"]);
                    break;
            }

        // render gui
        RenderGui();

        // frame data
        frameData.NewFrame(TimeSpan.FromSeconds(deltaSec), TimeSpan.FromSeconds(deltaUpdateTimeSec), TimeSpan.FromSeconds(deltaRenderTimeSec));
        ScreenString($"FPS: {frameData.Rate:0.0}, update: {frameData.UpdateTimePercentage * 100:0.00}%, render: {frameData.RenderTimePercentage * 100:0.00}%",
            new() { Size = 22 }, new(2, 2), Colors.Lime);

        var triVertexCount = vaoGui.Vertices.Length;

        // store the ai plan targets' vertices
        foreach (var villager in world.GetEntities<Villager>())
            if (villager.AIPlan?.FirstTarget is { } firstAiTarget)
                ScreenLine(villager.Box, firstAiTarget.Box, Colors.Yellow);

        // selection box
        if (world.SelectedEntity is not null)
            ScreenLineQuad(world.SelectedEntity.Box, Colors.White);
        var lineVertexCount = vaoGui.Vertices.Length - triVertexCount;

        shaderProgram.Use();
        atlas.Bind();
        windowUbo.Bind(windowUboBindingPoint);
        vaoGui.Draw(PrimitiveType.Triangles, vertexOrIndexCount: triVertexCount);
        vaoGui.Draw(PrimitiveType.Lines, triVertexCount, lineVertexCount);
    }
}

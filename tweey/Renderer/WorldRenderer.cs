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

    readonly VertexArrayObject<GuiVertex, Nothing> vaoGui = new(false, 10240, 0);
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
                Visible = () => world.SelectedEntity is not null,
                BackgroundColor = new(.1f, .1f, .1f, 1),
                MinWidth = () => WidthPercentage(50),
                MinHeight = () => HeightPercentage(20),
                Padding = new(8),
                Children =
                {
                    new StackView(StackType.Horizontal)
                    {
                        Children =
                        {
                            new ImageView
                            {
                                Source = () => GetImagePath(world.SelectedEntity!),
                                InheritParentSize = true,
                            },
                            new LabelView
                            {
                                Text = () => $"{world.SelectedEntity!.GetType().Name}: ",
                                FontSize = 30,
                            },
                            new LabelView
                            {
                                Text = () => world.SelectedEntity!.Name,
                                FontSize = 30,
                                ForegroundColor = Colors.Aqua
                            },
                        }
                    },
                    new LabelView
                    {
                        Text = () => world.SelectedEntity is Villager villager ? villager.AIPlan is { } aiPlan ? aiPlan.Description : "Idle." : null,
                        FontSize = 18,
                    },
                    new LabelView
                    {
                        Text = () => "Inventory:",
                        FontSize = 18,
                    },
                    new RepeaterView<ResourceQuantity>
                    {
                        Source = () => world.SelectedEntity switch
                        {
                            Villager villager => villager.Inventory.ResourceQuantities,
                            Building building => building.Inventory.ResourceQuantities,
                            ResourceBucket resourceBucket => resourceBucket.ResourceQuantities,
                            _ => null
                        },
                        ContainerView = new StackView(StackType.Vertical),
                        ItemView = rq => new StackView(StackType.Horizontal)
                        {
                            Children =
                            {
                                new LabelView
                                {
                                    Text = () => $"{rq.Quantity}x {rq.Resource.Name}",
                                    FontSize = 18,
                                }
                            }
                        },
                        EmptyView = new LabelView
                        {
                            Text = () => "Nothing",
                            FontSize = 18,
                        }
                    }
                }
            }, Anchor.BottomLeft));

        gui.RootViewDescriptions.Add(new(
            new LabelView
            {
                Text = () => $"FPS: {Math.Round(frameData.Rate, 1, MidpointRounding.ToPositiveInfinity):0.0}, update: {frameData.UpdateTimePercentage * 100:0.00}%, render: {frameData.RenderTimePercentage * 100:0.00}%",
                FontSize = 22,
                Padding = new(2),
                ForegroundColor = Colors.Lime
            }));
    }

    public void Resize(int width, int height)
    {
        windowUbo.Data.WindowSize = new(width, height);
        windowUbo.Update();
    }

    float pixelZoom = 35;

    FrameData frameData;
    static string GetImagePath(Building building) => $"Data/Buildings/{building.FileName}.png";
    static string GetImagePath(Resource resource) => $"Data/Resources/{resource.FileName}.png";
    static string GetImagePath(Villager _) => $"Data/Misc/villager.png";
    static string GetImagePath(PlaceableEntity entity) => entity switch
    {
        Building building => GetImagePath(building),
        ResourceBucket resource => GetImagePath(resource.ResourceQuantities.First(rq => rq.Quantity > 0).Resource),
        Villager villager => GetImagePath(villager),
        _ => throw new NotImplementedException()
    };

    public void Render(double deltaSec, double deltaUpdateTimeSec, double deltaRenderTimeSec)
    {
        vaoGui.Vertices.Clear();

        // store the actual entities' vertices
        foreach (var entity in world.GetEntities())
            switch (entity)
            {
                case Building building:
                    ScreenFillQuad(Box2.FromCornerSize(building.Location, building.Width, building.Height), building.Color, atlas[GetImagePath(building)]);
                    break;
                case ResourceBucket resourceBucket:
                    var resQ = resourceBucket.ResourceQuantities.Single(r => r.Quantity > 0);
                    ScreenFillQuad(Box2.FromCornerSize(resourceBucket.Location, 1, 1), Colors.White, atlas[GetImagePath(resQ.Resource)]);
                    break;
                case Villager villager:
                    ScreenFillQuad(Box2.FromCornerSize(villager.Location, 1, 1), Colors.White, atlas[GetImagePath(villager)]);
                    break;
            }

        // render gui
        RenderGui();

        // frame data
        frameData.NewFrame(TimeSpan.FromSeconds(deltaSec), TimeSpan.FromSeconds(deltaUpdateTimeSec), TimeSpan.FromSeconds(deltaRenderTimeSec));

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

    public Vector2i GetLocationFromScreenPoint(Vector2i screenPoint) => screenPoint / (int)pixelZoom;
}

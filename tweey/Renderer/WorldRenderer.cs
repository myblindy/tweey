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

    [VertexDefinition, StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GuiVertex
    {
        public Vector2 Location;
        public Vector4 Color;
        public Vector3 Tex0;

        public GuiVertex(Vector2 location, Vector4 color, Vector3 tex0) =>
            (Location, Color, Tex0) = (location, color, tex0);
    }

    readonly StreamingVertexArrayObject<GuiVertex> vaoGui = new();
    readonly ShaderProgram shaderProgram = new("gui");
    readonly GuiSpace gui = new();

    public WorldRenderer(World world)
    {
        this.world = world;
        fontRenderer = new(atlas);
        shaderProgram.UniformBlockBind("ubo_window", windowUboBindingPoint);
        shaderProgram.Uniform("atlasSampler", 0);

        InitializeGui();
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
        // store the actual entities' vertices
        foreach (var entity in world.GetEntities())
            switch (entity)
            {
                case Building building:
                    var color = building.Color;
                    if (!building.IsBuilt) color *= new Vector4(1f, .4f, .4f, .4f);
                    ScreenFillQuad(Box2.FromCornerSize(building.Location, building.Width, building.Height), color, atlas[GetImagePath(building)]);
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
        vaoGui.UploadNewData();

        shaderProgram.Use();
        atlas.Bind();
        windowUbo.Bind(windowUboBindingPoint);
        vaoGui.Draw(PrimitiveType.Triangles, vertexOrIndexCount: triVertexCount);
        vaoGui.Draw(PrimitiveType.Lines, triVertexCount, lineVertexCount);
    }

    public Vector2i GetLocationFromScreenPoint(Vector2i screenPoint) => screenPoint / (int)pixelZoom;
}

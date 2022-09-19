namespace Tweey.Renderer;

partial class WorldRenderer
{
    readonly World world;
    readonly GrowableTextureAtlas3D atlas;
    readonly FontRenderer fontRenderer;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct WindowUbo
    {
        Vector4 WindowSizeAndZero;
        public Vector2 WindowSize
        {
            get => new(WindowSizeAndZero.X, WindowSizeAndZero.Y);
            set => (WindowSizeAndZero.X, WindowSizeAndZero.Y) = (value.X, value.Y);
        }
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

    readonly StreamingVertexArrayObject<GuiVertex> guiVAO = new();
    readonly ShaderProgram guiShaderProgram = new("gui");
    readonly ShaderProgram guiLightMapShaderProgram = new("gui-lightmap");
    readonly GuiSpace gui = new();

    public WorldRenderer(World world)
    {
        this.world = world;

        var maxTextureSize = Math.Min(4096, GraphicsEngine.MaxTextureSize);
        atlas = new(maxTextureSize, maxTextureSize, 1);
        fontRenderer = new(atlas);

        guiShaderProgram.UniformBlockBind("ubo_window", windowUboBindingPoint);
        guiShaderProgram.Uniform("atlasSampler", 0);

        guiLightMapShaderProgram.UniformBlockBind("ubo_window", windowUboBindingPoint);
        guiLightMapShaderProgram.Uniform("atlasSampler", 0);
        guiLightMapShaderProgram.Uniform("lightMapSampler", 1);

        grassAtlasEntry = atlas[grassTilePath];
        blankAtlasEntry = atlas[GrowableTextureAtlas3D.BlankName];

        InitializeLightMap();
        InitializeGui();

        windowUbo.Bind(windowUboBindingPoint);
        lightMapFBUbo.Bind(lightsUboBindingPoint);
    }

    public void Resize(int width, int height)
    {
        windowUbo.Data.WindowSize = new(width, height);
        windowUbo.UploadData();

        ResizeLightMap(width, height);
    }

    float pixelZoom = 35;

    static string GetImagePath(BuildingTemplate building) => $"Data/Buildings/{building.FileName}.png";
    static string GetImagePath(Resource resource) => $"Data/Resources/{resource.FileName}.png";
    static string GetImagePath(Villager _) => $"Data/Misc/villager.png";
    static string GetImagePath(Tree tree) => $"Data/Trees/{tree.FileName}.png";
    static string GetImagePath(PlaceableEntity entity) => entity switch
    {
        BuildingTemplate buildingTemplate => GetImagePath(buildingTemplate),
        ResourceBucket resource => GetImagePath(resource.ResourceQuantities.First(rq => rq.Quantity > 0).Resource),
        Villager villager => GetImagePath(villager),
        Tree tree => GetImagePath(tree),
        _ => throw new NotImplementedException()
    };
    const string grassTilePath = "Data/Misc/grass.png";

    readonly AtlasEntry grassAtlasEntry, blankAtlasEntry;

    public void Render(double deltaSec, double deltaUpdateTimeSec, double deltaRenderTimeSec)
    {
        // render lightmap to texture
        RenderLightMapToFrameBuffer();

        // render to screen
        GraphicsEngine.UnbindFrameBuffer();

        // render the background (tri0)
        var grassTileSize = 6;
        for (int y = 0; y < windowUbo.Data.WindowSize.Y / pixelZoom; y += grassTileSize)
            for (int x = 0; x < windowUbo.Data.WindowSize.X / pixelZoom; x += grassTileSize)
                ScreenFillQuad(Box2.FromCornerSize(new Vector2i(x, y), new(grassTileSize, grassTileSize)), new(.8f, .8f, .8f, 1), grassAtlasEntry);

        // store the actual entities' vertices(tri0)
        foreach (var entity in world.GetEntities())
            switch (entity)
            {
                case Building building:
                    var color = Colors4.White;
                    if (!building.IsBuilt) color *= new Vector4(1f, .4f, .4f, .4f);
                    ScreenFillQuad(Box2.FromCornerSize(building.Location, building.Width, building.Height), color, atlas[GetImagePath(building)]);
                    break;
                case Tree tree:
                    ScreenFillQuad(Box2.FromCornerSize(tree.Location, 1, 1), Colors4.White, atlas[GetImagePath(tree)]);
                    break;
                case ResourceBucket resourceBucket:
                    var resQ = resourceBucket.ResourceQuantities.Single(r => r.Quantity > 0);
                    ScreenFillQuad(Box2.FromCornerSize(resourceBucket.Location, 1, 1), Colors4.White, atlas[GetImagePath(resQ.Resource)]);
                    break;
                case Villager villager:
                    ScreenFillQuad(Box2.FromCornerSize(villager.InterpolatedLocation, 1, 1), Colors4.White, atlas[GetImagePath(villager)]);
                    break;
            }

        var countTri0 = guiVAO.Vertices.Count;

        // store the ai plan targets' vertices (lines)
        if (world.ShowDetails)
        {
            foreach (var villager in world.GetEntities<Villager>())
                if (villager.AIPlan?.FirstTarget is { } firstAiTarget)
                    ScreenLine(villager.InterpolatedBox, firstAiTarget.Box, Colors4.Yellow);
        }
        else if (world.SelectedEntity is Villager selectedVillager && selectedVillager.AIPlan?.FirstTarget is { } firstSelectedAiTarget)
            ScreenLine(selectedVillager.InterpolatedBox, firstSelectedAiTarget.Box, Colors4.Yellow);

        // selection box (lines)
        if (world.SelectedEntity is { } selectedEntity)
        {
            ScreenLineQuad(selectedEntity.Box, Colors4.Red);
            ScreenLineQuad(selectedEntity.InterpolatedBox, Colors4.White);
        }
        var countLines1 = guiVAO.Vertices.Count - countTri0;

        // render top layer (tri2)
        foreach (var entity in world.GetEntities<Villager>())
            switch (entity)
            {
                case Villager villager:
                    ScreenString(villager.Name, new() { Size = 16 }, new Vector2((villager.InterpolatedLocation.X + .5f) * pixelZoom, villager.InterpolatedLocation.Y * pixelZoom - 20),
                        Colors4.White, new(0, 0, 0, .4f), HorizontalAlignment.Center);
                    if (world.ShowDetails)
                        ScreenString(villager.AIPlan?.Description, new() { Size = 13 }, new Vector2((villager.InterpolatedLocation.X + .5f) * pixelZoom, (villager.InterpolatedLocation.Y + 1) * pixelZoom),
                            Colors4.White, new(0, 0, 0, .4f), HorizontalAlignment.Center);
                    break;
            }

        // building template
        if (world.CurrentBuildingTemplate is { })
        {
            var box = world.CurrentBuildingTemplate.GetBoxAtLocation(world.MouseWorldPosition);
            var valid = !world.GetEntities<Building>().Any(b => b.Box.Intersects(box));
            ScreenFillQuad(box, valid ? Colors4.Lime : Colors4.Red, atlas[GetImagePath(world.CurrentBuildingTemplate)]);
        }

        // render gui (tri2)
        RenderGui();

        var countTri2 = guiVAO.Vertices.Count - countTri0 - countLines1;

        guiVAO.UploadNewData();

        // draw the world (with light mapping)
        guiLightMapShaderProgram.Use();
        guiLightMapShaderProgram.Uniform("ambientColor", new Vector4(.3f, .3f, .3f, 1f));
        atlas.Bind(0);
        lightMapTexture.Bind(1);

        GraphicsEngine.Viewport(0, 0, (int)windowUbo.Data.WindowSize.X, (int)windowUbo.Data.WindowSize.Y);
        GraphicsEngine.BlendNormalAlpha();

        guiVAO.Draw(PrimitiveType.Triangles, vertexOrIndexCount: countTri0);

        // draw the gui overlays, which shouldn't be light mapped
        guiShaderProgram.Use();
        guiVAO.Draw(PrimitiveType.Lines, countTri0, countLines1);
        guiVAO.Draw(PrimitiveType.Triangles, countTri0 + countLines1, countTri2);

        // frame data
        FrameData.NewFrame(TimeSpan.FromSeconds(deltaSec), TimeSpan.FromSeconds(deltaUpdateTimeSec), TimeSpan.FromSeconds(deltaRenderTimeSec));
    }

    public Vector2i GetLocationFromScreenPoint(Vector2i screenPoint) => screenPoint / (int)pixelZoom;
}
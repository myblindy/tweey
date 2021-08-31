namespace Tweey.Renderer;

class WorldRenderer
{
    readonly World world;
    readonly GrowableTextureAtlas3D atlas = new(2048, 2048, 5);

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

    public WorldRenderer(World world)
    {
        this.world = world;
        shaderProgram.UniformBlockBind("ubo_window", windowUboBindingPoint);
        shaderProgram.Uniform("atlasSampler", 0);
    }

    public void Resize(int width, int height)
    {
        windowUbo.Data.WindowSize = new Vector2(width, height);
        windowUbo.Update();
    }

    float pixelZoom = 45;

    static readonly Vector4 colorWhite = new(1, 1, 1, 1);
    static readonly Vector4 colorRed = new(1, 0, 0, 1);
    static readonly Vector4 colorCyan = new(0, 1, 1, 1);
    static readonly Vector4 colorYellow = new(1, 1, 0, 1);

    public void Render(double _)
    {
        vaoGui.Vertices.Clear();

        void worldQuad(Box2 box, Vector4 color, AtlasEntry entry)
        {
            var br = box.BottomRight + Vector2.One;
            vaoGui.Vertices.Add(new(box.TopLeft * pixelZoom, color, entry.TextureCoordinate0));
            vaoGui.Vertices.Add(new(br * pixelZoom, color, entry.TextureCoordinate1));
            vaoGui.Vertices.Add(new(new((box.Right + 1) * pixelZoom, box.Top * pixelZoom), color, new(entry.TextureCoordinate1.X, entry.TextureCoordinate0.Y, entry.TextureCoordinate0.Z)));

            vaoGui.Vertices.Add(new(new(box.Left * pixelZoom, (box.Bottom + 1) * pixelZoom), color, new(entry.TextureCoordinate0.X, entry.TextureCoordinate1.Y, entry.TextureCoordinate0.Z)));
            vaoGui.Vertices.Add(new(br * pixelZoom, color, entry.TextureCoordinate1));
            vaoGui.Vertices.Add(new(box.TopLeft * pixelZoom, color, entry.TextureCoordinate0));
        }

        void worldLine(Box2 box1, Box2 box2, Vector4 color)
        {
            var blankEntry = atlas[GrowableTextureAtlas3D.BlankName];
            vaoGui.Vertices.Add(new((box1.Center + new Vector2(.5f, .5f)) * pixelZoom, color, blankEntry.TextureCoordinate0));
            vaoGui.Vertices.Add(new((box2.Center + new Vector2(.5f, .5f)) * pixelZoom, color, blankEntry.TextureCoordinate1));
        }

        // store the actual entities' vertices
        foreach (var entity in world.GetEntities())
            switch (entity)
            {
                case Building building:
                    worldQuad(Box2.FromCornerSize(building.Location, building.Width, building.Height), building.Color, atlas[$"Data/Buildings/{building.FileName}.png"]);
                    break;
                case ResourceBucket resourceBucket:
                    var resQ = resourceBucket.ResourceQuantities.Single(r => r.Quantity > 0);
                    worldQuad(Box2.FromCornerSize(resourceBucket.Location, 1, 1), colorWhite, atlas[$"Data/Resources/{resQ.Resource.FileName}.png"]);
                    break;
                case Villager villager:
                    worldQuad(Box2.FromCornerSize(villager.Location, 1, 1), colorWhite, atlas["Data/Misc/villager.png"]);
                    break;
            }
        var entityVertexCount = vaoGui.Vertices.Length;

        // store the ai plan targets' vertices
        foreach (var villager in world.GetEntities<Villager>())
            if (villager.AIPlan?.FirstTarget is { } firstAiTarget)
                worldLine(villager.Box, firstAiTarget.Box, colorYellow);
        var aiPlanVertexCount = vaoGui.Vertices.Length - entityVertexCount;

        shaderProgram.Use();
        atlas.Bind();
        windowUbo.Bind(windowUboBindingPoint);
        vaoGui.Draw(PrimitiveType.Triangles, vertexOrIndexCount: entityVertexCount);
        vaoGui.Draw(PrimitiveType.Lines, entityVertexCount, aiPlanVertexCount);
    }
}

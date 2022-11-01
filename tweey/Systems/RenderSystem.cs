using System.Diagnostics;

namespace Tweey.Systems;

[EcsSystem(Archetypes.Render)]
partial class RenderSystem
{
    readonly World world;
    readonly ShaderPrograms shaderPrograms = new();
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
    public record struct GuiVertex(Vector2 Location, Vector4 Color, Vector3 Tex0);

    readonly StreamingVertexArrayObject<GuiVertex> guiVAO = new();
    readonly ShaderProgram guiShaderProgram;
    readonly ShaderProgram guiLightMapShaderProgram;
    readonly GuiSpace gui = new();

    const string grassTilePath = "Data/Misc/grass.png";
    readonly AtlasEntry grassAtlasEntry, blankAtlasEntry;

    readonly StaticVertexArrayObject<LightMapFBVertex> lightMapFBVao =
        new(new LightMapFBVertex[]
        {
                new(new(-1, -1), new (0, 0)),
                new(new(1, -1), new (1, 0)),
                new(new(-1, 1), new (0, 1)),

                new(new(-1, 1), new (0, 1)),
                new(new(1, -1), new (1, 0)),
                new(new(1, 1), new (1, 1)),
        });
    readonly ShaderProgram lightMapFBShaderProgram;
    readonly UniformBufferObject<LightMapFBUbo> lightMapFBUbo = new();

    Texture2D lightMapOcclusionTexture = null!;
    FrameBuffer lightMapOcclusionFrameBuffer = null!;
    readonly StreamingVertexArrayObject<LightMapOcclusionFBVertex> lightMapOcclusionVAO = new();
    readonly ShaderProgram lightMapOcclusionShaderProgram;
    readonly Texture2D lightMapOcclusionCircleTexture;

    const int lightsUboBindingPoint = 2;
    Texture2D lightMapTexture = null!;
    FrameBuffer lightMapFrameBuffer = null!;

    [VertexDefinition, StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct LightMapOcclusionFBVertex(Vector2 Location, Vector2 Tex0);

    [VertexDefinition, StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct LightMapFBVertex(Vector2 Location, Vector2 Tex0);

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    unsafe struct LightMapFBUbo
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Light
        {
            public Vector4 LocationAndAngle { get; private set; }
            public Vector4 RangeAndStartColor { get; private set; }

            public const int Size = sizeof(float) * 8;
            public static readonly Vector2 FullAngle = new(0, 1);

            public Light(Vector2 location, float range, Vector3 startColor, Vector2 angleMinMax)
            {
                LocationAndAngle = new(location, angleMinMax.X, angleMinMax.Y);
                RangeAndStartColor = new(range, startColor.X, startColor.Y, startColor.Z);
            }

            public void ClearToInvalid() =>
                (LocationAndAngle, RangeAndStartColor) = (new(-100000, -100000, FullAngle.X, FullAngle.Y), new(0, 0, 0, 0));
        }

        public const int MaxLightCount = 16;
        fixed byte Data[Light.Size * MaxLightCount];
        public ref Light this[int idx]
        {
            get
            {
                Debug.Assert(idx >= 0 && idx < MaxLightCount);
                fixed (byte* p = Data)
                    return ref ((Light*)p)[idx];
            }
        }
    }

    public RenderSystem(World world)
    {
        guiShaderProgram = new(shaderPrograms, DiskLoader.Instance.VFS, "gui");
        guiLightMapShaderProgram = new(shaderPrograms, DiskLoader.Instance.VFS, "gui-lightmap");

        this.world = world;

        var maxTextureSize = Math.Min(2048, GraphicsEngine.MaxTextureSize);
        atlas = new(maxTextureSize, maxTextureSize, 1, DiskLoader.Instance.VFS);
        fontRenderer = new(atlas, DiskLoader.Instance.VFS);

        guiShaderProgram.UniformBlockBind("ubo_window", windowUboBindingPoint);
        guiShaderProgram.Uniform("atlasSampler", 0);

        guiLightMapShaderProgram.UniformBlockBind("ubo_window", windowUboBindingPoint);
        guiLightMapShaderProgram.Uniform("atlasSampler", 0);
        guiLightMapShaderProgram.Uniform("lightMapSampler", 1);

        grassAtlasEntry = atlas[grassTilePath];
        blankAtlasEntry = atlas[GrowableTextureAtlas3D.BlankName];

        using (var lightMapOcclusionCircleTextureStream = DiskLoader.Instance.VFS.OpenRead(@"Data\Misc\large-circle.png")!)
            lightMapOcclusionCircleTexture = new(lightMapOcclusionCircleTextureStream,
                SizedInternalFormat.R8, minFilter: TextureMinFilter.NearestMipmapNearest, magFilter: TextureMagFilter.Nearest);

        lightMapFBShaderProgram = new(shaderPrograms, DiskLoader.Instance.VFS, "lightmap");
        lightMapOcclusionShaderProgram = new(shaderPrograms, DiskLoader.Instance.VFS, "lightmap-occlusion");

        lightMapFBShaderProgram.UniformBlockBind("ubo_window", windowUboBindingPoint);
        lightMapFBShaderProgram.UniformBlockBind("ubo_lights", lightsUboBindingPoint);
        lightMapFBShaderProgram.Uniform("occlusionSampler", 0);

        lightMapOcclusionShaderProgram.UniformBlockBind("ubo_window", windowUboBindingPoint);
        lightMapOcclusionShaderProgram.Uniform("circleSampler", 0);

        InitializeGui();

        windowUbo.Bind(windowUboBindingPoint);
        lightMapFBUbo.Bind(lightsUboBindingPoint);
    }

    [Message]
    public void ResizeMessage(int width, int height)
    {
        windowUbo.Data.WindowSize = new(width, height);
        windowUbo.UploadData();

        lightMapFrameBuffer?.Dispose();
        lightMapTexture?.Dispose();
        lightMapTexture = new(width, height, SizedInternalFormat.Rgba8, minFilter: TextureMinFilter.Nearest, magFilter: TextureMagFilter.Nearest);
        lightMapFrameBuffer = new(new[] { lightMapTexture });

        lightMapOcclusionFrameBuffer?.Dispose();
        lightMapOcclusionTexture?.Dispose();
        lightMapOcclusionTexture = new(width, height, SizedInternalFormat.R8);
        lightMapOcclusionFrameBuffer = new(new[] { lightMapOcclusionTexture });
    }

    unsafe void RenderLightMapToFrameBuffer()
    {
        // setup the occlusion map for rendering and build the occlusions
        void markOcclusionBox(Box2 box, bool circle = false, float scale = 1f)
        {
            var zoom = world.Zoom;
            var uvHalf = new Vector2(.5f);         // the center of the circle texture is white, use that for the box case

            var center = box.Center + new Vector2(.5f) - world.Offset;
            var rx = box.Size.X / 2 * scale;
            var ry = box.Size.Y / 2 * scale;

            lightMapOcclusionVAO.Vertices.Add(new((center + new Vector2(-rx, ry)) * zoom, circle ? new(0, 0) : uvHalf));
            lightMapOcclusionVAO.Vertices.Add(new((center + new Vector2(rx, -ry)) * zoom, circle ? new(1, 1) : uvHalf));
            lightMapOcclusionVAO.Vertices.Add(new((center + new Vector2(rx, ry)) * zoom, circle ? new(1, 0) : uvHalf));

            lightMapOcclusionVAO.Vertices.Add(new((center + new Vector2(-rx, -ry)) * zoom, circle ? new(0, 1) : uvHalf));
            lightMapOcclusionVAO.Vertices.Add(new((center + new Vector2(rx, -ry)) * zoom, circle ? new(1, 1) : uvHalf));
            lightMapOcclusionVAO.Vertices.Add(new((center + new Vector2(-rx, ry)) * zoom, circle ? new(0, 0) : uvHalf));
        }

        IterateComponents((in IterationResult w) =>
        {
            if (w.RenderableComponent.OcclusionScale > 0)
                markOcclusionBox(w.LocationComponent.Box, w.RenderableComponent.OcclusionCircle, w.RenderableComponent.OcclusionScale);
        });

        lightMapOcclusionVAO.UploadNewData();

        lightMapOcclusionFrameBuffer.Bind(FramebufferTarget.Framebuffer);
        GraphicsEngine.Clear();
        lightMapOcclusionShaderProgram.Use();
        lightMapOcclusionCircleTexture.Bind(0);
        GraphicsEngine.BlendAdditive();            // no alpha channel, use additive blending
        lightMapOcclusionVAO.Draw(PrimitiveType.Triangles);

        // setup the light map for rendering
        // upload the light data to the shader
        var lightCount = 0;

        // setup the re-callable engine to render the light maps
        lightMapFrameBuffer.Bind(FramebufferTarget.Framebuffer);
        GraphicsEngine.Clear();
        lightMapFBShaderProgram.Use();
        lightMapOcclusionTexture.Bind(0);
        GraphicsEngine.BlendAdditive();

        void renderLights()
        {
            if (lightCount == 0) return;

            for (int idx = lightCount; idx < LightMapFBUbo.MaxLightCount; ++idx)
                lightMapFBUbo.Data[idx].ClearToInvalid();

            lightMapFBUbo.UploadData();

            // render the light map
            lightMapFBVao.Draw(PrimitiveType.Triangles);
        }

        void addLight(Vector2 location, float range, Vector3 color, Vector2 angleMinMax)
        {
            lightMapFBUbo.Data[lightCount++] = new(location, range, color, angleMinMax);
            if (lightCount == LightMapFBUbo.MaxLightCount)
            {
                renderLights();
                lightCount = 0;
            }
        }

        Vector2 getAngleMinMaxFromHeading(double heading, double coneAngle) =>
            new((float)((-heading - coneAngle + 2.25) % 1.0), (float)((-heading + coneAngle + 2.25) % 1.0));

        // call the engine once for each light
        IterateComponents((in IterationResult w) =>
        {
            if (w.RenderableComponent.LightEmission.W == 0)
                return;

            addLight((w.LocationComponent.Box.Center + new Vector2(.5f) - world.Offset) * world.Zoom, w.RenderableComponent.LightRange * world.Zoom,
                w.RenderableComponent.LightEmission.GetXYZ(), w.RenderableComponent.LightFullCircle ? LightMapFBUbo.Light.FullAngle :
                    getAngleMinMaxFromHeading(EcsCoordinator.GetHeadingComponent(w.Entity).Heading, w.RenderableComponent.LightAngleRadius));

            //if (entity is Villager villager)
            //    addLight(new Vector2(entity.InterpolatedLocation.X + .5f - world.Offset.X, entity.InterpolatedLocation.Y + .5f - world.Offset.Y) * world.Zoom, 12 * world.Zoom,
            //        lightCount == 1 ? new(.5f, .5f, .9f) : new(.9f, .5f, .5f), getAngleMinMaxFromHeading(villager.Heading, .1));
            //else if (entity is Building { IsBuilt: true, Name: "Siren" })
            //{
            //    const float range = 12;
            //    const float coneAngle = .25f / 2;
            //    var heading = (float)(world.TotalTime.TotalSeconds / 4) % 1f;

            //    var red = new Vector3(.6f, .1f, .1f);
            //    var blue = new Vector3(.1f, .1f, .6f);
            //    addLight((entity.Center + new Vector2(.5f) - world.Offset) * world.Zoom, range * world.Zoom, red, getAngleMinMaxFromHeading(heading, coneAngle));
            //    addLight((entity.Center + new Vector2(.5f) - world.Offset) * world.Zoom, range * world.Zoom, blue, getAngleMinMaxFromHeading(heading + .25f, coneAngle));
            //    addLight((entity.Center + new Vector2(.5f) - world.Offset) * world.Zoom, range * world.Zoom, red, getAngleMinMaxFromHeading(heading + .5f, coneAngle));
            //    addLight((entity.Center + new Vector2(.5f) - world.Offset) * world.Zoom, range * world.Zoom, blue, getAngleMinMaxFromHeading(heading + .75f, coneAngle));
            //}
            //else if (entity is Building { IsBuilt: true, EmitLight: { } emitLight })
            //    addLight((entity.Center + new Vector2(.5f) - world.Offset) * world.Zoom, emitLight.Range * world.Zoom, emitLight.Color, LightMapFBUbo.Light.FullAngle);
        });

        if (world.DebugShowLightAtMouse)
        {
            var totalTimeSec = (float)world.TotalTime.TotalSeconds;
            addLight(new Vector2(world.MouseScreenPosition.X, world.MouseScreenPosition.Y) - world.Offset * world.Zoom, 16 * world.Zoom,
                new(MathF.Sin(totalTimeSec / 2f) / 2 + 1, MathF.Sin(totalTimeSec / 4f) / 2 + 1, MathF.Sin(totalTimeSec / 6f) / 2 + 1),
                LightMapFBUbo.Light.FullAngle);
        }

        renderLights();
    }

    public void Run(double deltaSec, double updateDeltaSec, double renderDeltaSec)
    {
        // render lightmap to texture
        RenderLightMapToFrameBuffer();

        // render to screen
        GraphicsEngine.UnbindFrameBuffer();

        // render the background (tri0)
        const int grassTileSize = 6;
        var normalizedGrassOffset = (world.Offset / grassTileSize).ToVector2i().ToNumericsVector2() * grassTileSize;

        for (int y = -grassTileSize; y < windowUbo.Data.WindowSize.Y / world.Zoom + grassTileSize; y += grassTileSize)
            for (int x = -grassTileSize; x < windowUbo.Data.WindowSize.X / world.Zoom + grassTileSize; x += grassTileSize)
            {
                ScreenFillQuad(Box2.FromCornerSize(new Vector2(x, y) + normalizedGrassOffset,
                    new(grassTileSize)), new(.8f, .8f, .8f, 1), grassAtlasEntry);
            }
    }
}

using System.Diagnostics;
using Tweey.Renderer.Textures;
using Tweey.Renderer.VertexArrayObjects;

namespace Tweey.Renderer;

public partial class WorldRenderer
{
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
    readonly ShaderProgram lightMapFBShaderProgram = new("lightmap");
    readonly UniformBufferObject<LightMapFBUbo> lightMapFBUbo = new();

    Texture2D lightMapOcclusionTexture = null!;
    FrameBuffer lightMapOcclusionFrameBuffer = null!;
    readonly StreamingVertexArrayObject<LightMapOcclusionFBVertex> lightMapOcclusionVAO = new();
    readonly ShaderProgram lightMapOcclusionShaderProgram = new("lightmap-occlusion");
    readonly Texture2D lightMapOcclusionCircleTexture =
        new(@"Data\Misc\large-circle.png", SizedInternalFormat.R8, minFilter: TextureMinFilter.NearestMipmapNearest, magFilter: TextureMagFilter.Nearest);

    const int lightsUboBindingPoint = 2;
    Texture2D lightMapTexture = null!;
    FrameBuffer lightMapFrameBuffer = null!;

    [VertexDefinition, StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LightMapOcclusionFBVertex
    {
        public Vector2 Location;
        public Vector2 Tex0;

        public LightMapOcclusionFBVertex(Vector2 location, Vector2 tex0)
        {
            Location = location;
            Tex0 = tex0;
        }
    }

    [VertexDefinition, StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LightMapFBVertex
    {
        public Vector2 Location;
        public Vector2 Tex0;

        public LightMapFBVertex(Vector2 location, Vector2 tex0)
        {
            Location = location;
            Tex0 = tex0;
        }
    }

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

    void InitializeLightMap()
    {
        lightMapFBShaderProgram.UniformBlockBind("ubo_window", windowUboBindingPoint);
        lightMapFBShaderProgram.UniformBlockBind("ubo_lights", lightsUboBindingPoint);
        lightMapFBShaderProgram.Uniform("occlusionSampler", 0);

        lightMapOcclusionShaderProgram.UniformBlockBind("ubo_window", windowUboBindingPoint);
        lightMapOcclusionShaderProgram.Uniform("circleSampler", 0);
    }

    void ResizeLightMap(int width, int height)
    {
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
            var zoom = pixelZoom;
            var uvHalf = new Vector2(.5f, .5f);         // the center of the circle texture is white, use that for the box case

            var center = box.Center + new Vector2(.5f, .5f);
            var rx = box.Size.X / 2 * scale;
            var ry = box.Size.Y / 2 * scale;

            lightMapOcclusionVAO.Vertices.Add(new((center + new Vector2(-rx, ry)) * zoom, circle ? new(0, 0) : uvHalf));
            lightMapOcclusionVAO.Vertices.Add(new((center + new Vector2(rx, -ry)) * zoom, circle ? new(1, 1) : uvHalf));
            lightMapOcclusionVAO.Vertices.Add(new((center + new Vector2(rx, ry)) * zoom, circle ? new(1, 0) : uvHalf));

            lightMapOcclusionVAO.Vertices.Add(new((center + new Vector2(-rx, -ry)) * zoom, circle ? new(0, 1) : uvHalf));
            lightMapOcclusionVAO.Vertices.Add(new((center + new Vector2(rx, -ry)) * zoom, circle ? new(1, 1) : uvHalf));
            lightMapOcclusionVAO.Vertices.Add(new((center + new Vector2(-rx, ry)) * zoom, circle ? new(0, 0) : uvHalf));
        }

        foreach (var entity in world.GetEntities())
            if (entity is Tree)
                markOcclusionBox(entity.Box, true, .3f);
            else if (entity is Building { IsBuilt: true } building)
            {
                if (building.EmitLight is not null && building.Name != "Siren")
                    markOcclusionBox(building.Box, false);
            }

        lightMapOcclusionVAO.UploadNewData();

        lightMapOcclusionFrameBuffer.Bind(FramebufferTarget.Framebuffer);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        lightMapOcclusionShaderProgram.Use();
        lightMapOcclusionCircleTexture.Bind(0);
        GL.BlendFunc(BlendingFactor.One, BlendingFactor.One);            // no alpha channel, use additive blending
        lightMapOcclusionVAO.Draw(PrimitiveType.Triangles);

        // setup the light map for rendering
        // upload the light data to the shader
        var lightCount = 0;

        // setup the re-callable engine to render the light maps
        lightMapFrameBuffer.Bind(FramebufferTarget.Framebuffer);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        lightMapFBShaderProgram.Use();
        lightMapOcclusionTexture.Bind(0);
        GL.BlendFunc(BlendingFactor.One, BlendingFactor.One);   // additive blending

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
        foreach (var entity in world.GetEntities())
            if (entity is Villager villager)
                addLight(new Vector2(entity.InterpolatedLocation.X + .5f, entity.InterpolatedLocation.Y + .5f) * pixelZoom, 12 * pixelZoom,
                    lightCount == 1 ? new(.5f, .5f, .9f) : new(.9f, .5f, .5f), getAngleMinMaxFromHeading(villager.Heading, .1));
            else if (entity is Building { IsBuilt: true, Name: "Siren" })
            {
                const float range = 12;
                const float coneAngle = .25f / 2;
                var heading = (float)(world.TotalTime.TotalSeconds / 4) % 1f;

                var red = new Vector3(.6f, .1f, .1f);
                var blue = new Vector3(.1f, .1f, .6f);
                addLight((entity.Center + new Vector2(.5f)) * pixelZoom, range * pixelZoom, red, getAngleMinMaxFromHeading(heading, coneAngle));
                addLight((entity.Center + new Vector2(.5f)) * pixelZoom, range * pixelZoom, blue, getAngleMinMaxFromHeading(heading + .25f, coneAngle));
                addLight((entity.Center + new Vector2(.5f)) * pixelZoom, range * pixelZoom, red, getAngleMinMaxFromHeading(heading + .5f, coneAngle));
                addLight((entity.Center + new Vector2(.5f)) * pixelZoom, range * pixelZoom, blue, getAngleMinMaxFromHeading(heading + .75f, coneAngle));
            }
            else if (entity is Building { IsBuilt: true, EmitLight: { } emitLight, Name: { } name })
                addLight((entity.Center + new Vector2(.5f)) * pixelZoom, emitLight.Range * pixelZoom, emitLight.Color, LightMapFBUbo.Light.FullAngle);

        if (world.DebugShowLightAtMouse)
        {
            var totalTimeSec = (float)world.TotalTime.TotalSeconds;
            addLight(new Vector2(world.MouseScreenPosition.X, world.MouseScreenPosition.Y), 16 * pixelZoom,
                new(MathF.Sin(totalTimeSec / 2f) / 2 + 1, MathF.Sin(totalTimeSec / 4f) / 2 + 1, MathF.Sin(totalTimeSec / 6f) / 2 + 1),
                LightMapFBUbo.Light.FullAngle);
        }

        renderLights();
    }
}

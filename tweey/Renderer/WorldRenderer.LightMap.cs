using System;
using System.Diagnostics;
using System.Drawing;
using Tweey.Actors;
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
    [SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Vertex structure definition")]
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
    [SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Vertex structure definition")]
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
            public Vector4 Location;
            public Vector4 RangeAndStartColor;

            public const int Size = sizeof(float) * 8;

            public Light(Vector2 location, float range, Vector3 startColor)
            {
                Location = new(location, 0, 0);
                RangeAndStartColor = new(range, startColor.X, startColor.Y, startColor.Z);
            }

            public void ClearToInvalid() =>
                (Location, RangeAndStartColor) = (new(-100000, -100000, 0, 0), new(0, 0, 0, 0));
        }

        Vector4 ActualLightCountAndZero;
        public int ActualLightCount { get => (int)ActualLightCountAndZero.X; set => ActualLightCountAndZero.X = value; }

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
        lightMapTexture = new(width, height, SizedInternalFormat.Rgba8);
        lightMapFrameBuffer = new(new[] { lightMapTexture });

        lightMapOcclusionFrameBuffer?.Dispose();
        lightMapOcclusionTexture?.Dispose();
        lightMapOcclusionTexture = new(width, height, SizedInternalFormat.R8);
        lightMapOcclusionFrameBuffer = new(new[] { lightMapOcclusionTexture });
    }

    unsafe void RenderLightMapToFrameBuffer(out ulong drawCalls, out ulong tris)
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
                markOcclusionBox(building.Box, false);

        lightMapOcclusionVAO.UploadNewData();

        lightMapOcclusionFrameBuffer.Bind(FramebufferTarget.Framebuffer);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        lightMapOcclusionShaderProgram.Use();
        lightMapOcclusionCircleTexture.Bind(0);
        GL.BlendFunc(BlendingFactor.One, BlendingFactor.One);            // no alpha channel, use additive blending
        lightMapOcclusionVAO.Draw(PrimitiveType.Triangles);

        // setup the light map for rendering
        // upload the light data to the shader
        lightMapFBUbo.Data.ActualLightCount = 0;
        foreach (var villager in world.GetEntities<Villager>())
            lightMapFBUbo.Data[lightMapFBUbo.Data.ActualLightCount++] =
                new(new Vector2(villager.Location.X + .5f, villager.Location.Y + .5f) * pixelZoom, 12 * pixelZoom,
                    lightMapFBUbo.Data.ActualLightCount == 1 ? new(.5f, .5f, .9f) : new(.9f, .5f, .5f));

        if (world.DebugShowLightAtMouse)
        {
            var totalTimeSec = (float)world.TotalTime.TotalSeconds;
            lightMapFBUbo.Data[lightMapFBUbo.Data.ActualLightCount++] =
                new(new Vector2(world.MouseScreenPosition.X, world.MouseScreenPosition.Y), 16 * pixelZoom,
                    new(MathF.Sin(totalTimeSec / 2f) / 2 + 1, MathF.Sin(totalTimeSec / 4f) / 2 + 1, MathF.Sin(totalTimeSec / 6f) / 2 + 1));
        }

        for (int idx = lightMapFBUbo.Data.ActualLightCount; idx < LightMapFBUbo.MaxLightCount; ++idx)
            lightMapFBUbo.Data[idx].ClearToInvalid();

        lightMapFBUbo.UploadData();

        // render the light map
        lightMapFrameBuffer.Bind(FramebufferTarget.Framebuffer);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        lightMapFBShaderProgram.Use();
        lightMapOcclusionTexture.Bind(0);

        GL.BlendFunc(BlendingFactor.One, BlendingFactor.One);   // additive blending
        lightMapFBVao.Draw(PrimitiveType.Triangles);

        drawCalls = 1;
        tris = 2;
    }
}

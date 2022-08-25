using System.Diagnostics;
using Tweey.Renderer.Textures;

namespace Tweey.Renderer;

public partial class WorldRenderer
{
    StaticVertexArrayObject<LightMapFBVertex> lightMapFBVao;
    readonly ShaderProgram lightMapFBShaderProgram = new("lightmap");
    readonly UniformBufferObject<LightMapFBUbo> lightMapFBUbo = new();
    Texture2D lightMapOcclusionTexture = null!;
    byte[] lightMapOcclusionTextureBacking = null!;
    const int lightsUboBindingPoint = 2;
    Texture2D lightMapTexture = null!;
    FrameBuffer lightMapFrameBuffer = null!;
    Vector2i lightMapCellsSize;

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
        }

        Vector4 ActualLightCountAndCellSizeAndZero;
        public int ActualLightCount { get => (int)ActualLightCountAndCellSizeAndZero.X; set => ActualLightCountAndCellSizeAndZero.X = value; }
        public Vector2 CellSize
        {
            get => new(ActualLightCountAndCellSizeAndZero.Y, ActualLightCountAndCellSizeAndZero.Z);
            set => ActualLightCountAndCellSizeAndZero = new(ActualLightCountAndCellSizeAndZero.X, value.X, value.Y, 0);
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

    [MemberNotNull(nameof(lightMapFBVao))]
    void InitializeLightMap()
    {
        lightMapFBVao = new(new LightMapFBVertex[]
        {
            new(new(-1, -1), new (0, 0)),
            new(new(1, -1), new (1, 0)),
            new(new(-1, 1), new (0, 1)),

            new(new(-1, 1), new (0, 1)),
            new(new(1, -1), new (1, 0)),
            new(new(1, 1), new (1, 1)),
        });

        lightMapFBShaderProgram.Uniform("occlusionSampler", 0);
    }

    void ResizeLightMap(int width, int height)
    {
        lightMapFrameBuffer?.Dispose();
        lightMapTexture?.Dispose();

        var cellsX = (int)Math.Ceiling(width / pixelZoom) * 3;
        var cellsY = (int)Math.Ceiling(height / pixelZoom) * 3;
        lightMapTexture = new(cellsX, cellsY, SizedInternalFormat.Rgba8);
        lightMapFrameBuffer = new(new[] { lightMapTexture });

        lightMapOcclusionTexture?.Dispose();
        lightMapOcclusionTexture = new(cellsX, cellsY, SizedInternalFormat.R8);
        lightMapOcclusionTextureBacking = new byte[cellsX * cellsY];

        lightMapCellsSize = new(cellsX, cellsY);
    }

    unsafe void RenderLightMapToFrameBuffer(out ulong drawCalls, out ulong tris)
    {
        // build the occlusions
        Array.Clear(lightMapOcclusionTextureBacking);

        void markOcclusionCenterVector(Vector2i worldPosition)
        {
            if (worldPosition.X * 3 + 1 is { } x && x >= 0 && x < lightMapCellsSize.X
                && worldPosition.Y * 3 + 1 is { } y && y >= 0 && y < lightMapCellsSize.Y)
            {
                lightMapOcclusionTextureBacking[y * lightMapCellsSize.X + x] = byte.MaxValue;
            }
        }

        void markOcclusionBox(Box2 box)
        {
            var boxSize = box.Size;
            int xStart = (int)box.Left * 3, xEnd = xStart + (int)boxSize.X * 3;
            int yStart = (int)box.Top * 3, yEnd = yStart + (int)boxSize.Y * 3;

            xStart = Math.Max(0, xStart); xEnd = Math.Min(xEnd, lightMapCellsSize.X);
            yStart = Math.Max(0, yStart); yEnd = Math.Min(yEnd, lightMapCellsSize.Y);

            for (var y = yStart; y <= yEnd; ++y)
                for (var x = xStart; x <= xEnd; ++x)
                    lightMapOcclusionTextureBacking[y * lightMapCellsSize.X + x] = byte.MaxValue;
        }

        foreach (var entity in world.GetEntities())
            if (entity is Tree)
                markOcclusionCenterVector(entity.Location.ToVector2i());
            else if (entity is Building { IsBuilt: true } building)
                markOcclusionBox(building.Box);

        // upload the occlusions
        fixed (byte* p = lightMapOcclusionTextureBacking)
        {
            GL.PixelStorei(PixelStoreParameter.UnpackAlignment, 1);
            GL.PixelStorei(PixelStoreParameter.UnpackRowLength, lightMapCellsSize.X);
            GL.TextureSubImage2D(lightMapOcclusionTexture.Handle, 0, 0, 0, lightMapCellsSize.X, lightMapCellsSize.Y, PixelFormat.Red, PixelType.UnsignedByte, p);
        }

        // setup the light map for rendering
        lightMapFrameBuffer.Bind(FramebufferTarget.Framebuffer);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        // upload the light data to the shader
        lightMapFBUbo.Data.CellSize = lightMapCellsSize.ToNumericsVector2();

        lightMapFBUbo.Data.ActualLightCount = 0;
        foreach (var villager in world.GetEntities<Villager>())
            lightMapFBUbo.Data[lightMapFBUbo.Data.ActualLightCount++] =
                new(new(villager.Location.X * 3 + 1, villager.Location.Y * 3 + 1), 12 * 3,
                    lightMapFBUbo.Data.ActualLightCount == 1 ? new(.5f, .5f, .9f) : new(.9f, .5f, .5f));

        if (world.DebugShowLightAtMouse)
        {
            var totalTimeSec = (float)world.TotalTime.TotalSeconds;
            lightMapFBUbo.Data[lightMapFBUbo.Data.ActualLightCount++] =
                new(new(world.MouseWorldPosition.X * 3 + 1, world.MouseWorldPosition.Y * 3 + 1), 16 * 3,
                    new(MathF.Sin(totalTimeSec / 2f) / 2 + 1, MathF.Sin(totalTimeSec / 4f) / 2 + 1, MathF.Sin(totalTimeSec / 6f) / 2 + 1));
        }

        lightMapFBUbo.Update();

        // render the light map
        lightMapFBShaderProgram.Use();
        lightMapFBShaderProgram.UniformBlockBind("ubo_lights", lightsUboBindingPoint);
        lightMapOcclusionTexture.Bind(0);

        GL.Viewport(0, 0, lightMapCellsSize.X, lightMapCellsSize.Y);
        GL.BlendFunc(BlendingFactor.One, BlendingFactor.One);   // additive blending
        lightMapFBVao.Draw(PrimitiveType.Triangles);

        drawCalls = 1;
        tris = 2;
    }
}

using System.Diagnostics;

namespace Tweey.Renderer;

public partial class WorldRenderer
{
    StaticVertexArrayObject<LightMapFBVertex> lightMapFBVao;
    readonly ShaderProgram lightMapFBShaderProgram = new("lightmap");
    readonly ShaderProgram lightMapScreenShaderProgram = new("lightmap-copy");
    readonly UniformBufferObject<LightMapFBUbo> lightMapFBUbo = new();
    const int lightsUboBindingPoint = 2;
    Texture2D? lightMapTexture;
    FrameBuffer? lightMapFrameBuffer;
    Vector2i lightMapCellsSize;

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
    }

    void ResizeLightMap(int width, int height)
    {
        lightMapFrameBuffer?.Dispose();
        lightMapTexture?.Dispose();

        var cellsX = (int)Math.Ceiling(width / pixelZoom) * 3;
        var cellsY = (int)Math.Ceiling(height / pixelZoom) * 3;
        lightMapTexture = new(cellsX, cellsY, SizedInternalFormat.Rgba8);
        lightMapFrameBuffer = new(new[] { lightMapTexture });

        lightMapCellsSize = new(cellsX, cellsY);
    }

    void RenderLightMapToFrameBuffer()
    {
        lightMapFrameBuffer!.Bind(FramebufferTarget.Framebuffer);
        GL.ClearColor(.4f, .4f, .4f, 1f);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        lightMapFBUbo.Data.CellSize = lightMapCellsSize.ToNumericsVector2();

        lightMapFBUbo.Data.ActualLightCount = 0;
        foreach (var villager in world.GetEntities<Villager>())
            lightMapFBUbo.Data[lightMapFBUbo.Data.ActualLightCount++] =
                new(new(villager.Location.X * 3 + 1, villager.Location.Y * 3 + 1), 6 * 3,
                    lightMapFBUbo.Data.ActualLightCount == 1 ? new(.3f, .3f, .8f) : new(.8f, .3f, .3f));

        lightMapFBUbo.Update();

        lightMapFBShaderProgram.Use();
        lightMapFBShaderProgram.UniformBlockBind("ubo_lights", lightsUboBindingPoint);

        GL.Viewport(0, 0, lightMapCellsSize.X, lightMapCellsSize.Y);
        GL.BlendFunc(BlendingFactor.One, BlendingFactor.One);   // additive blending
        lightMapFBVao.Draw(PrimitiveType.Triangles);
    }

    void RenderLightMapToScreen()
    {
        lightMapScreenShaderProgram.Use();

        lightMapScreenShaderProgram.Uniform("lightMapSampler", 0);
        lightMapTexture!.Bind();

        GL.BlendFunc(BlendingFactor.DstColor, BlendingFactor.OneMinusSrcAlpha);     // multiplicative
        lightMapFBVao.Draw(PrimitiveType.Triangles);
    }
}

using System.Diagnostics;

namespace Tweey.Renderer;

public partial class WorldRenderer
{
    StaticVertexArrayObject<LightMapFBVertex> lightMapFBVao;
    readonly ShaderProgram lightMapFBShaderProgram = new("lightmap");
    readonly UniformBufferObject<LightMapFBUbo> lightMapFBUbo = new();
    Texture2D? lightMapTexture;
    FrameBuffer? lightMapFrameBuffer;

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
            public Vector2 Location;
            public float Range;
            public Vector3 StartColor;

            public const int Size = sizeof(float) * 6;

            public Light(Vector2 location, float range, Vector3 startColor)
            {
                Location = location;
                Range = range;
                StartColor = startColor;
            }
        }

        public int ActualLightCount;

        public const int LightCount = 16;
        fixed byte Data[Light.Size * LightCount];
        public ref Light this[int idx]
        {
            get
            {
                Debug.Assert(idx >= 0 && idx < LightCount);
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
            new(new(-1, -1), new (1, 1)),
        });
    }

    void ResizeLightMap(int width, int height)
    {
        lightMapFrameBuffer?.Dispose();
        lightMapTexture?.Dispose();

        lightMapTexture = new((int)Math.Ceiling(width / pixelZoom) * 3, (int)Math.Ceiling(height / pixelZoom) * 3, SizedInternalFormat.Rgba8);
        lightMapFrameBuffer = new(new[] { lightMapTexture });
    }

    void RenderLightMap()
    {
        lightMapFrameBuffer!.Bind(FramebufferTarget.Framebuffer);
        GL.ClearColor(.4f, .4f, .4f, 1f);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        lightMapFBUbo.Data.ActualLightCount = 1;
        lightMapFBUbo.Data[0] = new(new(.5f, .5f), .1f, new(.8f, .1f, .1f));
        lightMapFBUbo.Update();

        lightMapFBShaderProgram.Use();
        lightMapFBVao.Draw(PrimitiveType.Triangles);
    }
}

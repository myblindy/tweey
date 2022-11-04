using System.Diagnostics;

namespace Tweey.Support;

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

[VertexDefinition, StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GuiVertex
{
    public Vector2 Location;
    public Vector4 Color;
    public Vector3 Tex0;

    public GuiVertex(Vector2 location, Vector4 color, Vector3 tex0) =>
        (Location, Color, Tex0) = (location, color, tex0);
}

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

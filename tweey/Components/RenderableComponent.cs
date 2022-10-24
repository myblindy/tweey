namespace Tweey.Components;

[EcsComponent]
struct RenderableComponent
{
    public AtlasEntry AtlasEntry { get; set; }

    public bool OcclusionCircle { get; set; }
    public float OcclusionScale { get; set; }

    public Vector4 LightEmission { get; set; }
    public float LightRange { get; set; }
    public bool LightFullCircle { get; set; }
    public float LightAngleRadius { get; set; }
}

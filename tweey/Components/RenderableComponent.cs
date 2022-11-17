namespace Tweey.Components;

[EcsComponent]
record struct RenderableComponent(string? AtlasEntryName, bool OcclusionCircle = false, float OcclusionScale = 0f,
    Vector4 LightEmission = default, float LightRange = 0f, bool LightFullCircle = true, float LightAngleRadius = 0f);

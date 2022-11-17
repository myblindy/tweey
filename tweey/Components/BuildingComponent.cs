namespace Tweey.Components;

[EcsComponent]
record struct BuildingComponent(BuildingTemplate Template, double BuildWorkTicks)
{
    public bool IsBuilt => BuildWorkTicks <= 0;
}

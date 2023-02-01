namespace Tweey.Components;

[EcsComponent]
struct BuildingComponent
{
    public BuildingComponent(BuildingTemplate template, double buildWorkTicks) : this()
    {
        Template = template;
        BuildWorkTicks = buildWorkTicks;
    }

    public BuildingTemplate Template { get; }

    double buildWorkTicks;
    public double BuildWorkTicks
    {
        get => buildWorkTicks; set
        {
            var oldIsBuilt = IsBuilt;

            buildWorkTicks = value;
            BuildWorkTicksChanged?.Invoke();

            if (IsBuilt != oldIsBuilt)
                IsBuiltChanged?.Invoke();
        }
    }
    public event Action? BuildWorkTicksChanged;

    public bool IsBuilt => BuildWorkTicks <= 0;
    public event Action? IsBuiltChanged;
}

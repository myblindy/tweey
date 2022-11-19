namespace Tweey.Components;

[EcsComponent]
record struct PlantComponent(PlantTemplate Template, double WorkTicks, CustomDateTime PlantedTime)
{
    public PlantComponent(PlantTemplate template, double workTicks) : this(template, workTicks, CustomDateTime.Invalid)
    {
    }

    public float GetGrowth(World world) =>
        (float)Math.Min(1, (world.WorldTime - PlantedTime).TotalDays / Template.DaysFromSpawnToFullGrowth);

    public bool IsMature(World world) =>
        GetGrowth(world) >= 1;
}

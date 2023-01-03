namespace Tweey.Components;

[EcsComponent]
struct VillagerComponent
{
    public Needs Needs { get; }
    public double MaxCarryWeight { get; }
    public double PickupSpeedMultiplier { get; }
    public double MovementRateMultiplier { get; }
    public double WorkSpeedMultiplier { get; }
    public double HarvestSpeedMultiplier { get; }
    public double PlantSpeed { get; }

    public VillagerComponent(double MaxCarryWeight, double PickupSpeedMultiplier, double MovementRateMultiplier,
        double WorkSpeedMultiplier, double HarvestSpeedMultiplier, double PlantSpeed, double TiredMax, double TiredDecayPerWorldSecond)
    {
        this.MaxCarryWeight = MaxCarryWeight;
        this.PickupSpeedMultiplier = PickupSpeedMultiplier;
        this.MovementRateMultiplier = MovementRateMultiplier;
        this.WorkSpeedMultiplier = WorkSpeedMultiplier;
        this.HarvestSpeedMultiplier = HarvestSpeedMultiplier;
        this.PlantSpeed = PlantSpeed;
        Needs = new Needs
        {
            Tired = TiredMax,
            TiredMax = TiredMax,
            TiredDecayPerWorldSecond = TiredDecayPerWorldSecond
        };
    }
}

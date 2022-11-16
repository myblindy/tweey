namespace Tweey.Components;

[EcsComponent]
record struct VillagerComponent(double MaxCarryWeight, double PickupSpeedMultiplier, double MovementRateMultiplier, 
    double WorkSpeedMultiplier, double HarvestSpeedMultiplier);

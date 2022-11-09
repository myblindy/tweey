namespace Tweey.Components;

[EcsComponent]
record struct VillagerComponent(string Name, double MaxCarryWeight, double PickupSpeedMultiplier, double MovementRateMultiplier, double WorkSpeedMultiplier);

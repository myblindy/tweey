namespace Tweey.Components;

[EcsComponent]
record struct BuildingComponent(bool IsBuilt, ResourceBucket BuildCost, double BuildWorkTicks);

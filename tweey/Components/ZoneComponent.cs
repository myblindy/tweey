namespace Tweey.Components;

[EcsComponent]
record struct ZoneComponent(ZoneType Type, PlantTemplate? PlantTemplate);

enum ZoneType { Storage, Grow, MarkHarvest }
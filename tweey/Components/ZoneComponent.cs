namespace Tweey.Components;

[EcsComponent]
record struct ZoneComponent(ZoneType Type);

enum ZoneType { Storage, Grow, MarkHarvest }
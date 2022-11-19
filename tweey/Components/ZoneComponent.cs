namespace Tweey.Components;

[EcsComponent]
record struct ZoneComponent(ZoneType Type, PlantTemplate? PlantTemplate)
{
    public HashSet<Vector2i> WorkedTiles { get; } = new();

    public ZoneComponent() : this(0, null) { }
}

enum ZoneType { Storage, Grow, MarkHarvest }
namespace Tweey.Components;

[EcsArchetypes]
static class Archetypes
{
    public const EcsComponents PlacedResource =
        EcsComponents.Resource | EcsComponents.Location | EcsComponents.Inventory | EcsComponents.Renderable;

    public const EcsComponents Building =
        EcsComponents.Building | EcsComponents.Location | EcsComponents.Inventory | EcsComponents.Workable;

    public const EcsComponents Plant =
        EcsComponents.Plant | EcsComponents.Location | EcsComponents.Inventory | EcsComponents.Workable;

    public const EcsComponents FarmedPlant =
        EcsComponents.Plant | EcsComponents.Location | EcsComponents.Inventory | EcsComponents.Workable | EcsComponents.PlantIsFarmed;

    public const EcsComponents Villager =
        EcsComponents.Villager | EcsComponents.Location | EcsComponents.Identity;

    public const EcsComponents Zone =
        EcsComponents.Zone | EcsComponents.Location;

    public const EcsComponents Render =
        EcsComponents.Renderable | EcsComponents.Location;

    public const EcsComponents Worker =
        EcsComponents.Worker | EcsComponents.Location | EcsComponents.Inventory | EcsComponents.Villager;
}

[EcsPartition(Archetypes.Render)]
partial class RenderPartitionByLocation
{
    public int Width => 60;
    public int Height => 60;

    public partial Vector2 GetWorldLocation(in LocationComponent LocationComponent, in RenderableComponent RenderableComponent) =>
        LocationComponent.Box.Center;
}

[EcsPartition(Archetypes.Plant)]
partial class PlantPartitionByLocation
{
    public int Width => 40;
    public int Height => 40;

    public partial Vector2 GetWorldLocation(in WorkableComponent WorkableComponent, in InventoryComponent InventoryComponent, in LocationComponent LocationComponent, in PlantComponent PlantComponent) =>
        LocationComponent.Box.Center;
}

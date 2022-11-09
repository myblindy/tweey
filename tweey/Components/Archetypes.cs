namespace Tweey.Components;

[EcsArchetypes]
static class Archetypes
{
    public const EcsComponents PlacedResource =
        EcsComponents.Resource | EcsComponents.Location | EcsComponents.Inventory | EcsComponents.Renderable;

    public const EcsComponents Building =
        EcsComponents.Building | EcsComponents.Location | EcsComponents.Inventory | EcsComponents.Workable;

    public const EcsComponents Villager =
        EcsComponents.Villager | EcsComponents.Location;

    public const EcsComponents Zone =
        EcsComponents.Zone | EcsComponents.Location;

    public const EcsComponents Render =
        EcsComponents.Renderable | EcsComponents.Location;

    public const EcsComponents Worker =
        EcsComponents.Worker | EcsComponents.Location | EcsComponents.Inventory | EcsComponents.Villager;
}

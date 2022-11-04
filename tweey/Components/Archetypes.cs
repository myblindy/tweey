namespace Tweey.Components;

[EcsArchetypes]
static class Archetypes
{
    public const EcsComponents PlacedResource =
        EcsComponents.Resource | EcsComponents.Location | EcsComponents.Inventory;

    public const EcsComponents Building =
        EcsComponents.Building | EcsComponents.Location;

    public const EcsComponents Villager =
        EcsComponents.Villager | EcsComponents.Location;

    public const EcsComponents Render =
        EcsComponents.Renderable | EcsComponents.Location;
}

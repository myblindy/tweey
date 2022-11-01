namespace Tweey.Components;

[EcsArchetypes]
static class Archetypes
{
    public const EcsComponents ResourceLocationInventory = EcsComponents.Resource | EcsComponents.Location | EcsComponents.Inventory;
    public const EcsComponents Render = EcsComponents.Renderable | EcsComponents.Location;
}

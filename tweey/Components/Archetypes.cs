namespace Tweey.Components;

[EcsArchetypes]
static class Archetypes
{
    public const EcsComponents PlacedResource =
        EcsComponents.Resource | EcsComponents.Location | EcsComponents.Inventory | EcsComponents.Renderable;

    public const EcsComponents Building =
        EcsComponents.Building | EcsComponents.Location | EcsComponents.Inventory | EcsComponents.Workable;

    public const EcsComponents Plant =
        EcsComponents.Tree | EcsComponents.Location | EcsComponents.Inventory | EcsComponents.Workable;

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

    public partial int GetLocation(in LocationComponent locationComponent, in RenderableComponent renderableComponent)
    {
        var worldSize = WorldSize - Vector2i.One;
        var x = (int)((locationComponent.Box.Center.X / worldSize.X) * (Width - 1));
        var y = (int)((locationComponent.Box.Center.Y / worldSize.Y) * (Height - 1));
        return y * Width + x;
    }
}

namespace Tweey.Components;

[EcsArchetypes]
static class Archetypes
{
    public const EcsComponents PlacedResource =
        EcsComponents.Resource | EcsComponents.Location | EcsComponents.Inventory | EcsComponents.Renderable;

    public const EcsComponents Building =
        EcsComponents.Building | EcsComponents.Location | EcsComponents.Inventory | EcsComponents.Workable;

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
    public int Width => 40;
    public int Height => 40;

    public partial int GetLocation(in LocationComponent locationComponent, in RenderableComponent renderableComponent, Box2 worldSize)
    {
        var x = locationComponent.Box.Center.X / worldSize.Size.X * Width;
        var y = locationComponent.Box.Center.Y / worldSize.Size.Y * Height;
        return (int)(y * Width + x);
    }
}

namespace Tweey.Systems;

[EcsSystem(Archetypes.PlacedResource)]
partial class SetPlacedResourceAtlasEntrySystem
{
    public void Run(double deltaSec, double updateDeltaSec, double renderDeltaSec) =>
        IterateComponents((in IterationResult w) => w.RenderableComponent.AtlasEntryName ??=
            w.InventoryComponent.Inventory.ResourceQuantities.FirstOrDefault()?.Resource.FileName is { } resFileName ? $"Data/Resources/{resFileName}.png" : null);
}

namespace Tweey.Systems;

[EcsSystem(Archetypes.PlacedResource)]
partial class SetPlacedResourceAtlasEntrySystem
{
    public partial void Run() =>
        IterateComponents((in IterationResult w) => w.RenderableComponent.AtlasEntryName ??=
            w.InventoryComponent.Inventory.GetResourceQuantities(ResourceMarker.Default).FirstOrDefault()?.Resource.FileName is { } resFileName ? $"Data/Resources/{resFileName}.png" : null);
}

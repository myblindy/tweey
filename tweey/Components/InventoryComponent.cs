namespace Tweey.Components;

[EcsComponent]
readonly record struct InventoryComponent
{
    public ResourceBucket Inventory { get; } = new();

    public InventoryComponent() { }
}

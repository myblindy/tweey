namespace Tweey.Loaders
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    class ResourceIn
    {
        public string Name { get; set; }
        public double Weight { get; set; }
        public float[] Color { get; set; }
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public class Resource
    {
        public string? Name { get; set; }
        public double Weight { get; set; }
        public Vector4 Color { get; set; }
    }

    public record ResourceQuantity(Resource Resource)
    {
        public ResourceQuantity(Resource resource, double quantity) : this(resource) =>
            Quantity = quantity;

        public double Quantity { get; set; }
        public double Weight => Resource.Weight * Quantity;
    }

    class ResourceTemplates : BaseTemplates<ResourceIn, Resource>
    {
        public ResourceTemplates(ILoader loader) : base(loader, "Resources", x => x.Name!)
        {
        }
    }
}

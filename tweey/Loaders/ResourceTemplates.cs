namespace Tweey.Loaders;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
public class ResourceIn
{
    public string Name { get; set; }
    public double Weight { get; set; }
    public double PickupSpeedMultiplier { get; set; }
    public double Nourishment { get; set; }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

public class Resource : ITemplateFileName
{
    public string Name { get; set; } = null!;
    public double Weight { get; set; }
    public double PickupSpeedMultiplier { get; set; }
    public double Nourishment { get; set; }
    public string FileName { get; set; } = null!;

    public string ImageFileName => $"Data/Resources/{FileName}.png";
}

public record ResourceQuantity(Resource Resource)
{
    public ResourceQuantity(Resource resource, double quantity) : this(resource) =>
        Quantity = quantity;

    public double Quantity { get; set; }
    public double Weight => Resource.Weight * Quantity;
    public double PickupSpeedMultiplier => Resource.PickupSpeedMultiplier * Quantity;
}

public class ResourceTemplates : BaseTemplates<ResourceIn, Resource>
{
    public ResourceTemplates(ILoader loader) : base(loader, "Resources", x => x.Name!)
    {
    }
}

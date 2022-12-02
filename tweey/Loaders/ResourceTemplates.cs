namespace Tweey.Loaders;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
public class ResourceIn
{
    public string Name { get; set; }
    public double Weight { get; set; }
    public double PickupSpeedMultiplier { get; set; }
    public double Nourishment { get; set; }
    public List<string> Groups { get; set; }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

public class Resource : ITemplateFileName
{
    public required string Name { get; set; }
    public required double Weight { get; set; }
    public required double PickupSpeedMultiplier { get; set; }
    public required double Nourishment { get; set; }
    public string FileName { get; set; } = null!;
    public required string[] Groups { get; set; }

    public string ImageFileName => $"Data/Resources/{FileName}.png";
}

public record ResourceQuantity(Resource Resource)
{
    public ResourceQuantity(Resource resource, int quantity) : this(resource) =>
        Quantity = quantity;

    public int Quantity { get; set; }
    public double Weight => Resource.Weight * Quantity;
    public double PickupSpeedMultiplier => Resource.PickupSpeedMultiplier * Quantity;

    public bool IsEmpty => Quantity == 0;

    public override string ToString() => $"{Quantity} {Resource.Name}";
}

public class ResourceTemplates : BaseTemplates<ResourceIn, Resource>
{
    public ResourceTemplates(ILoader loader) : base(loader, "Resources", x => x.Name!)
    {
    }
}

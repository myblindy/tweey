namespace Tweey.Loaders;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
public class ResourceIn
{
    public string Name { get; set; }
    public double Weight { get; set; }
    public float[] Color { get; set; }
    public double PickupSpeedMultiplier { get; set; }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

public class Resource
{
    public string? Name { get; set; }
    public double Weight { get; set; }
    public Vector4 Color { get; set; }
    public double PickupSpeedMultiplier { get; set; }
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
    static readonly IMapper mapper = new Mapper(new MapperConfiguration(cfg =>
        cfg.CreateMap<ResourceIn, Resource>()
            .ForMember(x => x.Color, opt => opt.MapFrom(src => src.Color.Length == 3 ? new Vector4(src.Color[0], src.Color[1], src.Color[2], 1) : new(src.Color)))));

    public ResourceTemplates(ILoader loader) : base(loader, mapper, "Resources", x => x.Name!)
    {
    }
}

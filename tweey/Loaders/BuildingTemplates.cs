namespace Tweey.Loaders;

public enum BuildingType { Production, Storage }

public class BuildingTemplate : PlaceableEntity, ITemplateFileName
{
    public override string? Name { get; set; }
    public BuildingType Type { get; set; }
    public Vector4 Color { get; set; }
    public string? FileName { get; set; }
    public int BuildWorkTicks { get; set; }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public ResourceBucket BuildCost { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
}

public class BuildingCostResouceTemplateIn
{
    public string? Resource { get; set; }
    public int Quantity { get; set; }
}

public class BuildingCostTemplateIn
{
    public int WorkTicks { get; set; }
    public List<BuildingCostResouceTemplateIn>? Resources { get; set; }
}

public class BuildingTemplateIn
{
    public string? Name { get; set; }
    public BuildingType Type { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public float[]? Color { get; set; }
    public BuildingCostTemplateIn? BuildCost { get; set; }
}

public class BuildingTemplates : BaseTemplates<BuildingTemplateIn, BuildingTemplate>
{
    public BuildingTemplates(ILoader loader, ResourceTemplates resourceTemplates)
        : base(loader, "Buildings", x => x.Name!, resourceTemplates)
    {
    }
}

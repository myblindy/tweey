namespace Tweey.Loaders;

public enum BuildingType { Production, Storage }

public class BuildingTemplate : PlaceableEntity, ITemplateFileName
{
    public override string Name { get; set; } = null!;
    public BuildingType Type { get; set; }
    public Vector4 Color { get; set; }
    public string FileName { get; set; } = null!;
    public int BuildWorkTicks { get; set; }
    public ResourceBucket BuildCost { get; set; } = null!;
}

public class BuildingCostResouceTemplateIn
{
    public string? Resource { get; set; }
    public int Quantity { get; set; }
}

public class BuildingCostTemplateIn
{
    public int WorkTicks { get; set; }
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "System.Text.Json doesn't support this")]
    public List<BuildingCostResouceTemplateIn> Resources { get; set; } = null!;
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

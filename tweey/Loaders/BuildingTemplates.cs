namespace Tweey.Loaders;

public enum BuildingType { Production, Storage }

public class BuildingProductionLineTemplate
{
    public ResourceBucket Inputs { get; set; } = null!;
    public ResourceBucket Outputs { get; set; } = null!;
}

public class BuildingLightTemplate
{
    public float Range { get; set; }
    [JsonConverter(typeof(Vector3JsonConverter))]
    public Vector3 Color { get; set; }
}

public class BuildingTemplate : PlaceableEntity, ITemplateFileName
{
    public override string Name { get; set; } = null!;
    public BuildingType Type { get; set; }
    public string FileName { get; set; } = null!;
    public int BuildWorkTicks { get; set; }
    public ResourceBucket BuildCost { get; set; } = null!;
    public ReadOnlyCollection<BuildingProductionLineTemplate> ProductionLines { get; set; } = null!;
    public BuildingLightTemplate? EmitLight { get; set; }
}

[SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "System.Text.Json doesn't support this")]
public class BuildingProductionLineTemplateIn
{
    public List<BuildingResouceQuantityTemplateIn> Inputs { get; set; } = null!;
    public List<BuildingResouceQuantityTemplateIn> Outputs { get; set; } = null!;
}

public class BuildingResouceQuantityTemplateIn
{
    public string? Resource { get; set; }
    public int Quantity { get; set; }
}

[SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "System.Text.Json doesn't support this")]
public class BuildingCostTemplateIn
{
    public int WorkTicks { get; set; }
    public List<BuildingResouceQuantityTemplateIn> Resources { get; set; } = null!;
}

[SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "System.Text.Json doesn't support this")]
public class BuildingTemplateIn
{
    public string? Name { get; set; }
    public BuildingType Type { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public BuildingCostTemplateIn? BuildCost { get; set; }
    public List<BuildingProductionLineTemplateIn>? ProductionLines { get; set; }
    public BuildingLightTemplate? EmitLight { get; set; }
}

public class BuildingTemplates : BaseTemplates<BuildingTemplateIn, BuildingTemplate>
{
    public BuildingTemplates(ILoader loader, ResourceTemplates resourceTemplates)
        : base(loader, "Buildings", x => x.Name!, resourceTemplates)
    {
    }
}

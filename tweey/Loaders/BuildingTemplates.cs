namespace Tweey.Loaders;

internal enum BuildingType { Production, Storage }

internal class BuildingProductionLineTemplate
{
    public ResourceBucket Inputs { get; set; } = null!;
    public ResourceBucket Outputs { get; set; } = null!;
    public int WorkTicks { get; set; }
}

internal class BuildingLightTemplate
{
    public float Range { get; set; }
    [JsonConverter(typeof(Vector3JsonConverter))]
    public Vector3 Color { get; set; }
}

internal class BuildingTemplate : ITemplateFileName
{
    public string Name { get; set; } = null!;
    public int Width { get; set; }
    public int Height { get; set; }
    public BuildingType Type { get; set; }
    public string FileName { get; set; } = null!;
    public int BuildWorkTicks { get; set; }
    public ResourceBucket BuildCost { get; set; } = null!;
    public int MaxWorkersAmount { get; set; }
    public ReadOnlyCollection<BuildingProductionLineTemplate> ProductionLines { get; set; } = null!;
    public BuildingLightTemplate? EmitLight { get; set; }

    public string ImageFileName => $"Data/Buildings/{FileName}.png";
}

internal class BuildingProductionLineTemplateIn
{
    public List<BuildingResouceQuantityTemplateIn> Inputs { get; set; } = null!;
    public List<BuildingResouceQuantityTemplateIn> Outputs { get; set; } = null!;
    public int WorkTicks { get; set; }
}

internal class BuildingResouceQuantityTemplateIn
{
    public string? Resource { get; set; }
    public int Quantity { get; set; }
}

internal class BuildingCostTemplateIn
{
    public int WorkTicks { get; set; }
    public List<BuildingResouceQuantityTemplateIn> Resources { get; set; } = null!;
}

internal class BuildingTemplateIn
{
    public string? Name { get; set; }
    public BuildingType Type { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public BuildingCostTemplateIn? BuildCost { get; set; }
    public int MaxWorkersAmount { get; set; }
    public List<BuildingProductionLineTemplateIn>? ProductionLines { get; set; }
    public BuildingLightTemplate? EmitLight { get; set; }
}

internal class BuildingTemplates : BaseTemplates<BuildingTemplateIn, BuildingTemplate>
{
    public BuildingTemplates(ILoader loader, ResourceTemplates resourceTemplates)
        : base(loader, "Buildings", x => x.FileName!, resourceTemplates)
    {
    }
}

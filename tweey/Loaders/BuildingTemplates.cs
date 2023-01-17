namespace Tweey.Loaders;

internal enum BuildingType { Production, Rest, Toilet, Chair, Table }

internal class BuildingProductionLineTemplate
{
    public required string Name { get; set; }
    public required BuildingResouceQuantityTemplate[] PossibleInputs { get; set; }
    public required ResourceBucket Outputs { get; set; }
    public required int WorkTicks { get; set; }
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
    public bool WorkInside { get; set; }
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
    public string Name { get; set; } = null!;
    public List<BuildingResouceQuantityTemplate> Inputs { get; set; } = null!;
    public List<BuildingResouceQuantityTemplate> Outputs { get; set; } = null!;
    public int WorkTicks { get; set; }
}

internal class BuildingResouceQuantityTemplate
{
    public string? Resource { get; set; }
    public int Quantity { get; set; }
}

internal class BuildingCostTemplateIn
{
    public int WorkTicks { get; set; }
    public List<BuildingResouceQuantityTemplate> Resources { get; set; } = null!;
}

internal class BuildingTemplateIn
{
    public string? Name { get; set; }
    public BuildingType Type { get; set; }
    public bool WorkInside { get; set; }
    public int Width { get; set; } = 1;
    public int Height { get; set; } = 1;
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

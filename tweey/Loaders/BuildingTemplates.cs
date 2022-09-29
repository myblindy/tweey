namespace Tweey.Loaders;

public enum BuildingType { Production, Storage }

public class BuildingProductionLineTemplate
{
    public ResourceBucket Inputs { get; set; } = null!;
    public ResourceBucket Outputs { get; set; } = null!;
    public int WorkTicks { get; set; }
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
    public override Box2 InterpolatedBox => Box;
    public override Vector2 InterpolatedLocation { get => Location; set => Location = value; }
    public BuildingType Type { get; set; }
    public string FileName { get; set; } = null!;
    public int BuildWorkTicks { get; set; }
    public ResourceBucket BuildCost { get; set; } = null!;
    public int MaxWorkersAmount { get; set; }
    public ReadOnlyCollection<BuildingProductionLineTemplate> ProductionLines { get; set; } = null!;
    public BuildingLightTemplate? EmitLight { get; set; }
}

public class BuildingProductionLineTemplateIn
{
    public List<BuildingResouceQuantityTemplateIn> Inputs { get; set; } = null!;
    public List<BuildingResouceQuantityTemplateIn> Outputs { get; set; } = null!;
    public int WorkTicks { get; set; }
}

public class BuildingResouceQuantityTemplateIn
{
    public string? Resource { get; set; }
    public int Quantity { get; set; }
}

public class BuildingCostTemplateIn
{
    public int WorkTicks { get; set; }
    public List<BuildingResouceQuantityTemplateIn> Resources { get; set; } = null!;
}

public class BuildingTemplateIn
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

public class BuildingTemplates : BaseTemplates<BuildingTemplateIn, BuildingTemplate>
{
    public BuildingTemplates(ILoader loader, ResourceTemplates resourceTemplates)
        : base(loader, "Buildings", x => x.FileName!, resourceTemplates)
    {
    }
}

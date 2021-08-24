namespace Tweey.Loaders;

public enum BuildingType { Production, Storage }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
public class BuildingTemplate : PlaceableEntity
{
    public string Name { get; set; }
    public BuildingType Type { get; set; }
    public Vector4 Color { get; set; }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

public class BuildingTemplateIn
{
    public string? Name { get; set; }
    public BuildingType Type { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public float[]? Color { get; set; }
}

public class BuildingTemplates : BaseTemplates<BuildingTemplateIn, BuildingTemplate>
{
        public BuildingTemplates(ILoader loader) : base(loader, "Buildings", x => x.Name)
    {
    }
}

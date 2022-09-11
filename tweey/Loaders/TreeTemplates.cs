namespace Tweey.Loaders;

public class TreeTemplate : PlaceableEntity, ITemplateFileName
{
    public TreeTemplate() => (Width, Height) = (1, 1);

    public override string Name { get; set; } = null!;
    public override Box2 InterpolatedBox => Box;
    public override Vector2 InterpolatedLocation { get => Location; set => Location = value; }
    public int WorkTicks { get; set; }
    public ResourceBucket Inventory { get; set; } = null!;
    public string FileName { get; set; } = null!;
}

public class TreeResouceTemplateIn
{
    public string Resource { get; set; } = null!;
    public int Quantity { get; set; }
}

public class TreeTemplateIn
{
    public string Name { get; set; } = null!;
    public int WorkTicks { get; set; }
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "System.Text.Json doesn't support this")]
    public List<TreeResouceTemplateIn>? ContainingResources { get; set; }
}

public class TreeTemplates : BaseTemplates<TreeTemplateIn, TreeTemplate>
{
    public TreeTemplates(ILoader loader, ResourceTemplates resourceTemplates)
        : base(loader, "Trees", x => x.Name!, resourceTemplates)
    {
    }
}

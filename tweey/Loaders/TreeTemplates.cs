namespace Tweey.Loaders;

public class TreeTemplate : PlaceableEntity, ITemplateFileName
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public TreeTemplate() => (Width, Height) = (1, 1);
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public override string Name { get; set; }
    public int WorkTicks { get; set; }
    public ResourceBucket Inventory { get; set; }
    public string FileName { get; set; }
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
    public List<TreeResouceTemplateIn>? ContainingResources { get; } = new();
}

public class TreeTemplates : BaseTemplates<TreeTemplateIn, TreeTemplate>
{
    public TreeTemplates(ILoader loader, ResourceTemplates resourceTemplates)
        : base(loader, "Trees", x => x.Name!, resourceTemplates)
    {
    }
}

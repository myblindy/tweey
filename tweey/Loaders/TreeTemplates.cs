namespace Tweey.Loaders;

internal class TreeTemplate : ITemplateFileName
{
    public string Name { get; set; } = null!;
    public int WorkTicks { get; set; }
    public ResourceBucket Inventory { get; set; } = null!;
    public string FileName { get; set; } = null!;
}

internal class TreeResouceTemplateIn
{
    public string Resource { get; set; } = null!;
    public int Quantity { get; set; }
}

internal class TreeTemplateIn
{
    public string Name { get; set; } = null!;
    public int WorkTicks { get; set; }
    public List<TreeResouceTemplateIn>? ContainingResources { get; set; }
}

internal class TreeTemplates : BaseTemplates<TreeTemplateIn, TreeTemplate>
{
    public TreeTemplates(ILoader loader, ResourceTemplates resourceTemplates)
        : base(loader, "Trees", x => x.Name!, resourceTemplates)
    {
    }
}

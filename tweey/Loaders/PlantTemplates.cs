using System.Globalization;
using System.Text.RegularExpressions;

namespace Tweey.Loaders;

internal class PlantTemplate : ITemplateFileName
{
    public required string Name { get; set; }
    public required int HarvestWorkTicks { get; set; }
    public required ResourceBucket Inventory { get; set; }
    public required double DaysFromSpawnToFullGrowth { get; set; }
    public required bool IsOccludingLight { get; set; }
    public required bool IsTree { get; set; }
    public string FileName { get; set; } = null!;

    public Collection<(double growth, string image)> Images { get; } = new();
    public string GetImageFileName(double growth)
    {
        var (_, lastImage) = Images[0];
        for (int i = 1; i < Images.Count; ++i)
            if (Images[i].growth > growth)
                return lastImage;
            else
                (_, lastImage) = Images[i];
        return lastImage;
    }
}

internal class PlantResouceTemplateIn
{
    public string Resource { get; set; } = null!;
    public int Quantity { get; set; }
}

internal class PlantTemplateIn
{
    public string Name { get; set; } = null!;
    public int HarvestWorkTicks { get; set; }
    public double DaysFromSpawnToFullGrowth { get; set; }
    public bool IsOccludingLight { get; set; }
    public bool IsTree { get; set; }
    public List<PlantResouceTemplateIn>? ContainingResources { get; set; }
}

internal partial class PlantTemplates : BaseTemplates<PlantTemplateIn, PlantTemplate>
{
    public PlantTemplates(ILoader loader, ResourceTemplates resourceTemplates)
        : base(loader, "Plants", x => x.FileName!, resourceTemplates)
    {
        var files = new List<(string name, string path, int value)>();
        foreach (var file in DiskLoader.Instance.VFS.EnumerateFiles("Data/Plants", SearchOption.TopDirectoryOnly))
            if (PathComponentExtractRegex().Match(file) is { Success: true } m)
                files.Add((m.Groups[2].Value, m.Groups[1].Value, m.Groups[3].Success ? int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture) : 0));

        foreach (var (name, path, value) in files.OrderBy(w => w.value))
            if (Keys.Contains(name))
                this[name].Images.Add((value / 100.0, path));
    }

    [GeneratedRegex("^(Data[/\\\\]Plants[/\\\\](.*?)(?:-(\\d+))?\\.png)$", RegexOptions.IgnoreCase)]
    private static partial Regex PathComponentExtractRegex();
}

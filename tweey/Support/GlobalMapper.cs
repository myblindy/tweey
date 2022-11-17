namespace Tweey.Support;

internal partial class InternalMapper : FastAutoMapper.FastAutoMapperBase { }

internal static class GlobalMapper
{
    public static InternalMapper Mapper { get; } = new();

    static GlobalMapper()
    {
        Mapper.CreateMap<BiomeIn, Biome>()
            .ForMember(x => x.Trees, (src, trees) => src.Trees?.Select(t => (((TreeTemplates)trees!)[t.Name], t.Chance)).ToArray() ?? Array.Empty<(TreeTemplate, double)>())
            .ForMember(x => x.TileName, src => src.TileName ?? src.Name);
        Mapper.CreateMap<BuildingTemplateIn, BuildingTemplate>()
            .ForMember(x => x.BuildWorkTicks, src => src.BuildCost!.WorkTicks)
            .ForMember(x => x.BuildCost, (src, resources) => new ResourceBucket(src.BuildCost!.Resources!.Select(rq => new ResourceQuantity(((ResourceTemplates)resources!)[rq.Resource!], rq.Quantity))))
            .ForMember(x => x.ProductionLines, (src, resources) => new((src.ProductionLines ?? Enumerable.Empty<BuildingProductionLineTemplateIn>())
                .Select(pl => new BuildingProductionLineTemplate
                {
                    Inputs = new(pl.Inputs.Select(rq => new ResourceQuantity(((ResourceTemplates)resources!)[rq.Resource!], rq.Quantity))),
                    Outputs = new(pl.Outputs.Select(rq => new ResourceQuantity(((ResourceTemplates)resources!)[rq.Resource!], rq.Quantity))),
                    WorkTicks = pl.WorkTicks
                })
                .ToArray()));
        Mapper.CreateMap<ResourceIn, Resource>();
        Mapper.CreateMap<TreeTemplateIn, TreeTemplate>()
            .ForMember(x => x.Inventory, (src, resources) => new ResourceBucket(src.ContainingResources!.Select(rq => new ResourceQuantity(((ResourceTemplates)resources!)[rq.Resource!], rq.Quantity))));
    }
}

namespace Tweey.Support;

internal partial class InternalMapper : FastAutoMapper.FastAutoMapperBase { }

internal static class GlobalMapper
{
    public static InternalMapper Mapper { get; } = new();

    static GlobalMapper()
    {
        Mapper.CreateMap<BuildingTemplate, Building>()
            .ForMember(x => x.BuildCost, src => src.BuildCost.Clone());
        Mapper.CreateMap<TreeTemplate, Tree>()
            .ForMember(x => x.Inventory, src => src.Inventory.Clone());
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

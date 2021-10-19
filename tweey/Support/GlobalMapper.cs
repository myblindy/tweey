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
            .ForMember(x => x.Color, src => src.Color!.Length == 3 ? new Vector4(src.Color[0], src.Color[1], src.Color[2], 1) : new(src.Color))
            .ForMember(x => x.BuildWorkTicks, src => src.BuildCost!.WorkTicks)
            .ForMember(x => x.BuildCost, (src, resources) => new ResourceBucket(src.BuildCost!.Resources!.Select(rq => new ResourceQuantity(((ResourceTemplates)resources)[rq.Resource!], rq.Quantity))));
        Mapper.CreateMap<ResourceIn, Resource>();
        Mapper.CreateMap<TreeTemplateIn, TreeTemplate>()
            .ForMember(x => x.Inventory, (src, resources) => new ResourceBucket(src.ContainingResources!.Select(rq => new ResourceQuantity(((ResourceTemplates)resources)[rq.Resource!], rq.Quantity))));
    }
}

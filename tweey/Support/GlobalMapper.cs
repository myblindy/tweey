namespace Tweey.Support;

internal partial class InternalMapper : FastAutoMapper.FastAutoMapperBase { }

internal static class GlobalMapper
{
    public static InternalMapper Mapper { get; } = new();

    static GlobalMapper()
    {
        Mapper.CreateMap<BuildingTemplate, Building>();
        Mapper.CreateMap<BuildingTemplateIn, BuildingTemplate>()
            .ForMember(x => x.Color, src => src.Color!.Length == 3 ? new Vector4(src.Color[0], src.Color[1], src.Color[2], 1) : new(src.Color));
        Mapper.CreateMap<ResourceIn, Resource>()
            .ForMember(x => x.Color, src => src.Color!.Length == 3 ? new Vector4(src.Color[0], src.Color[1], src.Color[2], 1) : new(src.Color));
    }
}

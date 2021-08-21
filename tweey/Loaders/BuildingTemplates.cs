using AutoMapper;
using System.Diagnostics.CodeAnalysis;

namespace Tweey.Loaders
{
    enum BuildingType { WorkPlace, Storage }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    class BuildingTemplate
    {
        public string Name { get; set; }
        public BuildingType Type { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public Vector4 Color { get; set; }
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    class BuildingTemplateIn
    {
        public string? Name { get; set; }
        public BuildingType Type { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public float[]? Color { get; set; }
    }

    class BuildingTemplates : BaseTemplates<BuildingTemplateIn, BuildingTemplate>
    {
        static readonly IMapper mapper = new Mapper(new MapperConfiguration(cfg =>
            cfg.CreateMap<BuildingTemplateIn, BuildingTemplate>()
                .ForMember(x => x.Color, opt => opt.MapFrom(src => src.Color!.Length == 3 ? new Vector4(src.Color[0], src.Color[1], src.Color[2], 1) : new(src.Color)))));

        public BuildingTemplates(ILoader loader) : base(loader, mapper, "Buildings", x => x.Name)
        {
        }
    }
}

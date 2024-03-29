﻿namespace Tweey.Support;

internal partial class InternalMapper : FastAutoMapper.FastAutoMapperBase { }

internal static class GlobalMapper
{
    public static InternalMapper Mapper { get; } = new();

    static GlobalMapper()
    {
        Mapper.CreateMap<BiomeIn, Biome>()
            .ForMember(x => x.TileName, src => src.TileName ?? src.Name)
            .ForMember(x => x.Plants, (src, plants) => src.Plants?.Select(pin => (((PlantTemplates)plants!)[pin.Name], pin.Chance)).ToArray() ?? Array.Empty<(PlantTemplate, double)>());
        Mapper.CreateMap<BuildingTemplateIn, BuildingTemplate>()
            .ForMember(x => x.BuildWorkTicks, src => src.BuildCost!.WorkTicks)
            .ForMember(x => x.BuildCost, (src, resources) => new ResourceBucket(src.BuildCost!.Resources!.Select(rq => new ResourceQuantity(((ResourceTemplates)resources!)[rq.Resource!], rq.Quantity))))
            .ForMember(x => x.ProductionLines, (src, resources) => new((src.ProductionLines ?? Enumerable.Empty<BuildingProductionLineTemplateIn>())
                .Select(pl => new BuildingProductionLineTemplate
                {
                    Name = pl.Name,
                    PossibleInputs = pl.Inputs.ToArray(),
                    Outputs = new(pl.Outputs.Select(rq => new ResourceQuantity(((ResourceTemplates)resources!)[rq.Resource!], rq.Quantity))),
                    WorkTicks = pl.WorkTicks
                })
                .ToArray()));
        Mapper.CreateMap<ResourceIn, Resource>()
            .ForMember(x => x.Groups, src => src.Groups?.ToArray() ?? Array.Empty<string>());
        Mapper.CreateMap<PlantTemplateIn, PlantTemplate>()
            .ForMember(x => x.Inventory, (src, resources) => new ResourceBucket(src.ContainingResources!.Select(rq => new ResourceQuantity(((ResourceTemplates)resources!)[rq.Resource!], rq.Quantity))));
        Mapper.CreateMap<ThoughtTemplateIn, ThoughtTemplate>()
            .ForMember(x => x.DurationInWorldTime, src => TimeSpan.FromDays(src.DurationInWorldDays));
        Mapper.CreateMap<RoomTemplateIn, RoomTemplate>()
            .ForMember(x => x.Requirements, (src, extra) => src.Requirements.Select(r => new RoomRequirementTemplate
            {
                Type = r.Type,
                Value = r.Value,
                Building = (((BuildingTemplates, ThoughtTemplates))(extra!)).Item1![r.Building]
            }).ToArray())
            .ForMember(x => x.Thoughts, (src, extra) => src.Thoughts.Select(t => new RoomThoughtTemplate
            {
                Action = t.Action,
                Thought = (((BuildingTemplates, ThoughtTemplates))(extra!)).Item2[t.Thought]
            }).ToArray());
    }
}

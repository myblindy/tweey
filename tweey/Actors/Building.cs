using AutoMapper;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using tweey.Actors;
using tweey.Actors.Interfaces;
using tweey.Loaders;

namespace tweey.Actors
{
    class Building : BuildingTemplate, IPlaceableEntity, IResourceNeed
    {
        private static readonly Mapper mapper = new(new MapperConfiguration(cfg => cfg.CreateMap<BuildingTemplate, Building>()));

        public Vector2 Location { get; set; }

        public ImmutableArray<Resource> StorageResourceNeeds { get; set; }

        public static Building FromTemplate(BuildingTemplate template, Vector2 location, IEnumerable<Resource> storageResourceNeeds)
        {
            var b = mapper.Map<Building>(template);
            b.Location = location;
            b.StorageResourceNeeds = storageResourceNeeds is ImmutableArray<Resource> immutableResourceArray ? immutableResourceArray : storageResourceNeeds.ToImmutableArray();
            return b;
        }

        private Building() { }
    }
}

using AutoMapper;
using System.Collections.Immutable;
using Tweey.Actors.Interfaces;
using Tweey.Loaders;

namespace Tweey.Actors
{
    class Building : BuildingTemplate, IPlaceableEntity, IResourceNeed
    {
        private static readonly Mapper mapper = new(new MapperConfiguration(cfg => cfg.CreateMap<BuildingTemplate, Building>()));

        public Vector2 Location { get; set; }

        public ImmutableArray<Resource> StorageResourceNeeds { get; set; }

        public bool Contains(Vector2i pt) => Location.X <= pt.X && pt.X < Location.X + Width && Location.Y <= pt.Y && pt.Y < Location.Y + Height;

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

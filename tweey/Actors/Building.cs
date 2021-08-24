using System.Collections.Immutable;
using Tweey.Actors.Interfaces;
using Tweey.Loaders;
using Tweey.Support;

namespace Tweey.Actors
{
    public class Building : BuildingTemplate, IPlaceableEntity, IResourceNeed
    {
        public Vector2 Location { get; set; }

        public ImmutableArray<Resource> StorageResourceNeeds { get; set; }

        public bool Contains(Vector2i pt) => Location.X <= pt.X && pt.X < Location.X + Width && Location.Y <= pt.Y && pt.Y < Location.Y + Height;

        public static Building FromTemplate(BuildingTemplate template, Vector2 location, IEnumerable<Resource> storageResourceNeeds)
        {
            var b = GlobalMapper.Mapper.Map(template);
            b.Location = location;
            b.StorageResourceNeeds = storageResourceNeeds is ImmutableArray<Resource> immutableResourceArray ? immutableResourceArray : storageResourceNeeds.ToImmutableArray();
            return b;
        }

        public Building() { }
    }
}

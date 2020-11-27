using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using tweey.Actors;
using tweey.Actors.Interfaces;
using tweey.Loaders;

namespace tweey
{
    class World
    {
        public ResourceTemplates Resources { get; }
        public BuildingTemplates BuildingTemplates { get; }

        public List<IPlaceableEntity> PlacedEntities { get; } = new();

        public World(ILoader loader) =>
            (Resources, BuildingTemplates) = (new(loader), new(loader));

        public void PlaceEntity(IPlaceableEntity entity)
        {
            PlacedEntities.Add(entity);

            if (entity is IResourceNeed resourceNeed)
            {

            }
        }
    }
}

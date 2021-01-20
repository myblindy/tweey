using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using tweey.Actors;
using tweey.Actors.Interfaces;
using tweey.AI;
using tweey.Loaders;

namespace tweey
{
    class World
    {
        public AIManager AIManager { get; }

        public ResourceTemplates Resources { get; }
        public BuildingTemplates BuildingTemplates { get; }

        public List<IPlaceableEntity> PlacedEntities { get; } = new();

        public World(ILoader loader)
        {
            Resources = new(loader);
            BuildingTemplates = new(loader, Resources);
            AIManager = new(this);
        }

        public void PlaceEntity(IPlaceableEntity entity)
        {
            PlacedEntities.Add(entity);

            if (entity is IResourceNeed resourceNeed)
                AIManager.AddResourceNeed(resourceNeed);
        }

        internal void Update(double time)
        {
            foreach (var villager in PlacedEntities.OfType<Villager>())
                switch (AIManager.GetJobStatus(villager))
                {
                    case JobStatus.None:
                        AIManager.QueueJobSearch(villager);
                        break;
                }
        }
    }
}

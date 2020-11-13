using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tweey.Loaders;

namespace tweey
{
    class World
    {
        public ResourceTemplates Resources { get; }
        readonly BuildingTemplates buildingTemplates;

        record PlacedResourceBucket(ResourceBucket ResourceBucket, double X, double Y) { }
        readonly List<PlacedResourceBucket> placedResourceBuckets = new();

        public World(ILoader loader) =>
            (Resources, buildingTemplates) = (new(loader), new(loader));

        public void PlaceResources(double x, double y, ResourceBucket resources) =>
            placedResourceBuckets.Add(new(resources, x, y));
    }
}

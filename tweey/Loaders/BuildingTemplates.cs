using System.Collections.Generic;
using tweey.Actors.Interfaces;

namespace tweey.Loaders
{
    enum BuildingType { Production, Storage }

    class ResourceQuantity : IResourceNeed
    {
        public int Quantity { get; set; }
        public string Resource { get; set; }
        public Resource ResourceObject { get; set; }

        public ResourceQuantity() { }
        public ResourceQuantity(Resource resource, int quantity) =>
            (ResourceObject, Quantity) = (resource, quantity);
    }

    class BuildingTemplate : IResourceNeeds<ResourceQuantity>
    {
        public string Name { get; set; }
        public BuildingType Type { get; set; }
        public List<ResourceQuantity> Inputs { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
    }

    class BuildingTemplates : BaseTemplates<BuildingTemplate>
    {
        public BuildingTemplates(ILoader loader, ResourceTemplates resourceTemplates) : base(loader, "Buildings", x => x.Name)
        {
            foreach (var resName in this)
                if (this[resName].Inputs is not null)
                    foreach (var input in this[resName].Inputs)
                        input.ResourceObject = resourceTemplates[input.Resource];
        }
    }
}

using System.Collections.Generic;
using System.Collections.Immutable;
using tweey.Loaders;

namespace tweey.Actors.Interfaces
{
    interface IResourceNeed
    {
        public int Quantity { get; set; }
        public string Resource { get; set; }
        public Resource ResourceObject { get; set; }
    }

    interface IResourceNeeds<TResourceNeed> where TResourceNeed : IResourceNeed
    {
        public List<TResourceNeed> Inputs { get; }
    }
}

using System.Collections.Immutable;
using tweey.Loaders;

namespace tweey.Actors.Interfaces
{
    interface IResourceNeed
    {
        public ImmutableArray<Resource> StorageResourceNeeds { get; }
    }
}

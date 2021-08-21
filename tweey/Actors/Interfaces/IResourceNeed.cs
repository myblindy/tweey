using System.Collections.Immutable;
using Tweey.Loaders;

namespace Tweey.Actors.Interfaces
{
    interface IResourceNeed
    {
        public ImmutableArray<Resource> StorageResourceNeeds { get; }
    }
}

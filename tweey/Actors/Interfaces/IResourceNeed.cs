namespace Tweey.Actors.Interfaces
{
    interface IResourceNeed
    {
        public ImmutableArray<Resource> StorageResourceNeeds { get; }
    }
}

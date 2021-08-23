namespace Tweey.Actors
{
    class Building : BuildingTemplate, IResourceNeed
    {
        private static readonly Mapper mapper = new(new MapperConfiguration(cfg => cfg.CreateMap<BuildingTemplate, Building>()));

        public ImmutableArray<Resource> StorageResourceNeeds { get; set; }

        public static Building FromTemplate(BuildingTemplate template, Vector2 location, IEnumerable<Resource> storageResourceNeeds)
        {
            var b = mapper.Map<Building>(template);
            b.Location = location;
            b.StorageResourceNeeds = storageResourceNeeds is ImmutableArray<Resource> immutableResourceArray ? immutableResourceArray : storageResourceNeeds.ToImmutableArray();
            return b;
        }
    }
}

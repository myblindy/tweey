namespace Tweey.Actors;


public class Building : BuildingTemplate, IResourceNeed
{
    public ResourceBucket Inventory { get; } = new();

    public ImmutableArray<Resource> StorageResourceNeeds { get; set; }

    public static Building FromTemplate(BuildingTemplate template, Vector2 location, IEnumerable<Resource> storageResourceNeeds)
    {
            var b = GlobalMapper.Mapper.Map(template);
        b.Location = location;
        b.StorageResourceNeeds = storageResourceNeeds is ImmutableArray<Resource> immutableResourceArray ? immutableResourceArray : storageResourceNeeds.ToImmutableArray();
        return b;
    }
}

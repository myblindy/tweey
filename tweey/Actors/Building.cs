namespace Tweey.Actors;

public class Building : BuildingTemplate, IResourceNeed
{
    public Villager[] AssignedWorkers { get; set; }
    public BitArray AssignedWorkersWorking { get; set; }
    public bool IsBuilt { get; set; }
    public ResourceBucket Inventory { get; } = new();

    public ImmutableArray<Resource> StorageResourceNeeds { get; set; }

    public static Building FromTemplate(BuildingTemplate template, Vector2 location, bool built, IEnumerable<Resource> storageResourceNeeds)
    {
        var b = GlobalMapper.Mapper.Map(template);
        b.Location = location;
        b.IsBuilt = built;
        b.StorageResourceNeeds = storageResourceNeeds is ImmutableArray<Resource> immutableResourceArray ? immutableResourceArray : storageResourceNeeds.ToImmutableArray();

        if (!built)
        {
            // one spot to work to build this thing
            b.AssignedWorkers = new Villager[1];
            b.AssignedWorkersWorking = new(1);
        }
        else
        {
            // TODO implement worker slots
        }

        return b;
    }
}

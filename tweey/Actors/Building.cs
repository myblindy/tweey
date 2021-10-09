namespace Tweey.Actors;

public class Building : BuildingTemplate
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Villager[] AssignedWorkers { get; set; }
    public BitArray AssignedWorkersWorking { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public bool IsBuilt { get; set; }
    public ResourceBucket Inventory { get; } = new();

    public static Building FromTemplate(BuildingTemplate template, Vector2 location, bool built)
    {
        var b = GlobalMapper.Mapper.Map(template);
        b.Location = location;
        b.IsBuilt = built;

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

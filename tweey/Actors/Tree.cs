namespace Tweey.Actors;

public class Tree : TreeTemplate
{
    public Villager? AssignedVillager { get; set; }
    public bool AssignedVillagerWorking { get; set; }

    public static Tree FromTemplate(TreeTemplate template, Vector2 location)
    {
        var t = GlobalMapper.Mapper.Map(template);
        t.Location = location;

        return t;
    }
}

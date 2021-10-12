namespace Tweey.Actors;

class Tree : TreeTemplate
{
    public override string? Name { get; set; }

    public static Tree FromTemplate(TreeTemplate template, Vector2 location)
    {
        var t = GlobalMapper.Mapper.Map(template);
        t.Location = location;

        return t;
    }
}

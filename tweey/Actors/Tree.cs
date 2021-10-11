namespace Tweey.Actors;

class Tree : PlaceableEntity
{
    public Tree() => (Width, Height) = (1, 1);
    public override string? Name { get; set; }
}

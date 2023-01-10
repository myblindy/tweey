namespace Twee.Core.Ecs;

public readonly struct Entity : IEquatable<Entity>
{
    public int ID { get; }

    public Entity(int id) => ID = id;

    public static Entity Invalid { get; } = new(-1);

    public override bool Equals(object? obj) => obj is Entity entity && Equals(entity);
    public bool Equals(Entity other) => ID == other.ID;
    public static bool operator ==(Entity a, Entity b) => a.Equals(b);
    public static bool operator !=(Entity a, Entity b) => !a.Equals(b);
    public override int GetHashCode() => ID;

    public static implicit operator int(Entity entity) => entity.ID;

    public override string ToString() => ID == -1 ? "Invalid" : ID.ToString();
}

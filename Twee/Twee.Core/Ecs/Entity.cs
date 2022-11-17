namespace Twee.Core.Ecs;

public readonly struct Entity : IEquatable<Entity>
{
    public required int ID { get; init; }

    public static Entity Invalid = new() { ID = -1 };

    public override bool Equals(object? obj) => obj is Entity entity && Equals(entity);
    public bool Equals(Entity other) => ID == other.ID;
    public static bool operator ==(Entity a, Entity b) => a.Equals(b);
    public static bool operator !=(Entity a, Entity b) => !a.Equals(b);
    public override int GetHashCode() => ID;

    public static implicit operator int(Entity entity) => entity.ID;
}

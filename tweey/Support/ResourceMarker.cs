namespace Tweey.Support;

readonly struct ResourceMarker : IEquatable<ResourceMarker>
{
    readonly ulong ID;
    ResourceMarker(ulong id) => ID = id;

    static ulong lastID = 1;
    public static ResourceMarker Create() => new(++lastID);

    public static bool operator ==(ResourceMarker left, ResourceMarker right) => left.ID == right.ID;
    public static bool operator !=(ResourceMarker left, ResourceMarker right) => left.ID != right.ID;
    public bool Equals(ResourceMarker other) => ID == other.ID;
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is ResourceMarker otherRM && base.Equals(otherRM);
    public override int GetHashCode() => ID.GetHashCode();

    public override string ToString() => this == All ? "All" : this == Default ? "Default" : ID.ToString();

    public static readonly ResourceMarker All = new(0);
    public static readonly ResourceMarker Default = new(1);
}

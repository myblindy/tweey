namespace Tweey.Components;

[EcsComponent]
record struct ThoughtWhenInRangeComponent(ThoughtTemplate ThoughtTemplate, TimeSpan CooldownInWorldTime, double Range)
{
    public Dictionary<Entity, CustomDateTime> CooldownExpirations { get; } = new();
}

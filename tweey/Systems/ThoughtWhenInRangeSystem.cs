namespace Tweey.Systems;

[EcsSystem(Archetypes.ThoughtWhenInRange)]
partial class ThoughtWhenInRangeSystem
{
    private readonly World world;

    public ThoughtWhenInRangeSystem(World world)
    {
        this.world = world;
    }

    public partial void Run()
    {
        using var entitiesThatTriggerThoughts = CollectionPool<(Entity entity, Vector2 location)>.Get();
        IterateComponents((in IterationResult w) => entitiesThatTriggerThoughts.Add((w.Entity, w.LocationComponent.Box.Center)));

        if (entitiesThatTriggerThoughts.Count > 0)
        {
            EcsCoordinator.IterateVillagerArchetype((in EcsCoordinator.VillagerIterationResult vw) =>
            {
                foreach (var (entityThatTriggersThoughts, locationOfEntityThatTriggersThoughts) in entitiesThatTriggerThoughts)
                    if (entityThatTriggersThoughts != vw.Entity)
                    {
                        ref var thoughtWhenInRangeComponent = ref entityThatTriggersThoughts.GetThoughtWhenInRangeComponent();
                        if (!thoughtWhenInRangeComponent.CooldownExpirations.TryGetValue(vw.Entity, out var cooldownExpiry))
                            cooldownExpiry = default;

                        if ((locationOfEntityThatTriggersThoughts - vw.LocationComponent.Box.Center).LengthSquared() < thoughtWhenInRangeComponent.Range * thoughtWhenInRangeComponent.Range
                            && cooldownExpiry <= world.WorldTime)
                        {
                            vw.VillagerComponent.AddThought(world, thoughtWhenInRangeComponent.ThoughtTemplate);
                            thoughtWhenInRangeComponent.CooldownExpirations[vw.Entity] = world.WorldTime + thoughtWhenInRangeComponent.CooldownInWorldTime;
                        }
                    }
            });
        }
    }
}

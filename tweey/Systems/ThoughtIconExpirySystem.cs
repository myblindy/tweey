namespace Tweey.Systems;

[EcsSystem(Archetypes.Villager)]
partial class ThoughtIconExpirySystem
{
    readonly Dictionary<Entity, TimeSpan> entityThoughtIconExpiry = new();
    readonly World world;

    public ThoughtIconExpirySystem(World world)
    {
        this.world = world;
    }

    public partial void Run()
    {
        IterateComponents((in IterationResult w) =>
        {
            if (w.VillagerComponent.ThoughtIcons.Count > 0)
            {
                if (!entityThoughtIconExpiry.TryGetValue(w.Entity, out var expiry))
                    entityThoughtIconExpiry[w.Entity] = expiry = world.TotalRealTime + TimeSpan.FromSeconds(3);

                // accelerate the expiration if we have multiple thought icons to get through
                if (expiry <= world.TotalRealTime || (w.VillagerComponent.ThoughtIcons.Count > 1 && (expiry - world.TotalRealTime).TotalSeconds <= 1.5))
                {
                    w.VillagerComponent.ThoughtIcons.Dequeue();
                    entityThoughtIconExpiry[w.Entity] = world.TotalRealTime + TimeSpan.FromSeconds(3);
                }
            }
        });
    }
}

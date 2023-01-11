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

                if (expiry <= world.TotalRealTime)
                {
                    w.VillagerComponent.ThoughtIcons.Dequeue();
                    entityThoughtIconExpiry[w.Entity] = world.TotalRealTime + TimeSpan.FromSeconds(3);
                }
            }
        });
    }
}

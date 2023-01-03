namespace Tweey.Systems;

[EcsSystem(Archetypes.Villager)]
partial class NeedsUpdateSystem
{
    readonly World world;

    public NeedsUpdateSystem(World world) =>
        this.world = world;

    public partial void Run() =>
        IterateComponents((in IterationResult w) => w.VillagerComponent.Needs.Decay(world.DeltaWorldTime.TotalSeconds));
}

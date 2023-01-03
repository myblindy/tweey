namespace Tweey.Systems;

[EcsSystem(Archetypes.Villager)]
partial class MoodUpdateSystem
{
    private readonly World world;

    public MoodUpdateSystem(World world) =>
        this.world = world;

    public partial void Run()
    {
        IterateComponents((in IterationResult w) =>
        {
            w.VillagerComponent.Thoughts.RemoveAll(t => t.Expiration <= world.WorldTime);

            var worldTimeMultiplier = world.DeltaWorldTime.TotalSeconds;
            const double maxChangePerWorldSecond = 0.001;
            w.VillagerComponent.MoodPercentage += Math.Clamp(w.VillagerComponent.MoodPercentageTarget - w.VillagerComponent.MoodPercentage,
                -maxChangePerWorldSecond * worldTimeMultiplier, maxChangePerWorldSecond * worldTimeMultiplier);
        });
    }
}

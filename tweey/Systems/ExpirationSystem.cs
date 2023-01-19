namespace Tweey.Systems;

[EcsSystem(Archetypes.Expiration)]
partial class ExpirationSystem
{
    private readonly World world;

    public ExpirationSystem(World world)
    {
        this.world = world;
    }

    public partial void Run()
    {
        using var expiredEntities = CollectionPool<Entity>.Get();
        IterateComponents((in IterationResult w) =>
        {
            if (w.ExpirationComponent.Date <= world.WorldTime || w.ExpirationComponent.IsExpired?.Invoke() == true)
                expiredEntities.Add(w.Entity);
        });

        foreach (var expiredEntity in expiredEntities)
            expiredEntity.Delete();
    }
}

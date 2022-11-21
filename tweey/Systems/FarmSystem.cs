namespace Tweey.Systems;

[EcsSystem(Archetypes.FarmedPlant)]
partial class FarmSystem
{
    readonly World world;

    public FarmSystem(World world) =>
        this.world = world;

    public partial void Run()
    {
        IterateComponents((in IterationResult w) =>
        {
            if (w.PlantComponent.IsMature(world))
            {
                w.Entity.RemovePlantIsFarmedComponent();
                w.Entity.AddMarkForHarvestComponent();
            }
        });
    }
}

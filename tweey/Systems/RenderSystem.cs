using Tweey.Components;

namespace Tweey.Systems;

[EcsSystem, Uses<LocationComponent>]
partial class RenderSystem
{
    public void Run(double deltaSec)
    {
        IterateComponents((in IterationResult w) =>
        {
            w.LocationComponent.Location = new(1, 5);
        });

        IterateComponents((in IterationResult w) =>
        {
        });
    }
}

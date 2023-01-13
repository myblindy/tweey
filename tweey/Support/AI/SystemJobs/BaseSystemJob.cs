namespace Tweey.Support.AI.SystemJobs;

abstract class BaseSystemJob
{
    protected World World { get; }

    public BaseSystemJob(World world)
    {
        World = world;
    }

    public virtual bool IsConfigurable => true;
    public abstract string Name { get; }

    public abstract bool TryToRun(Entity workerEntity, [NotNullWhen(true)] out AIHighLevelPlan[]? plans);
}

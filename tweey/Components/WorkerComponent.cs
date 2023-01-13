namespace Tweey.Components;

[EcsComponent]
struct WorkerComponent
{
    public AIHighLevelPlan[]? Plans { get; set; }
    public int[] SystemJobPriorities { get; set; }

    public AIHighLevelPlan? CurrentHighLevelPlan { get; set; }
    public AILowLevelPlan? CurrentLowLevelPlan { get; set; }
}

using Tweey.Support.AI.HighLevelPlans;
using Tweey.Support.AI.LowLevelPlans;

namespace Tweey.Components;

[EcsComponent]
struct WorkerComponent
{
    public AIHighLevelPlan[]? Plans { get; set; }

    public AIHighLevelPlan? CurrentHighLevelPlan { get; set; }
    public AILowLevelPlan? CurrentLowLevelPlan { get; set; }
}

namespace Tweey.Components;

[EcsComponent]
struct VillagerComponent
{
    public record struct Thought(ThoughtTemplate Template, CustomDateTime Expiration);

    public Needs Needs { get; }
    public FastList<Thought> Thoughts { get; } = new();
    public Queue<string> ThoughtIcons { get; } = new();
    public double MaxCarryWeight { get; }
    public double PickupSpeedMultiplier { get; }
    public double MovementRateMultiplier { get; }
    public double WorkSpeedMultiplier { get; }
    public double HarvestSpeedMultiplier { get; }
    public double PlantSpeed { get; }
    public ResourceMarker PocketFoodMarker { get; } = ResourceMarker.Create();

    public double MoodPercentageTarget => Thoughts.Sum(w => w.Template.MoodChange);
    public double MoodPercentage { get; set; } = 50;

    public VillagerComponent(double MaxCarryWeight, double PickupSpeedMultiplier, double MovementRateMultiplier,
        double WorkSpeedMultiplier, double HarvestSpeedMultiplier, double PlantSpeed, double TiredMax, double TiredDecayPerWorldSecond,
        double PoopMax, double PoopDecayPerWorldSecond)
    {
        this.MaxCarryWeight = MaxCarryWeight;
        this.PickupSpeedMultiplier = PickupSpeedMultiplier;
        this.MovementRateMultiplier = MovementRateMultiplier;
        this.WorkSpeedMultiplier = WorkSpeedMultiplier;
        this.HarvestSpeedMultiplier = HarvestSpeedMultiplier;
        this.PlantSpeed = PlantSpeed;
        Needs = new Needs
        {
            Tired = TiredMax,
            TiredMax = TiredMax,
            TiredDecayPerWorldSecond = TiredDecayPerWorldSecond,
            Poop = PoopMax,
            PoopMax = PoopMax,
            PoopDecayPerWorldSecond = PoopDecayPerWorldSecond
        };
    }

    public void AddThought(World world, ThoughtTemplate thought)
    {
        var existingCount = Thoughts.Count(w => w.Template == thought);
        if (existingCount < thought.StackLimit)
            Thoughts.Add(new(thought, world.WorldTime + thought.DurationInWorldTime));
        else
        {
            var oldestThought = Thoughts.Where(w => w.Template == thought).MinBy(w => w.Expiration);
            var oldestThoughtIndex = Thoughts.IndexOf(oldestThought);
            Thoughts[oldestThoughtIndex] = new(thought, world.WorldTime + thought.DurationInWorldTime);
        }

        if (thought.IconFileName is not null)
            ThoughtIcons.Enqueue(thought.IconFileName);
    }
}

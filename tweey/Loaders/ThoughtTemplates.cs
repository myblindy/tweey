namespace Tweey.Loaders;

class ThoughtTemplate : ITemplateFileName
{
    public string FileName { get; set; } = null!;
    public required string Description { get; set; }
    public required double MoodChange { get; set; }
    public required TimeSpan DurationInWorldTime { get; set; }
    public required int StackLimit { get; set; }
}

class ThoughtTemplateIn
{
    public string Description { get; set; } = null!;
    public double MoodChange { get; set; }
    public double DurationInWorldDays { get; set; }
    public int StackLimit { get; set; } = 1;
}

class ThoughtTemplates : BaseTemplates<ThoughtTemplateIn, ThoughtTemplate>
{
    public const string SleptOnGround = "slept-on-ground";
    public const string ExtremelyLowExpectations = "extremely-low-expectations";
    public const string PoopedOnTheGround = "pooped-on-ground";
    public const string PoopSeen = "poop-seen";

    public ThoughtTemplates(ILoader loader)
        : base(loader, "Thoughts", x => x.FileName!)
    {
    }
}

namespace Tweey.Loaders;

class ThoughtTemplate : ITemplateFileName
{
    public string FileName { get; set; } = null!;
    public required string Description { get; set; }
    public required double MoodChange { get; set; }
    public required TimeSpan DurationInWorldTime { get; set; }
    public required int MaxStacks { get; set; }
}

class ThoughtTemplateIn
{
    public string Description { get; set; } = null!;
    public double MoodChange { get; set; }
    public double DurationInWorldDays { get; set; }
    public int MaxStacks { get; set; } = 1;
}

class ThoughtTemplates : BaseTemplates<ThoughtTemplateIn, ThoughtTemplate>
{
    public const string SleptOnGround = "slept-on-ground";
    public const string ExtremelyLowExpectations = "extremely-low-expectations";

    public ThoughtTemplates(ILoader loader)
        : base(loader, "Thoughts", x => x.FileName!)
    {
    }
}

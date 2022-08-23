

namespace Tweey.Support;

public class Needs
{
            public required double HungerMax { get; set; }
        public double Hunger { get; set; }
        public double HungerPercentage => Hunger / HungerMax;
        public required double HungerPerSecond { get; set; }
    
    public void UpdateWithChanges(NeedsChange change)
    {
                    Hunger = Math.Clamp(Hunger + change.Hunger, 0, HungerMax);
            }

    public void Update(double deltaSec)
    {
                    Hunger = Math.Clamp(Hunger + HungerPerSecond * deltaSec, 0, HungerMax);
            }
}

public class NeedsChange
{
            public double Hunger { get; set; }
    }
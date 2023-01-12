

namespace Tweey.Support;

class Needs
{
            public required double TiredMax { get; set; }
        public double Tired { get; set; }
        public double TiredPercentage => Tired / TiredMax;
        public required double TiredDecayPerWorldSecond { get; set; }
            public required double PoopMax { get; set; }
        public double Poop { get; set; }
        public double PoopPercentage => Poop / PoopMax;
        public required double PoopDecayPerWorldSecond { get; set; }
            public required double HungerMax { get; set; }
        public double Hunger { get; set; }
        public double HungerPercentage => Hunger / HungerMax;
        public required double HungerDecayPerWorldSecond { get; set; }
    
    public void UpdateWithChanges(in NeedsChange change)
    {
                    Tired = Math.Clamp(Tired + change.Tired, 0, TiredMax);
                    Poop = Math.Clamp(Poop + change.Poop, 0, PoopMax);
                    Hunger = Math.Clamp(Hunger + change.Hunger, 0, HungerMax);
            }

    public void Decay(double deltaWorldSec)
    {
                    Tired = Math.Clamp(Tired - TiredDecayPerWorldSecond * deltaWorldSec, 0, TiredMax);
                    Poop = Math.Clamp(Poop - PoopDecayPerWorldSecond * deltaWorldSec, 0, PoopMax);
                    Hunger = Math.Clamp(Hunger - HungerDecayPerWorldSecond * deltaWorldSec, 0, HungerMax);
            }
}

readonly struct NeedsChange
{
            public double Tired { get; init; }
            public double Poop { get; init; }
            public double Hunger { get; init; }
    }
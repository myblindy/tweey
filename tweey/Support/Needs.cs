

namespace Tweey.Support;

class Needs
{
            public required double TiredMax { get; set; }
        public double Tired { get; set; }
        public double TiredPercentage => Tired / TiredMax;
        public required double TiredDecayPerWorldSecond { get; set; }
    
    public void UpdateWithChanges(in NeedsChange change)
    {
                    Tired = Math.Clamp(Tired + change.Tired, 0, TiredMax);
            }

    public void Decay(double deltaWorldSec)
    {
                    Tired = Math.Clamp(Tired - TiredDecayPerWorldSecond * deltaWorldSec, 0, TiredMax);
            }
}

readonly struct NeedsChange
{
            public double Tired { get; init; }
    }
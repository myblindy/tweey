namespace Tweey.Support
{
    public record ActionTime(double ActionsPerSecond)
    {
        double timerSec;

        public void AdvanceTime(double deltaSec) => timerSec += deltaSec;

        public int ConsumeActions()
        {
            var actionCount = (int)(timerSec * ActionsPerSecond);
            timerSec -= actionCount / ActionsPerSecond;
            return actionCount;
        }
    }
}

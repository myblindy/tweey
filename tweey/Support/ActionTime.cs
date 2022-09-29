namespace Tweey.Support;

public class ActionTime
{
    public ActionTime() { }
    public ActionTime(double actionsPerSecond) : this() => ActionsPerSecond = actionsPerSecond;

    public double ActionsPerSecond { get; set; }

    double timerSec;

    public void Reset(double actionsPerSecond) => (ActionsPerSecond, timerSec) = (actionsPerSecond, 0);

    public void AdvanceTime(double deltaSec) => timerSec += deltaSec;

    public (int actions, double fractionalRemainder) ConsumeActions()
    {
        var actionCount = (int)(timerSec * ActionsPerSecond);
        timerSec -= actionCount / ActionsPerSecond;
        return (actionCount, timerSec * ActionsPerSecond);
    }

    public (int actions, double fractionalRemainder) AdvanceTimeAndConsumeActions(double deltaSec)
    {
        AdvanceTime(deltaSec);
        return ConsumeActions();
    }
}

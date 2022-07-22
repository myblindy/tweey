namespace Tweey.Support;

struct FrameData
{
    static readonly TimeSpan maxTime = TimeSpan.FromSeconds(1);
    TimeSpan cummulativeTime, cummulativeUpdateTime, cummulativeRenderTime;
    int frameCount;

    public double Rate { get; private set; }

    public double UpdateTimePercentage => cummulativeUpdateTime.TotalSeconds / cummulativeTime.TotalSeconds;
    public double RenderTimePercentage => cummulativeRenderTime.TotalSeconds / cummulativeTime.TotalSeconds;

    public void NewFrame(TimeSpan deltaTime, TimeSpan deltaUpdateTime, TimeSpan deltaRenderTime)
    {
        ++frameCount;
        if (cummulativeTime + deltaTime > maxTime)
        {
            Rate = frameCount / cummulativeTime.TotalSeconds;
            (frameCount, cummulativeTime, cummulativeUpdateTime, cummulativeRenderTime) = 
                (0, default, default, default);
        }

        cummulativeTime += deltaTime;
        cummulativeUpdateTime += deltaUpdateTime;
        cummulativeRenderTime += deltaRenderTime;
    }
}

namespace Tweey.Support;

struct FrameData
{
    static readonly TimeSpan maxTime = TimeSpan.FromSeconds(1);
    TimeSpan cummulativeTime, cummulativeUpdateTime, cummulativeRenderTime;
    ulong cummulativeDrawCallCount, cummulativeTriangleCount, cummulativeLineCount;
    int frameCount;

    public double Rate { get; private set; }

    public double UpdateTimePercentage => cummulativeUpdateTime.TotalSeconds / cummulativeTime.TotalSeconds;
    public double RenderTimePercentage => cummulativeRenderTime.TotalSeconds / cummulativeTime.TotalSeconds;
    public ulong DrawCallCount => frameCount is 0 ? 0 : cummulativeDrawCallCount / (ulong)frameCount;
    public ulong TriangleCount => frameCount is 0 ? 0 : cummulativeTriangleCount / (ulong)frameCount;
    public ulong LineCount => frameCount is 0 ? 0 : cummulativeLineCount / (ulong)frameCount;

    public void NewFrame(TimeSpan deltaTime, TimeSpan deltaUpdateTime, TimeSpan deltaRenderTime, ulong drawCalls, ulong triangles, ulong lines)
    {
        ++frameCount;
        if (cummulativeTime + deltaTime > maxTime)
        {
            Rate = frameCount / cummulativeTime.TotalSeconds;
            (frameCount, cummulativeTime, cummulativeUpdateTime, cummulativeRenderTime, cummulativeDrawCallCount, cummulativeTriangleCount, cummulativeLineCount) =
                (1, default, default, default, 0, 0, 0);
        }

        cummulativeTime += deltaTime;
        cummulativeUpdateTime += deltaUpdateTime;
        cummulativeRenderTime += deltaRenderTime;
        cummulativeDrawCallCount += drawCalls;
        cummulativeTriangleCount += triangles;
        cummulativeLineCount += lines;
    }
}

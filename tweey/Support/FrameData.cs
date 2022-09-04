namespace Tweey.Support;

static class FrameData
{
    static readonly TimeSpan maxTime = TimeSpan.FromSeconds(1);

    struct Snapshot
    {
        public TimeSpan DeltaTime, DeltaUpdateTime, DeltaRenderTime;
        public ulong DrawCallCount, TriangleCount, LineCount;
    }
    static readonly Snapshot[] snapshots = new Snapshot[200];
    static int currentSnapshotIndex = -1;

    static IEnumerable<Snapshot> ActiveSnapshots => snapshots.Take(currentSnapshotIndex);
    static ulong ActiveSnapshotsCount => (ulong)currentSnapshotIndex;

    public static double Rate => currentSnapshotIndex < 0 ? 0 : ActiveSnapshotsCount / ActiveSnapshots.Sum(w => w.DeltaTime).TotalSeconds;
    public static double UpdateTimePercentage => currentSnapshotIndex <= 0 ? 0 : ActiveSnapshots.Sum(w => w.DeltaUpdateTime) / ActiveSnapshots.Sum(w => w.DeltaTime);
    public static double RenderTimePercentage => currentSnapshotIndex <= 0 ? 0 : ActiveSnapshots.Sum(w => w.DeltaRenderTime) / ActiveSnapshots.Sum(w => w.DeltaTime);
    public static ulong DrawCallCount => currentSnapshotIndex <= 0 ? 0 : ActiveSnapshots.Sum(w => w.DrawCallCount) / ActiveSnapshotsCount;
    public static ulong TriangleCount => currentSnapshotIndex <= 0 ? 0 : ActiveSnapshots.Sum(w => w.TriangleCount) / ActiveSnapshotsCount;
    public static ulong LineCount => currentSnapshotIndex <= 0 ? 0 : ActiveSnapshots.Sum(w => w.LineCount) / ActiveSnapshotsCount;

    public static void NewTriangleDraw(ulong count) =>
        (snapshots[currentSnapshotIndex + 1].DrawCallCount, snapshots[currentSnapshotIndex + 1].TriangleCount) =
        (snapshots[currentSnapshotIndex + 1].DrawCallCount + 1, snapshots[currentSnapshotIndex + 1].TriangleCount + count);

    public static void NewLineDraw(ulong count) =>
        (snapshots[currentSnapshotIndex + 1].DrawCallCount, snapshots[currentSnapshotIndex + 1].LineCount) =
        (snapshots[currentSnapshotIndex + 1].DrawCallCount + 1, snapshots[currentSnapshotIndex + 1].LineCount + count);

    public static void NewFrame(TimeSpan deltaTime, TimeSpan deltaUpdateTime, TimeSpan deltaRenderTime)
    {
        snapshots[currentSnapshotIndex + 1].DeltaTime = deltaTime;
        snapshots[currentSnapshotIndex + 1].DeltaUpdateTime = deltaUpdateTime;
        snapshots[currentSnapshotIndex + 1].DeltaRenderTime = deltaRenderTime;

        if (++currentSnapshotIndex == snapshots.Length - 1)
        {
            Array.Copy(snapshots, snapshots.Length / 2, snapshots, 0, snapshots.Length / 2);
            currentSnapshotIndex = snapshots.Length / 2 - 1;
        }
        snapshots[currentSnapshotIndex + 1] = new();
    }
}

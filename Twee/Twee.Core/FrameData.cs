using System.Data;
using Twee.Core.Support;

namespace Twee.Core;

public static class FrameData
{
    struct Snapshot
    {
        public TimeSpan DeltaTime, DeltaUpdateTime, DeltaRenderTime;
        public TimeSpan[]? CustomTimes;
        public ulong DrawCallCount, TriangleCount, LineCount;

        public void CopyTo(ref Snapshot target)
        {
            target.DeltaTime = DeltaTime;
            target.DeltaUpdateTime = DeltaUpdateTime;
            target.DeltaRenderTime = DeltaRenderTime;
            target.DrawCallCount = DrawCallCount;
            target.TriangleCount = TriangleCount;
            target.LineCount = LineCount;

            if (CustomTimes?.Length is { } len && len > 0)
                while (len-- > 0)
                    target.CustomTimes![len] = CustomTimes[len];
        }

        public void Clear()
        {
            DeltaTime = DeltaUpdateTime = DeltaRenderTime = default;
            DrawCallCount = TriangleCount = LineCount = 0;
        }
    }
    static readonly Snapshot[] snapshots = new Snapshot[100];
    static int currentSnapshotIndex = -1;

    public static void Init(int customTimesCount = 0)
    {
        foreach (ref var snapshot in snapshots.AsSpan())
            snapshot.CustomTimes = new TimeSpan[customTimesCount];
    }

    static IEnumerable<Snapshot> ActiveSnapshots => snapshots.Take(currentSnapshotIndex);
    static ulong ActiveSnapshotsCount => (ulong)currentSnapshotIndex;

    public static double Rate => currentSnapshotIndex < 0 ? 0 : ActiveSnapshotsCount / ActiveSnapshots.Sum(w => w.DeltaTime).TotalSeconds;
    public static double UpdateTimePercentage => currentSnapshotIndex <= 0 ? 0 : ActiveSnapshots.Sum(w => w.DeltaUpdateTime) / ActiveSnapshots.Sum(w => w.DeltaTime);
    public static double RenderTimePercentage => currentSnapshotIndex <= 0 ? 0 : ActiveSnapshots.Sum(w => w.DeltaRenderTime) / ActiveSnapshots.Sum(w => w.DeltaTime);
    public static ulong DrawCallCount => currentSnapshotIndex <= 0 ? 0 : ActiveSnapshots.Sum(w => w.DrawCallCount) / ActiveSnapshotsCount;
    public static ulong TriangleCount => currentSnapshotIndex <= 0 ? 0 : ActiveSnapshots.Sum(w => w.TriangleCount) / ActiveSnapshotsCount;
    public static ulong LineCount => currentSnapshotIndex <= 0 ? 0 : ActiveSnapshots.Sum(w => w.LineCount) / ActiveSnapshotsCount;

    public static double GetCustomTimePercentage(int idx) => currentSnapshotIndex <= 0 ? 0 : ActiveSnapshots.Sum(w => w.CustomTimes![idx]) / ActiveSnapshots.Sum(w => w.DeltaTime);

    public static void NewCustomTime(int idx, TimeSpan time) =>
        snapshots[currentSnapshotIndex + 1].CustomTimes![idx] = time;

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
            for (int i = snapshots.Length / 2; i < snapshots.Length; ++i)
            {
                ref var target = ref snapshots[i - snapshots.Length / 2];
                snapshots[i].CopyTo(ref target);
            }
            currentSnapshotIndex = snapshots.Length / 2 - 1;
        }
        snapshots[currentSnapshotIndex + 1].Clear();
    }
}

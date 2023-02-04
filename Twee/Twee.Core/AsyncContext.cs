using System.Collections.Concurrent;

namespace Twee.Core;

public class AsyncContext : SynchronizationContext
{
    private sealed class WorkItem
    {
        private readonly SendOrPostCallback Callback;
        private readonly object? State;
        private readonly ManualResetEventSlim? Reset;

        public WorkItem(SendOrPostCallback callback, object? state, ManualResetEventSlim? reset)
        {
            Callback = callback ?? throw new ArgumentNullException(nameof(callback));
            State = state;
            Reset = reset;
        }

        public void Execute()
        {
            Callback(State);
            Reset?.Set();
        }
    }

    private readonly ConcurrentQueue<WorkItem> WorkItems = new ConcurrentQueue<WorkItem>();
    private readonly Thread ExecutingThread;

    public AsyncContext(Thread executingThread) =>
        ExecutingThread = executingThread ?? throw new ArgumentNullException(nameof(executingThread));

    bool HasWorkItems => !WorkItems.IsEmpty;

    public void RunContinuations()
    {
        while (ExecuteAndReturnNextWorkItem() is not null) { }
    }

    private WorkItem? ExecuteAndReturnNextWorkItem()
    {
        if (WorkItems.TryDequeue(out var currentItem))
            currentItem.Execute();
        return currentItem;
    }

    public override void Post(SendOrPostCallback d, object? state) =>
        WorkItems.Enqueue(new WorkItem(d, state, null));

    public override void Send(SendOrPostCallback d, object? state)
    {
        if (Thread.CurrentThread == ExecutingThread)
        {
            WorkItem requestedWorkItem = new WorkItem(d, state, null);
            WorkItems.Enqueue(requestedWorkItem);

            WorkItem? executedWorkItem;
            do
            {
                executedWorkItem = ExecuteAndReturnNextWorkItem();
            } while (executedWorkItem != null && executedWorkItem != requestedWorkItem);
        }
        else
        {
            using var reset = new ManualResetEventSlim();
            WorkItems.Enqueue(new WorkItem(d, state, reset));
            reset.Wait();
        }
    }
}
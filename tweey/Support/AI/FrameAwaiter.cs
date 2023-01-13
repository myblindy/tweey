namespace Tweey.Systems;

partial class AISystem
{
    sealed class FrameAwaiter : INotifyCompletion, IFrameAwaiter
    {
        private readonly FastList<Action> continuations = new();

        public bool IsCompleted => false;

        public IAwaiter GetAwaiter() => this;
        public void GetResult() { }
        public void OnCompleted(Action continuation) => continuations.Add(continuation);

        public void Run()
        {
            if (continuations.Count > 0)
            {
                using var continuationsCopy = continuations.ToPooledCollection();
                continuations.Clear();

                foreach (var continuation in continuationsCopy)
                    continuation();
            }
        }

        public void UnsafeOnCompleted(Action continuation) => OnCompleted(continuation);
    }
}
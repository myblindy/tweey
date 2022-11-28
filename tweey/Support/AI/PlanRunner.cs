namespace Tweey.Systems;

partial class AISystem
{
    record PlanRunner(World World, Entity Entity) : IDisposable
    {
        private bool disposedValue;
        IEnumerator<AIHighLevelPlan>? highLevelEnumerator;
        IEnumerator<AILowLevelPlan>? lowLevelEnumerator;

        /// <summary>
        /// Runs the next step for the configured AI steps.
        /// </summary>
        /// <returns><see cref="false"/> if done, otherwise <see cref="true"/>.</returns>
        public bool Run()
        {
            if (highLevelEnumerator is null)
            {
                highLevelEnumerator = Entity.GetWorkerComponent().Plans!.Select(w => w).GetEnumerator();
                if (!highLevelEnumerator.MoveNext())
                {
                    Entity.GetWorkerComponent().CurrentHighLevelPlan = null;
                    Entity.GetWorkerComponent().CurrentLowLevelPlan = null;
                    highLevelEnumerator.Dispose();
                    return false;
                }
                Entity.GetWorkerComponent().CurrentHighLevelPlan = highLevelEnumerator.Current;
            }

            if (lowLevelEnumerator is null)
            {
                lowLevelEnumerator = highLevelEnumerator.Current.GetLowLevelPlans().GetEnumerator();

                retry0:
                if (!lowLevelEnumerator.MoveNext())
                {
                    lowLevelEnumerator.Dispose();
                    if (!highLevelEnumerator.MoveNext())
                    {
                        highLevelEnumerator.Dispose();
                        return false;
                    }
                    Entity.GetWorkerComponent().CurrentHighLevelPlan = highLevelEnumerator.Current;
                    lowLevelEnumerator = highLevelEnumerator.Current.GetLowLevelPlans().GetEnumerator();
                    goto retry0;
                }
                Entity.GetWorkerComponent().CurrentLowLevelPlan = lowLevelEnumerator.Current;
            }

            if (!lowLevelEnumerator.Current.Run())
            {
                retry1:
                if (!lowLevelEnumerator.MoveNext())
                {
                    lowLevelEnumerator.Dispose();
                    if (!highLevelEnumerator.MoveNext())
                    {
                        highLevelEnumerator.Dispose();
                        return false;
                    }
                    Entity.GetWorkerComponent().CurrentHighLevelPlan = highLevelEnumerator.Current;
                    lowLevelEnumerator = highLevelEnumerator.Current.GetLowLevelPlans().GetEnumerator();
                    goto retry1;
                }
                Entity.GetWorkerComponent().CurrentLowLevelPlan = lowLevelEnumerator.Current;
            }

            return true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // managed
                    lowLevelEnumerator?.Dispose();
                    highLevelEnumerator?.Dispose();
                }

                // TODO: unmanaged
                disposedValue = true;
            }
        }

        ~PlanRunner()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
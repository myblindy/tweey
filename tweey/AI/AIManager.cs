using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tweey.Actors;
using tweey.Actors.Interfaces;

namespace tweey.AI
{
    class AIManager
    {
        abstract class PlanStep
        {
            public abstract bool Update(double timestep, Villager villager);
        }

        class PlanStepMoveToPlaceable : PlanStep
        {
            public IPlaceableEntity TargetEntity { get; init; }

            public PlanStepMoveToPlaceable(IPlaceableEntity targetEntity) =>
                TargetEntity = targetEntity;

            public override bool Update(double timestep, Villager villager) => true;
        }

        class Plan
        {
            public JobStatus JobStatus { get; set; }
            public PlanStep[] PlanSteps { get; set; }
        }

        readonly World world;
        readonly ReaderWriterLockSlim @lock = new();
        readonly Dictionary<Villager, Plan> searchStatus = new();
        readonly List<IResourceNeed> resourceNeeds = new();
        readonly Thread aiThread;

        public AIManager(World world)
        {
            (this.world, aiThread) = (world, new(AIThreadHandler) { Name = "AI Thread", IsBackground = true });
            aiThread.Start();
        }

        public JobStatus GetJobStatus(Villager villager)
        {
            try
            {
                @lock.EnterReadLock();
                return searchStatus.TryGetValue(villager, out var plan) ? plan.JobStatus : JobStatus.None;
            }
            finally { @lock.ExitReadLock(); }
        }

        internal void AddResourceNeed(IResourceNeed resourceNeed)
        {
            try
            {
                @lock.EnterReadLock();
                resourceNeeds.Add(resourceNeed);
            }
            finally { @lock.ExitReadLock(); }
        }

        public void QueueJobSearch(Villager villager)
        {
            try
            {
                @lock.EnterWriteLock();
                if (searchStatus.TryGetValue(villager, out var plan))
                    plan.JobStatus = JobStatus.Queued;
                else
                    searchStatus.Add(villager, new() { JobStatus = JobStatus.Queued });
            }
            finally { @lock.ExitWriteLock(); }
        }

        void AIThreadHandler()
        {
            var queuedVillagers = new List<Villager>();
            var resourceNeeds = new List<IResourceNeed>();
            var calculatedVillagerPlans = new Dictionary<Villager, PlanStep[]>();

            while (true)
            {
                queuedVillagers.Clear();
                resourceNeeds.Clear();
                calculatedVillagerPlans.Clear();

                // make a list of villagers queued to be processed by the AI, as well as any registered resource needs
                try
                {
                    @lock.EnterReadLock();

                    foreach (var villager in searchStatus.Where(kvp => kvp.Value.JobStatus == JobStatus.Queued).Select(kvp => kvp.Key))
                        queuedVillagers.Add(villager);

                    resourceNeeds.AddRange(this.resourceNeeds);
                }
                finally { @lock.ExitReadLock(); }

                // process what we copied
                foreach (var villager in queuedVillagers)
                    foreach (var resourceNeed in resourceNeeds)
                        foreach (var resourceBucket in world.PlacedEntities.OfType<ResourceBucket>())
                            if (resourceBucket.Resources.Any(rq => resourceNeed.Resource == rq.Resource))
                            {
                                // found a bucket that has at least one of the required resources
                                calculatedVillagerPlans.Add(villager, new[]
                                {
                                    new PlanStepMoveToPlaceable(resourceBucket),
                                });
                            }

                // update their plans
                try
                {
                    @lock.EnterWriteLock();
                    foreach (var villagerPlan in calculatedVillagerPlans)
                    {
                        var plan = searchStatus[villagerPlan.Key];
                        plan.PlanSteps = villagerPlan.Value;
                        plan.JobStatus = JobStatus.Found;
                    }
                }
                finally { @lock.ExitWriteLock(); }
            }
        }
    }

    enum JobStatus
    {
        None,
        Queued,
        Found,
    }
}

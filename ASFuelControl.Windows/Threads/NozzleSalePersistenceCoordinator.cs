using System;
using System.Collections.Generic;

namespace ASFuelControl.Windows.Threads
{
    internal static class NozzleSalePersistenceCoordinator
    {
        private static readonly object SyncRoot = new object();
        private static readonly Dictionary<Guid, PersistenceState> States =
            new Dictionary<Guid, PersistenceState>();
        private static readonly TimeSpan SuccessfulSettlementPeriod =
            TimeSpan.FromSeconds(60);
        private static readonly TimeSpan FailedSettlementPeriod =
            TimeSpan.FromMinutes(2);
        private static readonly TimeSpan MaximumActivePeriod =
            TimeSpan.FromMinutes(10);

        public static void Begin(Guid nozzleId)
        {
            DateTime utcNow = DateTime.UtcNow;
            lock (SyncRoot)
            {
                PersistenceState state;
                bool stateExists = States.TryGetValue(nozzleId, out state);
                if (!stateExists)
                {
                    state = new PersistenceState();
                    States.Add(nozzleId, state);
                }

                state.ActiveCount++;
                state.ActiveSinceUtc = utcNow;
                state.DeferUntilUtc = DateTime.MaxValue;
            }
        }

        public static void End(Guid nozzleId, bool persistenceVerified)
        {
            DateTime utcNow = DateTime.UtcNow;
            lock (SyncRoot)
            {
                PersistenceState state;
                bool stateExists = States.TryGetValue(nozzleId, out state);
                if (!stateExists)
                {
                    return;
                }

                if (state.ActiveCount > 0)
                {
                    state.ActiveCount--;
                }

                if (state.ActiveCount > 0)
                {
                    return;
                }

                TimeSpan settlementPeriod = persistenceVerified
                    ? SuccessfulSettlementPeriod
                    : FailedSettlementPeriod;
                state.DeferUntilUtc = utcNow.Add(settlementPeriod);
            }
        }

        public static bool ShouldDefer(Guid nozzleId)
        {
            DateTime utcNow = DateTime.UtcNow;
            lock (SyncRoot)
            {
                PersistenceState state;
                bool stateExists = States.TryGetValue(nozzleId, out state);
                if (!stateExists)
                {
                    return false;
                }

                if (state.ActiveCount > 0)
                {
                    TimeSpan activeDuration = utcNow.Subtract(state.ActiveSinceUtc);
                    if (activeDuration <= MaximumActivePeriod)
                    {
                        return true;
                    }

                    state.ActiveCount = 0;
                    state.DeferUntilUtc = utcNow.Add(FailedSettlementPeriod);
                }

                if (utcNow < state.DeferUntilUtc)
                {
                    return true;
                }

                States.Remove(nozzleId);
                return false;
            }
        }

        private sealed class PersistenceState
        {
            public int ActiveCount { set; get; }

            public DateTime ActiveSinceUtc { set; get; }

            public DateTime DeferUntilUtc { set; get; }
        }
    }
}

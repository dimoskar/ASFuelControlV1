using System;
using System.Collections.Generic;
using System.Threading;

namespace ASFuelControl.Data.Implementation
{
    public enum OutboundWorkType
    {
        Sales,
        Alerts,
        TankFillings,
        FuelPrices,
        Balances,
        TankChecks,
        AlertEvaluation,
        AlertPrinting,
        InvoicePrinting,
        FiscalDocumentPrinting
    }

    public sealed class OutboundWorkState
    {
        private readonly AutoResetEvent wakeSignal = new AutoResetEvent(false);
        private long lastChangedUtcTicks = DateTime.MinValue.Ticks;
        private long version;

        internal OutboundWorkState(OutboundWorkType workType)
        {
            this.WorkType = workType;
        }

        public OutboundWorkType WorkType { get; private set; }

        public DateTime LastChangedUtc
        {
            get
            {
                long ticks = Interlocked.Read(ref this.lastChangedUtcTicks);
                DateTime lastChangedUtc = new DateTime(ticks, DateTimeKind.Utc);
                return lastChangedUtc;
            }
        }

        public long Version
        {
            get
            {
                long currentVersion = Interlocked.Read(ref this.version);
                return currentVersion;
            }
        }

        public WaitHandle WakeSignal
        {
            get
            {
                return this.wakeSignal;
            }
        }

        internal void Notify(DateTime changedUtc)
        {
            long changedUtcTicks = changedUtc.Ticks;
            Interlocked.Exchange(ref this.lastChangedUtcTicks, changedUtcTicks);
            Interlocked.Increment(ref this.version);
            this.wakeSignal.Set();
        }
    }

    public static class OutboundWorkCoordinator
    {
        private static readonly IDictionary<OutboundWorkType, OutboundWorkState> States =
            CreateStates();

        public static OutboundWorkState GetState(OutboundWorkType workType)
        {
            OutboundWorkState state;
            bool stateFound = States.TryGetValue(workType, out state);
            if (!stateFound)
            {
                throw new ArgumentOutOfRangeException("workType");
            }

            return state;
        }

        public static void NotifyCommittedChanges(
            IEnumerable<object> insertedEntities,
            IEnumerable<object> updatedEntities)
        {
            NotifyCommittedChanges(insertedEntities, updatedEntities, null);
        }

        public static void NotifyCommittedChanges(
            IEnumerable<object> insertedEntities,
            IEnumerable<object> updatedEntities,
            ISet<OutboundWorkType> suppressedWorkTypes)
        {
            HashSet<OutboundWorkType> affectedWorkTypes =
                new HashSet<OutboundWorkType>();
            AddAffectedWorkTypes(insertedEntities, affectedWorkTypes);
            AddAffectedWorkTypes(updatedEntities, affectedWorkTypes);

            DateTime changedUtc = DateTime.UtcNow;
            foreach (OutboundWorkType workType in affectedWorkTypes)
            {
                bool workTypeSuppressed =
                    suppressedWorkTypes != null && suppressedWorkTypes.Contains(workType);
                if (workTypeSuppressed)
                {
                    continue;
                }
                OutboundWorkState state = GetState(workType);
                state.Notify(changedUtc);
            }
        }

        private static IDictionary<OutboundWorkType, OutboundWorkState> CreateStates()
        {
            Dictionary<OutboundWorkType, OutboundWorkState> states =
                new Dictionary<OutboundWorkType, OutboundWorkState>();
            AddState(states, OutboundWorkType.Sales);
            AddState(states, OutboundWorkType.Alerts);
            AddState(states, OutboundWorkType.TankFillings);
            AddState(states, OutboundWorkType.FuelPrices);
            AddState(states, OutboundWorkType.Balances);
            AddState(states, OutboundWorkType.TankChecks);
            AddState(states, OutboundWorkType.AlertEvaluation);
            AddState(states, OutboundWorkType.AlertPrinting);
            AddState(states, OutboundWorkType.InvoicePrinting);
            AddState(states, OutboundWorkType.FiscalDocumentPrinting);
            return states;
        }

        private static void AddState(
            IDictionary<OutboundWorkType, OutboundWorkState> states,
            OutboundWorkType workType)
        {
            OutboundWorkState state = new OutboundWorkState(workType);
            states.Add(workType, state);
        }

        private static void AddAffectedWorkTypes(
            IEnumerable<object> entities,
            ISet<OutboundWorkType> affectedWorkTypes)
        {
            if (entities == null)
            {
                return;
            }

            foreach (object entity in entities)
            {
                AddAffectedWorkTypes(entity, affectedWorkTypes);
            }
        }

        private static void AddAffectedWorkTypes(
            object entity,
            ISet<OutboundWorkType> affectedWorkTypes)
        {
            if (entity is SalesTransaction)
            {
                affectedWorkTypes.Add(OutboundWorkType.Sales);
                affectedWorkTypes.Add(OutboundWorkType.AlertEvaluation);
            }
            if (entity is Invoice)
            {
                affectedWorkTypes.Add(OutboundWorkType.Sales);
                affectedWorkTypes.Add(OutboundWorkType.InvoicePrinting);
            }
            InvoiceLine invoiceLine = entity as InvoiceLine;
            if (invoiceLine != null)
            {
                affectedWorkTypes.Add(OutboundWorkType.InvoicePrinting);
                if (invoiceLine.SaleTransactionId.HasValue)
                {
                    affectedWorkTypes.Add(OutboundWorkType.Sales);
                }
                if (invoiceLine.TankFillingId.HasValue)
                {
                    affectedWorkTypes.Add(OutboundWorkType.TankFillings);
                    affectedWorkTypes.Add(OutboundWorkType.FiscalDocumentPrinting);
                }
            }
            if (entity is SystemEvent)
            {
                affectedWorkTypes.Add(OutboundWorkType.Alerts);
                affectedWorkTypes.Add(OutboundWorkType.AlertPrinting);
                affectedWorkTypes.Add(OutboundWorkType.FiscalDocumentPrinting);
            }
            if (entity is Tank || entity is Dispenser || entity is Nozzle)
            {
                affectedWorkTypes.Add(OutboundWorkType.AlertEvaluation);
            }
            if (entity is TankFilling)
            {
                affectedWorkTypes.Add(OutboundWorkType.TankFillings);
                affectedWorkTypes.Add(OutboundWorkType.FiscalDocumentPrinting);
            }
            if (entity is FuelTypePrice)
            {
                affectedWorkTypes.Add(OutboundWorkType.FuelPrices);
            }
            if (entity is Balance)
            {
                affectedWorkTypes.Add(OutboundWorkType.Balances);
                affectedWorkTypes.Add(OutboundWorkType.FiscalDocumentPrinting);
            }
            if (entity is TankCheck)
            {
                affectedWorkTypes.Add(OutboundWorkType.TankChecks);
            }
            if (entity is Titrimetry)
            {
                affectedWorkTypes.Add(OutboundWorkType.AlertEvaluation);
                affectedWorkTypes.Add(OutboundWorkType.FiscalDocumentPrinting);
            }
        }
    }
}

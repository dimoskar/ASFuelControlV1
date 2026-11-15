using System;

namespace ASFuelControl.Common.Sales
{
    internal sealed class PendingControllerCompletion
    {
        public Guid Id = Guid.NewGuid();
        public FuelPumpControllerBase Owner;
        public long Sequence;
        public DateTime ReceivedUtc = DateTime.UtcNow;
        public FuelPoint FuelPoint;
        public int NozzleIndex;
        public bool DirectSale;
        public decimal ReportedTotal;
        public decimal ReportedPriceTotal;
        public decimal Amount;
        public decimal Volume;
        public decimal StartTotal;
        public decimal UnitPrice;
        public bool Work;
        public bool NeedsInvoice;
        public bool SnapshotCaptured;
        public bool SourceValuesCaptured;
        public int TotalDecimalPlaces;
        public bool TotalsEvaluated;
        public bool WaitingForTotals;
        public decimal EndTotal;
        public bool DecisionMade;
        public bool ShouldCreateSale;
        public bool SaleRetained;
        public bool TotalsNotified;
        public bool SuspendHandled;
        public bool FleetHandled;
        public bool ValuesPublished;
        public SaleData Sale;
        public DateTime RetryAfterUtc;
        public string LastError;
    }
}

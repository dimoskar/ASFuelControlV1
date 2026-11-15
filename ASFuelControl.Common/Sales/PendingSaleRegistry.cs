using System;
using System.Collections.Concurrent;
using System.Linq;

namespace ASFuelControl.Common.Sales
{
    public sealed class PendingSaleRecord
    {
        internal PendingSaleRecord(SaleData sale, long sequence)
        {
            this.Sale = sale;
            this.ReceivedUtc = DateTime.UtcNow;
            this.ReceivedSequence = sequence;
            this.NozzleNumber = -1;
        }

        public SaleData Sale { get; private set; }
        public DateTime ReceivedUtc { get; private set; }
        public long ReceivedSequence { get; private set; }
        public IController SourceController { get; internal set; }
        public int Channel { get; internal set; }
        public int Address { get; internal set; }
        public int NozzleNumber { get; internal set; }
        public Guid DispenserId { get; set; }
        public bool ApplicationContextCaptured { get; set; }
        public bool Prepared { get; set; }
        public bool ReceivedForProcessing { get; set; }
        public bool ContextCaptured { get; set; }
    }

    // Owned by the controller layer: retention does not depend on a Windows subscriber.
    public sealed class PendingSaleRegistry
    {
        public static readonly PendingSaleRegistry Instance = new PendingSaleRegistry();
        public static readonly object SyncRoot = new object();
        private readonly ConcurrentDictionary<Guid, PendingSaleRecord> sales = new ConcurrentDictionary<Guid, PendingSaleRecord>();
        private long sequence;

        private PendingSaleRegistry()
        {
        }

        public PendingSaleRecord this[Guid saleId]
        {
            get
            {
                return this.sales[saleId];
            }
        }

        public PendingSaleRecord[] Values
        {
            get
            {
                return this.sales.Values.ToArray();
            }
        }

        public PendingSaleRecord Retain(SaleData sale)
        {
            if (sale == null)
            {
                throw new ArgumentNullException("sale");
            }
            lock (SyncRoot)
            {
                PendingSaleRecord record;
                bool retained = this.sales.TryGetValue(sale.SalesId, out record);
                if (retained)
                {
                    return record;
                }
                this.sequence++;
                record = new PendingSaleRecord(sale, this.sequence);
                this.sales.TryAdd(sale.SalesId, record);
                if (sale.SaleEndTime == DateTime.MinValue)
                {
                    sale.SaleEndTime = DateTime.Now;
                }
                return record;
            }
        }

        public PendingSaleRecord RetainFromController(SaleData sale, IController controller, int channel, int address, int nozzleNumber)
        {
            lock (SyncRoot)
            {
                PendingSaleRecord record = this.Retain(sale);
                if (record.SourceController == null)
                {
                    record.SourceController = controller;
                    record.Channel = channel;
                    record.Address = address;
                    record.NozzleNumber = nozzleNumber;
                }
                return record;
            }
        }

        public bool TryGetValue(Guid saleId, out PendingSaleRecord record)
        {
            return this.sales.TryGetValue(saleId, out record);
        }

        public bool ContainsKey(Guid saleId)
        {
            return this.sales.ContainsKey(saleId);
        }

        // The application calls this only after a fresh database read verifies completion.
        public void ReleaseVerified(SaleData sale)
        {
            lock (SyncRoot)
            {
                PendingSaleRecord record;
                bool retained = this.sales.TryGetValue(sale.SalesId, out record);
                bool sameSale = retained && object.ReferenceEquals(record.Sale, sale);
                if (!sameSale || !record.Prepared || !sale.SaleProcessed)
                {
                    throw new InvalidOperationException("Only a verified prepared sale can be released.");
                }
                this.sales.TryRemove(sale.SalesId, out record);
            }
        }
    }
}

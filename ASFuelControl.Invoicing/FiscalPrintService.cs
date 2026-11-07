using System;
using System.Linq;
using ASFuelControl.Invoicing.Persistence;
using Exedron.Invoicing.Core;

namespace ASFuelControl.Invoicing
{
    // Validate performs local checks only. Dispatch returns a spool/transport receipt,
    // not a claim that paper physically emerged. Exceptions after entry are ambiguous.
    public interface IFiscalPrintTransport
    {
        void Validate(string destination, string format, byte[] document);
        string Dispatch(string destination, string format, byte[] document, Guid jobId);
    }
    public sealed class FiscalPrintMaterialization
    {
        public string Format { get; set; }
        public byte[] Document { get; set; }
    }
    public interface IFiscalPrintMaterializer
    {
        FiscalPrintMaterialization Materialize(string destination, string storedFormat,
            byte[] storedPayload);
    }
    public sealed class FiscalPrintService
    {
        private readonly SqlFiscalOperationStore store;
        private readonly IFiscalPrintTransport transport;
        private readonly IFiscalPrintMaterializer materializer;
        public FiscalPrintService(SqlFiscalDatabase database, IFiscalPrintTransport transport,
            IFiscalPrintMaterializer materializer = null)
        {
            store = new SqlFiscalOperationStore(database);
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
            this.materializer = materializer;
        }

        public FiscalOperationRecord Print(IFiscalDocumentLease lease, Guid? acceptedAttemptId, string destination,
            string format, Func<byte[]> render, string reprintReason = null, bool offlineReceipt = false)
        {
            var all = store.Load(lease, true);
            var last = all.LastOrDefault();
            if (last != null && (last.State == "Dispatching" || last.State == "Unknown"))
            {
                if (last.State == "Dispatching") store.SavePrint(lease, last, FiscalPrintState.Unknown, "Process interrupted during printer dispatch; reconcile the original job.");
                return last;
            }
            if (last != null && (last.State == "HandedOff" || last.State == "Confirmed") && string.IsNullOrWhiteSpace(reprintReason)) return last;
            if (last == null || last.State != "Prepared")
            {
                // Every subsequent attempt copies the FIRST saved document, even after
                // rejection or a local failure. Never render today's entity/settings.
                // An explicit operator reprint may, however, use a freshly resolved
                // printer. Automatic recovery keeps the original destination.
                var original = all.FirstOrDefault();
                var explicitReprint = !string.IsNullOrWhiteSpace(reprintReason);
                Guid? originalAttemptId = original?.OriginalAttemptId ??
                    acceptedAttemptId;
                string savedDestination = explicitReprint ? destination :
                    original?.Scope ?? destination;
                string savedFormat = original?.PayloadFormat ?? format;
                byte[] savedPayload = original?.Payload;
                if (savedPayload == null)
                {
                    savedPayload = render();
                }
                string savedReason = reprintReason ?? last?.Reason;
                bool savedOfflineReceipt = original?.OfflineReceipt ?? offlineReceipt;
                last = store.BeginPrint(lease, originalAttemptId, savedDestination,
                    savedFormat, savedPayload, savedReason, savedOfflineReceipt);
            }
            FiscalPrintMaterialization materialized;
            try
            {
                materialized = Materialize(last);
                transport.Validate(last.Scope, materialized.Format, materialized.Document);
            }
            catch (Exception ex)
            {
                store.SavePrint(lease, last, FiscalPrintState.FailedBeforeDispatch, ex.Message);
                return last;
            }
            store.SavePrint(lease, last, FiscalPrintState.Dispatching, "Saved payload ready for printer handoff. Job " + last.AttemptId);
            string receipt;
            try
            {
                receipt = transport.Dispatch(last.Scope, materialized.Format,
                    materialized.Document, last.AttemptId);
                if (string.IsNullOrWhiteSpace(receipt)) throw new InvalidOperationException("Printer transport returned no handoff receipt.");
            }
            catch (Exception ex)
            {
                store.SavePrint(lease, last, FiscalPrintState.Unknown, ex.Message);
                return last;
            }
            // Do not catch a failed outcome write as a transport failure. Dispatching
            // remains durable and the next call cannot send another copy.
            store.SavePrint(lease, last, FiscalPrintState.HandedOff, receipt);
            return last;
        }

        private FiscalPrintMaterialization Materialize(FiscalOperationRecord record)
        {
            if (materializer == null)
            {
                return new FiscalPrintMaterialization
                {
                    Format = record.PayloadFormat,
                    Document = record.Payload
                };
            }

            FiscalPrintMaterialization result = materializer.Materialize(record.Scope,
                record.PayloadFormat, record.Payload);
            if (result == null || string.IsNullOrWhiteSpace(result.Format) ||
                result.Document == null || result.Document.Length == 0)
            {
                throw new InvalidOperationException(
                    "The stored print data could not be materialized.");
            }
            return result;
        }

        public void Reconcile(IFiscalDocumentLease lease, Guid jobId, bool confirmedPrinted, string evidence)
        {
            if (string.IsNullOrWhiteSpace(evidence)) throw new ArgumentException("Operator/device evidence is required.");
            var job = store.Load(lease, true).Single(p => p.AttemptId == jobId);
            if (job.State == "Dispatching") store.SavePrint(lease, job, FiscalPrintState.Unknown, "Dispatch outcome requires reconciliation.");
            if (job.State != "Unknown") throw new InvalidOperationException("Only an uncertain print job can be reconciled.");
            store.SavePrint(lease, job, confirmedPrinted ? FiscalPrintState.Confirmed : FiscalPrintState.Rejected, evidence);
        }
    }
}

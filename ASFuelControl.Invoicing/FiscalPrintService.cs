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
    public sealed class FiscalPrintService
    {
        private readonly SqlFiscalOperationStore store;
        private readonly IFiscalPrintTransport transport;
        public FiscalPrintService(SqlFiscalDatabase database, IFiscalPrintTransport transport)
        { store = new SqlFiscalOperationStore(database); this.transport = transport ?? throw new ArgumentNullException(nameof(transport)); }

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
                var original = all.FirstOrDefault();
                last = store.BeginPrint(lease, original?.OriginalAttemptId ?? acceptedAttemptId,
                    original?.Scope ?? destination, original?.PayloadFormat ?? format,
                    original?.Payload ?? render(), reprintReason ?? last?.Reason, original?.OfflineReceipt ?? offlineReceipt);
            }
            try { transport.Validate(last.Scope, last.PayloadFormat, last.Payload); }
            catch (Exception ex)
            {
                store.SavePrint(lease, last, FiscalPrintState.FailedBeforeDispatch, ex.Message);
                return last;
            }
            store.SavePrint(lease, last, FiscalPrintState.Dispatching, "Saved payload ready for printer handoff. Job " + last.AttemptId);
            string receipt;
            try
            {
                receipt = transport.Dispatch(last.Scope, last.PayloadFormat, last.Payload, last.AttemptId);
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

using System;
using System.Linq;
using System.Text;
using ASFuelControl.Invoicing.Persistence;
using Exedron.Invoicing.Core;
using Exedron.Invoicing.Core.Models;
using Newtonsoft.Json;

namespace ASFuelControl.Invoicing
{
    // Balance/tank-filling counterpart of InvoiceWorkflowService. Alerts/titrimetry
    // have no implemented fiscal signer and cannot enter this workflow.
    public sealed class SignedDocumentWorkflowService
    {
        private readonly SqlFiscalDatabase database;
        private readonly FiscalSigningService signing;
        private readonly FiscalPrintService printing;
        private readonly Func<InvoiceWorkflowSnapshot,InvoiceResponse,byte[]> render;
        public SignedDocumentWorkflowService(SqlFiscalDatabase database, FiscalSigningService signing,
            FiscalPrintService printing, Func<InvoiceWorkflowSnapshot,InvoiceResponse,byte[]> render)
        { this.database = database; this.signing = signing; this.printing = printing; this.render = render; }
        public InvoiceWorkflowResult Process(FiscalDocumentReference document, Func<InvoiceWorkflowSnapshot> prepare,
            Action<InvoiceWorkflowResult> project, string reprintReason = null)
        {
            if (document.Kind != FiscalDocumentKind.Balance && document.Kind != FiscalDocumentKind.TankFilling)
                throw new NotSupportedException("Only balances and tank fillings have a supported signing workflow.");
            using(var lease = database.EnterDocument(document))
            {
                var snapshots = new SqlInvoiceWorkflowStore(database);
                var bytes = snapshots.Load(lease);
                if (bytes == null)
                {
                    if (reprintReason != null) throw new InvalidOperationException("No saved document exists to reprint.");
                    var history = new SqlFiscalHistoryStore(database);
                    foreach(FiscalJournalFamily family in Enum.GetValues(typeof(FiscalJournalFamily)))
                        if (history.Load(lease,family).Any()) throw new InvalidOperationException("Existing fiscal history requires reconciliation.");
                    var input = prepare(); Validate(document,input);
                    bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(input)); snapshots.Save(lease,bytes);
                }
                var snapshot = JsonConvert.DeserializeObject<InvoiceWorkflowSnapshot>(Encoding.UTF8.GetString(bytes)); Validate(document,snapshot);
                var last = new SqlFiscalOperationStore(database).Load(lease,true).LastOrDefault();
                var result = new InvoiceWorkflowResult();
                if (last == null)
                {
                    var signed = signing.Sign(document,snapshot.Signing.Scope,new FrozenSource(snapshot.Signing));
                    result.FiscalState = signed.State.ToString();
                    if (signed.State != FiscalSigningState.Signed) return result;
                    result.AcceptedAttemptId = signed.Request.AttemptId;
                    result.Response = new InvoiceResponse { InvoiceSignature = signed.Result.Signature, QrCodeUrl = signed.Result.QrCode };
                    project(result);
                }
                else { result.FiscalState = "Signed"; result.AcceptedAttemptId = last.OriginalAttemptId; }
                result.Print = printing.Print(lease,result.AcceptedAttemptId.Value,snapshot.PrintDestination,snapshot.PrintFormat,
                    () => render(snapshot,result.Response),reprintReason);
                project(result); return result;
            }
        }
        private static void Validate(FiscalDocumentReference document, InvoiceWorkflowSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Version != 1 || snapshot.InvoiceId != document.DocumentId || snapshot.Signing == null ||
                snapshot.Signing.Document.Kind != document.Kind || snapshot.Signing.Document.DocumentId != document.DocumentId ||
                snapshot.Provider != null || snapshot.Payment != null || snapshot.PrintContent == null || snapshot.PrintContent.Length == 0 ||
                string.IsNullOrWhiteSpace(snapshot.PrintDestination) || string.IsNullOrWhiteSpace(snapshot.PrintFormat)) throw new InvalidOperationException("Invalid signed-document snapshot.");
        }
        private sealed class FrozenSource : IFiscalSigningSource
        {
            private readonly FiscalSigningRequest request;
            internal FrozenSource(FiscalSigningRequest request) { this.request = request; }
            public FiscalSigningRequest Prepare(FiscalDocumentReference document,FiscalSigningScope scope) => request;
        }
    }
}

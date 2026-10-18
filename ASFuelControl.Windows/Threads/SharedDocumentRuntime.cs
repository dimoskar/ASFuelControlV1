using System;
using System.Linq;
using System.Text;
using ASFuelControl.Invoicing;
using ASFuelControl.Invoicing.Persistence;
using Exedron.Invoicing.Core;
using Exedron.Invoicing.Adapters.Samtec;
using Newtonsoft.Json;

namespace ASFuelControl.Windows.Threads
{
    internal static partial class SharedInvoiceRuntime
    {
        internal static bool HandlesDocument(string kind, Guid id)
        {
            var document = new FiscalDocumentReference((FiscalDocumentKind)Enum.Parse(typeof(FiscalDocumentKind),kind),id);
            if (Enabled && settings.Value.Documents?.ContainsKey(document.Kind) == true) return true;
            if (settings.Value.ProtectedKinds.Contains(document.Kind)) return true;
            using(var lease = sql.Value.EnterDocument(document)) return new SqlFiscalOwnershipStore(sql.Value).Owns(lease);
        }
        internal static void ProcessDocument(string kind, Guid id)
        {
            var document = new FiscalDocumentReference((FiscalDocumentKind)Enum.Parse(typeof(FiscalDocumentKind),kind),id);
            var signing = new FiscalSigningService(sql.Value,scope => {
                var profile = (settings.Value.Devices ?? new SamtecDeviceProfile[0]).Single(d => FiscalSigningCoordinator.SameScope(d.Scope,scope));
                return new SamtecFiscalSigner(profile);
            });
            var service = new SignedDocumentWorkflowService(sql.Value,signing,new FiscalPrintService(sql.Value,new SharedInvoicePrinting(() => settings.Value.DefaultPrinter)),SharedInvoicePrinting.Render);
            service.Process(document,() => PrepareDocument(document), result => {
                using(var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                {
                    bool printed = result.Print?.State == "HandedOff" || result.Print?.State == "Confirmed";
                    if (document.Kind == FiscalDocumentKind.Balance)
                    {
                        var balance = db.Balances.Single(b => b.BalanceId == id);
                        if (result.Response != null) balance.DocumentSign = result.Response.InvoiceSignature;
                        if (printed && !balance.PrintDate.HasValue) balance.PrintDate = DateTime.Now;
                    }
                    else if (printed)
                    {
                        // SignSignature is the legacy polling flag for fillings. Set it
                        // only after handoff, so print failures remain in the worker queue.
                        using(var lease = sql.Value.EnterDocument(document))
                            db.TankFillings.Single(f => f.TankFillingId == id).SignSignature =
                                new SqlFiscalSigningStore(sql.Value).Load(lease).Last(a => a.State == FiscalSigningState.Signed).Result.Signature;
                    }
                    db.SaveChanges();
                }
            });
        }
        private static InvoiceWorkflowSnapshot PrepareDocument(FiscalDocumentReference document)
        {
            if (!Enabled) throw new InvalidOperationException("First issuance through the shared workflow is disabled.");
            var route = settings.Value.Documents[document.Kind];
            var printer = SharedInvoicePrinting.ResolveProfile(route.Printer,settings.Value.DefaultPrinter);
            if (route.Signing == null || route.Provider != null || route.Payment != null ||
                route.Signing.IssuerVat != Data.Implementation.OptionHandler.Instance.GetOption("CompanyTIN") ||
                route.Signing.IssuerBranch != Data.Implementation.OptionHandler.Instance.GetIntOption("CompanyBranch",0))
                throw new InvalidOperationException("An explicit matching fiscal-device route is required.");
            using(var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                string content; FiscalSigningRequest signing;
                if (document.Kind == FiscalDocumentKind.Balance)
                {
                    var balance = db.Balances.Single(b => b.BalanceId == document.DocumentId);
                    if (balance.EndDate < settings.Value.FirstInvoiceDate) throw new InvalidOperationException("Historical balance needs reconciliation.");
                    content = balance.BalanceText;
                    signing = HostFiscalSigningMapper.Balance(balance,route.Signing,content);
                }
                else
                {
                    var filling = db.TankFillings.Single(f => f.TankFillingId == document.DocumentId);
                    if (filling.TransactionTime < settings.Value.FirstInvoiceDate) throw new InvalidOperationException("Historical tank filling needs reconciliation.");
                    content = PrintAgent.GenerateTankFillingContext(db,filling);
                    signing = HostFiscalSigningMapper.TankFilling(filling,route.Signing,content,filling.LevelStart > filling.LevelEnd ? 181 : 40);
                }
                return new InvoiceWorkflowSnapshot { InvoiceId = document.DocumentId, Signing = signing,
                    PrintContent = Encoding.UTF8.GetBytes(content), PrintFormat = SharedInvoicePrinting.Format,
                    PrintDestination = JsonConvert.SerializeObject(printer) };
            }
        }
    }
}

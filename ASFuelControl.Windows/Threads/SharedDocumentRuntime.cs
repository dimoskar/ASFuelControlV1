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
            bool printPhysical = Data.Implementation.OptionHandler.Instance.GetBoolOption("PrintBalancelsOnPrinter",false);
            bool archived = false;
            var signing = new FiscalSigningService(sql.Value,scope => {
                var profile = (settings.Value.Devices ?? new SamtecDeviceProfile[0]).Single(d => FiscalSigningCoordinator.SameScope(d.Scope,scope));
                return new SamtecFiscalSigner(profile);
            });
            var service = new SignedDocumentWorkflowService(sql.Value,signing,
                new FiscalPrintService(sql.Value,new SharedInvoicePrinting()),SharedDocumentReportRenderer.Render);
            service.Process(document,() => PrepareDocument(document,printPhysical), result => {
                using(var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                {
                    bool printed = result.Print?.State == "HandedOff" || result.Print?.State == "Confirmed";
                    string signature = result.Response?.InvoiceSignature;
                    if (string.IsNullOrWhiteSpace(signature))
                    {
                        using(var lease = sql.Value.EnterDocument(document))
                            signature = new SqlFiscalSigningStore(sql.Value).Load(lease)
                                .Last(attempt => attempt.State == FiscalSigningState.Signed).Result.Signature;
                    }
                    if (document.Kind == FiscalDocumentKind.Balance)
                    {
                        var balance = db.Balances.Single(b => b.BalanceId == id);
                        if (balance.DocumentSign != signature)
                        {
                            balance.DocumentSign = signature;
                            db.SaveChanges();
                        }
                        if (!archived)
                        {
                            new PrintAgent().StoreBalanceReport(balance,true,signature);
                            archived = true;
                        }
                        if ((!printPhysical || printed) && !balance.PrintDate.HasValue)
                            balance.PrintDate = DateTime.Now;
                    }
                    else if (document.Kind == FiscalDocumentKind.TankFilling)
                    {
                        var filling = db.TankFillings.Single(f => f.TankFillingId == id);
                        if (!archived)
                        {
                            new PrintAgent().StoreTankFillingReport(filling,signature);
                            archived = true;
                        }
                        // SignSignature is the legacy polling completion flag. Keep
                        // it empty until the mandatory archive and optional physical
                        // handoff have both completed.
                        if (!printPhysical || printed)
                            filling.SignSignature = signature;
                    }
                    db.SaveChanges();
                }
            },null,printPhysical);
        }
        private static InvoiceWorkflowSnapshot PrepareDocument(FiscalDocumentReference document, bool printPhysical)
        {
            if (!Enabled) throw new InvalidOperationException("First issuance through the shared workflow is disabled.");
            var route = settings.Value.Documents[document.Kind];
            var printer = new SharedPrinterProfile { Thermal = false, NonThermalPaperSize = "A4",
                WidthHundredths = 315, HeightHundredths = 1100, Copies = 1, FontSize = 8 };
            if (printPhysical) printer = SharedInvoicePrinting.ResolveProfile(printer,null);
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

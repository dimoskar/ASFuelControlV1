using System;
using System.Linq;
using System.Windows.Forms;
using Telerik.WinControls;

namespace ASFuelControl.Windows.UI.Forms
{
    internal sealed class AadeTraderImportResult
    {
        internal Guid ExistingTraderId { get; set; }
        internal ViewModels.TraderDetailsViewModel Trader { get; set; }
        internal string Status { get; set; }
    }

    /// <summary>
    /// One UI workflow for every AADE trader import entry point. The lookup never
    /// saves a returned party directly: official fields are reviewed in TraderForm
    /// and duplicate VAT numbers resolve to the existing trader.
    /// </summary>
    internal static class AadeTraderImportWorkflow
    {
        internal static AadeTraderImportResult Lookup(Control owner,
            ViewModels.InvoiceCounterpartySelection role,string suggestedVat)
        {
            using (var form = new GetTaxNumberForm(role,NormalizeSuggestedVat(suggestedVat)))
            {
                if (form.ShowDialog(owner) != DialogResult.OK)
                    return null;
                return new AadeTraderImportResult {
                    ExistingTraderId = form.ExistingTraderId,
                    Trader = form.TraderResponse?.Trader,
                    Status = form.TraderResponse?.Status
                };
            }
        }

        internal static Guid CreateOrSelect(Control owner,
            ViewModels.InvoiceCounterpartySelection role,string suggestedVat)
        {
            var result = Lookup(owner,role,suggestedVat);
            if (result == null)
                return Guid.Empty;
            if (result.ExistingTraderId != Guid.Empty)
                return EnsureRole(owner,result.ExistingTraderId,role)
                    ? result.ExistingTraderId : Guid.Empty;
            if (result.Trader == null)
                return Guid.Empty;

            ApplyRole(result.Trader,role);
            Guid traderId;
            using (var form = new TraderForm())
            {
                form.LoadNew(result.Trader);
                form.ShowDialog(owner);
                traderId = form.SelectedCustomerId;
            }
            if (traderId == Guid.Empty)
                return Guid.Empty;
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                return db.Traders.Any(value => value.TraderId == traderId)
                    ? traderId : Guid.Empty;
        }

        internal static bool EnsureRole(Control owner,Guid traderId,
            ViewModels.InvoiceCounterpartySelection role)
        {
            if (role == ViewModels.InvoiceCounterpartySelection.Any)
                return true;
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var trader = db.Traders.SingleOrDefault(value => value.TraderId == traderId);
                if (trader == null)
                    return false;
                bool alreadyValid = role == ViewModels.InvoiceCounterpartySelection.Customer
                    ? trader.IsCustomer : trader.IsSupplier;
                if (alreadyValid)
                    return true;
                string roleName = role == ViewModels.InvoiceCounterpartySelection.Customer
                    ? "πελάτης" : "προμηθευτής";
                if (RadMessageBox.Show(owner,
                    "Υπάρχει ήδη συναλλασσόμενος με αυτό το Α.Φ.Μ. Να χαρακτηριστεί επίσης ως " + roleName + ";",
                    "Υπάρχων συναλλασσόμενος",MessageBoxButtons.YesNo,RadMessageIcon.Question) != DialogResult.Yes)
                    return false;
                if (role == ViewModels.InvoiceCounterpartySelection.Customer)
                    trader.IsCustomer = true;
                else
                    trader.IsSupplier = true;
                db.SaveChanges();
                return true;
            }
        }

        internal static void ApplyRole(ViewModels.TraderDetailsViewModel trader,
            ViewModels.InvoiceCounterpartySelection role)
        {
            if (trader == null)
                return;
            if (role == ViewModels.InvoiceCounterpartySelection.Customer)
            {
                trader.IsCustomer = true;
                trader.IsSupplier = false;
            }
            else if (role == ViewModels.InvoiceCounterpartySelection.Supplier)
            {
                trader.IsCustomer = false;
                trader.IsSupplier = true;
            }
        }

        private static string NormalizeSuggestedVat(string value)
        {
            string normalized = (value ?? "").Trim().Replace(" ","").ToUpperInvariant();
            if (normalized.StartsWith("GR",StringComparison.Ordinal))
                normalized = normalized.Substring(2);
            return normalized.Length == 9 && normalized.All(char.IsDigit)
                ? normalized : "";
        }
    }
}

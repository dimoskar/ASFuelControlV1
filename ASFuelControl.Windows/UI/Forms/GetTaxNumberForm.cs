using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Telerik.WinControls;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.Forms
{
    public partial class GetTaxNumberForm : RadForm
    {
        public ViewModels.AadeTraderResponse TraderResponse { set; get; }
        public Guid ExistingTraderId { get; private set; }

        public GetTaxNumberForm()
            : this(ViewModels.InvoiceCounterpartySelection.Any,null)
        {
        }

        internal GetTaxNumberForm(ViewModels.InvoiceCounterpartySelection mode,string suggestedVat)
        {
            InitializeComponent();
            Text = "Αναζήτηση συναλλασσόμενου στην ΑΑΔΕ";
            this.Height = 131;
            textBoxExt1.Text = suggestedVat ?? "";
            radButton2.Visible = mode != ViewModels.InvoiceCounterpartySelection.Supplier;
            radButton1.Visible = mode != ViewModels.InvoiceCounterpartySelection.Customer;
        }

        private async void sfButton1_Click(object sender, EventArgs e)
        {
            await SearchAsync();
        }

        private void sfButton2_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private async void textBoxExt1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                await SearchAsync();
            }
        }

        private async Task SearchAsync()
        {
            string taxNumber = NormalizeVat(textBoxExt1.Text);
            if (taxNumber == null)
            {
                RadMessageBox.Show(this,"Το Α.Φ.Μ. για αναζήτηση στην ΑΑΔΕ πρέπει να περιέχει 9 ψηφία.",
                    "Μη έγκυρο Α.Φ.Μ.",MessageBoxButtons.OK,RadMessageIcon.Exclamation);
                return;
            }
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var oldTrader = db.Traders.FirstOrDefault(t => t.TaxRegistrationNumber == taxNumber ||
                    t.TaxRegistrationNumber == "GR" + taxNumber);
                if (oldTrader != null)
                {
                    ExistingTraderId = oldTrader.TraderId;
                    RadMessageBox.Show(this,"Υπάρχει ήδη συναλλασσόμενος με το ίδιο Α.Φ.Μ. και θα χρησιμοποιηθεί η υπάρχουσα εγγραφή.",
                        "Υπάρχων συναλλασσόμενος",MessageBoxButtons.OK,RadMessageIcon.Info);
                    DialogResult = DialogResult.OK;
                    Close();
                    return;
                }
            }
            ViewModels.AadeServiceViewModel vm = new ViewModels.AadeServiceViewModel();
            if (string.IsNullOrWhiteSpace(vm.UserName) || string.IsNullOrWhiteSpace(vm.Password))
            {
                RadMessageBox.Show(this,"Δεν έχουν ρυθμιστεί τα στοιχεία πρόσβασης της υπηρεσίας ΑΑΔΕ.",
                    "Ρυθμίσεις ΑΑΔΕ",MessageBoxButtons.OK,RadMessageIcon.Exclamation);
                return;
            }
            SetBusy(true);
            ViewModels.AadeTraderResponse traderData;
            try
            {
                traderData = await vm.GetTraderFromTaxNumberAsync(taxNumber);
            }
            finally
            {
                SetBusy(false);
            }
            this.labelStatus.Text = "";
            if (traderData.ErrorMessage != null && traderData.ErrorMessage != "")
            {
                this.traderDetailsViewModelBindingSource.DataSource = new ViewModels.TraderDetailsViewModel();
                this.traderDetailsViewModelBindingSource.ResetBindings(false);
                Telerik.WinControls.RadMessageBox.Show(this, traderData.ErrorMessage, "Σφάλμα κλήσης ΑΑΔΕ...", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                this.TraderResponse = null;
                this.Height = 131;
                this.CenterToScreen();
                return;
            }
            this.TraderResponse = traderData;
            this.traderDetailsViewModelBindingSource.DataSource = this.TraderResponse.Trader;
            this.traderDetailsViewModelBindingSource.ResetBindings(false);
            this.labelStatus.Text = this.TraderResponse.Status;
            this.Height = 436;
            this.CenterToScreen();
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            if (!ConfirmInactiveTrader()) return;
            TraderResponse.Trader.IsCustomer = true;
            TraderResponse.Trader.IsSupplier = false;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            if (!ConfirmInactiveTrader()) return;
            TraderResponse.Trader.IsCustomer = false;
            TraderResponse.Trader.IsSupplier = true;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void SetBusy(bool busy)
        {
            UseWaitCursor = busy;
            textBoxExt1.Enabled = !busy;
            sfButton1.Enabled = !busy;
            sfButton2.Enabled = !busy;
        }

        private bool ConfirmInactiveTrader()
        {
            if (TraderResponse?.Trader == null)
                return false;
            string status = (TraderResponse.Status ?? "").ToUpperInvariant();
            bool inactive = status.Contains("ΑΝΕΝΕΡΓ") || status.Contains("ΔΙΑΚΟΠ") ||
                status.Contains("INACTIVE");
            return !inactive || RadMessageBox.Show(this,
                "Η ΑΑΔΕ επέστρεψε κατάσταση «" + TraderResponse.Status + "». Θέλετε να συνεχίσετε;",
                "Κατάσταση συναλλασσόμενου",MessageBoxButtons.YesNo,RadMessageIcon.Exclamation) == DialogResult.Yes;
        }

        private static string NormalizeVat(string value)
        {
            string normalized = (value ?? "").Trim().Replace(" ","").ToUpperInvariant();
            if (normalized.StartsWith("GR",StringComparison.Ordinal))
                normalized = normalized.Substring(2);
            return normalized.Length == 9 && normalized.All(char.IsDigit)
                ? normalized : null;
        }
    }
}

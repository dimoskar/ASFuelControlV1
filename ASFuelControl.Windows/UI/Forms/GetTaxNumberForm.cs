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

        public GetTaxNumberForm()
        {
            InitializeComponent();
            this.Height = 131;
        }

        private void sfButton1_Click(object sender, EventArgs e)
        {
            Search();
        }

        private void sfButton2_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void textBoxExt1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                Search();
            }
        }

        private void Search()
        {
            using (var db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                var oldTrader = db.Traders.FirstOrDefault(t => t.TaxRegistrationNumber == this.textBoxExt1.Text);
                if (oldTrader != null)
                {
                    Telerik.WinControls.RadMessageBox.Show(this, "Υπάρχει ήδη συναλλασσόμενος με το ίδιο Α.Φ.Μ.", "Σφάλμα...", MessageBoxButtons.OK, RadMessageIcon.Exclamation );
                    return;
                }
            }
            ViewModels.AadeServiceViewModel vm = new ViewModels.AadeServiceViewModel();
            var traderData = vm.GetTraderFromTaxNumber(this.textBoxExt1.Text);
            this.labelStatus.Text = "";
            if (traderData.ErrorMessage != null && traderData.ErrorMessage != "")
            {
                this.traderDetailsViewModelBindingSource.DataSource = new ViewModels.TraderDetailsViewModel();
                this.traderDetailsViewModelBindingSource.ResetBindings(false);
                Telerik.WinControls.RadMessageBox.Show(this, traderData.ErrorMessage, "Σφάλμα κλήσης ΓΓΠΣ...", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
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
            TraderResponse.Trader.IsCustomer = true;
            TraderResponse.Trader.IsSupplier = false;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            TraderResponse.Trader.IsCustomer = false;
            TraderResponse.Trader.IsSupplier = true;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}

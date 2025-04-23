using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Telerik.WinControls;

namespace ASFuelControl.Windows.UI.Catalogs
{
    public partial class TraderCatalog : UserControl
    {
        ViewModels.TraderCatalogViewModel vm = new ViewModels.TraderCatalogViewModel();

        public TraderCatalog()
        {
            InitializeComponent();
            this.traderViewModelBindingSource.DataSource = vm;
            this.radGridView1.DataMember = "Traders";
        }

        public void RefreshData()
        {
            this.vm.LoadData();
            
            //this.radGridView1.DataSource = this.traderViewModelBindingSource;
            //this.traderViewModelBindingSource.ResetBindings(false);
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            this.vm.LoadData();
        }

        private void radGridView1_CellDoubleClick(object sender, Telerik.WinControls.UI.GridViewCellEventArgs e)
        {
            if (this.radGridView1.CurrentRow == null)
                return;
            var trader = this.radGridView1.CurrentRow.DataBoundItem as ViewModels.TraderViewModel;
            if (trader == null)
                return;
            Forms.TraderForm tf = new Forms.TraderForm();
            tf.LoadTrader(trader.TraderId);
            tf.Show(this);
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (this.radGridView1.CurrentRow == null)
                return;
            var trader = this.radGridView1.CurrentRow.DataBoundItem as ViewModels.TraderViewModel;
            if (trader == null)
                return;
            if(!trader.CanDelete)
            {
                RadMessageBox.Show("Υπάρχουν εκδομένα παραστατικά για τον επιλεγμένο συναλλασσόμενο!\r\nΟ συναλλασσόμενος που επιλέξατε δεν μπορεί να διαγραφεί!", "Ενημέρωση Διαγραφής...", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                return;
            }
            if (RadMessageBox.Show("Θέλετε να διαγράψετε την επιλεγμένη εγγραφή", "Διαγραφή Συναλλασσόμενου...", MessageBoxButtons.YesNo, RadMessageIcon.Question) == DialogResult.No)
                return;
            trader.EntityState = ViewModels.EntityStateEnum.Deleted;
            trader.Save(trader.TraderId);
            this.vm.LoadData();
        }

        private void addCustomerMenuItem_Click(object sender, EventArgs e)
        {
            Forms.TraderForm tf = new Forms.TraderForm();
            tf.CreateCustomer();
            tf.Show(this);
        }

        private void addSupplierMenuItem_Click(object sender, EventArgs e)
        {
            Forms.TraderForm tf = new Forms.TraderForm();
            tf.CreateSuplier();
            tf.Show(this);
        }

        private void radTextBox1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                this.vm.Filter = this.radTextBox1.Text;
                this.vm.LoadData();
            }
            e.Handled = true;
        }

        private void radMenuItem1_Click(object sender, EventArgs e)
        {
            using (Forms.GetTaxNumberForm gtnf = new Forms.GetTaxNumberForm())
            {
                var res = gtnf.ShowDialog(this);
                if (res == DialogResult.OK && gtnf.TraderResponse != null)
                {
                    if (gtnf.TraderResponse.Trader != null)
                    {
                        if (gtnf.TraderResponse.Trader != null)
                        {
                            Forms.TraderForm tf = new Forms.TraderForm();
                            tf.LoadNew(gtnf.TraderResponse.Trader);
                            tf.Show(this);
                        }
                    }
                    if (gtnf.TraderResponse.ErrorMessage != null && gtnf.TraderResponse.ErrorMessage != "")
                    {
                        Telerik.WinControls.RadMessageBox.Show(this, gtnf.TraderResponse.ErrorMessage, "Σφάλμα κλήσης ΓΓΠΣ...", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                        return;
                    }
                }
            }
        }
    }
}

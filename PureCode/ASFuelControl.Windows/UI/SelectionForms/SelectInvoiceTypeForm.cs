using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.SelectionForms
{
    public partial class SelectInvoiceTypeForm : RadForm
    {
        Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
        private Guid literCheck;

        public Guid SelectedInvoiceTypeId { set; get; }
        public Guid SelectedTrader { set; get; }
        public int TransactionType { set; get; }
        public int InternalType { set; get; }
        public bool HasFinancialTransactions { set; get; }

        public SelectInvoiceTypeForm()
        {
            InitializeComponent();
            this.InternalType = -1;
            this.literCheck = Data.Implementation.OptionHandler.Instance.GetGuidOption("LiterCheckInvoiceType", Guid.Empty);
            this.Load += new EventHandler(SelectInvoiceTypeForm_Load);
            
        }

        void SelectInvoiceTypeForm_Load(object sender, EventArgs e)
        {
            if (this.HasFinancialTransactions)
            {
                this.invoiceTypeBindingSource.DataSource = this.db.InvoiceTypes.Where(it => it.TransactionType == 0 && it.InvoiceTypeId != literCheck &&
                        (
                            (it.IsInternal.HasValue && it.IsInternal.Value && this.InternalType == 1) ||
                            ((!it.IsInternal.HasValue || !it.IsInternal.Value) && this.InternalType == 0) ||
                            (this.InternalType == -1)
                        ) && it.HasFinancialTransactions.HasValue && it.HasFinancialTransactions.Value && (!it.IsCancelation.HasValue || !it.IsCancelation.Value)
                    ).OrderBy(it => it.Description).ToList();

                Data.Trader trader = this.db.Traders.Where(t => t.TraderId == this.SelectedTrader).FirstOrDefault();
                if (trader != null)
                {
                    this.invoiceTypeBindingSource.Position = this.db.InvoiceTypes.Where(it => it.TransactionType == this.TransactionType &&
                        it.InvoiceTypeId != literCheck &&
                        (
                            (it.IsInternal.HasValue && it.IsInternal.Value && this.InternalType == 1) ||
                            ((!it.IsInternal.HasValue || !it.IsInternal.Value) && this.InternalType == 0) ||
                            (this.InternalType == -1)
                        ) && it.HasFinancialTransactions.HasValue && it.HasFinancialTransactions.Value && (!it.IsCancelation.HasValue || !it.IsCancelation.Value)
                    ).OrderBy(it => it.Description).ToList().IndexOf(trader.InvoiceType);
                }
            }
            else
            {

                this.invoiceTypeBindingSource.DataSource = this.db.InvoiceTypes.Where(it => it.TransactionType == 0 && it.InvoiceTypeId != literCheck &&
                        (
                            (it.IsInternal.HasValue && it.IsInternal.Value && this.InternalType == 1) ||
                            ((!it.IsInternal.HasValue || !it.IsInternal.Value) && this.InternalType == 0) ||
                            (this.InternalType == -1)
                        )
                    ).OrderBy(it => it.Description).ToList();

                Data.Trader trader = this.db.Traders.Where(t => t.TraderId == this.SelectedTrader).FirstOrDefault();
                if (trader != null)
                {
                    this.invoiceTypeBindingSource.Position = this.db.InvoiceTypes.Where(it => it.TransactionType == this.TransactionType &&
                        it.InvoiceTypeId != literCheck &&
                        (
                            (it.IsInternal.HasValue && it.IsInternal.Value && this.InternalType == 1) ||
                            ((!it.IsInternal.HasValue || !it.IsInternal.Value) && this.InternalType == 0) ||
                            (this.InternalType == -1)
                        )
                    ).OrderBy(it => it.Description).ToList().IndexOf(trader.InvoiceType);
                }
            }
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            if (this.invoiceTypeRadGridView.CurrentRow == null)
                return;
            
            Data.InvoiceType invType = this.invoiceTypeRadGridView.CurrentRow.DataBoundItem as Data.InvoiceType;
            if (invType == null)
                return;
            this.SelectedInvoiceTypeId = invType.InvoiceTypeId;
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            this.SelectedInvoiceTypeId = Guid.Empty;
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }
    }
}

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
    public partial class InvoiceFormEx : RadForm
    {
        ViewModels.InvoiceDetailsViewModel ivm = new ViewModels.InvoiceDetailsViewModel();

        private Guid[] relatedIds = null;

        public InvoiceFormEx()
        {
            InitializeComponent();

            //var dispColumn = this.radGridView1.Columns["Dispenser"] as GridViewComboBoxColumn;
            //dispColumn.DataSource = dispensersBindingSource;
            //dispColumn.ValueMember = "DispenserId";
            //dispColumn.DisplayMember = "Description";

            //var nozzleColumn = this.radGridView1.Columns["Nozzle"] as GridViewComboBoxColumn;
            //nozzleColumn.DataSource = this.nozzlesBindingSource;
            //nozzleColumn.ValueMember = "NozzleId";
            //nozzleColumn.DisplayMember = "Description";

            this.invoiceViewModelBindingSource.DataSource = ivm;
            this.FormClosing += InvoiceFormEx_FormClosing;
            ivm.QueryInvoiceData += Ivm_QueryInvoiceData;
            ivm.PropertyChanged += Ivm_PropertyChanged;
            ivm.RefreshView += Ivm_RefreshView;
        }

        public void CreateNew()
        {
            ivm.CreateNew();
            this.invoiceViewModelBindingSource.ResetBindings(false);
            this.invoiceLinesBindingSource.ResetBindings(false);
            SetView();
        }

        public void CreateNew(Guid invoiceTypeId)
        {
            ivm.CreateNew();
            ivm.InvoiceTypeId = invoiceTypeId;
            this.invoiceViewModelBindingSource.ResetBindings(false);
            this.invoiceLinesBindingSource.ResetBindings(false);
            SetView();
        }

        public void LoadInvoice(Guid invoiceId)
        {
            ivm.Load(invoiceId);
            
            this.invoiceViewModelBindingSource.ResetBindings(false);
            this.invoiceLinesBindingSource.ResetBindings(false);
            this.panel6.Visible = false;
            if (ivm.IsPrinted.HasValue && ivm.IsPrinted.Value)
            {
                //if (string.IsNullOrEmpty(ivm.InvoiceSignature))
                this.panel6.Visible = true;
            }
            var invType = ivm.InvoiceTypes.FirstOrDefault(i => i.InvoiceTypeId == ivm.InvoiceTypeId);
            if (invType.TransactionType == 1 && invType.IsLaserPrint.HasValue && invType.IsLaserPrint.Value && (!invType.Invalidated.HasValue || !invType.Invalidated.Value))
            {
                this.radGridView1.Columns["UnitPrice"].FieldName = "UnitPriceWhole";
                this.radSpinEditor3.DataBindings.Clear();
                this.radSpinEditor3.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.invoiceViewModelBindingSource, "DiscountAmountWhole", true));
            }
            else
            {
                this.radGridView1.Columns["UnitPrice"].FieldName = "UnitPriceRetail";
                this.radSpinEditor3.DataBindings.Clear();
                this.radSpinEditor3.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.invoiceViewModelBindingSource, "DiscountAmount", true));
            }
            this.SetView();
        }

        public void TransformInvoices(Guid[] invoiceIds, Guid invoiceType, Guid? traderId, Guid? vehicleId, int mode)
        {
            ivm.TransformInvoices(invoiceIds, invoiceType, traderId, vehicleId, mode);
            this.invoiceViewModelBindingSource.ResetBindings(false);
            this.invoiceLinesBindingSource.ResetBindings(false);
            SetView();
        }

        public Guid[] TransformInvoicesNoView(Guid[] invoiceIds, Guid invoiceType, Guid? traderId, Guid? vehicleId, int mode)
        {
            List<Guid> newIds = new List<Guid>();
            if (mode == 1)
            {
                foreach (Guid invId in invoiceIds)
                {
                    ViewModels.InvoiceDetailsViewModel ivmc = new ViewModels.InvoiceDetailsViewModel();
                    var invoiceId = ivmc.TransformInvoices(new Guid[] { invId }, invoiceType, traderId, vehicleId, mode);
                    if (invoiceId == Guid.Empty)
                        continue;
                    Threads.PrintAgent.ExcludeInvoices.Add(invoiceId);
                    ivmc.Save(ivmc.InvoiceId);
                    newIds.Add(ivmc.InvoiceId);
                }
            }
            this.ivm.RelatedInvoiceIds = newIds.ToArray();
            return newIds.ToArray();
        }

        public void CreateInvoices(Guid[] invoiceIds, Guid invoiceType, Guid? traderId, Guid? vehicleId, int mode, Guid[] newIds)
        {
            relatedIds = newIds;
            this.ivm.CreateInvoices(invoiceIds, invoiceType, traderId, vehicleId, mode);
        }

        private void DeleteRelatedInvoice()
        {
            if (this.relatedIds == null)
                return;
            using (Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                List<Data.InvoiceLine> invLinesToDelete = new List<Data.InvoiceLine>();
                List<Data.Invoice> invToDelete = new List<Data.Invoice>();
                List<Data.FinTransaction> finToDelete = new List<Data.FinTransaction>();
                int minNumber = int.MaxValue;
                foreach (var invId in this.relatedIds)
                {
                    var invoice = db.Invoices.FirstOrDefault(i => i.InvoiceId == invId);
                    if (minNumber > invoice.Number && invoice.Number > 0)
                    {
                        minNumber = invoice.Number;
                        invoice.InvoiceType.LastNumber = minNumber - 1;
                    }
                    if (invoice.FinTransactions.Count > 0)
                        db.Delete(invoice.FinTransactions);
                    if (invoice.InvoiceLines.Count > 0)
                    {
                        foreach(var invLine in invoice.InvoiceLines)
                        {
                            if (invLine.InvoiceLineRelations_ChildRelationId.Count > 0)
                                db.Delete(invLine.InvoiceLineRelations_ChildRelationId);
                            if (invLine.InvoiceLineRelations_ParentLineId.Count > 0)
                                db.Delete(invLine.InvoiceLineRelations_ParentLineId);
                        }
                        db.Delete(invoice.InvoiceLines);
                    }
                    if (invoice.ParentInvoiceRelations.Count > 0)
                        db.Delete(invoice.ParentInvoiceRelations);
                    if (invoice.ChildInvoiceRelations.Count > 0)
                        db.Delete(invoice.ChildInvoiceRelations);
                    db.Delete(invoice);

                }
                db.SaveChanges();
            }
        }

        private void InvoiceFormEx_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!this.ivm.HasChanges)
                return;
            var res = RadMessageBox.Show("Θέλετε να αποθηκεύσετε τις αλλαγές?", "Εχούν γίνει αλλαγές...", MessageBoxButtons.YesNoCancel, RadMessageIcon.Question);
            if (res == DialogResult.Cancel)
            {
                e.Cancel = true;
            }
            else if (res == DialogResult.Yes)
            {
                var success = this.ivm.Save(this.ivm.InvoiceId);
                if (!success)
                {
                    string message = string.Join("\r\n", this.ivm.Errors.Select(s => s.ErrorMessage));
                    RadMessageBox.Show("Σφάλμα Αποθήκευσης", message, MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                }
            }
            else if(res == DialogResult.No)
            {
                DeleteRelatedInvoice();
                if (this.ivm.RelatedInvoiceIds == null)
                    return;
                using (Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                {
                    foreach (var inv in this.ivm.RelatedInvoiceIds)
                    {
                        if (inv == Guid.Empty)
                            continue;
                        var invoice = db.Invoices.SingleOrDefault(i => i.InvoiceId == inv);
                        if (invoice == null)
                            continue;
                        var rels1 = invoice.InvoiceLines.SelectMany(il => il.InvoiceLineRelations_ChildRelationId);
                        var rels2 = invoice.InvoiceLines.SelectMany(il => il.InvoiceLineRelations_ParentLineId);
                        if(rels1 != null && rels1.Count() > 0)
                            db.Delete(rels1);
                        if (rels2 != null && rels2.Count() > 0)
                            db.Delete(rels2);
                        if (invoice.ChildInvoiceRelations.Count > 0)
                            db.Delete(invoice.ChildInvoiceRelations);
                        if (invoice.ParentInvoiceRelations.Count > 0)
                            db.Delete(invoice.ParentInvoiceRelations);
                        if (invoice.FinTransactions.Count > 0)
                            db.Delete(invoice.FinTransactions);
                        if (invoice.InvoiceLines.Count > 0)
                            db.Delete(invoice.InvoiceLines);
                        db.Delete(invoice);
                    }
                    db.SaveChanges();
                }
            }
        }

        private void btnSelectTrader_Click(object sender, EventArgs e)
        {
            SelectionForms.TraderSelectForm tf = new SelectionForms.TraderSelectForm();
            tf.ShowDialog(this);
            ViewModels.TraderViewModel tvm = tf.SelectedEntity as ViewModels.TraderViewModel;
            if (tvm != null)
                this.ivm.TraderId = tvm.TraderId;
        }

        private void btnSelectVehicle_Click(object sender, EventArgs e)
        {
            SelectionForms.VehicleSelectForm tf = new SelectionForms.VehicleSelectForm();
            tf.ShowDialog(this);
            ViewModels.VehicleViewModel tvm = tf.SelectedEntity as ViewModels.VehicleViewModel;
            if (tvm != null)
                this.ivm.VehicleId = tvm.VehicleId;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            
            var success = this.ivm.Save(ivm.InvoiceId);
            if (!success)
            {
                string message = string.Join("\r\n", this.ivm.Errors.Select(s => s.ErrorMessage));
                RadMessageBox.Show(message, "Σφάλμα Αποθήκευσης", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
            }
        }

        private void btnDeleteVehicle_Click(object sender, EventArgs e)
        {
            if (this.radGridView1.CurrentRow == null)
                return;
            var invLine = this.radGridView1.CurrentRow.DataBoundItem as ViewModels.InvoiceLineViewModel;
            if (invLine == null)
                return;
            if(!invLine.CanDelete)
            {
                RadMessageBox.Show("Η επιλεγμένη εγγραφή περιέχει συνδέσεις.\r\nΔεν μπορεί να γίνει διαγραφή!", "Ενημέρωση διαγραφής γραμμής Παραστατικού", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                return;
            }
            if (RadMessageBox.Show("Θέλετε να διαγράψετε την επιλεγμένη εγγραφή", "Διαγραφή Γραμμής Παραστατικού", MessageBoxButtons.YesNo, RadMessageIcon.Question) == DialogResult.No)
                return;
            this.ivm.DeleteInvoiceLine(invLine.InvoiceLineId);
        }

        private void btnAddVehicle_Click(object sender, EventArgs e)
        {
            this.ivm.AddInvoiceLine();
        }

        private void radGridView1_CellBeginEdit(object sender, GridViewCellCancelEventArgs e)
        {
            if (e.Row == null)
                return;
            var line = e.Row.DataBoundItem as ViewModels.InvoiceLineViewModel;
            if (line == null)
                return;
            if (e.Column.Name != "TankId")
            {
                if (!line.ParentInvoice.IsPrinted.HasValue || !line.ParentInvoice.IsPrinted.Value)
                {
                    if (e.Column.Name == "UnitPrice" || e.Column.Name == "DiscountAmount")
                    {
                        e.Cancel = false;
                        return;
                    }
                }
                e.Cancel = !line.CanEdit;
            }
            else
            {
                e.Cancel = false;
            }
        }

        private void btnPrint_Click(object sender, EventArgs e)
        {
            if (this.ivm.HasChanges)
            {
                bool success = this.ivm.Save(ivm.InvoiceId);
                if (!success)
                {
                    string message = string.Join("\r\n", this.ivm.Errors.Select(s => s.ErrorMessage));
                    RadMessageBox.Show(message, "Σφάλμα Αποθήκευσης", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                    return;
                }
            }
            using (Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                try
                {
                    Data.Invoice invoice = db.Invoices.SingleOrDefault(i => i.InvoiceId == this.ivm.InvoiceId);
                    if (invoice == null)
                        return;
                    
                    if (invoice.InvoiceLines.Count == 0)
                    {
                        Telerik.WinControls.RadMessageBox.Show("Το επιλεγμένο Παραστατικό δεν περιέχει γραμμές παραστατικού", "Σφάλμα εκτύπωσης", MessageBoxButtons.OK, Telerik.WinControls.RadMessageIcon.Error);
                        return;
                    }
                    DialogResult res = Telerik.WinControls.RadMessageBox.Show("Θέλετε να εκτύπώσετε το επιλεγμένο Παραστατικό;", "Επανεκτύπωση Παραστατικού", MessageBoxButtons.YesNo, Telerik.WinControls.RadMessageIcon.Question);
                    invoice.RecalculateInvoice();
                    db.SaveChanges();
                    if (res == System.Windows.Forms.DialogResult.Yes)
                    {
                        if (invoice.InvoiceSignature == null || invoice.InvoiceSignature == "")
                        {
                            Threads.PrintAgent pa = new Threads.PrintAgent();
                            pa.SignInvoice(db, invoice);
                        }
                        else
                            Threads.PrintAgent.PrintInvoiceDirect(invoice);
                    }
                }
                catch(Exception ex)
                {
                    Logging.Logger.Instance.LogToFile("Print", ex);

                }
            }
        }

        private void Ivm_QueryInvoiceData(object sender, ViewModels.QueryInvoiceDataArgs e)
        {
            using (Forms.InvoiceTransformationForm itff = new Forms.InvoiceTransformationForm())
            {
                itff.LoadData(e);
                var result = itff.ShowDialog(this);
                if (result != DialogResult.OK)
                    e.Cancel = true;

            }
        }

        private void Ivm_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == "InvoiceTypeId" || e.PropertyName == "IsPrinted" || e.PropertyName == "InvoiceSignature")
            {
                var invType = ivm.InvoiceTypes.FirstOrDefault(i => i.InvoiceTypeId == ivm.InvoiceTypeId);
                if (invType == null)
                    return;
                if (invType.TransactionType == 1 && invType.IsLaserPrint.HasValue && invType.IsLaserPrint.Value && (!invType.Invalidated.HasValue || !invType.Invalidated.Value))
                {
                    this.radGridView1.Columns["UnitPrice"].FieldName = "UnitPriceWhole";
                    this.radSpinEditor3.DataBindings.Clear();
                    this.radSpinEditor3.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.invoiceViewModelBindingSource, "DiscountAmountWhole", true));
                }
                else
                {
                    this.radGridView1.Columns["UnitPrice"].FieldName = "UnitPriceRetail";
                    this.radSpinEditor3.DataBindings.Clear();
                    this.radSpinEditor3.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.invoiceViewModelBindingSource, "DiscountAmount", true));
                }
                SetView();
            }
        }

        private void invoiceLinesBindingSource_CurrentItemChanged(object sender, EventArgs e)
        {
            if (this.invoiceLinesBindingSource.Current == null)
                this.ivm.CurrentLine = null;
            else
                this.ivm.CurrentLine = this.invoiceLinesBindingSource.Current as ViewModels.InvoiceLineViewModel;
        }

        private void Ivm_RefreshView(object sender, EventArgs e)
        {
            this.invoiceViewModelBindingSource.ResetBindings(false);
            this.invoiceLinesBindingSource.ResetBindings(false);
            this.invoiceViewModelBindingSource.ResetBindings(false);
            this.invoiceLinesBindingSource.ResetBindings(false);
        }

        private void SetView()
        {
            this.btnSave.Visible = !this.ivm.SimplePrint;
            this.panel6.Visible = false;
            if (ivm.IsPrinted.HasValue && ivm.IsPrinted.Value)
            {
                //if (string.IsNullOrEmpty(ivm.InvoiceSignature))
                //{
                this.panel6.Visible = true;
                this.btnSave.Visible = true;
                //}
            }
            if (this.ivm.SimplePrint || this.ivm.DispenserType)
            {
                this.tableLayoutPanel12.ColumnStyles[0].Width = 0;
                this.tableLayoutPanel12.ColumnStyles[1].Width = 100;
                this.tableLayoutPanel12.RowStyles[0].SizeType = SizeType.Absolute;
                this.tableLayoutPanel12.RowStyles[0].Height = 150;
                this.tableLayoutPanel12.RowStyles[1].SizeType = SizeType.Percent;
                this.tableLayoutPanel12.RowStyles[1].Height = 100;
                this.tableLayoutPanel12.RowStyles[2].Height = 30;
            }
            else
            {
                this.tableLayoutPanel12.ColumnStyles[0].Width = 100;
                this.tableLayoutPanel12.ColumnStyles[1].Width = 0;
                this.tableLayoutPanel12.RowStyles[0].SizeType = SizeType.Percent;
                this.tableLayoutPanel12.RowStyles[0].Height = 100;
                this.tableLayoutPanel12.RowStyles[1].SizeType = SizeType.Absolute;
                this.tableLayoutPanel12.RowStyles[1].Height = 120;
                this.tableLayoutPanel12.RowStyles[2].Height = 30;
            }
        }
    }
}

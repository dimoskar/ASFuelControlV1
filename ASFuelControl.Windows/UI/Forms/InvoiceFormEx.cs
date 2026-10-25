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
        protected readonly ViewModels.InvoiceDetailsViewModel ivm;
        private readonly ViewModels.InvoiceEditorLayout editorLayout = ViewModels.InvoiceEditorLayout.Legacy;

        private Guid[] relatedIds;

        public InvoiceFormEx() : this(new ViewModels.InvoiceDetailsViewModel())
        {
        }

        protected InvoiceFormEx(ViewModels.InvoiceDetailsViewModel viewModel)
            : this(viewModel, ViewModels.InvoiceEditorLayout.Legacy)
        {
        }

        protected InvoiceFormEx(ViewModels.InvoiceDetailsViewModel viewModel,
            ViewModels.InvoiceEditorLayout layout)
        {
            if (viewModel == null)
                throw new ArgumentNullException("viewModel");
            ivm = viewModel;
            editorLayout = layout;
            if (layout == ViewModels.InvoiceEditorLayout.Legacy)
                InitializeComponent();
            else
                InitializeFixedLayout(layout);
            InitializeFiscalStatus();
            fiscalPanel.ActionCompleted += (s, e) => {
                if (!IsDisposed && fiscalPanel.Owned) LoadInvoice(ivm.InvoiceId);
            };

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
            ApplyPriceBindings();
            this.SetView();
            fiscalPanel.LoadInvoice(invoiceId);
            myDieselPanel.LoadInvoice(invoiceId);
        }

        public Guid TransformInvoices(Guid[] invoiceIds, Guid invoiceType, Guid? traderId, Guid? vehicleId,
            int mode, bool createFiscalRelations = true)
        {
            var invoiceId = ivm.TransformInvoices(invoiceIds, invoiceType, traderId, vehicleId, mode, createFiscalRelations);
            if (invoiceId == Guid.Empty)
                DeleteRelatedInvoices();
            this.invoiceViewModelBindingSource.ResetBindings(false);
            this.invoiceLinesBindingSource.ResetBindings(false);
            SetView();
            return invoiceId;
        }

        public Guid[] CreateIntermediateTransformation(Guid[] invoiceIds, Guid invoiceType,
            Guid? traderId, Guid? vehicleId, int mode, string notesAddition)
        {
            var newIds = new List<Guid>();
            try
            {
                foreach (var sourceId in invoiceIds)
                {
                    var related = new ViewModels.InvoiceDetailsViewModel();
                    var relatedId = related.TransformInvoices(new[] { sourceId }, invoiceType,
                        traderId, vehicleId, mode);
                    if (relatedId == Guid.Empty)
                        throw new InvalidOperationException("Δεν ήταν δυνατή η δημιουργία του πιστωτικού παραστατικού.");
                    if (!string.IsNullOrWhiteSpace(notesAddition))
                        related.Notes = string.IsNullOrWhiteSpace(related.Notes)
                            ? notesAddition
                            : notesAddition + "\r\n" + related.Notes;
                    lock (Threads.PrintAgent.ExcludeInvoices)
                        Threads.PrintAgent.ExcludeInvoices.Add(relatedId);
                    newIds.Add(relatedId);
                    if (!related.Save(relatedId))
                        throw new InvalidOperationException(string.Join("\r\n",
                            (related.Errors ?? new ViewModels.ValidationError[0]).Select(error => error.ErrorMessage)));
                }
                this.relatedIds = newIds.ToArray();
                return this.relatedIds;
            }
            catch
            {
                this.relatedIds = newIds.ToArray();
                DeleteRelatedInvoices();
                throw;
            }
        }

        private bool SaveInvoice()
        {
            var success = this.ivm.Save(this.ivm.InvoiceId);
            if (success)
            {
                lock (Threads.PrintAgent.ExcludeInvoices)
                {
                    foreach (var id in this.relatedIds ?? new Guid[0])
                        Threads.PrintAgent.ExcludeInvoices.Remove(id);
                }
                this.relatedIds = null;
            }
            return success;
        }

        private void DeleteRelatedInvoices()
        {
            if (this.relatedIds == null || this.relatedIds.Length == 0)
                return;
            using (Threads.SharedInvoiceRuntime.EditUnissued(this.relatedIds))
            using (Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                foreach (var invoiceId in this.relatedIds)
                {
                    var invoice = db.Invoices.SingleOrDefault(item => item.InvoiceId == invoiceId);
                    if (invoice == null)
                        continue;
                    foreach (var line in invoice.InvoiceLines.ToArray())
                    {
                        if (line.InvoiceLineRelations_ChildRelationId.Count > 0)
                            db.Delete(line.InvoiceLineRelations_ChildRelationId);
                        if (line.InvoiceLineRelations_ParentLineId.Count > 0)
                            db.Delete(line.InvoiceLineRelations_ParentLineId);
                    }
                    if (invoice.ParentInvoiceRelations.Count > 0)
                        db.Delete(invoice.ParentInvoiceRelations);
                    if (invoice.ChildInvoiceRelations.Count > 0)
                        db.Delete(invoice.ChildInvoiceRelations);
                    if (invoice.FinTransactions.Count > 0)
                        db.Delete(invoice.FinTransactions);
                    if (invoice.InvoiceLines.Count > 0)
                        db.Delete(invoice.InvoiceLines);
                    db.Delete(invoice);
                }
                db.SaveChanges();
            }
            lock (Threads.PrintAgent.ExcludeInvoices)
            {
                foreach (var id in this.relatedIds)
                    Threads.PrintAgent.ExcludeInvoices.Remove(id);
            }
            this.relatedIds = null;
        }

        private void InvoiceFormEx_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (fiscalPanel.Busy || myDieselPanel.Busy)
            {
                e.Cancel = true;
                RadMessageBox.Show(this,"Υπάρχει ενέργεια έκδοσης σε εξέλιξη. Περιμένετε να ολοκληρωθεί πριν κλείσετε το παραστατικό.",
                    "Κλείσιμο παραστατικού",MessageBoxButtons.OK,RadMessageIcon.Info);
                return;
            }
            if (myDieselPanel.HasBlockingCase && !fiscalPanel.Owned &&
                RadMessageBox.Show(this,"Η υπόθεση myDiesel παραμένει ενεργή ή χρειάζεται ανάκτηση. Επιλέξτε «Όχι» για να επιστρέψετε στις διαθέσιμες ενέργειες ή «Ναι» για να την αφήσετε καταγεγραμμένη και να κλείσετε.",
                    "Ενεργή υπόθεση myDiesel",MessageBoxButtons.YesNo,RadMessageIcon.Exclamation) != DialogResult.Yes)
            { e.Cancel = true; return; }
            try { CloseInvoice(e); }
            catch (Exception ex)
            {
                e.Cancel = true;
                RadMessageBox.Show(ex.Message,"Έλεγχος παραστατικού",MessageBoxButtons.OK,RadMessageIcon.Exclamation);
            }
        }

        private void CloseInvoice(FormClosingEventArgs e)
        {
            if (Threads.SharedInvoiceRuntime.Owns(this.ivm.InvoiceId)) return;
            if (!this.ivm.HasChanges)
            {
                DeleteRelatedInvoices();
                return;
            }
            var res = RadMessageBox.Show(this,"Θέλετε να αποθηκεύσετε τις αλλαγές?", "Εχούν γίνει αλλαγές...", MessageBoxButtons.YesNoCancel, RadMessageIcon.Question);
            if (res == DialogResult.Cancel)
            {
                e.Cancel = true;
            }
            else if (res == DialogResult.Yes)
            {
                var success = this.SaveInvoice();
                if (!success)
                {
                    string message = string.Join("\r\n", this.ivm.Errors.Select(s => s.ErrorMessage));
                    RadMessageBox.Show(message, "Σφάλμα Αποθήκευσης", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                    e.Cancel = true;
                }
            }
            else if(res == DialogResult.No)
            {
                DeleteRelatedInvoices();
            }
        }

        private void btnSelectTrader_Click(object sender, EventArgs e)
        {
            SelectionForms.TraderSelectForm tf = new SelectionForms.TraderSelectForm();
            tf.SelectionMode = this.ivm.CounterpartySelection;
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
            if (Threads.SharedInvoiceRuntime.Owns(this.ivm.InvoiceId)) return;
            
            var success = this.SaveInvoice();
            if (!success)
            {
                string message = string.Join("\r\n", this.ivm.Errors.Select(s => s.ErrorMessage));
                RadMessageBox.Show(message, "Σφάλμα Αποθήκευσης", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
            }
            else fiscalPanel.LoadInvoice(this.ivm.InvoiceId);
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
            e.Cancel = !line.CanEdit;
        }

        private async void btnPrint_Click(object sender, EventArgs e)
        {
            if (fiscalPanel.Busy || myDieselPanel.Busy) return;
            // Owned documents use their saved workflow; never save/recalculate edited
            // screen data before a reprint. The status panel locks issued fields.
            if (Threads.SharedInvoiceRuntime.Handles(this.ivm.InvoiceId))
            {
                try
                {
                    if (!Threads.SharedInvoiceRuntime.Owns(this.ivm.InvoiceId))
                    {
                        if (this.ivm.HasChanges && !this.SaveInvoice())
                            throw new InvalidOperationException(string.Join("\r\n", this.ivm.Errors.Select(s => s.ErrorMessage)));
                        if (RadMessageBox.Show("Θέλετε να εκτυπώσετε το αποθηκευμένο παραστατικό;", "Εκτύπωση Παραστατικού",
                            MessageBoxButtons.YesNo, RadMessageIcon.Question) != DialogResult.Yes) return;
                    }
                    fiscalPanel.LoadInvoice(this.ivm.InvoiceId);
                    await fiscalPanel.PrintInvoice();
                }
                catch (Exception ex)
                {
                    Logging.Logger.Instance.LogToFile("Shared invoice print", ex);
                    RadMessageBox.Show(ex.Message, "Σφάλμα Εκτύπωσης", MessageBoxButtons.OK, RadMessageIcon.Error);
                }
                return;
            }
            if (this.ivm.HasChanges)
            {
                bool success = this.SaveInvoice();
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
                    if (res != System.Windows.Forms.DialogResult.Yes)
                        return;
                    invoice.RecalculateInvoice();
                    db.SaveChanges();
                    if (invoice.InvoiceSignature == null || invoice.InvoiceSignature == "")
                    {
                        Threads.PrintAgent pa = new Threads.PrintAgent();
                        pa.SignInvoice(db, invoice);
                    }
                    else
                        Threads.PrintAgent.PrintInvoiceDirect(invoice);
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
                ApplyPriceBindings();
                SetView();
            }
        }

        private void ApplyPriceBindings()
        {
            bool wholesalePrices = !this.ivm.PricesIncludeVat;
            if (this.radGridView1 != null && this.radGridView1.Columns["UnitPrice"] != null)
                this.radGridView1.Columns["UnitPrice"].FieldName = wholesalePrices
                    ? "UnitPriceWhole"
                    : "UnitPriceRetail";
            if (this.radSpinEditor3 != null)
            {
                this.radSpinEditor3.DataBindings.Clear();
                this.radSpinEditor3.DataBindings.Add(new Binding("Value", this.invoiceViewModelBindingSource,
                    wholesalePrices ? "DiscountAmountWhole" : "DiscountAmount", true));
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
            if (editorLayout != ViewModels.InvoiceEditorLayout.Legacy)
            {
                this.btnSave.Visible = true;
                this.panel6.Visible = ivm.IsPrinted == true;
                return;
            }
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

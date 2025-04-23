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
    public partial class TraderForm : RadForm
    {
        private ViewModels.TraderDetailsViewModel traderViewModel = new ViewModels.TraderDetailsViewModel();
        private ASFuelControl.RousisRFID.Controller rfid = new ASFuelControl.RousisRFID.Controller();
        System.Threading.Thread th = null;
        private bool stopThread = false;

        public Guid SelectedCustomerId
        {
            get
            {
                if (this.traderViewModel == null)
                    return Guid.Empty;
                return this.traderViewModel.TraderId;
            }
        }

        public TraderForm()
        {
            InitializeComponent();
            this.FormClosing += TraderForm_FormClosing;
            this.traderViewModel.PropertyChanged += TraderViewModel_PropertyChanged;
            Telerik.WinControls.UI.GridViewSummaryItem gridViewSummaryItem1 = new Telerik.WinControls.UI.GridViewSummaryItem();
            Telerik.WinControls.UI.GridViewSummaryItem gridViewSummaryItem2 = new Telerik.WinControls.UI.GridViewSummaryItem();
            Telerik.WinControls.UI.GridViewSummaryItem gridViewSummaryItem3 = new Telerik.WinControls.UI.GridViewSummaryItem();
            Telerik.WinControls.UI.GridViewSummaryItem gridViewSummaryItem11 = new Telerik.WinControls.UI.GridViewSummaryItem();
            Telerik.WinControls.UI.GridViewSummaryItem gridViewSummaryItem21 = new Telerik.WinControls.UI.GridViewSummaryItem();
            Telerik.WinControls.UI.GridViewSummaryItem gridViewSummaryItem31 = new Telerik.WinControls.UI.GridViewSummaryItem();

            gridViewSummaryItem1.Aggregate = Telerik.WinControls.UI.GridAggregateFunction.Sum;
            gridViewSummaryItem1.FormatString = "{0:N2}";
            gridViewSummaryItem1.Name = "CreditAmount";
            gridViewSummaryItem2.Aggregate = Telerik.WinControls.UI.GridAggregateFunction.Sum;
            gridViewSummaryItem2.FormatString = "{0:N2}";
            gridViewSummaryItem2.Name = "DebitAmount";
            gridViewSummaryItem3.Aggregate = Telerik.WinControls.UI.GridAggregateFunction.Count;
            gridViewSummaryItem3.FormatString = "{0:N2}";
            gridViewSummaryItem3.Name = "InvoiceDescription";
            
            Telerik.WinControls.UI.GridViewSummaryRowItem summaryRow = new Telerik.WinControls.UI.GridViewSummaryRowItem(new Telerik.WinControls.UI.GridViewSummaryItem[] {
                gridViewSummaryItem1,
                gridViewSummaryItem2,
                gridViewSummaryItem3});

            this.radGridView3.MasterTemplate.SummaryRowsBottom.Add(summaryRow);
            radGridView3.MasterView.SummaryRows[0].PinPosition = Telerik.WinControls.UI.PinnedRowPosition.Bottom;

            gridViewSummaryItem11.Aggregate = Telerik.WinControls.UI.GridAggregateFunction.Sum;
            gridViewSummaryItem11.FormatString = "{0:N2}";
            gridViewSummaryItem11.Name = "CreditAmount";
            gridViewSummaryItem21.Aggregate = Telerik.WinControls.UI.GridAggregateFunction.Sum;
            gridViewSummaryItem21.FormatString = "{0:N2}";
            gridViewSummaryItem21.Name = "DebitAmount";
            gridViewSummaryItem31.Aggregate = Telerik.WinControls.UI.GridAggregateFunction.Count;
            gridViewSummaryItem31.FormatString = "{0:N2}";
            gridViewSummaryItem31.Name = "InvoiceDescription";

            Telerik.WinControls.UI.GridViewSummaryRowItem summaryRow1 = new Telerik.WinControls.UI.GridViewSummaryRowItem(new Telerik.WinControls.UI.GridViewSummaryItem[] {
                gridViewSummaryItem11,
                gridViewSummaryItem21,
                gridViewSummaryItem31});

            this.radGridView3.MasterTemplate.SummaryRowsBottom.Add(summaryRow1);
            radGridView3.MasterView.SummaryRows[0].PinPosition = Telerik.WinControls.UI.PinnedRowPosition.Bottom;
            radGridView3.MasterView.SummaryRows[1].PinPosition = Telerik.WinControls.UI.PinnedRowPosition.Bottom;

            this.radGridView3.ViewCellFormatting += RadGridView3_ViewCellFormatting;

            InitilizeEnrollment();

            this.radPageView1.SelectedPage = this.radPageViewPage1;
            this.countryComboBox.DataSource = Data.Implementation.Country.GetCountries();
            this.countryComboBox.DisplayMember = "DisplayValue";
            this.countryComboBox.ValueMember = "CodeValue";
        }

        public void CreateCustomer()
        {
            this.traderViewModelBindingSource.DataSource = this.traderViewModel;
            this.traderViewModel.CreateNew();
            this.traderViewModel.Name = "(Νέος Πελάτης)";
            this.traderViewModel.Country = "GR";
            this.traderViewModel.IsCustomer = true;
            this.traderViewModelBindingSource.ResetBindings(false);
            this.vehiclesBindingSource.ResetBindings(false);
        }

        public void CreateSuplier()
        {
            this.traderViewModelBindingSource.DataSource = this.traderViewModel;
            this.traderViewModel.CreateNew();
            this.traderViewModel.Name = "(Νέος Προμηθευτής)";
            this.traderViewModel.Country = "GR";
            this.traderViewModel.IsSupplier = true;
            this.traderViewModelBindingSource.ResetBindings(false);
            this.vehiclesBindingSource.ResetBindings(false);
        }

        public void LoadNew(ViewModels.TraderDetailsViewModel tv)
        {
            this.traderViewModel = tv;
            this.traderViewModelBindingSource.DataSource = this.traderViewModel;
            this.traderViewModel.CreateNew();
            this.traderViewModel.Country = "GR";
            this.traderViewModelBindingSource.ResetBindings(false);
            this.vehiclesBindingSource.ResetBindings(false);
        }

        public void LoadTrader(Guid traderId)
        {
            this.traderViewModelBindingSource.DataSource = this.traderViewModel;
            traderViewModel.Load(traderId);
            this.traderViewModelBindingSource.ResetBindings(false);
            this.vehiclesBindingSource.ResetBindings(false);

            this.UpdateSums();
        }

        private void InitilizeEnrollment()
        {
            try
            {
                using (Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                {
                    var fmController = db.FleetManagmentCotrollers.ToArray().FirstOrDefault(c => c.EnrollmentDevice);
                    if (fmController == null)
                        return;
                    if (!fmController.EnrollmentDevice)
                        return;
                    if ((fmController.ComPort != null && fmController.ComPort.StartsWith("COM")) ||
                        (fmController.ControlerType.HasValue && fmController.ControlerType.Value == 0))
                    {
                        rfid.ComPort = fmController.ComPort;
                        rfid.Address = fmController.DeviceIndex.HasValue ? fmController.DeviceIndex.Value : 1;
                        rfid.EnrollmentDevice = fmController.EnrollmentDevice;
                        th = new System.Threading.Thread(new System.Threading.ThreadStart(this.ControllerThreadRun));
                        th.Start();
                    }
                }
            }
            catch
            {

            }
        }

        private void UpdateSums()
        {
            decimal sumC = this.traderViewModel.FinTransactions.Sum(f => f.CreditAmount);
            decimal sumD = this.traderViewModel.FinTransactions.Sum(f => f.DebitAmount);
            decimal sum = this.traderViewModel.FinTransactions.Sum(f => f.CreditAmount - f.DebitAmount);
            this.radGridView3.SummaryRowsBottom[0][0].FormatString = (sumC).ToString("N2");
            this.radGridView3.SummaryRowsBottom[0][1].FormatString = (sumD).ToString("N2");
            this.radGridView3.SummaryRowsBottom[0][2].FormatString = (sum).ToString("N2");

            decimal sumTotal = this.traderViewModel.PreviousCreditSum - this.traderViewModel.PreviousDebitSum;
            this.radGridView3.SummaryRowsBottom[1][0].FormatString = (sumC + this.traderViewModel.PreviousCreditSum).ToString("N2");
            this.radGridView3.SummaryRowsBottom[1][1].FormatString = (sumD + this.traderViewModel.PreviousDebitSum).ToString("N2");
            this.radGridView3.SummaryRowsBottom[1][2].FormatString = (sum + sumTotal).ToString("N2");
        }

        private void TraderViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "FinTransactions")
            {
                this.UpdateSums();
            }
        }

        private void RadGridView3_ViewCellFormatting(object sender, CellFormattingEventArgs e)
        {
            if (e.CellElement is Telerik.WinControls.UI.GridSummaryCellElement)
            {
                e.CellElement.TextAlignment = ContentAlignment.MiddleRight;
                e.CellElement.Font = new System.Drawing.Font(this.radGridView1.Font, FontStyle.Bold);
            }
        }

        private void btnAddVehicle_Click(object sender, EventArgs e)
        {
            this.traderViewModel.AddVehicle();
        }

        private void btnDeleteVehicle_Click(object sender, EventArgs e)
        {
            if (this.radGridView1.CurrentRow == null)
                return;
            var vehicle = this.radGridView1.CurrentRow.DataBoundItem as ViewModels.VehicleViewModel;
            if (vehicle == null)
                return;

            if (RadMessageBox.Show("Θέλετε να διαγράψετε την επιλεγμένη εγγραφή", "Διαγραφή Οχήματος", MessageBoxButtons.YesNo, RadMessageIcon.Question) == DialogResult.No)
                return;

            this.traderViewModel.DeleteVehicle(vehicle.VehicleId);
        }

        private void TraderForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.traderViewModel.HasChanges)
            {
                var res = RadMessageBox.Show("Θέλετε να αποθηκεύσετε τις αλλαγές?", "Εχούν γίνει αλλαγές...", MessageBoxButtons.YesNoCancel, RadMessageIcon.Question);
                if (res == DialogResult.Cancel)
                    e.Cancel = true;
                else if (res == DialogResult.Yes)
                {

                    var success = this.traderViewModel.Save(this.traderViewModel.TraderId);
                    if (!success)
                    {
                        string message = string.Join("\r\n", this.traderViewModel.Errors.Select(s => s.ErrorMessage));
                        RadMessageBox.Show(message, "Σφάλμα Αποθήκευσης", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                        e.Cancel = true;
                    }
                }
            }
            if(!e.Cancel)
            {
                stopThread = true;
                System.Threading.Thread.Sleep(200);
                this.rfid.DisConnect();
            }
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            this.traderViewModel.LoadInvoices();
            this.traderViewModelBindingSource.ResetBindings(false);
            this.invoicesBindingSource.ResetBindings(false);
        }

        private void radGridView2_CellDoubleClick(object sender, GridViewCellEventArgs e)
        {
            if (this.radGridView2.CurrentRow == null)
                return;
            var dataRow = this.radGridView2.CurrentRow.DataBoundItem as ViewModels.TraderInvoiceViewModel;
            if (dataRow == null)
                return;
            InvoiceFormEx invForm = new InvoiceFormEx();
            invForm.LoadInvoice(dataRow.InvoiceId);
            invForm.Show(this);
        }

        private void btnAddInvoice_Click(object sender, EventArgs e)
        {
            InvoiceFormEx invoiceForm = new InvoiceFormEx();
            invoiceForm.CreateNew();
            invoiceForm.Show(this);
        }

        private void btnDeleteInvoice_Click(object sender, EventArgs e)
        {
            if (this.radGridView2.CurrentRow == null)
                return;
            var invoice = this.radGridView2.CurrentRow.DataBoundItem as ViewModels.InvoiceViewModel;
            if (invoice == null)
                return;
            if (!invoice.CanDelete)
            {
                RadMessageBox.Show("Υπάρχουν συνδέσεις για το επιλεγμένο Παραστατικό!\r\nΤο παραστατικό που επιλέξατε δεν μπορεί να διαγραφεί!", "Ενημέρωση Διαγραφής...", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                return;
            }
            if (RadMessageBox.Show("Θέλετε να διαγράψετε την επιλεγμένη εγγραφή", "Διαγραφή Παραστατικού...", MessageBoxButtons.YesNo, RadMessageIcon.Question) == DialogResult.No)
                return;
            ViewModels.InvoiceDetailsViewModel delModel = new ViewModels.InvoiceDetailsViewModel();
            delModel.Load(invoice.InvoiceId);
            delModel.EntityState = ViewModels.EntityStateEnum.Deleted;
            delModel.Save(invoice.InvoiceId);
            this.traderViewModel.LoadInvoices();
            this.traderViewModelBindingSource.ResetBindings(false);
            this.invoicesBindingSource.ResetBindings(false);
        }

        private void btnAddFinTrans_Click(object sender, EventArgs e)
        {
            this.traderViewModel.AddFinTransaction();
            //this.traderViewModelBindingSource.ResetBindings(false);
            //this.finTransactionViewModelBindingSource.ResetBindings(false);
        }

        private void btnDeleteFinTrans_Click(object sender, EventArgs e)
        {
            if (this.radGridView3.CurrentRow == null)
                return;
            var fintrans = this.radGridView3.CurrentRow.DataBoundItem as ViewModels.FinTransactionViewModel;
            if (fintrans == null)
                return;
            if (RadMessageBox.Show("Θέλετε να διαγράψετε την επιλεγμένη εγγραφή", "Διαγραφή Ταμειακής Κίνησης...", MessageBoxButtons.YesNo, RadMessageIcon.Question) == DialogResult.No)
                return;
            ViewModels.FinTransactionViewModel delModel = new ViewModels.FinTransactionViewModel();
            delModel.Load(fintrans.FinTransactionId);
            delModel.EntityState = ViewModels.EntityStateEnum.Deleted;
            delModel.Save(fintrans.FinTransactionId);
            this.traderViewModel.LoadFinTransactions();
            this.traderViewModelBindingSource.ResetBindings(false);
            this.finTransactionViewModelBindingSource.ResetBindings(false);
        }

        private void radButton4_Click(object sender, EventArgs e)
        {
            this.traderViewModel.LoadFinTransactions();
            this.traderViewModelBindingSource.ResetBindings(false);
            this.finTransactionViewModelBindingSource.ResetBindings(false);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            var success = this.traderViewModel.Save(this.traderViewModel.TraderId);
            if (!success)
            {
                string message = string.Join("\r\n", this.traderViewModel.Errors.Select(s => s.ErrorMessage));
                RadMessageBox.Show(message, "Σφάλμα Αποθήκευσης", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
            }
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            RadPrintDocument prdoc = new RadPrintDocument();
            prdoc.DefaultPageSettings.Landscape = false;
            prdoc.DefaultPageSettings.Margins = new System.Drawing.Printing.Margins(50, 50, 50, 50);

            prdoc.HeaderHeight = 30;
            prdoc.HeaderFont = new Font("Arial", 18);
            prdoc.LeftHeader = "Οικονομική Καρτέλα: " + this.traderViewModel.Name;
            prdoc.MiddleHeader = "";
            prdoc.RightHeader = "";
            prdoc.ReverseHeaderOnEvenPages = true;
            prdoc.FooterHeight = 20;
            prdoc.FooterFont = new Font("Arial", 10);
            prdoc.LeftFooter = "Ημερομηνία [Date Printed] [Time Printed]";
            //prdoc.MiddleFooter = "Middle footer";
            prdoc.RightFooter = "Σελίδα [Page #] από [Total Pages]";
            prdoc.ReverseFooterOnEvenPages = true;
            prdoc.AssociatedObject = this.radGridView1;

            this.radGridView3.PrintStyle.FitWidthMode = PrintFitWidthMode.FitPageWidth;
            this.radGridView3.PrintStyle.AlternatingRowColor = Color.FromArgb(155, 240, 240, 240);
            this.radGridView3.PrintStyle.PrintAlternatingRowColor = true;
            this.radGridView3.PrintStyle.PrintHeaderOnEachPage = true;
            this.radGridView3.PrintStyle.PrintSummaries = true;
            this.radGridView3.PrintStyle.SummaryCellBackColor = Color.FromArgb(255, 200, 200, 200);

            //TableViewDefinitionPrintRenderer renderer = new TableViewDefinitionPrintRenderer(this.radGridView1);
            //renderer.PrintPages.Add(this.radGridView3.Columns.ToArray());
            //renderer.PrintPages.Add(this.radGridView3.Columns.ToArray());
            //this.radGridView3.PrintStyle.PrintRenderer = renderer;

            this.radGridView3.PrintPreview(prdoc);
        }

        private void radGridView1_CommandCellClick(object sender, EventArgs e)
        {
            if (this.radGridView1.CurrentRow == null)
                return;
            ViewModels.TraderVehicleViewModel vm = this.radGridView1.CurrentRow.DataBoundItem as ViewModels.TraderVehicleViewModel;
            if (vm == null)
                return;
            if (vm.CardId == null || vm.CardId == "")
            {
                if (rfid.LastScannedId == null || rfid.LastScannedId == "")
                    return;

                using (Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                {
                    var count = this.traderViewModel.Vehicles.Where(v => v.CardId == rfid.LastScannedId).Count();
                    if (count > 0)
                    {
                        MessageBox.Show("Η κάρτα αυτή είναι ήδη ανατεθημένη σε άλλο όχημα του πελάτη!", "Σφάλμα Ανάθεσης Κάρτας", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    count = db.Vehicles.Where(v => v.VehicleId != vm.VehicleId && v.CardId == rfid.LastScannedId).Count();
                    if (count > 0)
                    {
                        MessageBox.Show("Η κάρτα αυτή είναι ήδη ανατεθημένη σε όχημα!", "Σφάλμα Ανάθεσης Κάρτας", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                vm.CardId = rfid.LastScannedId;
                rfid.LastScannedId = "";
            }
            else
            {
                vm.CardId = "";
            }
        }

        private void ControllerThreadRun()
        {
            if (!rfid.IsConnected)
                rfid.Connect();
            if (!rfid.IsConnected)
                return;
            DateTime start = DateTime.Now;
            string lastDisplay = "WELCOME";
            string lastDisplay2 = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            rfid.SetDiplayLine1(lastDisplay);
            rfid.SetDiplayLine2(lastDisplay2);
            while (!stopThread && rfid.IsConnected)
            {
                try
                {
                    string id = rfid.GetLastId();
                    if (id == "")
                    {
                        if (rfid.LastScannedId == "")
                        {
                            rfid.SetDiplayLine1("WELCOME");
                            rfid.LastScannedId = null;
                        }
                        int diff = (int)DateTime.Now.Subtract(start).TotalSeconds;
                        if (diff > 10 && lastDisplay != "WELCOME")
                        {
                            rfid.SetDiplayLine1("WELCOME");
                            lastDisplay = "WELCOME";
                        }
                        string dtime = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                        if (lastDisplay2 != dtime)
                        {
                            rfid.SetDiplayLine2(dtime);
                            lastDisplay2 = dtime;
                        }
                        continue;
                    }
                    rfid.SetDiplayLine1(id);
                    lastDisplay = id;
                    this.rfid.LastScannedId = id;
                    this.radGridView1.Invoke((MethodInvoker)(() =>
                    {
                        if (this.radGridView1.CurrentRow == null)
                            return;
                        ViewModels.TraderVehicleViewModel vm = this.radGridView1.CurrentRow.DataBoundItem as ViewModels.TraderVehicleViewModel;
                        if (vm == null)
                            return;
                        if (vm.CardId == null || vm.CardId == "")
                        {
                            var count = this.traderViewModel.Vehicles.Where(v => v.CardId == id).Count();
                            if(count > 0)
                            {
                                MessageBox.Show("Η κάρτα αυτή είναι ήδη ανατεθημένη σε άλλο όχημα του πελάτη!", "Σφάλμα Ανάθεσης Κάρτας", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                            vm.ClearUIErrors();
                            vm.CardId = id;
                            if(vm.Errors.Count() > 0)
                            {
                                MessageBox.Show(vm.Errors[0].ErrorMessage, vm.Errors[0].Caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            this.rfid.LastScannedId = "";
                        }
                    }));

                    start = DateTime.Now;
                    System.Threading.Thread.Sleep(100);
                }
                catch
                {

                }
            }
        }
    }
}

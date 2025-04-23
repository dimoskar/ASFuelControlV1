using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls.UI;
using Telerik.WinControls;
using System.Drawing.Printing;

namespace ASFuelControl.Windows.UI.Forms
{
    public partial class CashForm : RadForm
    {
        private Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

        public CashForm()
        {
            InitializeComponent();

            this.radButton4.Visible = Program.AdminConnected;
                //(
                //    Program.CurrentUserLevel == Common.Enumerators.ApplicationUserLevelEnum.Administrator ||
                //    Program.CurrentUserLevel == Common.Enumerators.ApplicationUserLevelEnum.SuperUser
                //);

            List<Common.Enumerators.EnumItem> list = Common.Enumerators.EnumToList.EnumList<Common.Enumerators.PaymentTypeEnum>();
            Common.Enumerators.EnumItem dummy = new Common.Enumerators.EnumItem();
            dummy.Description = "(Όλα τα παραστατικά)";
            dummy.Value = -1;
            list.Insert(0, dummy);

            this.paymentTypeRadDropDownList.DataSource = list;
            this.paymentTypeRadDropDownList.DisplayMember = "Description";
            this.paymentTypeRadDropDownList.ValueMember = "Value";

            var q = this.database.InvoiceTypes.OrderBy(i => i.Description).ToList();
            Data.InvoiceType invTypeDummy = new Data.InvoiceType();
            invTypeDummy.Description = "(Όλα τα παραστατικά)";
            q.Insert(0, invTypeDummy);
            this.radDropDownList1.DataSource = q;
            this.radDropDownList1.DisplayMember = "Description";
            this.radDropDownList1.ValueMember = "InvoiceTypeId";

            this.radDateTimePicker1.Value = DateTime.Today;
            this.radDateTimePicker2.Value = DateTime.Today.AddDays(1).AddMilliseconds(-1);

            GridViewComboBoxColumn col = this.invoiceRadGridView.Columns["PaymentType"] as GridViewComboBoxColumn;
            col.DataSource = Common.Enumerators.EnumToList.EnumList<Common.Enumerators.PaymentTypeEnum>();
            col.DisplayMember = "Description";
            col.ValueMember = "Value";

            this.CreateSummaryRows();

            this.radButton3.ButtonElement.ToolTipText = "Είσπραξη Υπολοίπου";
            this.radButton4.ButtonElement.ToolTipText = "Ακύρωση Παραστατικού";
            this.radButton5.ButtonElement.ToolTipText = "Αντικατάσταση Παραστατικού";
            this.radButton7.ButtonElement.ToolTipText = "Επανεκτύπωση Παραστατικού";

            this.Load += new EventHandler(CashForm_Load);
            this.Disposed += CashForm_Disposed;
        }

        private void CashForm_Disposed(object sender, EventArgs e)
        {
            this.database.Dispose();
        }

        private void LoadData()
        {
            DateTime dtFrom = this.radDateTimePicker1.Value;
            DateTime dtTo = this.radDateTimePicker2.Value;
            Common.Enumerators.EnumItem paymentTypeItem = this.paymentTypeRadDropDownList.SelectedItem.DataBoundItem as Common.Enumerators.EnumItem;
            Guid invType = this.radDropDownList1.SelectedValue == null ? Guid.Empty : (Guid)this.radDropDownList1.SelectedValue;
            int paymentType = -1;
            if (paymentTypeItem != null)
                paymentType = paymentTypeItem.Value;
            var q = this.database.Invoices.Where(i => i.TransactionDate <= dtTo && i.TransactionDate >= dtFrom);
            if (paymentType != -1)
                q = q.Where(i => i.PaymentType == paymentType);
            if (invType != Guid.Empty)
                q = q.Where(i => i.InvoiceTypeId == invType);

            this.invoiceBindingSource.DataSource = q.OrderBy(i => i.TransactionDate);
            this.invoiceBindingSource.ResetBindings(false);
        }

        private void CreateSummaryRows()
        {
            GridViewSummaryItem summaryNettoAmountItem = new GridViewSummaryItem();
            summaryNettoAmountItem.Name = "NettoAmount";
            summaryNettoAmountItem.Aggregate = GridAggregateFunction.Sum;
            summaryNettoAmountItem.FormatString = "{0:N2}";

            GridViewSummaryItem summaryVatAmountItem = new GridViewSummaryItem();
            summaryVatAmountItem.Name = "VatAmount";
            summaryVatAmountItem.Aggregate = GridAggregateFunction.Sum;
            summaryVatAmountItem.FormatString = "{0:N2}";

            GridViewSummaryItem summaryTotalAmountItem = new GridViewSummaryItem();
            summaryTotalAmountItem.Name = "TotalAmount";
            summaryTotalAmountItem.Aggregate = GridAggregateFunction.Sum;
            summaryTotalAmountItem.FormatString = "{0:N2}";

            GridViewSummaryItem summaryCreditAmountItem = new GridViewSummaryItem();
            summaryCreditAmountItem.Name = "CreditAmount";
            summaryCreditAmountItem.Aggregate = GridAggregateFunction.Sum;
            summaryCreditAmountItem.FormatString = "{0:N2}";

            GridViewSummaryItem summaryCashAmountItem = new GridViewSummaryItem();
            summaryCashAmountItem.Name = "CashAmount";
            summaryCashAmountItem.Aggregate = GridAggregateFunction.Sum;
            summaryCashAmountItem.FormatString = "{0:N2}";

            GridViewSummaryItem summaryRestAmountItem = new GridViewSummaryItem();
            summaryRestAmountItem.Name = "RestAmount";
            summaryRestAmountItem.Aggregate = GridAggregateFunction.Sum;
            summaryRestAmountItem.FormatString = "{0:N2}";

            GridViewSummaryItem summaryDescriptionItem = new GridViewSummaryItem();
            summaryDescriptionItem.Name = "Description";
            summaryDescriptionItem.Aggregate = GridAggregateFunction.Count;
            summaryDescriptionItem.FormatString = "Σύνολα";
            //summaryDescriptionItem.AggregateExpression = "Σύνολα";

            GridViewSummaryRowItem summaryRowItem = new GridViewSummaryRowItem();
            summaryRowItem.Add(summaryNettoAmountItem);
            summaryRowItem.Add(summaryVatAmountItem);
            summaryRowItem.Add(summaryTotalAmountItem);
            summaryRowItem.Add(summaryCreditAmountItem);
            summaryRowItem.Add(summaryCashAmountItem);
            summaryRowItem.Add(summaryRestAmountItem);
            summaryRowItem.Add(summaryDescriptionItem);
            this.invoiceRadGridView.SummaryRowsTop.Add(summaryRowItem);
            this.invoiceRadGridView.SummaryRowsBottom.Add(summaryRowItem);
        }

        private void ExporExceltData()
        {
            string filename = "";
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Excel Files (*.xls)|*.xls";
                if (sfd.ShowDialog(this) == System.Windows.Forms.DialogResult.Cancel)
                    return;
                filename = sfd.FileName;
            }
            Telerik.WinControls.UI.Export.ExportToExcelML exporter = new Telerik.WinControls.UI.Export.ExportToExcelML(this.invoiceRadGridView);
            exporter.HiddenColumnOption = Telerik.WinControls.UI.Export.HiddenOption.DoNotExport;
            exporter.ExportVisualSettings = true;
            exporter.SheetMaxRows = Telerik.WinControls.UI.Export.ExcelMaxRows._1048576;
            exporter.SheetName = "Πωλήσεις";
            exporter.SummariesExportOption = Telerik.WinControls.UI.Export.SummariesOption.DoNotExport;
            exporter.RunExport(filename);
        }

        private void ExportPdfData()
        {
            string filename = "";
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "PDF Files (*.pdf)|*.pdf";
                if (sfd.ShowDialog(this) == System.Windows.Forms.DialogResult.Cancel)
                    return;
                filename = sfd.FileName;
            }
            Telerik.WinControls.UI.Export.ExportToPDF pdfExporter = new Telerik.WinControls.UI.Export.ExportToPDF(this.invoiceRadGridView);
            pdfExporter.HiddenColumnOption = Telerik.WinControls.UI.Export.HiddenOption.DoNotExport;
            pdfExporter.ExportVisualSettings = true;
            pdfExporter.FitToPageWidth = true;
            int height = pdfExporter.PdfExportSettings.PageHeight;
            int width = pdfExporter.PdfExportSettings.PageWidth;
            pdfExporter.PdfExportSettings.PageWidth = height;
            pdfExporter.PdfExportSettings.PageHeight = width;
            pdfExporter.Font = new Font(this.Font.FontFamily, 8f);
            pdfExporter.RunExport(filename);
        }

        void CashForm_Load(object sender, EventArgs e)
        {
            this.radButton6.Visible = Program.AdminConnected;
            this.LoadData();
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            this.LoadData();
        }

        private void invoiceBindingSource_PositionChanged(object sender, EventArgs e)
        {
            if (this.invoiceBindingSource.Position < 0)
            {
                this.radButton3.Enabled = false;
                return;
            }
            Data.Invoice invoice = this.invoiceBindingSource.Current as Data.Invoice;
            
            if(invoice == null || invoice.RestAmount == 0)
                this.radButton3.Enabled = false;
            else
                this.radButton3.Enabled = true;

            if (invoice == null || invoice.IsCanceled || invoice.IsReplaced)
            {
                this.radButton4.Enabled = false;
                this.radButton5.Enabled = false;
                this.radButton7.Enabled = false;
            }
            else
            {
                this.radButton4.Enabled = true;
                this.radButton5.Enabled = true;
                this.radButton7.Enabled = true;
            }
        }

        private void radButton3_Click(object sender, EventArgs e)
        {
            if (this.invoiceBindingSource.Position < 0)
            {
                this.radButton3.Enabled = false;
                return;
            }
            Data.Invoice invoice = this.invoiceBindingSource.Current as Data.Invoice;
            if(invoice == null)
                return;
            DialogResult res = RadMessageBox.Show("Θέλετε να καταχωρήσετε την είσπραξη του Παραστατικού\r\n" + invoice.Description, "Είσπραξη Μετρητών", MessageBoxButtons.YesNo, RadMessageIcon.Question);
            if (res == System.Windows.Forms.DialogResult.No)
                return;

            //invoice.CreatePaymentTransaction();
            this.database.SaveChanges();
            this.invoiceBindingSource.ResetBindings(false);
        }

        private void radGridView1_ToolTipTextNeeded(object sender, Telerik.WinControls.ToolTipTextNeededEventArgs e)
        {
            GridDataCellElement cell = sender as GridDataCellElement;

            if (cell != null && cell.ColumnInfo.Name == "InvoiceStatus")
            {
                Data.Invoice inv = cell.RowInfo.DataBoundItem as Data.Invoice;
                if (inv == null)
                    return;
                if (inv.IsCanceled)
                    e.ToolTipText = inv.CanceledInvoiceString;
                if (inv.IsReplaced)
                    e.ToolTipText = inv.ReplacedInvoiceString;
            }
        }

        private void radButton4_Click(object sender, EventArgs e)
        {
            if (this.invoiceBindingSource.Position < 0)
            {
                return;
            }
            Data.Invoice invoice = this.invoiceBindingSource.Current as Data.Invoice;
            if(invoice == null)
                return;
            using (Forms.CreateInvoiceForm cif = new CreateInvoiceForm())
            {
                cif.InvoiceToCancel = invoice;

                Data.InvoiceType cit = this.database.InvoiceTypes.Where(i => i.IsCancelation.HasValue && i.IsCancelation.Value).FirstOrDefault();
                if (cit == null)
                {
                    RadMessageBox.Show("Δεν υπάρχει τύπος παραστατικού για ακύρωση.", "Σφάλμα επιλογής", MessageBoxButtons.OK, RadMessageIcon.Error);
                    return;
                }
                cif.InvoiceTypeId = cit.InvoiceTypeId;
                cif.ShowDialog(this);
                this.database.Refresh(Telerik.OpenAccess.RefreshMode.OverwriteChangesFromStore, invoice);
                this.LoadData();
                this.invoiceBindingSource.ResetBindings(false);
            }
        }

        private void radButton5_Click(object sender, EventArgs e)
        {
            if (this.invoiceBindingSource.Position < 0)
            {
                return;
            }
            Data.Invoice invoice = this.invoiceBindingSource.Current as Data.Invoice;
            if (invoice == null)
                return;
            using (Forms.CreateInvoiceForm cif = new CreateInvoiceForm())
            {
                cif.InvoiceToReplace = invoice;
                using (SelectionForms.SelectInvoiceTypeForm sitf = new SelectionForms.SelectInvoiceTypeForm())
                {
                    sitf.HasFinancialTransactions = true;
                    DialogResult res = sitf.ShowDialog();
                    if (res == System.Windows.Forms.DialogResult.Cancel)
                        return;
                    if (sitf.SelectedInvoiceTypeId == Guid.Empty)
                        return;
                    cif.InvoiceTypeId = sitf.SelectedInvoiceTypeId;
                }
                cif.ShowDialog(this);
                this.database.Refresh(Telerik.OpenAccess.RefreshMode.OverwriteChangesFromStore, invoice);
                this.LoadData();
                this.invoiceBindingSource.ResetBindings(false);
            }
        }

        private void invoiceRadGridView_ViewCellFormatting(object sender, CellFormattingEventArgs e)
        {
            try
            {
                if (e.CellElement is GridSummaryCellElement)
                {
                    if (e.Column.Name == "Description")
                        e.CellElement.TextAlignment = ContentAlignment.MiddleLeft;
                    else
                        e.CellElement.TextAlignment = ContentAlignment.MiddleRight;
                    e.CellElement.Font = new Font(this.invoiceRadGridView.Font, FontStyle.Bold);
                }
                else
                {
                    Data.Invoice invoice = e.Row.DataBoundItem as Data.Invoice;
                    if (invoice == null)
                    {
                        e.CellElement.Font = new Font(this.invoiceRadGridView.Font, FontStyle.Regular);
                        return;
                    }
                    if (invoice.RestAmount == 0)
                    {
                        e.CellElement.Font = new Font(this.invoiceRadGridView.Font, FontStyle.Regular);
                        return;
                    }
                    e.CellElement.Font = new Font(this.invoiceRadGridView.Font, FontStyle.Bold);
                }
            }
            catch
            {
            }
        }

        private void radButton6_Click(object sender, EventArgs e)
        {
            using (CreateInvoiceForm cif = new CreateInvoiceForm())
            {
                using (SelectionForms.SelectDispenserForm sdf = new SelectionForms.SelectDispenserForm())
                {
                    DialogResult res = sdf.ShowDialog(this);
                    if (res == System.Windows.Forms.DialogResult.Cancel)
                        return;
                    if (sdf.SelectedDispenser == Guid.Empty)
                        return;
                    cif.DispenserId = sdf.SelectedDispenser;
                    cif.ShowDialog(this);
                    this.LoadData();
                    this.invoiceBindingSource.ResetBindings(false);
                }
            }
        }

        private void radButton7_Click(object sender, EventArgs e)
        {
            if (this.invoiceRadGridView.CurrentRow == null)
                return;
            Data.Invoice invoice = this.invoiceRadGridView.CurrentRow.DataBoundItem as Data.Invoice;

            if (invoice == null)
                return;
            if (invoice.InvoiceSignature == null || invoice.InvoiceSignature == "")
                return;
            if (invoice.InvoiceLines.Count == 0)
            {
                Telerik.WinControls.RadMessageBox.Show("Το επιλεγμένο Παραστατικό δεν περιέχει γραμμές παραστατικού", "Σφάλμα εκτύπωσης", MessageBoxButtons.OK, Telerik.WinControls.RadMessageIcon.Error);
                return;
            }
            DialogResult res = Telerik.WinControls.RadMessageBox.Show("Θέλετε να εκτύπώσετε το επιλεγμένο Παραστατικό;", "Επανεκτύπωση Παραστατικού", MessageBoxButtons.YesNo, Telerik.WinControls.RadMessageIcon.Question);
            if (res == System.Windows.Forms.DialogResult.Yes)
            {
               
                Threads.PrintAgent.PrintInvoiceDirect(invoice);
            }
        }

        private void radButton8_Click(object sender, EventArgs e)
        {
            RadPrintDocument prdoc = new RadPrintDocument();
            prdoc.DefaultPageSettings.Landscape = true;
            prdoc.DefaultPageSettings.Margins = new Margins(50, 50, 50, 50);

            this.invoiceRadGridView.PrintStyle.FitWidthMode = PrintFitWidthMode.FitPageWidth;
            this.invoiceRadGridView.PrintStyle.AlternatingRowColor = Color.FromArgb(155, 240,240,240);
            this.invoiceRadGridView.PrintStyle.PrintAlternatingRowColor = true;
            this.invoiceRadGridView.PrintStyle.PrintHeaderOnEachPage = true;
            this.invoiceRadGridView.PrintStyle.PrintSummaries = true;
            this.invoiceRadGridView.PrintStyle.SummaryCellBackColor = Color.FromArgb(255, 200, 200, 200);
            this.invoiceRadGridView.PrintPreview(prdoc);
        }

        private void radButton9_Click(object sender, EventArgs e)
        {
            this.ExportPdfData();
        }

        private void radButton10_Click(object sender, EventArgs e)
        {
            this.ExporExceltData();
        }

        private void invoiceRadGridView_CellDoubleClick(object sender, GridViewCellEventArgs e)
        {
            if (e.Row == null)
                return;
            Data.Invoice invoice = e.Row.DataBoundItem as Data.Invoice;
            if(invoice.InvoiceType.IsLaserPrint.HasValue && invoice.InvoiceType.IsLaserPrint.Value)
            {
                InvoiceFormEx invForm = new InvoiceFormEx();
                invForm.LoadInvoice(invoice.InvoiceId);
                invForm.Show(this);
            }
        }
    }
}

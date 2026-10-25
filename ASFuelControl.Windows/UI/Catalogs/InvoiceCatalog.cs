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
using Telerik.WinControls.UI;
using System.Drawing.Printing;

namespace ASFuelControl.Windows.UI.Catalogs
{
    public partial class InvoiceCatalog : UserControl
    {
        ViewModels.InvoiceCatalogViewModel vm = new ViewModels.InvoiceCatalogViewModel();
        private int refreshVersion;
        private Font summaryFont;
        private RadMenuItem heatingCorrectionMenu;
        public InvoiceCatalog()
        {
            InitializeComponent();
            heatingCorrectionMenu = new RadMenuItem("Διόρθωση επιδόματος θέρμανσης…") { Visibility = ElementVisibility.Collapsed };
            heatingCorrectionMenu.Click += delegate {
                try {
                    Threads.SharedInvoiceRuntime.RequireHeatingAdmin();
                    using (var form = new Forms.HeatingCorrectionBatchForm(vm.InvoicesFrom, vm.InvoicesTo)) form.ShowDialog(this);
                    RefreshData();
                } catch (Exception ex) {
                    RadMessageBox.Show(this, ex.Message, "Διόρθωση επιδόματος θέρμανσης", MessageBoxButtons.OK, RadMessageIcon.Error);
                }
            };
            radDropDownButton1.Items.Add(heatingCorrectionMenu);
            vm.QueryInvoiceData += Vm_QueryInvoiceData;
            this.Disposed += delegate
            {
                ++refreshVersion;
                vm.CancelPendingLoads();
                if (summaryFont != null) summaryFont.Dispose();
            };

            Telerik.WinControls.UI.GridViewSummaryItem gridViewSummaryItem1 = new Telerik.WinControls.UI.GridViewSummaryItem();
            Telerik.WinControls.UI.GridViewSummaryItem gridViewSummaryItem2 = new Telerik.WinControls.UI.GridViewSummaryItem();
            Telerik.WinControls.UI.GridViewSummaryItem gridViewSummaryItem3 = new Telerik.WinControls.UI.GridViewSummaryItem();
            Telerik.WinControls.UI.GridViewSummaryItem gridViewSummaryItem4 = new Telerik.WinControls.UI.GridViewSummaryItem();
            Telerik.WinControls.UI.GridViewSummaryItem gridViewSummaryItem5 = new Telerik.WinControls.UI.GridViewSummaryItem();
            Telerik.WinControls.UI.GridViewSummaryItem gridViewSummaryItem11 = new Telerik.WinControls.UI.GridViewSummaryItem();
            Telerik.WinControls.UI.GridViewSummaryItem gridViewSummaryItem21 = new Telerik.WinControls.UI.GridViewSummaryItem();
            Telerik.WinControls.UI.GridViewSummaryItem gridViewSummaryItem31 = new Telerik.WinControls.UI.GridViewSummaryItem();
            Telerik.WinControls.UI.GridViewSummaryItem gridViewSummaryItem41 = new Telerik.WinControls.UI.GridViewSummaryItem();
            Telerik.WinControls.UI.GridViewSummaryItem gridViewSummaryItem51 = new Telerik.WinControls.UI.GridViewSummaryItem();

            Telerik.WinControls.UI.GridViewSummaryItem gridViewSummaryItem1Text = new Telerik.WinControls.UI.GridViewSummaryItem();
            Telerik.WinControls.UI.GridViewSummaryItem gridViewSummaryItem2Text = new Telerik.WinControls.UI.GridViewSummaryItem();
            Telerik.WinControls.UI.GridViewSummaryItem gridViewSummaryItem3Text = new Telerik.WinControls.UI.GridViewSummaryItem();
            Telerik.WinControls.UI.GridViewSummaryItem gridViewSummaryItem4Text = new Telerik.WinControls.UI.GridViewSummaryItem();
            Telerik.WinControls.UI.GridViewSummaryItem gridViewSummaryItem5Text = new Telerik.WinControls.UI.GridViewSummaryItem();

            gridViewSummaryItem1.Aggregate = Telerik.WinControls.UI.GridAggregateFunction.Sum;
            gridViewSummaryItem1.FormatString = "{0:N2}";
            gridViewSummaryItem1.Name = "TotalAmount";
            gridViewSummaryItem2.Aggregate = Telerik.WinControls.UI.GridAggregateFunction.Sum;
            gridViewSummaryItem2.FormatString = "{0:N2}";
            gridViewSummaryItem2.Name = "TotalAmount";
            gridViewSummaryItem3.Aggregate = Telerik.WinControls.UI.GridAggregateFunction.Sum;
            gridViewSummaryItem3.FormatString = "{0:N2}";
            gridViewSummaryItem3.Name = "TotalAmount";
            gridViewSummaryItem4.Aggregate = Telerik.WinControls.UI.GridAggregateFunction.Sum;
            gridViewSummaryItem4.FormatString = "{0:N2}";
            gridViewSummaryItem4.Name = "TotalAmount";
            gridViewSummaryItem5.Aggregate = Telerik.WinControls.UI.GridAggregateFunction.Sum;
            gridViewSummaryItem5.FormatString = "{0:N2}";
            gridViewSummaryItem5.Name = "TotalAmount";

            gridViewSummaryItem11.Aggregate = Telerik.WinControls.UI.GridAggregateFunction.Sum;
            gridViewSummaryItem11.FormatString = "{0:N2}";
            gridViewSummaryItem11.Name = "RestAmount";
            gridViewSummaryItem21.Aggregate = Telerik.WinControls.UI.GridAggregateFunction.Sum;
            gridViewSummaryItem21.FormatString = "{0:N2}";
            gridViewSummaryItem21.Name = "RestAmount";
            gridViewSummaryItem31.Aggregate = Telerik.WinControls.UI.GridAggregateFunction.Sum;
            gridViewSummaryItem31.FormatString = "{0:N2}";
            gridViewSummaryItem31.Name = "RestAmount";
            gridViewSummaryItem41.Aggregate = Telerik.WinControls.UI.GridAggregateFunction.Sum;
            gridViewSummaryItem41.FormatString = "{0:N2}";
            gridViewSummaryItem41.Name = "RestAmount";
            gridViewSummaryItem51.Aggregate = Telerik.WinControls.UI.GridAggregateFunction.Sum;
            gridViewSummaryItem51.FormatString = "{0:N2}";
            gridViewSummaryItem51.Name = "RestAmount";

            gridViewSummaryItem1Text.Aggregate = GridAggregateFunction.Sum;
            gridViewSummaryItem1Text.FormatString = "Μετρητά";
            gridViewSummaryItem1Text.Name = "VatAmount";

            gridViewSummaryItem2Text.Aggregate = GridAggregateFunction.Sum;
            gridViewSummaryItem2Text.FormatString = "Επί πιστώσει";
            gridViewSummaryItem2Text.Name = "VatAmount";

            gridViewSummaryItem3Text.Aggregate = GridAggregateFunction.Sum;
            gridViewSummaryItem3Text.FormatString = "Πιστ. Κάρτα";
            gridViewSummaryItem3Text.Name = "VatAmount";

            gridViewSummaryItem4Text.Aggregate = GridAggregateFunction.Sum;
            gridViewSummaryItem4Text.FormatString = "Κάρτα Στόλου";
            gridViewSummaryItem4Text.Name = "VatAmount";

            gridViewSummaryItem5Text.Aggregate = GridAggregateFunction.Sum;
            gridViewSummaryItem5Text.FormatString = "Σύνολο";
            gridViewSummaryItem5Text.Name = "VatAmount";

            Telerik.WinControls.UI.GridViewSummaryRowItem summaryRow1 = new Telerik.WinControls.UI.GridViewSummaryRowItem(new Telerik.WinControls.UI.GridViewSummaryItem[] {
                gridViewSummaryItem1Text, gridViewSummaryItem1, gridViewSummaryItem11});
            Telerik.WinControls.UI.GridViewSummaryRowItem summaryRow2 = new Telerik.WinControls.UI.GridViewSummaryRowItem(new Telerik.WinControls.UI.GridViewSummaryItem[] {
                gridViewSummaryItem2Text, gridViewSummaryItem2, gridViewSummaryItem21});
            Telerik.WinControls.UI.GridViewSummaryRowItem summaryRow3 = new Telerik.WinControls.UI.GridViewSummaryRowItem(new Telerik.WinControls.UI.GridViewSummaryItem[] {
                gridViewSummaryItem3Text, gridViewSummaryItem3, gridViewSummaryItem31});
            Telerik.WinControls.UI.GridViewSummaryRowItem summaryRow4 = new Telerik.WinControls.UI.GridViewSummaryRowItem(new Telerik.WinControls.UI.GridViewSummaryItem[] {
                gridViewSummaryItem4Text, gridViewSummaryItem4, gridViewSummaryItem41});
            Telerik.WinControls.UI.GridViewSummaryRowItem summaryRow5 = new Telerik.WinControls.UI.GridViewSummaryRowItem(new Telerik.WinControls.UI.GridViewSummaryItem[] {
                gridViewSummaryItem5Text, gridViewSummaryItem5, gridViewSummaryItem51});

            this.invoiceRadGridView.MasterTemplate.SummaryRowsBottom.Add(summaryRow1);
            this.invoiceRadGridView.MasterTemplate.SummaryRowsBottom.Add(summaryRow2);
            this.invoiceRadGridView.MasterTemplate.SummaryRowsBottom.Add(summaryRow3);
            this.invoiceRadGridView.MasterTemplate.SummaryRowsBottom.Add(summaryRow4);
            this.invoiceRadGridView.MasterTemplate.SummaryRowsBottom.Add(summaryRow5);
            invoiceRadGridView.MasterView.SummaryRows[0].PinPosition = Telerik.WinControls.UI.PinnedRowPosition.Bottom;
            invoiceRadGridView.MasterView.SummaryRows[1].PinPosition = Telerik.WinControls.UI.PinnedRowPosition.Bottom;
            invoiceRadGridView.MasterView.SummaryRows[2].PinPosition = Telerik.WinControls.UI.PinnedRowPosition.Bottom;
            invoiceRadGridView.MasterView.SummaryRows[3].PinPosition = Telerik.WinControls.UI.PinnedRowPosition.Bottom;
            invoiceRadGridView.MasterView.SummaryRows[4].PinPosition = Telerik.WinControls.UI.PinnedRowPosition.Bottom;

            this.invoiceRadGridView.ViewCellFormatting += InvoiceRadGridView_ViewCellFormatting;
            this.invoiceRadGridView.GroupSummaryEvaluate += InvoiceRadGridView_GroupSummaryEvaluate;
        }

        public async void RefreshData()
        {
            if (IsDisposed || Disposing) return;
            int request = ++refreshVersion;
            SetLoading(true);
            try
            {
                if (this.invoiceCatalogViewModelBindingSource.DataSource != vm)
                    this.invoiceCatalogViewModelBindingSource.DataSource = vm;
                var snapshot = await vm.FetchDataAsync();
                if (snapshot == null || request != refreshVersion || IsDisposed || Disposing) return;
                this.invoiceRadGridView.BeginUpdate();
                try { vm.ApplyData(snapshot); }
                finally { this.invoiceRadGridView.EndUpdate(); }
            }
            catch (Exception ex)
            {
                if (request == refreshVersion && !IsDisposed && !Disposing)
                    RadMessageBox.Show(this, ex.Message, "Ανάκτηση παραστατικών", MessageBoxButtons.OK, RadMessageIcon.Error);
            }
            finally
            {
                if (request == refreshVersion && !IsDisposed && !Disposing) SetLoading(false);
            }
        }

        private void SetLoading(bool loading)
        {
            this.labelTitle.Text = loading ? "Κατάλογος Παραστατικών — Ανάκτηση..." : "Κατάλογος Παραστατικών";
            this.invoiceRadGridView.Enabled = !loading;
            this.btnAdd.Enabled = !loading;
            this.btnDelete.Enabled = !loading;
            this.btnPrint.Enabled = !loading;
            this.radDropDownButton1.Enabled = !loading;
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

        private void InvoiceRadGridView_ViewCellFormatting(object sender, CellFormattingEventArgs e)
        {
            if (e.CellElement is Telerik.WinControls.UI.GridSummaryCellElement)
            {
                if(e.CellElement.ColumnInfo.Name == "TotalAmount" || e.CellElement.ColumnInfo.Name == "RestAmount")
                    e.CellElement.TextAlignment = ContentAlignment.MiddleRight;
                else
                    e.CellElement.TextAlignment = ContentAlignment.MiddleLeft;
                if (summaryFont == null) summaryFont = new Font(this.invoiceRadGridView.Font, FontStyle.Bold);
                e.CellElement.Font = summaryFont;
            }
        }

        private void InvoiceRadGridView_GroupSummaryEvaluate(object sender, GroupSummaryEvaluationEventArgs e)
        {
            if (e.SummaryItem.Name != "TotalAmount" && e.SummaryItem.Name != "RestAmount") return;
            // Preserve the original full-result totals even when the grid's own
            // column filters hide some rows. No per-cell scans or array copies.
            for (int row = 0; row < 5; row++)
            {
                if (!this.invoiceRadGridView.SummaryRowsBottom[row].Contains(e.SummaryItem)) continue;
                e.Value = vm.GetSummary(row, e.SummaryItem.Name);
                return;
            }
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            this.RefreshData();
        }

        private void menuInvoiceReplacement_Click(object sender, EventArgs e)
        {
            Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
            try
            {
                var invoices = this.vm.Invoices.Where(s => s.Selected).ToArray();
                if (invoices.Length == 0)
                    return;

                var selectedIds = new List<Guid>();
                selectedIds.AddRange(invoices.Select(s => s.InvoiceId));

                var selectedTypeIds = new List<Guid>();
                selectedTypeIds.AddRange(invoices.Select(s => s.InvoiceTypeId).Distinct());

                var c = db.InvoiceLines.Where(i => selectedIds.Contains(i.InvoiceId)).Select(i => i.FuelTypeId).Distinct().Count();
                
                

                if(selectedTypeIds.Count != 1)
                {
                    db.Dispose();
                    RadMessageBox.Show("Τα επιλεγμένα παραστατικά πρέπει να είναι του ίδιου Τύπο Παραστατικού.", "Σφάλμα Επιλογής...", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                    return;
                }

                Guid invoiceTypeId = invoices.Select(i => i.InvoiceTypeId).First();
                

                Guid parentInvoiceTypeId = invoices.Select(i => i.InvoiceTypeId).First();
                Data.InvoiceTypeTransform transformation = null;
                using (SelectionForms.InvoiceTypeSelection sitf = new SelectionForms.InvoiceTypeSelection())
                {
                    if(selectedIds.Count > 1)
                    {
                        var q = db.InvoiceTypeTransforms.Where(i => i.ParentInvoiceTypeId == parentInvoiceTypeId);
                        q = q.Where(i =>
                            i.TransformationMode == (int)Common.Enumerators.InvoiceTransformationTypeEnum.ManyToOne ||
                            i.TransformationMode == (int)Common.Enumerators.InvoiceTransformationTypeEnum.Cancelation);
                        sitf.AllowedInvoiceTypes = q.Select(i=>i.ChildInvoiceTypeId).ToArray();
                    }
                    else
                        sitf.AllowedInvoiceTypes = db.InvoiceTypeTransforms.Where(i => i.ParentInvoiceTypeId == invoiceTypeId).Select(i=>i.ChildInvoiceTypeId).ToArray();

                    sitf.FilterAdminView = true;
                    sitf.ReplacementTypes = true;
                    //sitf.ToBeReplacedTypeId = invoiceTypeId;
                    var result = sitf.ShowDialog(this);
                    if (result != DialogResult.OK)
                        return;
                    invoiceTypeId = (sitf.SelectedEntity as ViewModels.InvoiceTypeViewModel).InvoiceTypeId;
                    if (invoiceTypeId == Guid.Empty)
                        return;

                    var invoiceType = db.InvoiceTypes.SingleOrDefault(i => i.InvoiceTypeId == invoiceTypeId);
                    if (invoiceType == null)
                        return;
                    if (c > 1 && invoiceType.NeedsVehicle.HasValue && invoiceType.NeedsVehicle.Value)
                    {
                        db.Dispose();
                        RadMessageBox.Show("Το καύσιμο των παραστατικών που επιλέγετε πρέπει να είναι ίδιο.", "Σφάλμα Επιλογής...", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                        return;
                    }

                    transformation = db.InvoiceTypeTransforms.Where(i => i.ChildInvoiceTypeId == invoiceTypeId && i.ParentInvoiceTypeId == parentInvoiceTypeId).FirstOrDefault();
                    
                }

                if(transformation.TransformationMode != (int)Common.Enumerators.InvoiceTransformationTypeEnum.ManyToOne && invoices.Length != 1)
                {
                    db.Dispose();
                    RadMessageBox.Show("Ο μετασχηματισμός που επλέξατε μπορεί να γίνει για ένα μόνο παραστατικό", "Σφάλμα Επιλογής...", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                    return;
                }

                if (transformation.TransformationMode != (int)Common.Enumerators.InvoiceTransformationTypeEnum.OneToMany)
                {
                    var cm = this.vm.Invoices.Where(s => s.Selected && (s.IsCanceled || s.IsReplaced)).Count();
                    if (cm > 0)
                    {
                        db.Dispose();
                        RadMessageBox.Show("Κάποιο από τα παραστατικά έχει ήδη μετασχηματιστεί!", "Σφάλμα Επιλογής...", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                        return;
                    }
                }

                var invoiceIds = invoices.Select(s => s.InvoiceId);
                var selectedInvoices = invoices.Select(i => i.InvoiceId).ToArray();
                var dbInvoices = db.Invoices.Where(i => invoiceIds.Contains(i.InvoiceId)).ToArray();
                var vehicleSelections = dbInvoices.Where(i => i.VehicleId.HasValue)
                    .Select(i => i.VehicleId.Value).Distinct().ToArray();
                Guid? vehicleId = vehicleSelections.Length == 1 ? new Guid?(vehicleSelections[0]) : null;
                var traderSelections = dbInvoices.Where(i => i.TraderId.HasValue)
                    .Select(i => i.TraderId.Value).Distinct().ToArray();
                Guid? traderId = traderSelections.Length == 1 ? new Guid?(traderSelections[0]) : null;
                if (!traderId.HasValue && vehicleId.HasValue)
                {
                    var selectedVehicle = db.Vehicles.SingleOrDefault(v => v.VehicleId == vehicleId.Value);
                    if (selectedVehicle != null)
                        traderId = selectedVehicle.TraderId;
                }
                Forms.InvoiceFormEx invoiceForm = Forms.InvoiceEditorFormFactory.CreateForInvoiceType(invoiceTypeId);

                if (transformation.CreationInvoiceTypeId.HasValue)
                {
                    invoiceForm.CreateIntermediateTransformation(selectedInvoices,
                        transformation.CreationInvoiceTypeId.Value, traderId, vehicleId,
                        (int)Common.Enumerators.InvoiceTransformationTypeEnum.OneToOne,
                        transformation.CreationNotesAddition);
                }
                var newInvoiceId = invoiceForm.TransformInvoices(selectedInvoices, invoiceTypeId, traderId,
                    vehicleId, transformation.TransformationMode, !transformation.CreationInvoiceTypeId.HasValue);
                if (newInvoiceId == Guid.Empty)
                {
                    invoiceForm.Dispose();
                    return;
                }
                invoiceForm.Show(this);
                invoiceForm.FormClosed += InvForm_FormClosed;
                //if (vehicleSelections.Length == 1 && vehicleId.HasValue)
                //{
                //    Forms.InvoiceFormEx invoiceForm = new Forms.InvoiceFormEx();
                //    invoiceForm.TransformInvoices(selectedInvoices, invoiceTypeId, traderId.Value, vehicleId.Value);
                //    invoiceForm.Show(this);
                //    //newInvoice = vm.ReplaceInvoices(selectedInvoices, invoiceTypeId, traderId.Value, vehicleId.Value);
                //    //newInvoice = vm.TransformInvoices(selectedInvoices, invoiceTypeId, traderId.Value, vehicleId.Value);
                //}
                //else
                //{
                //    using (SelectionForms.VehicleSelectForm tsf = new SelectionForms.VehicleSelectForm())
                //    {
                //        var result = tsf.ShowDialog(this);
                //        if (result != DialogResult.OK)
                //            return;
                //        vehicleId = (tsf.SelectedEntity as ViewModels.VehicleViewModel).VehicleId;
                //        traderId = (tsf.SelectedEntity as ViewModels.VehicleViewModel).TraderId;
                //    }
                //    newInvoice = vm.TransformInvoices(selectedInvoices, invoiceTypeId, traderId.Value, vehicleId.Value);
                //    //newInvoice = vm.ReplaceInvoices(selectedInvoices, invoiceTypeId, traderId.Value, vehicleId.Value);
                //}
                //if (newInvoice != Guid.Empty)
                //{

                //    var invoice = db.Invoices.SingleOrDefault(i => i.InvoiceId == newInvoice);
                //    if (invoice == null)
                //        return;
                //    if ((invoice.InvoiceType.IsLaserPrint.HasValue && invoice.InvoiceType.IsLaserPrint.Value) || invoice.InvoiceLines.Count > 1)
                //    {
                //        Forms.InvoiceFormEx invoiceForm = new Forms.InvoiceFormEx();
                //        invoiceForm.LoadInvoice(newInvoice);
                //        invoiceForm.Show(this);
                //    }
                //    else
                //    {
                //        Forms.InvoiceSimpleForm isfm = new Forms.InvoiceSimpleForm();
                //        isfm.LoadInvoice(newInvoice);
                //        isfm.Show(this);
                //    }
                //}
            }
            catch(Exception ex)
            {
                RadMessageBox.Show(ex.Message,"Μετασχηματισμός παραστατικού",MessageBoxButtons.OK,RadMessageIcon.Exclamation);
            }
            finally
            {
                db.Dispose();
            }
        }

        private void menuExcelExport_Click(object sender, EventArgs e)
        {
            ExporExceltData();
        }

        private void menuInvoiceCancel_Click(object sender, EventArgs e)
        {
            Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
            try
            {
                var invoices = this.vm.Invoices.Where(s => s.Selected).ToArray();
                if (invoices.Length == 0)
                    return;
                Guid invoiceTypeId = Guid.Empty;
                using (SelectionForms.InvoiceTypeSelection sitf = new SelectionForms.InvoiceTypeSelection())
                {
                    sitf.FilterAdminView = true;
                    sitf.CancelationTypes = true;
                    var result = sitf.ShowDialog(this);
                    if (result != DialogResult.OK)
                        return;
                    invoiceTypeId = (sitf.SelectedEntity as ViewModels.InvoiceTypeViewModel).InvoiceTypeId;
                }
                var selectedInvoices = invoices.Select(i => i.InvoiceId).ToArray();
                var vehicleSelections = db.Invoices.Where(i => invoices.Select(s => s.InvoiceId).Contains(i.InvoiceId)).Select(i => i.Vehicle).Distinct().ToArray();
                var vehicle = vehicleSelections.First();
                Guid? vehicleId = vehicle == null ? null : new Guid?(vehicleSelections.First().VehicleId);
                Guid? traderId = vehicle == null ? null : new Guid?(vehicleSelections.First().TraderId);
                Guid newInvoice = Guid.Empty;
                if (vehicleSelections.Length == 1 && vehicleId.HasValue)
                {
                    newInvoice = vm.CancelInvoices(selectedInvoices, invoiceTypeId, traderId.Value, vehicleId.Value);
                }
                else
                {
                    newInvoice = vm.CancelInvoices(selectedInvoices, invoiceTypeId, Guid.Empty, Guid.Empty);
                }
                if (newInvoice != Guid.Empty)
                {

                    var invoice = db.Invoices.SingleOrDefault(i => i.InvoiceId == newInvoice);
                    if (invoice == null)
                        return;
                    Forms.InvoiceFormEx invoiceForm = Forms.InvoiceEditorFormFactory.CreateForInvoice(newInvoice);
                    invoiceForm.LoadInvoice(newInvoice);
                    invoiceForm.Show(this);
                    invoiceForm.FormClosed += InvForm_FormClosed;
                    this.RefreshData();
                }
            }
            catch(Exception ex)
            {

            }
            finally
            {
                db.Dispose();
            }
        }

        private void menuPayment_Click(object sender, EventArgs e)
        {

        }

        private void menuPdfExport_Click(object sender, EventArgs e)
        {
            ExportPdfData();
        }

        private void invoiceRadGridView_CellBeginEdit(object sender, Telerik.WinControls.UI.GridViewCellCancelEventArgs e)
        {
            if (e.Row == null)
                return;
            var curInvoice = e.Row.DataBoundItem as Data.InvoiceCatalogView;
            var selectedInvoices = this.vm.Invoices.Where(i => i.Selected).ToArray();
            if (selectedInvoices.Length == 0)
                return;
            if (curInvoice == null)
                return;
            if (curInvoice != null && curInvoice.VehicleId.HasValue)
            {
                //var vehicleSelections = selectedInvoices.Select(s => s.VehicleId).Distinct();
                //if (!vehicleSelections.Contains(curInvoice.VehicleId))
                //{
                //    e.Cancel = true;
                //    return;
                //}
            }
            var invTypeSelections = selectedInvoices.Select(s => s.InvoiceTypeId).Distinct();
            if (!invTypeSelections.Contains(curInvoice.InvoiceTypeId))
            {
                e.Cancel = true;
                return;
            }
            
        }

        private void invoiceRadGridView_CellDoubleClick(object sender, Telerik.WinControls.UI.GridViewCellEventArgs e)
        {
            if (this.invoiceRadGridView.CurrentRow == null)
                return;
            var dataRow = this.invoiceRadGridView.CurrentRow.DataBoundItem as Data.InvoiceCatalogView;
            if (dataRow == null)
                return;

            if(e.Column.Name == "TraderDescription" && dataRow.TraderId.HasValue)
            {
                Forms.TraderForm tf = new Forms.TraderForm();
                tf.LoadTrader(dataRow.TraderId.Value);
                tf.Show(this);
                return;
            }
            else if (e.Column.Name == "VehiclePlateNumber" && dataRow.TraderId.HasValue && dataRow.VehicleId.HasValue)
            {
                Forms.TraderForm tf = new Forms.TraderForm();
                tf.LoadTrader(dataRow.TraderId.Value);
                tf.Show(this);
                return;
            }

            Forms.InvoiceFormEx isfm = Forms.InvoiceEditorFormFactory.CreateForInvoice(dataRow.InvoiceId);
            isfm.LoadInvoice(dataRow.InvoiceId);
            isfm.Show(this);
            isfm.FormClosed += InvForm_FormClosed;
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {

            if (this.invoiceRadGridView.CurrentRow == null)
                return;
            var dataRow = this.invoiceRadGridView.CurrentRow.DataBoundItem as Data.InvoiceCatalogView;
            if (dataRow == null)
                return;
            if(dataRow.IsPrinted)
            {
                RadMessageBox.Show("Δεν επιτρέπεται η διαγραφή της εγγραφής που επιλέξατε!", "Σφάλμα Διαγραφής...", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                return;
            }
            
            try
            {
                using (Threads.SharedInvoiceRuntime.EditUnissued(dataRow.InvoiceId))
                using (Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                {
                    var inv = db.Invoices.SingleOrDefault(i=>i.InvoiceId == dataRow.InvoiceId);
                    if (inv == null) return;
                    if (inv.IsPrinted == true || !string.IsNullOrWhiteSpace(inv.InvoiceSignature) || !string.IsNullOrWhiteSpace(inv.QRCodeData) ||
                        db.MyDataInvoices.Any(m => m.InvoiceId == inv.InvoiceId) ||
                        inv.InvoiceLines.Any(i => i.TankFillingId.HasValue || i.SaleTransactionId.HasValue))
                        throw new InvalidOperationException("Δεν επιτρέπεται η διαγραφή παραστατικού με ιστορικό έκδοσης ή κίνησης.");
                    if (RadMessageBox.Show("Θέλετε να διαγράψετε την εγγραφή που επιλέξατε;", "Διαγραφή...", MessageBoxButtons.YesNo, RadMessageIcon.Question) != DialogResult.Yes) return;
                    db.Delete(inv.InvoiceLines);
                    db.Delete(inv.ParentInvoiceRelations);
                    db.Delete(inv.ChildInvoiceRelations);
                    db.Delete(inv.FinTransactions);
                    db.Delete(inv);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                RadMessageBox.Show(ex.Message,"Σφάλμα Διαγραφής...",MessageBoxButtons.OK,RadMessageIcon.Exclamation);
                return;
            }

            this.RefreshData();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            Forms.InvoiceFormEx invForm = Forms.InvoiceEditorFormFactory.SelectForNewInvoice(this);
            if (invForm == null)
                return;
            invForm.Show(this);
            invForm.FormClosed += InvForm_FormClosed;
        }

        private void InvForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Forms.InvoiceFormEx invForm = sender as Forms.InvoiceFormEx;
            if (invForm == null)
                return;
            if(!invForm.IsDisposed && !invForm.Disposing)
                invForm.Dispose();
            this.RefreshData();
        }

        private void radDropDownButton1_DropDownOpening(object sender, EventArgs e)
        {
            this.vm.RefreshIsAdmin();
            heatingCorrectionMenu.Visibility = vm.IsAdmin ? ElementVisibility.Visible : ElementVisibility.Collapsed;
        }

        private void btnPrint_Click(object sender, EventArgs e)
        {
            RadPrintDocument prdoc = new RadPrintDocument();
            prdoc.DefaultPageSettings.Landscape = true;
            prdoc.DefaultPageSettings.Margins = new Margins(50, 50, 50, 50);

            this.invoiceRadGridView.PrintStyle.FitWidthMode = PrintFitWidthMode.FitPageWidth;
            this.invoiceRadGridView.PrintStyle.AlternatingRowColor = Color.FromArgb(155, 240, 240, 240);
            this.invoiceRadGridView.PrintStyle.PrintAlternatingRowColor = true;
            this.invoiceRadGridView.PrintStyle.PrintHeaderOnEachPage = true;
            this.invoiceRadGridView.PrintStyle.PrintSummaries = true;
            this.invoiceRadGridView.PrintStyle.SummaryCellBackColor = Color.FromArgb(255, 200, 200, 200);
            this.invoiceRadGridView.PrintPreview(prdoc);
        }

        private void Vm_QueryInvoiceData(object sender, ViewModels.QueryInvoiceDataArgs e)
        {
            using (Forms.InvoiceTransformationForm itff = new Forms.InvoiceTransformationForm())
            {
                itff.LoadData(e);
                var result = itff.ShowDialog(this);
                if(result != DialogResult.OK)
                    e.Cancel = true;

            }
        }

        private void radTextBox1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                this.vm.Filter = this.radTextBox1.Text;
                this.RefreshData();
            }
            e.Handled = true;
        }
    }
}


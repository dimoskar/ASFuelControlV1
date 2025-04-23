using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls.UI;
using System.IO;

namespace ASFuelControl.Windows.UI.SettingForms
{
    public partial class SaleTransactionsForm : RadForm
    {
        private Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);

        public SaleTransactionsForm()
        {
            InitializeComponent();
            this.radDateTimePicker1.Value = DateTime.Today;
            this.radDateTimePicker2.Value = DateTime.Now;
            this.Disposed += SaleTransactionsForm_Disposed;
        }

        private void SaleTransactionsForm_Disposed(object sender, EventArgs e)
        {
            this.database.Dispose();
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
            Telerik.WinControls.UI.Export.ExportToExcelML exporter = new Telerik.WinControls.UI.Export.ExportToExcelML(this.saleDataViewRadGridView);
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
            Telerik.WinControls.UI.Export.ExportToPDF pdfExporter = new Telerik.WinControls.UI.Export.ExportToPDF(this.saleDataViewRadGridView);
            pdfExporter.HiddenColumnOption = Telerik.WinControls.UI.Export.HiddenOption.DoNotExport;
            pdfExporter.ExportVisualSettings = true;
            pdfExporter.FitToPageWidth = true;
            int height = pdfExporter.PdfExportSettings.PageHeight;
            int width = pdfExporter.PdfExportSettings.PageWidth;
            pdfExporter.PdfExportSettings.PageWidth = 2 * height;
            pdfExporter.PdfExportSettings.PageHeight = 2 * width;
            pdfExporter.RunExport(filename);
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            this.saleDataViewBindingSource.DataSource = this.database.SaleDataViews.
                Where(sv => sv.TransactionTimeStamp <= this.radDateTimePicker2.Value && sv.TransactionTimeStamp >= this.radDateTimePicker1.Value).OrderByDescending(sv=>sv.TransactionTimeStamp);
            this.saleDataViewBindingSource.ResetBindings(false);
            
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            this.Close();

            
        }

        private void saleDataViewRadGridView_ViewCellFormatting(object sender, CellFormattingEventArgs e)
        {
            if (e.CellElement is GridHeaderCellElement)
            {
                switch (e.ColumnIndex)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                        e.CellElement.ForeColor = Color.White;
                        e.CellElement.BackColor = Color.Blue;
                        break;
                    case 6:
                    case 7:
                    case 8:
                        e.CellElement.ForeColor = Color.White;
                        e.CellElement.BackColor = Color.Green;
                        break;
                    case 9:
                    case 10:
                    case 11:
                        e.CellElement.ForeColor = Color.White;
                        e.CellElement.BackColor = Color.Black;
                        break;
                    case 12:
                    case 13:
                    case 14:
                        e.CellElement.ForeColor = Color.White;
                        e.CellElement.BackColor = Color.DarkGray;
                        break;
                    case 15:
                    case 16:
                    case 17:
                        e.CellElement.ForeColor = Color.White;
                        e.CellElement.BackColor = Color.Red;
                        break;
                }
                e.CellElement.BackColor2 = ControlPaint.Dark(e.CellElement.BackColor);
            }
        }

        private void radButton3_Click(object sender, EventArgs e)
        {
            this.ExporExceltData();
        }

        private void radButton4_Click(object sender, EventArgs e)
        {
            this.ExportPdfData();
        }
    }
}

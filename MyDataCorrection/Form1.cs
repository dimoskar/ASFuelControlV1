using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ASFuelControl.Invoicing.Persistence;
using Newtonsoft.Json;

namespace MyDataCorrection
{
    public partial class Form1 : Form
    {
        private FiscalMigrationPage page;
        private DateTime reportFrom,reportUntil;
        public Form1() { InitializeComponent(); }

        private void LoadPage(bool first)
        {
            try
            {
                if (first) { reportFrom = fromDate.Value.Date; reportUntil = untilDate.Value.Date.AddDays(1); }
                var last = first ? null : page?.Rows.LastOrDefault();
                var store = new SqlFiscalMigrationStore(new SqlFiscalDatabase(Properties.Settings.Default.DBConnection));
                page = store.Load(reportFrom,reportUntil,last?.Date,last?.InvoiceId);
                evidenceGrid.DataSource = page.Rows.ToArray();
                nextButton.Enabled = page.HasMore;
                exportButton.Enabled = page.Rows.Count != 0;
                statusLabel.Text = page.Rows.Count + " rows on this page. " + (page.HasMore ? "More rows: Next page." : "End of results.");
            }
            catch (Exception ex)
            {
                page = null; evidenceGrid.DataSource = null; nextButton.Enabled = exportButton.Enabled = false;
                statusLabel.Text = "Report unavailable. No data changed.";
                MessageBox.Show(ex.Message,"Evidence report",MessageBoxButtons.OK,MessageBoxIcon.Error);
            }
        }
        private void scanButton_Click(object sender,EventArgs e) { LoadPage(true); }
        private void nextButton_Click(object sender,EventArgs e) { if (page?.HasMore == true) LoadPage(false); }
        private void exportButton_Click(object sender,EventArgs e)
        {
            if (page == null) return;
            using (var dialog = new SaveFileDialog { Filter = "JSON evidence report|*.json",FileName = "fiscal-evidence.json" })
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    try { File.WriteAllText(dialog.FileName,JsonConvert.SerializeObject(new {
                        GeneratedUtc = DateTime.UtcNow,From = reportFrom,UntilExclusive = reportUntil,
                        Notice = "This page only. Recorded evidence is not verified acceptance. No rows were imported or changed.",
                        page.HasMore,page.Rows },Formatting.Indented)); }
                    catch (Exception ex) { MessageBox.Show(ex.Message,"Export",MessageBoxButtons.OK,MessageBoxIcon.Error); }
                }
        }
    }
}

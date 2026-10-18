namespace MyDataCorrection
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing) { if (disposing && components != null) components.Dispose(); base.Dispose(disposing); }
        private System.Windows.Forms.DateTimePicker fromDate,untilDate;
        private System.Windows.Forms.Button scanButton,nextButton,exportButton;
        private System.Windows.Forms.DataGridView evidenceGrid;
        private System.Windows.Forms.Label statusLabel;
        private void InitializeComponent()
        {
            this.Text = "Fiscal evidence review";
            this.ClientSize = new System.Drawing.Size(1120,600);
            this.MinimumSize = new System.Drawing.Size(900,450);
            var bar = new System.Windows.Forms.FlowLayoutPanel { Dock = System.Windows.Forms.DockStyle.Top,Height = 38,Padding = new System.Windows.Forms.Padding(5) };
            fromDate = new System.Windows.Forms.DateTimePicker { Width = 130,Format = System.Windows.Forms.DateTimePickerFormat.Short,Value = System.DateTime.Today.AddDays(-30) };
            untilDate = new System.Windows.Forms.DateTimePicker { Width = 130,Format = System.Windows.Forms.DateTimePickerFormat.Short,Value = System.DateTime.Today };
            scanButton = new System.Windows.Forms.Button { Text = "Review",Width = 95 };
            nextButton = new System.Windows.Forms.Button { Text = "Next page",Width = 95,Enabled = false };
            exportButton = new System.Windows.Forms.Button { Text = "Export page",Width = 110,Enabled = false };
            bar.Controls.Add(new System.Windows.Forms.Label { Text = "From",Width = 40,Padding = new System.Windows.Forms.Padding(0,5,0,0) }); bar.Controls.Add(fromDate);
            bar.Controls.Add(new System.Windows.Forms.Label { Text = "Through",Width = 55,Padding = new System.Windows.Forms.Padding(0,5,0,0) }); bar.Controls.Add(untilDate);
            bar.Controls.Add(scanButton); bar.Controls.Add(nextButton); bar.Controls.Add(exportButton);
            var note = new System.Windows.Forms.Label { Dock = System.Windows.Forms.DockStyle.Top,Height = 48,Padding = new System.Windows.Forms.Padding(8),Text = "Read-only review. MARKs, signatures and print flags are historical evidence; they do not establish provider, environment or payment outcome.\r\nUse the application's invoicing history for shared recovery. Cancellation and deletion are unavailable here." };
            evidenceGrid = new System.Windows.Forms.DataGridView { Dock = System.Windows.Forms.DockStyle.Fill,ReadOnly = true,AllowUserToAddRows = false,AllowUserToDeleteRows = false,AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells };
            evidenceGrid.DataBindingComplete += (sender,args) => {
                var order = new[] { "Classification","Date","Number","Series","Mark","LegacyRows","Attempts","Workflow","CancellationEvidence","SignatureEvidence","QrEvidence","PrintedFlag","ZeroAaPayload","InvoiceId" };
                for (int i = 0; i < order.Length; i++) if (evidenceGrid.Columns.Contains(order[i])) evidenceGrid.Columns[order[i]].DisplayIndex = i;
            };
            statusLabel = new System.Windows.Forms.Label { Dock = System.Windows.Forms.DockStyle.Bottom,Height = 28,Text = "Select dates and Review. Requires database migration 59.",Padding = new System.Windows.Forms.Padding(8,4,0,0) };
            this.Controls.Add(evidenceGrid); this.Controls.Add(note); this.Controls.Add(bar); this.Controls.Add(statusLabel);
            scanButton.Click += scanButton_Click; nextButton.Click += nextButton_Click; exportButton.Click += exportButton_Click;
        }
    }
}

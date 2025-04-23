namespace ASFuelControl.Windows.UI.Catalogs
{
    partial class ReportGrid
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.reportViewer1 = new Telerik.ReportViewer.WinForms.ReportViewer();
            this.panel1 = new System.Windows.Forms.Panel();
            this.radCommandBar1 = new Telerik.WinControls.UI.RadCommandBar();
            this.commandBarRowElement1 = new Telerik.WinControls.UI.CommandBarRowElement();
            this.commandBarStripElement1 = new Telerik.WinControls.UI.CommandBarStripElement();
            this.commandBarDropDownButton1 = new Telerik.WinControls.UI.CommandBarDropDownButton();
            this.radMenuItem1 = new Telerik.WinControls.UI.RadMenuItem();
            this.radMenuItem2 = new Telerik.WinControls.UI.RadMenuItem();
            this.radMenuItem5 = new Telerik.WinControls.UI.RadMenuItem();
            this.radMenuItem4 = new Telerik.WinControls.UI.RadMenuItem();
            this.radMenuItem7 = new Telerik.WinControls.UI.RadMenuItem();
            this.radMenuItem6 = new Telerik.WinControls.UI.RadMenuItem();
            this.radMenuItem3 = new Telerik.WinControls.UI.RadMenuItem();
            this.radMenuItem8 = new Telerik.WinControls.UI.RadMenuItem();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.radCommandBar1)).BeginInit();
            this.SuspendLayout();
            // 
            // reportViewer1
            // 
            this.reportViewer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.reportViewer1.Location = new System.Drawing.Point(0, 46);
            this.reportViewer1.Name = "reportViewer1";
            this.reportViewer1.Size = new System.Drawing.Size(1365, 572);
            this.reportViewer1.TabIndex = 0;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.radCommandBar1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
            this.panel1.Size = new System.Drawing.Size(1365, 46);
            this.panel1.TabIndex = 1;
            // 
            // radCommandBar1
            // 
            this.radCommandBar1.Dock = System.Windows.Forms.DockStyle.Top;
            this.radCommandBar1.Location = new System.Drawing.Point(0, 3);
            this.radCommandBar1.Name = "radCommandBar1";
            this.radCommandBar1.Rows.AddRange(new Telerik.WinControls.UI.CommandBarRowElement[] {
            this.commandBarRowElement1});
            this.radCommandBar1.Size = new System.Drawing.Size(1365, 40);
            this.radCommandBar1.TabIndex = 6;
            this.radCommandBar1.Text = "radCommandBar1";
            // 
            // commandBarRowElement1
            // 
            this.commandBarRowElement1.MinSize = new System.Drawing.Size(25, 25);
            this.commandBarRowElement1.Strips.AddRange(new Telerik.WinControls.UI.CommandBarStripElement[] {
            this.commandBarStripElement1});
            // 
            // commandBarStripElement1
            // 
            this.commandBarStripElement1.DisplayName = "commandBarStripElement1";
            // 
            // 
            // 
            this.commandBarStripElement1.Grip.Visibility = Telerik.WinControls.ElementVisibility.Collapsed;
            this.commandBarStripElement1.Items.AddRange(new Telerik.WinControls.UI.RadCommandBarBaseItem[] {
            this.commandBarDropDownButton1});
            this.commandBarStripElement1.Name = "commandBarStripElement1";
            // 
            // 
            // 
            this.commandBarStripElement1.OverflowButton.Visibility = Telerik.WinControls.ElementVisibility.Collapsed;
            this.commandBarStripElement1.StretchHorizontally = false;
            ((Telerik.WinControls.UI.RadCommandBarGrip)(this.commandBarStripElement1.GetChildAt(0))).Visibility = Telerik.WinControls.ElementVisibility.Collapsed;
            ((Telerik.WinControls.UI.RadCommandBarOverflowButton)(this.commandBarStripElement1.GetChildAt(2))).Visibility = Telerik.WinControls.ElementVisibility.Collapsed;
            // 
            // commandBarDropDownButton1
            // 
            this.commandBarDropDownButton1.AccessibleDescription = "Επιλογή Αναφοράς";
            this.commandBarDropDownButton1.AccessibleName = "Επιλογή Αναφοράς";
            this.commandBarDropDownButton1.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(1)))), ((int)(((byte)(1)))), ((int)(((byte)(1)))));
            this.commandBarDropDownButton1.DisplayName = "commandBarDropDownButton1";
            this.commandBarDropDownButton1.DrawText = true;
            this.commandBarDropDownButton1.Image = global::ASFuelControl.Windows.Properties.Resources.Statistics;
            this.commandBarDropDownButton1.Items.AddRange(new Telerik.WinControls.RadItem[] {
            this.radMenuItem1,
            this.radMenuItem2,
            this.radMenuItem5,
            this.radMenuItem4,
            this.radMenuItem7,
            this.radMenuItem6,
            this.radMenuItem3,
            this.radMenuItem8});
            this.commandBarDropDownButton1.Name = "commandBarDropDownButton1";
            this.commandBarDropDownButton1.StretchHorizontally = false;
            this.commandBarDropDownButton1.Text = "Επιλογή Αναφοράς";
            this.commandBarDropDownButton1.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.commandBarDropDownButton1.Visibility = Telerik.WinControls.ElementVisibility.Visible;
            // 
            // radMenuItem1
            // 
            this.radMenuItem1.AccessibleDescription = "Αναφορά Βάρδιας";
            this.radMenuItem1.AccessibleName = "Αναφορά Βάρδιας";
            this.radMenuItem1.Name = "radMenuItem1";
            this.radMenuItem1.Text = "Αναφορά Βάρδιας";
            this.radMenuItem1.Visibility = Telerik.WinControls.ElementVisibility.Visible;
            this.radMenuItem1.Click += new System.EventHandler(this.radMenuItem1_Click);
            // 
            // radMenuItem2
            // 
            this.radMenuItem2.AccessibleDescription = "Αναφορά Περιόδου";
            this.radMenuItem2.AccessibleName = "Αναφορά Περιόδου";
            this.radMenuItem2.Name = "radMenuItem2";
            this.radMenuItem2.Text = "Αναφορά Περιόδου";
            this.radMenuItem2.Visibility = Telerik.WinControls.ElementVisibility.Visible;
            this.radMenuItem2.Click += new System.EventHandler(this.radMenuItem2_Click);
            // 
            // radMenuItem5
            // 
            this.radMenuItem5.AccessibleDescription = "Κατάσταση Συναλλαγών";
            this.radMenuItem5.AccessibleName = "Κατάσταση Συναλλαγών";
            this.radMenuItem5.Name = "radMenuItem5";
            this.radMenuItem5.Text = "Κατάσταση Συναλλαγών";
            this.radMenuItem5.Visibility = Telerik.WinControls.ElementVisibility.Visible;
            this.radMenuItem5.Click += new System.EventHandler(this.radMenuItem5_Click);
            // 
            // radMenuItem4
            // 
            this.radMenuItem4.AccessibleDescription = "Παραστατικά";
            this.radMenuItem4.AccessibleName = "Παραστατικά";
            this.radMenuItem4.Name = "radMenuItem4";
            this.radMenuItem4.Text = "Παραστατικά";
            this.radMenuItem4.Visibility = Telerik.WinControls.ElementVisibility.Visible;
            this.radMenuItem4.Click += new System.EventHandler(this.radMenuItem4_Click);
            // 
            // radMenuItem7
            // 
            this.radMenuItem7.AccessibleDescription = "Αποθέματα Δεξαμενών";
            this.radMenuItem7.AccessibleName = "Αποθέματα Δεξαμενών";
            this.radMenuItem7.Name = "radMenuItem7";
            this.radMenuItem7.Text = "Αποθέματα Δεξαμενών";
            this.radMenuItem7.Visibility = Telerik.WinControls.ElementVisibility.Visible;
            this.radMenuItem7.Click += new System.EventHandler(this.radMenuItem7_Click);
            // 
            // radMenuItem6
            // 
            this.radMenuItem6.AccessibleDescription = "Παραλαβές";
            this.radMenuItem6.AccessibleName = "Παραλαβές";
            this.radMenuItem6.Name = "radMenuItem6";
            this.radMenuItem6.Text = "Παραλαβές";
            this.radMenuItem6.Visibility = Telerik.WinControls.ElementVisibility.Visible;
            this.radMenuItem6.Click += new System.EventHandler(this.radMenuItem6_Click);
            // 
            // radMenuItem3
            // 
            this.radMenuItem3.AccessibleDescription = "Ισοζύγια";
            this.radMenuItem3.AccessibleName = "Ισοζύγια";
            this.radMenuItem3.Name = "radMenuItem3";
            this.radMenuItem3.Text = "Ισοζύγια";
            this.radMenuItem3.Visibility = Telerik.WinControls.ElementVisibility.Visible;
            this.radMenuItem3.Click += new System.EventHandler(this.radMenuItem3_Click);
            // 
            // radMenuItem8
            // 
            this.radMenuItem8.AccessibleDescription = "Μετρητές Αντλιών";
            this.radMenuItem8.AccessibleName = "Μετρητές Αντλιών";
            this.radMenuItem8.Name = "radMenuItem8";
            this.radMenuItem8.Text = "Μετρητές Αντλιών";
            this.radMenuItem8.Visibility = Telerik.WinControls.ElementVisibility.Visible;
            this.radMenuItem8.Click += new System.EventHandler(this.radMenuItem8_Click);
            // 
            // ReportGrid
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.reportViewer1);
            this.Controls.Add(this.panel1);
            this.Name = "ReportGrid";
            this.Size = new System.Drawing.Size(1365, 618);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.radCommandBar1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private Telerik.ReportViewer.WinForms.ReportViewer reportViewer1;
        private System.Windows.Forms.Panel panel1;
        private Telerik.WinControls.UI.RadCommandBar radCommandBar1;
        private Telerik.WinControls.UI.CommandBarRowElement commandBarRowElement1;
        private Telerik.WinControls.UI.CommandBarStripElement commandBarStripElement1;
        private Telerik.WinControls.UI.CommandBarDropDownButton commandBarDropDownButton1;
        private Telerik.WinControls.UI.RadMenuItem radMenuItem1;
        private Telerik.WinControls.UI.RadMenuItem radMenuItem2;
        private Telerik.WinControls.UI.RadMenuItem radMenuItem3;
        private Telerik.WinControls.UI.RadMenuItem radMenuItem4;
        private Telerik.WinControls.UI.RadMenuItem radMenuItem5;
        private Telerik.WinControls.UI.RadMenuItem radMenuItem6;
        private Telerik.WinControls.UI.RadMenuItem radMenuItem7;
        private Telerik.WinControls.UI.RadMenuItem radMenuItem8;
    }
}

namespace ASFuelControl.Windows.UI.Catalogs
{
    partial class AlertGrid
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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.panelGeneral = new System.Windows.Forms.FlowLayoutPanel();
            this.panelTanks = new System.Windows.Forms.FlowLayoutPanel();
            this.radLabel1 = new Telerik.WinControls.UI.RadLabel();
            this.radLabel2 = new Telerik.WinControls.UI.RadLabel();
            this.radLabel3 = new Telerik.WinControls.UI.RadLabel();
            this.panelDispenser = new System.Windows.Forms.FlowLayoutPanel();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel3)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33332F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33334F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33334F));
            this.tableLayoutPanel1.Controls.Add(this.panelGeneral, 2, 1);
            this.tableLayoutPanel1.Controls.Add(this.panelTanks, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.radLabel1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.radLabel2, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.radLabel3, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.panelDispenser, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 74F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1293, 576);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // panelGeneral
            // 
            this.panelGeneral.AutoScroll = true;
            this.panelGeneral.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelGeneral.Location = new System.Drawing.Point(861, 74);
            this.panelGeneral.Margin = new System.Windows.Forms.Padding(0);
            this.panelGeneral.Name = "panelGeneral";
            this.panelGeneral.Size = new System.Drawing.Size(432, 502);
            this.panelGeneral.TabIndex = 5;
            // 
            // panelTanks
            // 
            this.panelTanks.AutoScroll = true;
            this.panelTanks.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelTanks.Location = new System.Drawing.Point(430, 74);
            this.panelTanks.Margin = new System.Windows.Forms.Padding(0);
            this.panelTanks.Name = "panelTanks";
            this.panelTanks.Size = new System.Drawing.Size(431, 502);
            this.panelTanks.TabIndex = 4;
            // 
            // radLabel1
            // 
            this.radLabel1.AutoSize = false;
            this.radLabel1.BackColor = System.Drawing.Color.WhiteSmoke;
            this.radLabel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.radLabel1.Font = new System.Drawing.Font("Segoe UI", 24F);
            this.radLabel1.Location = new System.Drawing.Point(0, 0);
            this.radLabel1.Margin = new System.Windows.Forms.Padding(0);
            this.radLabel1.Name = "radLabel1";
            this.radLabel1.Size = new System.Drawing.Size(430, 74);
            this.radLabel1.TabIndex = 0;
            this.radLabel1.Text = "Συναγερμοί Αντλίας";
            this.radLabel1.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            ((Telerik.WinControls.UI.RadLabelElement)(this.radLabel1.GetChildAt(0))).TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            ((Telerik.WinControls.UI.RadLabelElement)(this.radLabel1.GetChildAt(0))).Text = "Συναγερμοί Αντλίας";
            // 
            // radLabel2
            // 
            this.radLabel2.AutoSize = false;
            this.radLabel2.BackColor = System.Drawing.Color.WhiteSmoke;
            this.radLabel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.radLabel2.Font = new System.Drawing.Font("Segoe UI", 24F);
            this.radLabel2.Location = new System.Drawing.Point(430, 0);
            this.radLabel2.Margin = new System.Windows.Forms.Padding(0);
            this.radLabel2.Name = "radLabel2";
            this.radLabel2.Size = new System.Drawing.Size(431, 74);
            this.radLabel2.TabIndex = 1;
            this.radLabel2.Text = "Συναγερμοί Δεξαμενων";
            this.radLabel2.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            ((Telerik.WinControls.UI.RadLabelElement)(this.radLabel2.GetChildAt(0))).TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            ((Telerik.WinControls.UI.RadLabelElement)(this.radLabel2.GetChildAt(0))).Text = "Συναγερμοί Δεξαμενων";
            // 
            // radLabel3
            // 
            this.radLabel3.AutoSize = false;
            this.radLabel3.BackColor = System.Drawing.Color.WhiteSmoke;
            this.radLabel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.radLabel3.Font = new System.Drawing.Font("Segoe UI", 24F);
            this.radLabel3.Location = new System.Drawing.Point(861, 0);
            this.radLabel3.Margin = new System.Windows.Forms.Padding(0);
            this.radLabel3.Name = "radLabel3";
            this.radLabel3.Size = new System.Drawing.Size(432, 74);
            this.radLabel3.TabIndex = 2;
            this.radLabel3.Text = "Γενικοί Συναγερμοί";
            this.radLabel3.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            ((Telerik.WinControls.UI.RadLabelElement)(this.radLabel3.GetChildAt(0))).TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            ((Telerik.WinControls.UI.RadLabelElement)(this.radLabel3.GetChildAt(0))).Text = "Γενικοί Συναγερμοί";
            // 
            // panelDispenser
            // 
            this.panelDispenser.AutoScroll = true;
            this.panelDispenser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelDispenser.Location = new System.Drawing.Point(0, 74);
            this.panelDispenser.Margin = new System.Windows.Forms.Padding(0);
            this.panelDispenser.Name = "panelDispenser";
            this.panelDispenser.Size = new System.Drawing.Size(430, 502);
            this.panelDispenser.TabIndex = 3;
            // 
            // AlertGrid
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "AlertGrid";
            this.Size = new System.Drawing.Size(1293, 576);
            this.SizeChanged += new System.EventHandler(this.AlertGrid_SizeChanged);
            this.VisibleChanged += new System.EventHandler(this.AlertGrid_VisibleChanged);
            this.tableLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.radLabel1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel3)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private Telerik.WinControls.UI.RadLabel radLabel1;
        private System.Windows.Forms.FlowLayoutPanel panelGeneral;
        private System.Windows.Forms.FlowLayoutPanel panelTanks;
        private Telerik.WinControls.UI.RadLabel radLabel2;
        private Telerik.WinControls.UI.RadLabel radLabel3;
        private System.Windows.Forms.FlowLayoutPanel panelDispenser;
    }
}

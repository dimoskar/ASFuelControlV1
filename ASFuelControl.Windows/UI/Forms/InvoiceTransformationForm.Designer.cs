namespace ASFuelControl.Windows.UI.Forms
{
    partial class InvoiceTransformationForm
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            Telerik.WinControls.UI.GridViewTextBoxColumn gridViewTextBoxColumn1 = new Telerik.WinControls.UI.GridViewTextBoxColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn1 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn2 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn3 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn4 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn5 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn6 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            this.panel3 = new System.Windows.Forms.Panel();
            this.btnSelectVehicle = new Telerik.WinControls.UI.RadButton();
            this.btnSave = new Telerik.WinControls.UI.RadButton();
            this.btnSelectTrader = new Telerik.WinControls.UI.RadButton();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.radTextBox4 = new Telerik.WinControls.UI.RadTextBox();
            this.queryInvoiceDataArgsBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.radTextBox3 = new Telerik.WinControls.UI.RadTextBox();
            this.radLabel5 = new Telerik.WinControls.UI.RadLabel();
            this.radLabel6 = new Telerik.WinControls.UI.RadLabel();
            this.radGridView1 = new Telerik.WinControls.UI.RadGridView();
            this.volumesBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.panel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.btnSelectVehicle)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.btnSave)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.btnSelectTrader)).BeginInit();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.radTextBox4)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.queryInvoiceDataArgsBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radTextBox3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel5)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel6)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radGridView1.MasterTemplate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.volumesBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.btnSelectVehicle);
            this.panel3.Controls.Add(this.btnSave);
            this.panel3.Controls.Add(this.btnSelectTrader);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel3.Location = new System.Drawing.Point(0, 0);
            this.panel3.Margin = new System.Windows.Forms.Padding(0);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(694, 40);
            this.panel3.TabIndex = 13;
            // 
            // btnSelectVehicle
            // 
            this.btnSelectVehicle.DisplayStyle = Telerik.WinControls.DisplayStyle.Image;
            this.btnSelectVehicle.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnSelectVehicle.Image = global::ASFuelControl.Windows.Properties.Resources.Vehicle;
            this.btnSelectVehicle.ImageAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            this.btnSelectVehicle.Location = new System.Drawing.Point(40, 0);
            this.btnSelectVehicle.Margin = new System.Windows.Forms.Padding(0);
            this.btnSelectVehicle.Name = "btnSelectVehicle";
            // 
            // 
            // 
            this.btnSelectVehicle.RootElement.UseDefaultDisabledPaint = true;
            this.btnSelectVehicle.Size = new System.Drawing.Size(40, 40);
            this.btnSelectVehicle.TabIndex = 4;
            this.btnSelectVehicle.Text = "radButton1";
            this.btnSelectVehicle.UseCompatibleTextRendering = false;
            this.btnSelectVehicle.Click += new System.EventHandler(this.btnSelectVehicle_Click);
            // 
            // btnSave
            // 
            this.btnSave.DisplayStyle = Telerik.WinControls.DisplayStyle.Image;
            this.btnSave.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnSave.Image = global::ASFuelControl.Windows.Properties.Resources.Save;
            this.btnSave.ImageAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            this.btnSave.Location = new System.Drawing.Point(654, 0);
            this.btnSave.Margin = new System.Windows.Forms.Padding(0);
            this.btnSave.Name = "btnSave";
            // 
            // 
            // 
            this.btnSave.RootElement.UseDefaultDisabledPaint = true;
            this.btnSave.Size = new System.Drawing.Size(40, 40);
            this.btnSave.TabIndex = 3;
            this.btnSave.Text = "radButton2";
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnSelectTrader
            // 
            this.btnSelectTrader.DisplayStyle = Telerik.WinControls.DisplayStyle.Image;
            this.btnSelectTrader.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnSelectTrader.Image = global::ASFuelControl.Windows.Properties.Resources.CustomerSelect36;
            this.btnSelectTrader.ImageAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            this.btnSelectTrader.Location = new System.Drawing.Point(0, 0);
            this.btnSelectTrader.Margin = new System.Windows.Forms.Padding(0);
            this.btnSelectTrader.Name = "btnSelectTrader";
            // 
            // 
            // 
            this.btnSelectTrader.RootElement.UseDefaultDisabledPaint = true;
            this.btnSelectTrader.Size = new System.Drawing.Size(40, 40);
            this.btnSelectTrader.TabIndex = 2;
            this.btnSelectTrader.Text = "radButton1";
            this.btnSelectTrader.UseCompatibleTextRendering = false;
            this.btnSelectTrader.Click += new System.EventHandler(this.btnSelectTrader_Click);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 8;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 22F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 48F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.tableLayoutPanel1.Controls.Add(this.radTextBox4, 5, 0);
            this.tableLayoutPanel1.Controls.Add(this.radTextBox3, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.radLabel5, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.radLabel6, 4, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 40);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(694, 30);
            this.tableLayoutPanel1.TabIndex = 46;
            // 
            // radTextBox4
            // 
            this.radTextBox4.AutoSize = false;
            this.tableLayoutPanel1.SetColumnSpan(this.radTextBox4, 3);
            this.radTextBox4.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.queryInvoiceDataArgsBindingSource, "PlateNumber", true));
            this.radTextBox4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.radTextBox4.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.radTextBox4.Location = new System.Drawing.Point(527, 1);
            this.radTextBox4.Margin = new System.Windows.Forms.Padding(1);
            this.radTextBox4.Name = "radTextBox4";
            this.radTextBox4.ReadOnly = true;
            this.radTextBox4.Size = new System.Drawing.Size(166, 23);
            this.radTextBox4.TabIndex = 7;
            // 
            // queryInvoiceDataArgsBindingSource
            // 
            this.queryInvoiceDataArgsBindingSource.DataSource = typeof(ASFuelControl.Windows.ViewModels.QueryInvoiceDataArgs);
            // 
            // radTextBox3
            // 
            this.radTextBox3.AutoSize = false;
            this.tableLayoutPanel1.SetColumnSpan(this.radTextBox3, 3);
            this.radTextBox3.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.queryInvoiceDataArgsBindingSource, "TraderName", true));
            this.radTextBox3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.radTextBox3.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.radTextBox3.Location = new System.Drawing.Point(111, 1);
            this.radTextBox3.Margin = new System.Windows.Forms.Padding(1);
            this.radTextBox3.Name = "radTextBox3";
            this.radTextBox3.ReadOnly = true;
            this.radTextBox3.Size = new System.Drawing.Size(344, 23);
            this.radTextBox3.TabIndex = 6;
            // 
            // radLabel5
            // 
            this.radLabel5.AutoSize = false;
            this.radLabel5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.radLabel5.Location = new System.Drawing.Point(3, 3);
            this.radLabel5.Name = "radLabel5";
            this.radLabel5.Size = new System.Drawing.Size(104, 19);
            this.radLabel5.TabIndex = 8;
            this.radLabel5.Text = "Συναλλασσόμενος";
            this.radLabel5.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // radLabel6
            // 
            this.radLabel6.AutoSize = false;
            this.radLabel6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.radLabel6.Location = new System.Drawing.Point(459, 3);
            this.radLabel6.Name = "radLabel6";
            this.radLabel6.Size = new System.Drawing.Size(64, 19);
            this.radLabel6.TabIndex = 10;
            this.radLabel6.Text = "Όχημα";
            this.radLabel6.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // radGridView1
            // 
            this.radGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.radGridView1.Location = new System.Drawing.Point(0, 70);
            // 
            // radGridView1
            // 
            this.radGridView1.MasterTemplate.AllowAddNewRow = false;
            this.radGridView1.MasterTemplate.AllowDeleteRow = false;
            this.radGridView1.MasterTemplate.AutoGenerateColumns = false;
            gridViewTextBoxColumn1.FieldName = "Description";
            gridViewTextBoxColumn1.HeaderText = "Καύσιμο";
            gridViewTextBoxColumn1.IsAutoGenerated = true;
            gridViewTextBoxColumn1.Name = "Description";
            gridViewTextBoxColumn1.ReadOnly = true;
            gridViewTextBoxColumn1.Width = 200;
            gridViewDecimalColumn1.FieldName = "Volume";
            gridViewDecimalColumn1.HeaderText = "Όγκος Μετασχ.";
            gridViewDecimalColumn1.IsAutoGenerated = true;
            gridViewDecimalColumn1.Name = "Volume";
            gridViewDecimalColumn1.Width = 80;
            gridViewDecimalColumn2.DecimalPlaces = 5;
            gridViewDecimalColumn2.FieldName = "UnitPrice";
            gridViewDecimalColumn2.FormatString = "{0:N5}";
            gridViewDecimalColumn2.HeaderText = "Τιμή Μονάδος";
            gridViewDecimalColumn2.IsAutoGenerated = true;
            gridViewDecimalColumn2.Name = "UnitPrice";
            gridViewDecimalColumn2.Width = 80;
            gridViewDecimalColumn3.FieldName = "DiscountAmount";
            gridViewDecimalColumn3.HeaderText = "Έκπτωση";
            gridViewDecimalColumn3.IsAutoGenerated = true;
            gridViewDecimalColumn3.Name = "DiscountAmount";
            gridViewDecimalColumn3.Width = 80;
            gridViewDecimalColumn4.FieldName = "TaxValue";
            gridViewDecimalColumn4.HeaderText = "ΦΠΑ";
            gridViewDecimalColumn4.IsAutoGenerated = true;
            gridViewDecimalColumn4.Name = "TaxValue";
            gridViewDecimalColumn4.ReadOnly = true;
            gridViewDecimalColumn4.Width = 80;
            gridViewDecimalColumn5.FieldName = "VatPercentage";
            gridViewDecimalColumn5.HeaderText = "ΦΠΑ %";
            gridViewDecimalColumn5.IsAutoGenerated = true;
            gridViewDecimalColumn5.Name = "VatPercentage";
            gridViewDecimalColumn5.ReadOnly = true;
            gridViewDecimalColumn5.Width = 60;
            gridViewDecimalColumn6.FieldName = "MaxAllowedVolume";
            gridViewDecimalColumn6.HeaderText = "Επιτρεπόμενος Όγκος";
            gridViewDecimalColumn6.IsAutoGenerated = true;
            gridViewDecimalColumn6.Name = "MaxAllowedVolume";
            gridViewDecimalColumn6.ReadOnly = true;
            gridViewDecimalColumn6.Width = 80;
            this.radGridView1.MasterTemplate.Columns.AddRange(new Telerik.WinControls.UI.GridViewDataColumn[] {
            gridViewTextBoxColumn1,
            gridViewDecimalColumn1,
            gridViewDecimalColumn2,
            gridViewDecimalColumn3,
            gridViewDecimalColumn4,
            gridViewDecimalColumn5,
            gridViewDecimalColumn6});
            this.radGridView1.MasterTemplate.DataSource = this.volumesBindingSource;
            this.radGridView1.MasterTemplate.EnableGrouping = false;
            this.radGridView1.MasterTemplate.ShowRowHeaderColumn = false;
            this.radGridView1.Name = "radGridView1";
            this.radGridView1.Size = new System.Drawing.Size(694, 380);
            this.radGridView1.TabIndex = 47;
            this.radGridView1.Text = "radGridView1";
            // 
            // volumesBindingSource
            // 
            this.volumesBindingSource.DataMember = "Volumes";
            this.volumesBindingSource.DataSource = this.queryInvoiceDataArgsBindingSource;
            // 
            // InvoiceTransformationForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(694, 450);
            this.Controls.Add(this.radGridView1);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.panel3);
            this.Name = "InvoiceTransformationForm";
            // 
            // 
            // 
            this.RootElement.ApplyShapeToControl = true;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "InvoiceTransformationForm";
            this.panel3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.btnSelectVehicle)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.btnSave)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.btnSelectTrader)).EndInit();
            this.tableLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.radTextBox4)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.queryInvoiceDataArgsBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radTextBox3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel5)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel6)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radGridView1.MasterTemplate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radGridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.volumesBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel3;
        private Telerik.WinControls.UI.RadButton btnSelectVehicle;
        private Telerik.WinControls.UI.RadButton btnSave;
        private Telerik.WinControls.UI.RadButton btnSelectTrader;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private Telerik.WinControls.UI.RadTextBox radTextBox4;
        private Telerik.WinControls.UI.RadTextBox radTextBox3;
        private Telerik.WinControls.UI.RadLabel radLabel5;
        private Telerik.WinControls.UI.RadLabel radLabel6;
        private System.Windows.Forms.BindingSource queryInvoiceDataArgsBindingSource;
        private Telerik.WinControls.UI.RadGridView radGridView1;
        private System.Windows.Forms.BindingSource volumesBindingSource;
    }
}
namespace ASFuelControl.Windows.UI.SettingForms
{
    partial class InvoiceTypesForm
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
            Telerik.WinControls.UI.GridViewTextBoxColumn gridViewTextBoxColumn2 = new Telerik.WinControls.UI.GridViewTextBoxColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn1 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn2 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.GridViewCheckBoxColumn gridViewCheckBoxColumn1 = new Telerik.WinControls.UI.GridViewCheckBoxColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn3 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.GridViewTextBoxColumn gridViewTextBoxColumn3 = new Telerik.WinControls.UI.GridViewTextBoxColumn();
            Telerik.WinControls.UI.GridViewCheckBoxColumn gridViewCheckBoxColumn2 = new Telerik.WinControls.UI.GridViewCheckBoxColumn();
            Telerik.WinControls.UI.GridViewTextBoxColumn gridViewTextBoxColumn4 = new Telerik.WinControls.UI.GridViewTextBoxColumn();
            Telerik.WinControls.UI.GridViewCommandColumn gridViewCommandColumn1 = new Telerik.WinControls.UI.GridViewCommandColumn();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InvoiceTypesForm));
            this.panel1 = new System.Windows.Forms.Panel();
            this.radButton3 = new Telerik.WinControls.UI.RadButton();
            this.radButton2 = new Telerik.WinControls.UI.RadButton();
            this.radButton1 = new Telerik.WinControls.UI.RadButton();
            this.invoiceTypeBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.invoiceTypeRadGridView = new Telerik.WinControls.UI.RadGridView();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.radButton3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.invoiceTypeBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.invoiceTypeRadGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.invoiceTypeRadGridView.MasterTemplate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.radButton3);
            this.panel1.Controls.Add(this.radButton2);
            this.panel1.Controls.Add(this.radButton1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1184, 42);
            this.panel1.TabIndex = 4;
            // 
            // radButton3
            // 
            this.radButton3.Dock = System.Windows.Forms.DockStyle.Left;
            this.radButton3.Image = global::ASFuelControl.Windows.Properties.Resources.Delete;
            this.radButton3.Location = new System.Drawing.Point(141, 0);
            this.radButton3.Name = "radButton3";
            this.radButton3.Size = new System.Drawing.Size(141, 40);
            this.radButton3.TabIndex = 3;
            this.radButton3.Text = "Διαγραφή";
            this.radButton3.Click += new System.EventHandler(this.radButton3_Click);
            // 
            // radButton2
            // 
            this.radButton2.Dock = System.Windows.Forms.DockStyle.Left;
            this.radButton2.Image = global::ASFuelControl.Windows.Properties.Resources.Add;
            this.radButton2.Location = new System.Drawing.Point(0, 0);
            this.radButton2.Name = "radButton2";
            this.radButton2.Size = new System.Drawing.Size(141, 40);
            this.radButton2.TabIndex = 2;
            this.radButton2.Text = "Προσθήκη";
            this.radButton2.Click += new System.EventHandler(this.radButton2_Click);
            // 
            // radButton1
            // 
            this.radButton1.Dock = System.Windows.Forms.DockStyle.Right;
            this.radButton1.Enabled = false;
            this.radButton1.Image = global::ASFuelControl.Windows.Properties.Resources.Save;
            this.radButton1.Location = new System.Drawing.Point(1041, 0);
            this.radButton1.Name = "radButton1";
            this.radButton1.Size = new System.Drawing.Size(141, 40);
            this.radButton1.TabIndex = 0;
            this.radButton1.Text = "Αποθήκευση";
            this.radButton1.Click += new System.EventHandler(this.radButton1_Click);
            // 
            // invoiceTypeBindingSource
            // 
            this.invoiceTypeBindingSource.DataSource = typeof(ASFuelControl.Data.InvoiceType);
            // 
            // invoiceTypeRadGridView
            // 
            this.invoiceTypeRadGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.invoiceTypeRadGridView.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.invoiceTypeRadGridView.Location = new System.Drawing.Point(0, 42);
            // 
            // invoiceTypeRadGridView
            // 
            this.invoiceTypeRadGridView.MasterTemplate.AllowAddNewRow = false;
            this.invoiceTypeRadGridView.MasterTemplate.AllowDeleteRow = false;
            this.invoiceTypeRadGridView.MasterTemplate.AutoGenerateColumns = false;
            gridViewTextBoxColumn1.FieldName = "Description";
            gridViewTextBoxColumn1.HeaderText = "Περιγραφή";
            gridViewTextBoxColumn1.IsAutoGenerated = true;
            gridViewTextBoxColumn1.Name = "Description";
            gridViewTextBoxColumn1.Width = 250;
            gridViewTextBoxColumn2.FieldName = "Abbreviation";
            gridViewTextBoxColumn2.HeaderText = "Συντόμευση";
            gridViewTextBoxColumn2.IsAutoGenerated = true;
            gridViewTextBoxColumn2.Name = "Abbreviation";
            gridViewTextBoxColumn2.Width = 120;
            gridViewDecimalColumn1.DataType = typeof(int);
            gridViewDecimalColumn1.FieldName = "LastNumber";
            gridViewDecimalColumn1.HeaderText = "Αρ. Εκτύπωσης";
            gridViewDecimalColumn1.IsAutoGenerated = true;
            gridViewDecimalColumn1.Name = "LastNumber";
            gridViewDecimalColumn1.Width = 120;
            gridViewDecimalColumn2.DataType = typeof(int);
            gridViewDecimalColumn2.FieldName = "TransactionType";
            gridViewDecimalColumn2.HeaderText = "Τυπος Κίνησης";
            gridViewDecimalColumn2.IsAutoGenerated = true;
            gridViewDecimalColumn2.Name = "TransactionType";
            gridViewDecimalColumn2.Width = 120;
            gridViewCheckBoxColumn1.FieldName = "Printable";
            gridViewCheckBoxColumn1.HeaderText = "Εκτυπώσιμο";
            gridViewCheckBoxColumn1.IsAutoGenerated = true;
            gridViewCheckBoxColumn1.Name = "Printable";
            gridViewCheckBoxColumn1.Width = 100;
            gridViewDecimalColumn3.DataType = typeof(int);
            gridViewDecimalColumn3.FieldName = "OfficialEnumerator";
            gridViewDecimalColumn3.HeaderText = "Τύπος Παραστατικού ΓΓΠΣ";
            gridViewDecimalColumn3.IsAutoGenerated = true;
            gridViewDecimalColumn3.Name = "OfficialEnumerator";
            gridViewDecimalColumn3.Width = 170;
            gridViewTextBoxColumn3.FieldName = "Printer";
            gridViewTextBoxColumn3.HeaderText = "Εκτυπωτής";
            gridViewTextBoxColumn3.IsAutoGenerated = true;
            gridViewTextBoxColumn3.Name = "Printer";
            gridViewTextBoxColumn3.ReadOnly = true;
            gridViewTextBoxColumn3.Width = 120;
            gridViewCheckBoxColumn2.DataType = typeof(System.Nullable<bool>);
            gridViewCheckBoxColumn2.FieldName = "IsInternal";
            gridViewCheckBoxColumn2.HeaderText = "Εσωτ. Παραστ.";
            gridViewCheckBoxColumn2.Name = "IsInternal";
            gridViewCheckBoxColumn2.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            gridViewCheckBoxColumn2.Width = 100;
            gridViewTextBoxColumn4.FieldName = "InternalDeliveryDescription";
            gridViewTextBoxColumn4.HeaderText = "Περιγραφή Παραλαβής / Εξαγωγής";
            gridViewTextBoxColumn4.Name = "InternalDeliveryDescription";
            gridViewTextBoxColumn4.Width = 200;
            gridViewCommandColumn1.DefaultText = "...";
            gridViewCommandColumn1.Expression = "";
            gridViewCommandColumn1.HeaderText = "";
            gridViewCommandColumn1.Name = "column1";
            gridViewCommandColumn1.UseDefaultText = true;
            this.invoiceTypeRadGridView.MasterTemplate.Columns.AddRange(new Telerik.WinControls.UI.GridViewDataColumn[] {
            gridViewTextBoxColumn1,
            gridViewTextBoxColumn2,
            gridViewDecimalColumn1,
            gridViewDecimalColumn2,
            gridViewCheckBoxColumn1,
            gridViewDecimalColumn3,
            gridViewTextBoxColumn3,
            gridViewCheckBoxColumn2,
            gridViewTextBoxColumn4,
            gridViewCommandColumn1});
            this.invoiceTypeRadGridView.MasterTemplate.DataSource = this.invoiceTypeBindingSource;
            this.invoiceTypeRadGridView.Name = "invoiceTypeRadGridView";
            this.invoiceTypeRadGridView.ShowGroupPanel = false;
            this.invoiceTypeRadGridView.Size = new System.Drawing.Size(1184, 515);
            this.invoiceTypeRadGridView.TabIndex = 5;
            this.invoiceTypeRadGridView.Text = "radGridView1";
            this.invoiceTypeRadGridView.CommandCellClick += new Telerik.WinControls.UI.CommandCellClickEventHandler(this.invoiceTypeRadGridView_CommandCellClick);
            // 
            // InvoiceTypesForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1184, 557);
            this.Controls.Add(this.invoiceTypeRadGridView);
            this.Controls.Add(this.panel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "InvoiceTypesForm";
            // 
            // 
            // 
            this.RootElement.ApplyShapeToControl = true;
            this.Text = "InvoiceTypesForm";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.InvoiceTypesForm_FormClosing);
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.radButton3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.invoiceTypeBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.invoiceTypeRadGridView.MasterTemplate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.invoiceTypeRadGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private Telerik.WinControls.UI.RadButton radButton3;
        private Telerik.WinControls.UI.RadButton radButton2;
        private Telerik.WinControls.UI.RadButton radButton1;
        private System.Windows.Forms.BindingSource invoiceTypeBindingSource;
        private Telerik.WinControls.UI.RadGridView invoiceTypeRadGridView;
    }
}
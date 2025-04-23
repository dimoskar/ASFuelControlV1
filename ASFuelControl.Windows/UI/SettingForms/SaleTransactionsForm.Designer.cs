namespace ASFuelControl.Windows.UI.SettingForms
{
    partial class SaleTransactionsForm
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
            Telerik.WinControls.UI.GridViewDateTimeColumn gridViewDateTimeColumn1 = new Telerik.WinControls.UI.GridViewDateTimeColumn();
            Telerik.WinControls.UI.GridViewTextBoxColumn gridViewTextBoxColumn1 = new Telerik.WinControls.UI.GridViewTextBoxColumn();
            Telerik.WinControls.UI.GridViewTextBoxColumn gridViewTextBoxColumn2 = new Telerik.WinControls.UI.GridViewTextBoxColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn1 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn2 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn3 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn4 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn5 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn6 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn7 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn8 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn9 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn10 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn11 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn12 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn13 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn14 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn15 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn16 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn17 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SaleTransactionsForm));
            this.saleDataViewBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.saleDataViewRadGridView = new Telerik.WinControls.UI.RadGridView();
            this.radButton2 = new Telerik.WinControls.UI.RadButton();
            this.radLabel2 = new Telerik.WinControls.UI.RadLabel();
            this.radLabel1 = new Telerik.WinControls.UI.RadLabel();
            this.radDateTimePicker2 = new Telerik.WinControls.UI.RadDateTimePicker();
            this.radDateTimePicker1 = new Telerik.WinControls.UI.RadDateTimePicker();
            this.radButton1 = new Telerik.WinControls.UI.RadButton();
            this.radButton3 = new Telerik.WinControls.UI.RadButton();
            this.radButton4 = new Telerik.WinControls.UI.RadButton();
            ((System.ComponentModel.ISupportInitialize)(this.saleDataViewBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.saleDataViewRadGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.saleDataViewRadGridView.MasterTemplate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDateTimePicker2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDateTimePicker1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton4)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();
            // 
            // saleDataViewBindingSource
            // 
            this.saleDataViewBindingSource.DataSource = typeof(ASFuelControl.Data.SaleDataView);
            // 
            // saleDataViewRadGridView
            // 
            this.saleDataViewRadGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.saleDataViewRadGridView.Location = new System.Drawing.Point(12, 75);
            // 
            // saleDataViewRadGridView
            // 
            this.saleDataViewRadGridView.MasterTemplate.AllowAddNewRow = false;
            this.saleDataViewRadGridView.MasterTemplate.AllowDeleteRow = false;
            this.saleDataViewRadGridView.MasterTemplate.AllowEditRow = false;
            this.saleDataViewRadGridView.MasterTemplate.AutoGenerateColumns = false;
            gridViewDateTimeColumn1.CustomFormat = "dd/MM/yyyy HH:mm";
            gridViewDateTimeColumn1.FieldName = "TransactionTimeStamp";
            gridViewDateTimeColumn1.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            gridViewDateTimeColumn1.FormatString = "{0:dd/MM/yyyy HH:mm}";
            gridViewDateTimeColumn1.HeaderText = "Ημερομηνία";
            gridViewDateTimeColumn1.IsAutoGenerated = true;
            gridViewDateTimeColumn1.IsPinned = true;
            gridViewDateTimeColumn1.Name = "TransactionTimeStamp";
            gridViewDateTimeColumn1.PinPosition = Telerik.WinControls.UI.PinnedColumnPosition.Left;
            gridViewDateTimeColumn1.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            gridViewDateTimeColumn1.Width = 110;
            gridViewTextBoxColumn1.FieldName = "CurrentTankDescription";
            gridViewTextBoxColumn1.HeaderText = "Δεξαμενή";
            gridViewTextBoxColumn1.IsPinned = true;
            gridViewTextBoxColumn1.Name = "CurrentTankDescription";
            gridViewTextBoxColumn1.PinPosition = Telerik.WinControls.UI.PinnedColumnPosition.Left;
            gridViewTextBoxColumn1.Width = 120;
            gridViewTextBoxColumn2.FieldName = "CurrentNozzle";
            gridViewTextBoxColumn2.HeaderText = "Ακροσωλήνιο";
            gridViewTextBoxColumn2.IsPinned = true;
            gridViewTextBoxColumn2.Name = "CurrentNozzle";
            gridViewTextBoxColumn2.PinPosition = Telerik.WinControls.UI.PinnedColumnPosition.Left;
            gridViewTextBoxColumn2.Width = 120;
            gridViewDecimalColumn1.DataType = typeof(int);
            gridViewDecimalColumn1.DecimalPlaces = 0;
            gridViewDecimalColumn1.FieldName = "Number";
            gridViewDecimalColumn1.FormatString = "{0:N0}";
            gridViewDecimalColumn1.HeaderText = "Αριθμός Παραστατικού";
            gridViewDecimalColumn1.IsAutoGenerated = true;
            gridViewDecimalColumn1.IsPinned = true;
            gridViewDecimalColumn1.Name = "Number";
            gridViewDecimalColumn1.PinPosition = Telerik.WinControls.UI.PinnedColumnPosition.Left;
            gridViewDecimalColumn1.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            gridViewDecimalColumn1.Width = 80;
            gridViewDecimalColumn2.FieldName = "InvoiceLineVolume";
            gridViewDecimalColumn2.FormatString = "{0:N2}";
            gridViewDecimalColumn2.HeaderText = "Όγκος Παραστ.";
            gridViewDecimalColumn2.IsAutoGenerated = true;
            gridViewDecimalColumn2.IsPinned = true;
            gridViewDecimalColumn2.Name = "InvoiceLineVolume";
            gridViewDecimalColumn2.PinPosition = Telerik.WinControls.UI.PinnedColumnPosition.Left;
            gridViewDecimalColumn2.Width = 80;
            gridViewDecimalColumn3.DataType = typeof(System.Nullable<decimal>);
            gridViewDecimalColumn3.FieldName = "NettoAmount";
            gridViewDecimalColumn3.FormatString = "{0:N2}";
            gridViewDecimalColumn3.HeaderText = "Ποσό";
            gridViewDecimalColumn3.IsAutoGenerated = true;
            gridViewDecimalColumn3.IsPinned = true;
            gridViewDecimalColumn3.Name = "NettoAmount";
            gridViewDecimalColumn3.PinPosition = Telerik.WinControls.UI.PinnedColumnPosition.Left;
            gridViewDecimalColumn3.Width = 80;
            gridViewDecimalColumn4.DataType = typeof(System.Nullable<decimal>);
            gridViewDecimalColumn4.FieldName = "VatAmount";
            gridViewDecimalColumn4.FormatString = "{0:N2}";
            gridViewDecimalColumn4.HeaderText = "Φ.Π.Α.";
            gridViewDecimalColumn4.IsAutoGenerated = true;
            gridViewDecimalColumn4.IsPinned = true;
            gridViewDecimalColumn4.Name = "VatAmount";
            gridViewDecimalColumn4.PinPosition = Telerik.WinControls.UI.PinnedColumnPosition.Left;
            gridViewDecimalColumn4.Width = 80;
            gridViewDecimalColumn5.DataType = typeof(System.Nullable<decimal>);
            gridViewDecimalColumn5.FieldName = "TotalAmount";
            gridViewDecimalColumn5.FormatString = "{0:N2}";
            gridViewDecimalColumn5.HeaderText = "Σϋνολο";
            gridViewDecimalColumn5.IsAutoGenerated = true;
            gridViewDecimalColumn5.IsPinned = true;
            gridViewDecimalColumn5.Name = "TotalAmount";
            gridViewDecimalColumn5.PinPosition = Telerik.WinControls.UI.PinnedColumnPosition.Left;
            gridViewDecimalColumn5.Width = 80;
            gridViewDecimalColumn6.FieldName = "TotalizerStart";
            gridViewDecimalColumn6.FormatString = "{0:N2}";
            gridViewDecimalColumn6.HeaderText = "Εναρξη Totals";
            gridViewDecimalColumn6.IsAutoGenerated = true;
            gridViewDecimalColumn6.Name = "TotalizerStart";
            gridViewDecimalColumn6.Width = 80;
            gridViewDecimalColumn7.FieldName = "TotalizerEnd";
            gridViewDecimalColumn7.FormatString = "{0:N2}";
            gridViewDecimalColumn7.HeaderText = "Λήξη Totals";
            gridViewDecimalColumn7.IsAutoGenerated = true;
            gridViewDecimalColumn7.Name = "TotalizerEnd";
            gridViewDecimalColumn7.Width = 80;
            gridViewDecimalColumn8.FieldName = "TotalizerDiff";
            gridViewDecimalColumn8.HeaderText = "Διαφορά";
            gridViewDecimalColumn8.Name = "TotalizerDiff";
            gridViewDecimalColumn8.Width = 80;
            gridViewDecimalColumn9.FieldName = "UnitPrice";
            gridViewDecimalColumn9.FormatString = "{0:N2}";
            gridViewDecimalColumn9.HeaderText = "Τιμή Μονάδος";
            gridViewDecimalColumn9.IsAutoGenerated = true;
            gridViewDecimalColumn9.Name = "UnitPrice";
            gridViewDecimalColumn9.Width = 80;
            gridViewDecimalColumn10.FieldName = "Volume";
            gridViewDecimalColumn10.FormatString = "{0:N2}";
            gridViewDecimalColumn10.HeaderText = "\'Ογκος Πώλησης";
            gridViewDecimalColumn10.IsAutoGenerated = true;
            gridViewDecimalColumn10.Name = "Volume";
            gridViewDecimalColumn10.Width = 80;
            gridViewDecimalColumn11.FieldName = "TotalPrice";
            gridViewDecimalColumn11.FormatString = "{0:N2}";
            gridViewDecimalColumn11.HeaderText = "Σϋνολο Πώλησης";
            gridViewDecimalColumn11.IsAutoGenerated = true;
            gridViewDecimalColumn11.Name = "TotalPrice";
            gridViewDecimalColumn11.Width = 80;
            gridViewDecimalColumn12.FieldName = "StartLevel";
            gridViewDecimalColumn12.FormatString = "{0:N2}";
            gridViewDecimalColumn12.HeaderText = "Στάθμη Έναρξης";
            gridViewDecimalColumn12.IsAutoGenerated = true;
            gridViewDecimalColumn12.Name = "StartLevel";
            gridViewDecimalColumn12.Width = 80;
            gridViewDecimalColumn13.DataType = typeof(System.Nullable<decimal>);
            gridViewDecimalColumn13.FieldName = "EndLevel";
            gridViewDecimalColumn13.FormatString = "{0:N2}";
            gridViewDecimalColumn13.HeaderText = "Στάθμη Λήξης";
            gridViewDecimalColumn13.IsAutoGenerated = true;
            gridViewDecimalColumn13.Name = "EndLevel";
            gridViewDecimalColumn13.Width = 80;
            gridViewDecimalColumn14.FieldName = "TankLevelDiff";
            gridViewDecimalColumn14.HeaderText = "Διαφορά";
            gridViewDecimalColumn14.Name = "TankLevelDiff";
            gridViewDecimalColumn14.Width = 80;
            gridViewDecimalColumn15.FieldName = "StartVolume";
            gridViewDecimalColumn15.FormatString = "{0:N2}";
            gridViewDecimalColumn15.HeaderText = "Όγκος Δεξαμ. Πρίν";
            gridViewDecimalColumn15.IsAutoGenerated = true;
            gridViewDecimalColumn15.Name = "StartVolume";
            gridViewDecimalColumn15.Width = 80;
            gridViewDecimalColumn16.DataType = typeof(System.Nullable<decimal>);
            gridViewDecimalColumn16.FieldName = "EndVolume";
            gridViewDecimalColumn16.FormatString = "{0:N2}";
            gridViewDecimalColumn16.HeaderText = "Όγκος Δεξαμ. Μετά";
            gridViewDecimalColumn16.IsAutoGenerated = true;
            gridViewDecimalColumn16.Name = "EndVolume";
            gridViewDecimalColumn16.Width = 80;
            gridViewDecimalColumn17.FieldName = "TankVolumeDiff";
            gridViewDecimalColumn17.HeaderText = "Διαφορά";
            gridViewDecimalColumn17.Name = "TankVolumeDiff";
            gridViewDecimalColumn17.Width = 80;
            this.saleDataViewRadGridView.MasterTemplate.Columns.AddRange(new Telerik.WinControls.UI.GridViewDataColumn[] {
            gridViewDateTimeColumn1,
            gridViewTextBoxColumn1,
            gridViewTextBoxColumn2,
            gridViewDecimalColumn1,
            gridViewDecimalColumn2,
            gridViewDecimalColumn3,
            gridViewDecimalColumn4,
            gridViewDecimalColumn5,
            gridViewDecimalColumn6,
            gridViewDecimalColumn7,
            gridViewDecimalColumn8,
            gridViewDecimalColumn9,
            gridViewDecimalColumn10,
            gridViewDecimalColumn11,
            gridViewDecimalColumn12,
            gridViewDecimalColumn13,
            gridViewDecimalColumn14,
            gridViewDecimalColumn15,
            gridViewDecimalColumn16,
            gridViewDecimalColumn17});
            this.saleDataViewRadGridView.MasterTemplate.DataSource = this.saleDataViewBindingSource;
            this.saleDataViewRadGridView.Name = "saleDataViewRadGridView";
            this.saleDataViewRadGridView.Size = new System.Drawing.Size(946, 395);
            this.saleDataViewRadGridView.TabIndex = 1;
            this.saleDataViewRadGridView.Text = "radGridView1";
            this.saleDataViewRadGridView.ViewCellFormatting += new Telerik.WinControls.UI.CellFormattingEventHandler(this.saleDataViewRadGridView_ViewCellFormatting);
            // 
            // radButton2
            // 
            this.radButton2.Image = global::ASFuelControl.Windows.Properties.Resources.Search_small;
            this.radButton2.ImageAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            this.radButton2.Location = new System.Drawing.Point(347, 29);
            this.radButton2.Name = "radButton2";
            this.radButton2.Size = new System.Drawing.Size(31, 24);
            this.radButton2.TabIndex = 10;
            this.radButton2.Click += new System.EventHandler(this.radButton2_Click);
            // 
            // radLabel2
            // 
            this.radLabel2.Location = new System.Drawing.Point(188, 32);
            this.radLabel2.Name = "radLabel2";
            this.radLabel2.Size = new System.Drawing.Size(26, 18);
            this.radLabel2.TabIndex = 9;
            this.radLabel2.Text = "Εώς";
            // 
            // radLabel1
            // 
            this.radLabel1.Location = new System.Drawing.Point(12, 32);
            this.radLabel1.Name = "radLabel1";
            this.radLabel1.Size = new System.Drawing.Size(27, 18);
            this.radLabel1.TabIndex = 8;
            this.radLabel1.Text = "Από";
            // 
            // radDateTimePicker2
            // 
            this.radDateTimePicker2.CustomFormat = "dd/MM/yyyy HH:mm";
            this.radDateTimePicker2.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.radDateTimePicker2.Location = new System.Drawing.Point(221, 31);
            this.radDateTimePicker2.Name = "radDateTimePicker2";
            this.radDateTimePicker2.Size = new System.Drawing.Size(120, 20);
            this.radDateTimePicker2.TabIndex = 7;
            this.radDateTimePicker2.TabStop = false;
            this.radDateTimePicker2.Text = "18/03/2015 20:25";
            this.radDateTimePicker2.Value = new System.DateTime(2015, 3, 18, 20, 25, 46, 664);
            // 
            // radDateTimePicker1
            // 
            this.radDateTimePicker1.CustomFormat = "dd/MM/yyyy HH:mm";
            this.radDateTimePicker1.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.radDateTimePicker1.Location = new System.Drawing.Point(45, 31);
            this.radDateTimePicker1.Name = "radDateTimePicker1";
            this.radDateTimePicker1.Size = new System.Drawing.Size(120, 20);
            this.radDateTimePicker1.TabIndex = 6;
            this.radDateTimePicker1.TabStop = false;
            this.radDateTimePicker1.Text = "18/03/2015 20:25";
            this.radDateTimePicker1.Value = new System.DateTime(2015, 3, 18, 20, 25, 46, 664);
            // 
            // radButton1
            // 
            this.radButton1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.radButton1.Image = global::ASFuelControl.Windows.Properties.Resources.Exit;
            this.radButton1.ImageAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            this.radButton1.Location = new System.Drawing.Point(903, 14);
            this.radButton1.Name = "radButton1";
            this.radButton1.Size = new System.Drawing.Size(55, 55);
            this.radButton1.TabIndex = 11;
            this.radButton1.Click += new System.EventHandler(this.radButton1_Click);
            // 
            // radButton3
            // 
            this.radButton3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.radButton3.Image = global::ASFuelControl.Windows.Properties.Resources.Excel_Export;
            this.radButton3.ImageAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            this.radButton3.Location = new System.Drawing.Point(842, 14);
            this.radButton3.Name = "radButton3";
            this.radButton3.Size = new System.Drawing.Size(55, 55);
            this.radButton3.TabIndex = 12;
            this.radButton3.Click += new System.EventHandler(this.radButton3_Click);
            // 
            // radButton4
            // 
            this.radButton4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.radButton4.Image = global::ASFuelControl.Windows.Properties.Resources.Adobe_PDF_Export;
            this.radButton4.ImageAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            this.radButton4.Location = new System.Drawing.Point(781, 14);
            this.radButton4.Name = "radButton4";
            this.radButton4.Size = new System.Drawing.Size(55, 55);
            this.radButton4.TabIndex = 13;
            this.radButton4.Click += new System.EventHandler(this.radButton4_Click);
            // 
            // SaleTransactionsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(970, 482);
            this.Controls.Add(this.radButton4);
            this.Controls.Add(this.radButton3);
            this.Controls.Add(this.radButton1);
            this.Controls.Add(this.radButton2);
            this.Controls.Add(this.radLabel2);
            this.Controls.Add(this.radLabel1);
            this.Controls.Add(this.radDateTimePicker2);
            this.Controls.Add(this.radDateTimePicker1);
            this.Controls.Add(this.saleDataViewRadGridView);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(978, 512);
            this.Name = "SaleTransactionsForm";
            // 
            // 
            // 
            this.RootElement.ApplyShapeToControl = true;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Πωλήσεις";
            ((System.ComponentModel.ISupportInitialize)(this.saleDataViewBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.saleDataViewRadGridView.MasterTemplate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.saleDataViewRadGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDateTimePicker2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDateTimePicker1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton4)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.BindingSource saleDataViewBindingSource;
        private Telerik.WinControls.UI.RadGridView saleDataViewRadGridView;
        private Telerik.WinControls.UI.RadButton radButton2;
        private Telerik.WinControls.UI.RadLabel radLabel2;
        private Telerik.WinControls.UI.RadLabel radLabel1;
        private Telerik.WinControls.UI.RadDateTimePicker radDateTimePicker2;
        private Telerik.WinControls.UI.RadDateTimePicker radDateTimePicker1;
        private Telerik.WinControls.UI.RadButton radButton1;
        private Telerik.WinControls.UI.RadButton radButton3;
        private Telerik.WinControls.UI.RadButton radButton4;
    }
}
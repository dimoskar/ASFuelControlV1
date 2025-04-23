namespace ASFuelControl.Windows.UI.Forms
{
    partial class InvalidSaleForm
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
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn1 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn2 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn3 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.GridViewTextBoxColumn gridViewTextBoxColumn1 = new Telerik.WinControls.UI.GridViewTextBoxColumn();
            Telerik.WinControls.UI.GridViewDateTimeColumn gridViewDateTimeColumn1 = new Telerik.WinControls.UI.GridViewDateTimeColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn4 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn5 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn6 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InvalidSaleForm));
            this.salesTransactionBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.salesTransactionRadGridView = new Telerik.WinControls.UI.RadGridView();
            this.radLabel1 = new Telerik.WinControls.UI.RadLabel();
            this.radGroupBox1 = new Telerik.WinControls.UI.RadGroupBox();
            this.priceTotal1 = new Telerik.WinControls.UI.RadSpinEditor();
            this.unitPrice1 = new Telerik.WinControls.UI.RadSpinEditor();
            this.volume1 = new Telerik.WinControls.UI.RadSpinEditor();
            this.radLabel3 = new Telerik.WinControls.UI.RadLabel();
            this.radLabel2 = new Telerik.WinControls.UI.RadLabel();
            this.radGroupBox2 = new Telerik.WinControls.UI.RadGroupBox();
            this.priceTotal2 = new Telerik.WinControls.UI.RadSpinEditor();
            this.unitPrice2 = new Telerik.WinControls.UI.RadSpinEditor();
            this.volume2 = new Telerik.WinControls.UI.RadSpinEditor();
            this.radLabel4 = new Telerik.WinControls.UI.RadLabel();
            this.radLabel5 = new Telerik.WinControls.UI.RadLabel();
            this.radLabel6 = new Telerik.WinControls.UI.RadLabel();
            this.radButton1 = new Telerik.WinControls.UI.RadButton();
            this.radRadioButton1 = new Telerik.WinControls.UI.RadRadioButton();
            this.radRadioButton2 = new Telerik.WinControls.UI.RadRadioButton();
            this.radCheckBox1 = new Telerik.WinControls.UI.RadCheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.salesTransactionBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.salesTransactionRadGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.salesTransactionRadGridView.MasterTemplate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radGroupBox1)).BeginInit();
            this.radGroupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.priceTotal1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.unitPrice1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.volume1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radGroupBox2)).BeginInit();
            this.radGroupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.priceTotal2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.unitPrice2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.volume2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel4)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel5)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel6)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radRadioButton1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radRadioButton2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radCheckBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();
            // 
            // salesTransactionBindingSource
            // 
            this.salesTransactionBindingSource.DataSource = typeof(ASFuelControl.Data.SalesTransaction);
            this.salesTransactionBindingSource.PositionChanged += new System.EventHandler(this.salesTransactionBindingSource_PositionChanged);
            // 
            // salesTransactionRadGridView
            // 
            this.salesTransactionRadGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.salesTransactionRadGridView.Location = new System.Drawing.Point(12, 12);
            // 
            // salesTransactionRadGridView
            // 
            this.salesTransactionRadGridView.MasterTemplate.AllowAddNewRow = false;
            this.salesTransactionRadGridView.MasterTemplate.AllowDeleteRow = false;
            this.salesTransactionRadGridView.MasterTemplate.AllowEditRow = false;
            this.salesTransactionRadGridView.MasterTemplate.AutoGenerateColumns = false;
            gridViewDecimalColumn1.FieldName = "TotalizerStart";
            gridViewDecimalColumn1.HeaderText = "Έναρξη Μετρητή";
            gridViewDecimalColumn1.IsAutoGenerated = true;
            gridViewDecimalColumn1.Name = "TotalizerStart";
            gridViewDecimalColumn1.Width = 90;
            gridViewDecimalColumn2.FieldName = "TotalizerEnd";
            gridViewDecimalColumn2.HeaderText = "Λήξη Μετρητή";
            gridViewDecimalColumn2.IsAutoGenerated = true;
            gridViewDecimalColumn2.Name = "TotalizerEnd";
            gridViewDecimalColumn2.Width = 90;
            gridViewDecimalColumn3.FieldName = "TotalizerDiff";
            gridViewDecimalColumn3.HeaderText = "Διαφορά Μετρητή";
            gridViewDecimalColumn3.Name = "TotalizerDiff";
            gridViewDecimalColumn3.Width = 90;
            gridViewTextBoxColumn1.FieldName = "Nozzle.Description";
            gridViewTextBoxColumn1.HeaderText = "Ακροσωλήνιο";
            gridViewTextBoxColumn1.IsAutoGenerated = true;
            gridViewTextBoxColumn1.Name = "NozzleId";
            gridViewTextBoxColumn1.Width = 80;
            gridViewDateTimeColumn1.CustomFormat = "dd/MM/yy HH:mm";
            gridViewDateTimeColumn1.FieldName = "TransactionTimeStamp";
            gridViewDateTimeColumn1.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            gridViewDateTimeColumn1.HeaderText = "Ημερομηνία / Ώρα";
            gridViewDateTimeColumn1.IsAutoGenerated = true;
            gridViewDateTimeColumn1.Name = "TransactionTimeStamp";
            gridViewDateTimeColumn1.Width = 100;
            gridViewDecimalColumn4.FieldName = "Volume";
            gridViewDecimalColumn4.HeaderText = "Όγκος";
            gridViewDecimalColumn4.IsAutoGenerated = true;
            gridViewDecimalColumn4.Name = "Volume";
            gridViewDecimalColumn4.Width = 80;
            gridViewDecimalColumn5.DecimalPlaces = 3;
            gridViewDecimalColumn5.FieldName = "UnitPrice";
            gridViewDecimalColumn5.HeaderText = "Τιμή Λίτρου";
            gridViewDecimalColumn5.IsAutoGenerated = true;
            gridViewDecimalColumn5.Name = "UnitPrice";
            gridViewDecimalColumn5.Width = 80;
            gridViewDecimalColumn6.FieldName = "TotalPrice";
            gridViewDecimalColumn6.HeaderText = "Σύνολο";
            gridViewDecimalColumn6.IsAutoGenerated = true;
            gridViewDecimalColumn6.Name = "TotalPrice";
            gridViewDecimalColumn6.Width = 80;
            this.salesTransactionRadGridView.MasterTemplate.Columns.AddRange(new Telerik.WinControls.UI.GridViewDataColumn[] {
            gridViewDecimalColumn1,
            gridViewDecimalColumn2,
            gridViewDecimalColumn3,
            gridViewTextBoxColumn1,
            gridViewDateTimeColumn1,
            gridViewDecimalColumn4,
            gridViewDecimalColumn5,
            gridViewDecimalColumn6});
            this.salesTransactionRadGridView.MasterTemplate.DataSource = this.salesTransactionBindingSource;
            this.salesTransactionRadGridView.Name = "salesTransactionRadGridView";
            this.salesTransactionRadGridView.ShowGroupPanel = false;
            this.salesTransactionRadGridView.Size = new System.Drawing.Size(725, 228);
            this.salesTransactionRadGridView.TabIndex = 1;
            this.salesTransactionRadGridView.Text = "radGridView1";
            // 
            // radLabel1
            // 
            this.radLabel1.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radLabel1.Location = new System.Drawing.Point(15, 33);
            this.radLabel1.Name = "radLabel1";
            this.radLabel1.Size = new System.Drawing.Size(55, 25);
            this.radLabel1.TabIndex = 2;
            this.radLabel1.Text = "Όγκος";
            // 
            // radGroupBox1
            // 
            this.radGroupBox1.AccessibleRole = System.Windows.Forms.AccessibleRole.Grouping;
            this.radGroupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.radGroupBox1.Controls.Add(this.priceTotal1);
            this.radGroupBox1.Controls.Add(this.unitPrice1);
            this.radGroupBox1.Controls.Add(this.volume1);
            this.radGroupBox1.Controls.Add(this.radLabel3);
            this.radGroupBox1.Controls.Add(this.radLabel2);
            this.radGroupBox1.Controls.Add(this.radLabel1);
            this.radGroupBox1.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radGroupBox1.HeaderText = "Παραστατικό Βάση Οθόνης";
            this.radGroupBox1.Location = new System.Drawing.Point(12, 246);
            this.radGroupBox1.Name = "radGroupBox1";
            this.radGroupBox1.Size = new System.Drawing.Size(245, 155);
            this.radGroupBox1.TabIndex = 3;
            this.radGroupBox1.Text = "Παραστατικό Βάση Οθόνης";
            // 
            // priceTotal1
            // 
            this.priceTotal1.DecimalPlaces = 2;
            this.priceTotal1.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.priceTotal1.Location = new System.Drawing.Point(117, 72);
            this.priceTotal1.Maximum = new decimal(new int[] {
            999999999,
            0,
            0,
            0});
            this.priceTotal1.Minimum = new decimal(new int[] {
            999999999,
            0,
            0,
            -2147483648});
            this.priceTotal1.Name = "priceTotal1";
            this.priceTotal1.ReadOnly = true;
            this.priceTotal1.Size = new System.Drawing.Size(121, 27);
            this.priceTotal1.TabIndex = 7;
            this.priceTotal1.TabStop = false;
            this.priceTotal1.TextAlignment = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // unitPrice1
            // 
            this.unitPrice1.DecimalPlaces = 3;
            this.unitPrice1.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.unitPrice1.Location = new System.Drawing.Point(117, 113);
            this.unitPrice1.Maximum = new decimal(new int[] {
            999999999,
            0,
            0,
            0});
            this.unitPrice1.Minimum = new decimal(new int[] {
            999999999,
            0,
            0,
            -2147483648});
            this.unitPrice1.Name = "unitPrice1";
            this.unitPrice1.ReadOnly = true;
            this.unitPrice1.Size = new System.Drawing.Size(121, 27);
            this.unitPrice1.TabIndex = 6;
            this.unitPrice1.TabStop = false;
            this.unitPrice1.TextAlignment = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // volume1
            // 
            this.volume1.DecimalPlaces = 2;
            this.volume1.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.volume1.Location = new System.Drawing.Point(117, 31);
            this.volume1.Maximum = new decimal(new int[] {
            999999999,
            0,
            0,
            0});
            this.volume1.Minimum = new decimal(new int[] {
            999999999,
            0,
            0,
            -2147483648});
            this.volume1.Name = "volume1";
            this.volume1.ReadOnly = true;
            this.volume1.Size = new System.Drawing.Size(121, 27);
            this.volume1.TabIndex = 5;
            this.volume1.TabStop = false;
            this.volume1.TextAlignment = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // radLabel3
            // 
            this.radLabel3.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radLabel3.Location = new System.Drawing.Point(15, 73);
            this.radLabel3.Name = "radLabel3";
            this.radLabel3.Size = new System.Drawing.Size(62, 25);
            this.radLabel3.TabIndex = 4;
            this.radLabel3.Text = "Σύνολο";
            // 
            // radLabel2
            // 
            this.radLabel2.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radLabel2.Location = new System.Drawing.Point(15, 114);
            this.radLabel2.Name = "radLabel2";
            this.radLabel2.Size = new System.Drawing.Size(96, 25);
            this.radLabel2.TabIndex = 3;
            this.radLabel2.Text = "Τιμή Λίτρου";
            // 
            // radGroupBox2
            // 
            this.radGroupBox2.AccessibleRole = System.Windows.Forms.AccessibleRole.Grouping;
            this.radGroupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.radGroupBox2.Controls.Add(this.priceTotal2);
            this.radGroupBox2.Controls.Add(this.unitPrice2);
            this.radGroupBox2.Controls.Add(this.volume2);
            this.radGroupBox2.Controls.Add(this.radLabel4);
            this.radGroupBox2.Controls.Add(this.radLabel5);
            this.radGroupBox2.Controls.Add(this.radLabel6);
            this.radGroupBox2.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radGroupBox2.HeaderText = "Παραστ. Βάση Διαφοράς Όγκου";
            this.radGroupBox2.Location = new System.Drawing.Point(267, 247);
            this.radGroupBox2.Name = "radGroupBox2";
            this.radGroupBox2.Size = new System.Drawing.Size(245, 154);
            this.radGroupBox2.TabIndex = 4;
            this.radGroupBox2.Text = "Παραστ. Βάση Διαφοράς Όγκου";
            // 
            // priceTotal2
            // 
            this.priceTotal2.DecimalPlaces = 2;
            this.priceTotal2.Enabled = false;
            this.priceTotal2.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.priceTotal2.Location = new System.Drawing.Point(110, 72);
            this.priceTotal2.Maximum = new decimal(new int[] {
            999999999,
            0,
            0,
            0});
            this.priceTotal2.Minimum = new decimal(new int[] {
            999999999,
            0,
            0,
            -2147483648});
            this.priceTotal2.Name = "priceTotal2";
            this.priceTotal2.ReadOnly = true;
            this.priceTotal2.Size = new System.Drawing.Size(119, 27);
            this.priceTotal2.TabIndex = 13;
            this.priceTotal2.TabStop = false;
            this.priceTotal2.TextAlignment = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // unitPrice2
            // 
            this.unitPrice2.DecimalPlaces = 3;
            this.unitPrice2.Enabled = false;
            this.unitPrice2.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.unitPrice2.Location = new System.Drawing.Point(110, 113);
            this.unitPrice2.Maximum = new decimal(new int[] {
            999999999,
            0,
            0,
            0});
            this.unitPrice2.Minimum = new decimal(new int[] {
            999999999,
            0,
            0,
            -2147483648});
            this.unitPrice2.Name = "unitPrice2";
            this.unitPrice2.ReadOnly = true;
            this.unitPrice2.Size = new System.Drawing.Size(119, 27);
            this.unitPrice2.TabIndex = 12;
            this.unitPrice2.TabStop = false;
            this.unitPrice2.TextAlignment = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // volume2
            // 
            this.volume2.DecimalPlaces = 2;
            this.volume2.Enabled = false;
            this.volume2.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.volume2.Location = new System.Drawing.Point(110, 28);
            this.volume2.Maximum = new decimal(new int[] {
            999999999,
            0,
            0,
            0});
            this.volume2.Minimum = new decimal(new int[] {
            999999999,
            0,
            0,
            -2147483648});
            this.volume2.Name = "volume2";
            this.volume2.ReadOnly = true;
            this.volume2.Size = new System.Drawing.Size(119, 27);
            this.volume2.TabIndex = 11;
            this.volume2.TabStop = false;
            this.volume2.TextAlignment = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // radLabel4
            // 
            this.radLabel4.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radLabel4.Location = new System.Drawing.Point(8, 73);
            this.radLabel4.Name = "radLabel4";
            this.radLabel4.Size = new System.Drawing.Size(62, 25);
            this.radLabel4.TabIndex = 10;
            this.radLabel4.Text = "Σύνολο";
            // 
            // radLabel5
            // 
            this.radLabel5.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radLabel5.Location = new System.Drawing.Point(8, 114);
            this.radLabel5.Name = "radLabel5";
            this.radLabel5.Size = new System.Drawing.Size(96, 25);
            this.radLabel5.TabIndex = 9;
            this.radLabel5.Text = "Τιμή Λίτρου";
            // 
            // radLabel6
            // 
            this.radLabel6.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radLabel6.Location = new System.Drawing.Point(8, 30);
            this.radLabel6.Name = "radLabel6";
            this.radLabel6.Size = new System.Drawing.Size(55, 25);
            this.radLabel6.TabIndex = 8;
            this.radLabel6.Text = "Όγκος";
            // 
            // radButton1
            // 
            this.radButton1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.radButton1.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radButton1.Image = global::ASFuelControl.Windows.Properties.Resources.PrinterBig;
            this.radButton1.ImageAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            this.radButton1.Location = new System.Drawing.Point(518, 247);
            this.radButton1.Name = "radButton1";
            this.radButton1.Padding = new System.Windows.Forms.Padding(0, 0, 10, 0);
            this.radButton1.Size = new System.Drawing.Size(218, 154);
            this.radButton1.TabIndex = 5;
            this.radButton1.Text = "Εκτύπωση Παραστατικού";
            this.radButton1.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.radButton1.Click += new System.EventHandler(this.radButton1_Click);
            // 
            // radRadioButton1
            // 
            this.radRadioButton1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.radRadioButton1.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radRadioButton1.Location = new System.Drawing.Point(242, 248);
            this.radRadioButton1.Name = "radRadioButton1";
            this.radRadioButton1.Size = new System.Drawing.Size(15, 15);
            this.radRadioButton1.TabIndex = 8;
            this.radRadioButton1.TabStop = true;
            this.radRadioButton1.ToggleState = Telerik.WinControls.Enumerations.ToggleState.On;
            this.radRadioButton1.ToggleStateChanged += new Telerik.WinControls.UI.StateChangedEventHandler(this.radRadioButton1_ToggleStateChanged);
            // 
            // radRadioButton2
            // 
            this.radRadioButton2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.radRadioButton2.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radRadioButton2.Location = new System.Drawing.Point(494, 248);
            this.radRadioButton2.Name = "radRadioButton2";
            this.radRadioButton2.Size = new System.Drawing.Size(15, 15);
            this.radRadioButton2.TabIndex = 9;
            // 
            // radCheckBox1
            // 
            this.radCheckBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.radCheckBox1.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radCheckBox1.Location = new System.Drawing.Point(12, 407);
            this.radCheckBox1.Name = "radCheckBox1";
            this.radCheckBox1.Size = new System.Drawing.Size(127, 25);
            this.radCheckBox1.TabIndex = 10;
            this.radCheckBox1.Text = "Λιτρομέτρηση";
            // 
            // InvalidSaleForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(748, 434);
            this.Controls.Add(this.radCheckBox1);
            this.Controls.Add(this.radRadioButton1);
            this.Controls.Add(this.radRadioButton2);
            this.Controls.Add(this.radButton1);
            this.Controls.Add(this.radGroupBox2);
            this.Controls.Add(this.radGroupBox1);
            this.Controls.Add(this.salesTransactionRadGridView);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(756, 438);
            this.Name = "InvalidSaleForm";
            // 
            // 
            // 
            this.RootElement.ApplyShapeToControl = true;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Πωλήσεις με ενδεχόμενο σφάλμα...";
            ((System.ComponentModel.ISupportInitialize)(this.salesTransactionBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.salesTransactionRadGridView.MasterTemplate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.salesTransactionRadGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radGroupBox1)).EndInit();
            this.radGroupBox1.ResumeLayout(false);
            this.radGroupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.priceTotal1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.unitPrice1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.volume1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radGroupBox2)).EndInit();
            this.radGroupBox2.ResumeLayout(false);
            this.radGroupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.priceTotal2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.unitPrice2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.volume2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel4)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel5)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel6)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radRadioButton1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radRadioButton2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radCheckBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.BindingSource salesTransactionBindingSource;
        private Telerik.WinControls.UI.RadGridView salesTransactionRadGridView;
        private Telerik.WinControls.UI.RadLabel radLabel1;
        private Telerik.WinControls.UI.RadGroupBox radGroupBox1;
        private Telerik.WinControls.UI.RadSpinEditor priceTotal1;
        private Telerik.WinControls.UI.RadSpinEditor unitPrice1;
        private Telerik.WinControls.UI.RadSpinEditor volume1;
        private Telerik.WinControls.UI.RadLabel radLabel3;
        private Telerik.WinControls.UI.RadLabel radLabel2;
        private Telerik.WinControls.UI.RadGroupBox radGroupBox2;
        private Telerik.WinControls.UI.RadSpinEditor priceTotal2;
        private Telerik.WinControls.UI.RadSpinEditor unitPrice2;
        private Telerik.WinControls.UI.RadSpinEditor volume2;
        private Telerik.WinControls.UI.RadLabel radLabel4;
        private Telerik.WinControls.UI.RadLabel radLabel5;
        private Telerik.WinControls.UI.RadLabel radLabel6;
        private Telerik.WinControls.UI.RadButton radButton1;
        private Telerik.WinControls.UI.RadRadioButton radRadioButton1;
        private Telerik.WinControls.UI.RadRadioButton radRadioButton2;
        private Telerik.WinControls.UI.RadCheckBox radCheckBox1;
    }
}
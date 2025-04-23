namespace ASFuelControl.Windows.UI.Forms
{
    partial class AlertsForm
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
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn1 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.GridViewTextBoxColumn gridViewTextBoxColumn2 = new Telerik.WinControls.UI.GridViewTextBoxColumn();
            Telerik.WinControls.UI.GridViewDateTimeColumn gridViewDateTimeColumn2 = new Telerik.WinControls.UI.GridViewDateTimeColumn();
            Telerik.WinControls.UI.GridViewDateTimeColumn gridViewDateTimeColumn3 = new Telerik.WinControls.UI.GridViewDateTimeColumn();
            Telerik.WinControls.UI.GridViewTextBoxColumn gridViewTextBoxColumn3 = new Telerik.WinControls.UI.GridViewTextBoxColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn2 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.GridViewTextBoxColumn gridViewTextBoxColumn4 = new Telerik.WinControls.UI.GridViewTextBoxColumn();
            Telerik.WinControls.UI.GridViewTextBoxColumn gridViewTextBoxColumn5 = new Telerik.WinControls.UI.GridViewTextBoxColumn();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AlertsForm));
            this.systemEventBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.systemEventRadGridView = new Telerik.WinControls.UI.RadGridView();
            this.systemEventDatumBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.systemEventDatumRadGridView = new Telerik.WinControls.UI.RadGridView();
            this.radDateTimePicker1 = new Telerik.WinControls.UI.RadDateTimePicker();
            this.radDateTimePicker2 = new Telerik.WinControls.UI.RadDateTimePicker();
            this.radLabel1 = new Telerik.WinControls.UI.RadLabel();
            this.radLabel2 = new Telerik.WinControls.UI.RadLabel();
            this.radButton1 = new Telerik.WinControls.UI.RadButton();
            ((System.ComponentModel.ISupportInitialize)(this.systemEventBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.systemEventRadGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.systemEventRadGridView.MasterTemplate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.systemEventDatumBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.systemEventDatumRadGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.systemEventDatumRadGridView.MasterTemplate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDateTimePicker1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDateTimePicker2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();
            // 
            // systemEventBindingSource
            // 
            this.systemEventBindingSource.DataSource = typeof(ASFuelControl.Data.SystemEvent);
            // 
            // systemEventRadGridView
            // 
            this.systemEventRadGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.systemEventRadGridView.Location = new System.Drawing.Point(12, 39);
            // 
            // systemEventRadGridView
            // 
            this.systemEventRadGridView.MasterTemplate.AllowAddNewRow = false;
            this.systemEventRadGridView.MasterTemplate.AllowDeleteRow = false;
            this.systemEventRadGridView.MasterTemplate.AutoGenerateColumns = false;
            this.systemEventRadGridView.MasterTemplate.AutoSizeColumnsMode = Telerik.WinControls.UI.GridViewAutoSizeColumnsMode.Fill;
            gridViewDateTimeColumn1.FieldName = "EventDate";
            gridViewDateTimeColumn1.FormatString = "{0:dd/MM/yyyy HH:mm}";
            gridViewDateTimeColumn1.HeaderText = "Ημερομηνία";
            gridViewDateTimeColumn1.IsAutoGenerated = true;
            gridViewDateTimeColumn1.Name = "EventDate";
            gridViewDateTimeColumn1.Width = 96;
            gridViewTextBoxColumn1.FieldName = "Description";
            gridViewTextBoxColumn1.HeaderText = "Περιγραφή";
            gridViewTextBoxColumn1.IsAutoGenerated = true;
            gridViewTextBoxColumn1.Name = "Description";
            gridViewTextBoxColumn1.ReadOnly = true;
            gridViewTextBoxColumn1.Width = 192;
            gridViewDecimalColumn1.DataType = typeof(int);
            gridViewDecimalColumn1.FieldName = "EventType";
            gridViewDecimalColumn1.HeaderText = "Τύπος";
            gridViewDecimalColumn1.IsAutoGenerated = true;
            gridViewDecimalColumn1.Name = "EventType";
            gridViewDecimalColumn1.Width = 57;
            gridViewTextBoxColumn2.FieldName = "DeviceDescription";
            gridViewTextBoxColumn2.HeaderText = "Συσκευή Συναγερμού";
            gridViewTextBoxColumn2.IsAutoGenerated = true;
            gridViewTextBoxColumn2.Name = "DeviceDescription";
            gridViewTextBoxColumn2.Width = 144;
            gridViewDateTimeColumn2.DataType = typeof(System.Nullable<System.DateTime>);
            gridViewDateTimeColumn2.FieldName = "SentDate";
            gridViewDateTimeColumn2.FormatString = "{0:dd/MM/yyyy HH:mm}";
            gridViewDateTimeColumn2.HeaderText = "Ημερομ. Αποστολής";
            gridViewDateTimeColumn2.IsAutoGenerated = true;
            gridViewDateTimeColumn2.Name = "SentDate";
            gridViewDateTimeColumn2.Width = 115;
            gridViewDateTimeColumn3.DataType = typeof(System.Nullable<System.DateTime>);
            gridViewDateTimeColumn3.FieldName = "ResolvedDate";
            gridViewDateTimeColumn3.FormatString = "{0:dd/MM/yyyy HH:mm}";
            gridViewDateTimeColumn3.HeaderText = "Ημερ. Επίλυσης";
            gridViewDateTimeColumn3.IsAutoGenerated = true;
            gridViewDateTimeColumn3.Name = "ResolvedDate";
            gridViewDateTimeColumn3.Width = 96;
            gridViewTextBoxColumn3.FieldName = "ResolveMessage";
            gridViewTextBoxColumn3.HeaderText = "Περιγραφή Επίλυσης";
            gridViewTextBoxColumn3.IsAutoGenerated = true;
            gridViewTextBoxColumn3.Name = "ResolveMessage";
            gridViewTextBoxColumn3.Width = 192;
            gridViewDecimalColumn2.DataType = typeof(System.Nullable<int>);
            gridViewDecimalColumn2.FieldName = "AlarmType";
            gridViewDecimalColumn2.HeaderText = "Τύπος";
            gridViewDecimalColumn2.IsAutoGenerated = true;
            gridViewDecimalColumn2.Name = "AlarmType";
            gridViewDecimalColumn2.Width = 47;
            this.systemEventRadGridView.MasterTemplate.Columns.AddRange(new Telerik.WinControls.UI.GridViewDataColumn[] {
            gridViewDateTimeColumn1,
            gridViewTextBoxColumn1,
            gridViewDecimalColumn1,
            gridViewTextBoxColumn2,
            gridViewDateTimeColumn2,
            gridViewDateTimeColumn3,
            gridViewTextBoxColumn3,
            gridViewDecimalColumn2});
            this.systemEventRadGridView.MasterTemplate.DataSource = this.systemEventBindingSource;
            this.systemEventRadGridView.Name = "systemEventRadGridView";
            this.systemEventRadGridView.ReadOnly = true;
            this.systemEventRadGridView.Size = new System.Drawing.Size(953, 283);
            this.systemEventRadGridView.TabIndex = 1;
            this.systemEventRadGridView.Text = "radGridView1";
            // 
            // systemEventDatumBindingSource
            // 
            this.systemEventDatumBindingSource.DataSource = typeof(ASFuelControl.Data.SystemEventDatum);
            // 
            // systemEventDatumRadGridView
            // 
            this.systemEventDatumRadGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.systemEventDatumRadGridView.Location = new System.Drawing.Point(12, 328);
            // 
            // systemEventDatumRadGridView
            // 
            this.systemEventDatumRadGridView.MasterTemplate.AllowAddNewRow = false;
            this.systemEventDatumRadGridView.MasterTemplate.AllowDeleteRow = false;
            this.systemEventDatumRadGridView.MasterTemplate.AutoGenerateColumns = false;
            this.systemEventDatumRadGridView.MasterTemplate.AutoSizeColumnsMode = Telerik.WinControls.UI.GridViewAutoSizeColumnsMode.Fill;
            gridViewTextBoxColumn4.FieldName = "Description";
            gridViewTextBoxColumn4.HeaderText = "Παράμετρος";
            gridViewTextBoxColumn4.IsAutoGenerated = true;
            gridViewTextBoxColumn4.Name = "Description";
            gridViewTextBoxColumn4.Width = 341;
            gridViewTextBoxColumn5.FieldName = "Value";
            gridViewTextBoxColumn5.HeaderText = "Τιμή";
            gridViewTextBoxColumn5.IsAutoGenerated = true;
            gridViewTextBoxColumn5.Name = "Value";
            gridViewTextBoxColumn5.Width = 171;
            this.systemEventDatumRadGridView.MasterTemplate.Columns.AddRange(new Telerik.WinControls.UI.GridViewDataColumn[] {
            gridViewTextBoxColumn4,
            gridViewTextBoxColumn5});
            this.systemEventDatumRadGridView.MasterTemplate.DataSource = this.systemEventDatumBindingSource;
            this.systemEventDatumRadGridView.Name = "systemEventDatumRadGridView";
            this.systemEventDatumRadGridView.ReadOnly = true;
            this.systemEventDatumRadGridView.ShowGroupPanel = false;
            this.systemEventDatumRadGridView.Size = new System.Drawing.Size(532, 211);
            this.systemEventDatumRadGridView.TabIndex = 1;
            this.systemEventDatumRadGridView.Text = "radGridView1";
            // 
            // radDateTimePicker1
            // 
            this.radDateTimePicker1.CustomFormat = "dd/MM/yyyy";
            this.radDateTimePicker1.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.radDateTimePicker1.Location = new System.Drawing.Point(46, 12);
            this.radDateTimePicker1.Name = "radDateTimePicker1";
            this.radDateTimePicker1.Size = new System.Drawing.Size(87, 20);
            this.radDateTimePicker1.TabIndex = 2;
            this.radDateTimePicker1.TabStop = false;
            this.radDateTimePicker1.Text = "08/01/2015";
            this.radDateTimePicker1.Value = new System.DateTime(2015, 1, 8, 10, 21, 15, 318);
            // 
            // radDateTimePicker2
            // 
            this.radDateTimePicker2.CustomFormat = "dd/MM/yyyy";
            this.radDateTimePicker2.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.radDateTimePicker2.Location = new System.Drawing.Point(196, 11);
            this.radDateTimePicker2.Name = "radDateTimePicker2";
            this.radDateTimePicker2.Size = new System.Drawing.Size(87, 20);
            this.radDateTimePicker2.TabIndex = 3;
            this.radDateTimePicker2.TabStop = false;
            this.radDateTimePicker2.Text = "08/01/2015";
            this.radDateTimePicker2.Value = new System.DateTime(2015, 1, 8, 10, 21, 15, 318);
            // 
            // radLabel1
            // 
            this.radLabel1.Location = new System.Drawing.Point(13, 13);
            this.radLabel1.Name = "radLabel1";
            this.radLabel1.Size = new System.Drawing.Size(27, 18);
            this.radLabel1.TabIndex = 4;
            this.radLabel1.Text = "Από";
            // 
            // radLabel2
            // 
            this.radLabel2.Location = new System.Drawing.Point(164, 13);
            this.radLabel2.Name = "radLabel2";
            this.radLabel2.Size = new System.Drawing.Size(26, 18);
            this.radLabel2.TabIndex = 5;
            this.radLabel2.Text = "Εώς";
            // 
            // radButton1
            // 
            this.radButton1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.radButton1.Image = global::ASFuelControl.Windows.Properties.Resources.Invoice;
            this.radButton1.Location = new System.Drawing.Point(550, 328);
            this.radButton1.Name = "radButton1";
            this.radButton1.Size = new System.Drawing.Size(240, 54);
            this.radButton1.TabIndex = 6;
            this.radButton1.Text = "Δημιουργία Παραστατικού";
            this.radButton1.Click += new System.EventHandler(this.radButton1_Click);
            // 
            // AlertsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(985, 551);
            this.Controls.Add(this.radButton1);
            this.Controls.Add(this.radLabel2);
            this.Controls.Add(this.radLabel1);
            this.Controls.Add(this.radDateTimePicker2);
            this.Controls.Add(this.radDateTimePicker1);
            this.Controls.Add(this.systemEventDatumRadGridView);
            this.Controls.Add(this.systemEventRadGridView);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "AlertsForm";
            // 
            // 
            // 
            this.RootElement.ApplyShapeToControl = true;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Συναγερμοί";
            ((System.ComponentModel.ISupportInitialize)(this.systemEventBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.systemEventRadGridView.MasterTemplate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.systemEventRadGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.systemEventDatumBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.systemEventDatumRadGridView.MasterTemplate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.systemEventDatumRadGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDateTimePicker1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDateTimePicker2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.BindingSource systemEventBindingSource;
        private Telerik.WinControls.UI.RadGridView systemEventRadGridView;
        private System.Windows.Forms.BindingSource systemEventDatumBindingSource;
        private Telerik.WinControls.UI.RadGridView systemEventDatumRadGridView;
        private Telerik.WinControls.UI.RadDateTimePicker radDateTimePicker1;
        private Telerik.WinControls.UI.RadDateTimePicker radDateTimePicker2;
        private Telerik.WinControls.UI.RadLabel radLabel1;
        private Telerik.WinControls.UI.RadLabel radLabel2;
        private Telerik.WinControls.UI.RadButton radButton1;
    }
}
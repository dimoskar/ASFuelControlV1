namespace ASFuelControl.Windows.UI.Forms
{
    partial class SendHistoryForm
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
            Telerik.WinControls.UI.GridViewDateTimeColumn gridViewDateTimeColumn1 = new Telerik.WinControls.UI.GridViewDateTimeColumn();
            Telerik.WinControls.UI.GridViewTextBoxColumn gridViewTextBoxColumn2 = new Telerik.WinControls.UI.GridViewTextBoxColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn1 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.GridViewDateTimeColumn gridViewDateTimeColumn2 = new Telerik.WinControls.UI.GridViewDateTimeColumn();
            Telerik.WinControls.UI.GridViewCommandColumn gridViewCommandColumn1 = new Telerik.WinControls.UI.GridViewCommandColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn2 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.ConditionalFormattingObject conditionalFormattingObject1 = new Telerik.WinControls.UI.ConditionalFormattingObject();
            this.panel1 = new System.Windows.Forms.Panel();
            this.radButton3 = new Telerik.WinControls.UI.RadButton();
            this.radLabel2 = new Telerik.WinControls.UI.RadLabel();
            this.radDateTimePicker2 = new Telerik.WinControls.UI.RadDateTimePicker();
            this.radLabel1 = new Telerik.WinControls.UI.RadLabel();
            this.radDateTimePicker1 = new Telerik.WinControls.UI.RadDateTimePicker();
            this.radButton2 = new Telerik.WinControls.UI.RadButton();
            this.sendLogBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.sendLogRadGridView = new Telerik.WinControls.UI.RadGridView();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.radButton3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDateTimePicker2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDateTimePicker1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sendLogBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sendLogRadGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sendLogRadGridView.MasterTemplate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.radButton3);
            this.panel1.Controls.Add(this.radLabel2);
            this.panel1.Controls.Add(this.radDateTimePicker2);
            this.panel1.Controls.Add(this.radLabel1);
            this.panel1.Controls.Add(this.radDateTimePicker1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(813, 31);
            this.panel1.TabIndex = 6;
            // 
            // radButton3
            // 
            this.radButton3.Location = new System.Drawing.Point(331, 2);
            this.radButton3.Name = "radButton3";
            this.radButton3.Size = new System.Drawing.Size(110, 24);
            this.radButton3.TabIndex = 4;
            this.radButton3.Text = "Εμφάνιση";
            this.radButton3.Click += new System.EventHandler(this.radButton3_Click);
            // 
            // radLabel2
            // 
            this.radLabel2.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radLabel2.Location = new System.Drawing.Point(189, 4);
            this.radLabel2.Name = "radLabel2";
            this.radLabel2.Size = new System.Drawing.Size(31, 21);
            this.radLabel2.TabIndex = 3;
            this.radLabel2.Text = "Έως";
            this.radLabel2.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // radDateTimePicker2
            // 
            this.radDateTimePicker2.CustomFormat = "dd/MM/yyyy";
            this.radDateTimePicker2.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radDateTimePicker2.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.radDateTimePicker2.Location = new System.Drawing.Point(222, 3);
            this.radDateTimePicker2.Name = "radDateTimePicker2";
            this.radDateTimePicker2.Size = new System.Drawing.Size(103, 23);
            this.radDateTimePicker2.TabIndex = 2;
            this.radDateTimePicker2.TabStop = false;
            this.radDateTimePicker2.Text = "21/06/2014";
            this.radDateTimePicker2.Value = new System.DateTime(2014, 6, 21, 14, 12, 49, 57);
            // 
            // radLabel1
            // 
            this.radLabel1.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radLabel1.Location = new System.Drawing.Point(12, 4);
            this.radLabel1.Name = "radLabel1";
            this.radLabel1.Size = new System.Drawing.Size(32, 21);
            this.radLabel1.TabIndex = 1;
            this.radLabel1.Text = "Από";
            this.radLabel1.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // radDateTimePicker1
            // 
            this.radDateTimePicker1.CustomFormat = "dd/MM/yyyy";
            this.radDateTimePicker1.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radDateTimePicker1.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.radDateTimePicker1.Location = new System.Drawing.Point(50, 3);
            this.radDateTimePicker1.Name = "radDateTimePicker1";
            this.radDateTimePicker1.Size = new System.Drawing.Size(103, 23);
            this.radDateTimePicker1.TabIndex = 0;
            this.radDateTimePicker1.TabStop = false;
            this.radDateTimePicker1.Text = "21/06/2014";
            this.radDateTimePicker1.Value = new System.DateTime(2014, 6, 21, 14, 12, 49, 57);
            // 
            // radButton2
            // 
            this.radButton2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.radButton2.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radButton2.Location = new System.Drawing.Point(682, 459);
            this.radButton2.Name = "radButton2";
            this.radButton2.Size = new System.Drawing.Size(119, 24);
            this.radButton2.TabIndex = 10;
            this.radButton2.Text = "Κλείσιμο";
            this.radButton2.Click += new System.EventHandler(this.radButton2_Click);
            // 
            // sendLogBindingSource
            // 
            this.sendLogBindingSource.DataSource = typeof(ASFuelControl.Data.SendLog);
            // 
            // sendLogRadGridView
            // 
            this.sendLogRadGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sendLogRadGridView.Location = new System.Drawing.Point(12, 37);
            // 
            // sendLogRadGridView
            // 
            this.sendLogRadGridView.MasterTemplate.AllowAddNewRow = false;
            this.sendLogRadGridView.MasterTemplate.AllowDeleteRow = false;
            this.sendLogRadGridView.MasterTemplate.AllowEditRow = false;
            this.sendLogRadGridView.MasterTemplate.AutoGenerateColumns = false;
            this.sendLogRadGridView.MasterTemplate.AutoSizeColumnsMode = Telerik.WinControls.UI.GridViewAutoSizeColumnsMode.Fill;
            gridViewTextBoxColumn1.FieldName = "Action";
            gridViewTextBoxColumn1.HeaderText = "Action";
            gridViewTextBoxColumn1.IsAutoGenerated = true;
            gridViewTextBoxColumn1.Name = "Action";
            gridViewTextBoxColumn1.Width = 97;
            gridViewDateTimeColumn1.FieldName = "SendDate";
            gridViewDateTimeColumn1.FormatString = "{0:dd/MM/yyyy HH:mm}";
            gridViewDateTimeColumn1.HeaderText = "Πρώτη Προσπάθεια";
            gridViewDateTimeColumn1.IsAutoGenerated = true;
            gridViewDateTimeColumn1.Name = "SendDate";
            gridViewDateTimeColumn1.Width = 108;
            gridViewTextBoxColumn2.FieldName = "SendData";
            gridViewTextBoxColumn2.HeaderText = "Δεδομένα Αποστολής";
            gridViewTextBoxColumn2.IsAutoGenerated = true;
            gridViewTextBoxColumn2.Name = "SendData";
            gridViewTextBoxColumn2.Width = 245;
            gridViewDecimalColumn1.DataType = typeof(string);
            gridViewDecimalColumn1.FieldName = "StatusDesc";
            gridViewDecimalColumn1.HeaderText = "Κατάσταση";
            gridViewDecimalColumn1.IsAutoGenerated = true;
            gridViewDecimalColumn1.Name = "StatusDesc";
            gridViewDecimalColumn1.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            gridViewDecimalColumn1.Width = 93;
            gridViewDateTimeColumn2.DataType = typeof(System.Nullable<System.DateTime>);
            gridViewDateTimeColumn2.FieldName = "LastSent";
            gridViewDateTimeColumn2.FormatString = "{0:dd/MM/yyyy HH:mm}";
            gridViewDateTimeColumn2.HeaderText = "Τελ. Προσπάθεια";
            gridViewDateTimeColumn2.IsAutoGenerated = true;
            gridViewDateTimeColumn2.Name = "LastSent";
            gridViewDateTimeColumn2.Width = 110;
            gridViewCommandColumn1.AllowResize = false;
            gridViewCommandColumn1.DataType = typeof(System.Nullable<int>);
            gridViewCommandColumn1.DefaultText = "Ακύρωση Αποστολής";
            gridViewCommandColumn1.Expression = "";
            gridViewCommandColumn1.HeaderText = "";
            gridViewCommandColumn1.Name = "column1";
            gridViewCommandColumn1.UseDefaultText = true;
            gridViewCommandColumn1.Width = 120;
            conditionalFormattingObject1.ApplyToRow = true;
            conditionalFormattingObject1.CellBackColor = System.Drawing.Color.Empty;
            conditionalFormattingObject1.CellForeColor = System.Drawing.Color.Empty;
            conditionalFormattingObject1.Name = "NewCondition";
            conditionalFormattingObject1.RowBackColor = System.Drawing.Color.Empty;
            conditionalFormattingObject1.RowForeColor = System.Drawing.Color.Red;
            conditionalFormattingObject1.TValue1 = "0";
            gridViewDecimalColumn2.ConditionalFormattingObjectList.Add(conditionalFormattingObject1);
            gridViewDecimalColumn2.DataType = typeof(System.Nullable<int>);
            gridViewDecimalColumn2.FieldName = "SentStatus";
            gridViewDecimalColumn2.HeaderText = "column1";
            gridViewDecimalColumn2.IsVisible = false;
            gridViewDecimalColumn2.Name = "SentStatus";
            gridViewDecimalColumn2.Width = 46;
            this.sendLogRadGridView.MasterTemplate.Columns.AddRange(new Telerik.WinControls.UI.GridViewDataColumn[] {
            gridViewTextBoxColumn1,
            gridViewDateTimeColumn1,
            gridViewTextBoxColumn2,
            gridViewDecimalColumn1,
            gridViewDateTimeColumn2,
            gridViewCommandColumn1,
            gridViewDecimalColumn2});
            this.sendLogRadGridView.MasterTemplate.DataSource = this.sendLogBindingSource;
            this.sendLogRadGridView.Name = "sendLogRadGridView";
            this.sendLogRadGridView.Size = new System.Drawing.Size(789, 416);
            this.sendLogRadGridView.TabIndex = 11;
            this.sendLogRadGridView.Text = "radGridView1";
            this.sendLogRadGridView.CellFormatting += new Telerik.WinControls.UI.CellFormattingEventHandler(this.sendLogRadGridView_CellFormatting);
            this.sendLogRadGridView.CommandCellClick += new Telerik.WinControls.UI.CommandCellClickEventHandler(this.sendLogRadGridView_CommandCellClick);
            // 
            // SendHistoryForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(813, 495);
            this.Controls.Add(this.sendLogRadGridView);
            this.Controls.Add(this.radButton2);
            this.Controls.Add(this.panel1);
            this.MinimumSize = new System.Drawing.Size(744, 525);
            this.Name = "SendHistoryForm";
            // 
            // 
            // 
            this.RootElement.ApplyShapeToControl = true;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Ιστορικό Αποστολών";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.radButton3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDateTimePicker2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDateTimePicker1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sendLogBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sendLogRadGridView.MasterTemplate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sendLogRadGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private Telerik.WinControls.UI.RadLabel radLabel2;
        private Telerik.WinControls.UI.RadDateTimePicker radDateTimePicker2;
        private Telerik.WinControls.UI.RadLabel radLabel1;
        private Telerik.WinControls.UI.RadDateTimePicker radDateTimePicker1;
        private Telerik.WinControls.UI.RadButton radButton2;
        private Telerik.WinControls.UI.RadButton radButton3;
        private System.Windows.Forms.BindingSource sendLogBindingSource;
        private Telerik.WinControls.UI.RadGridView sendLogRadGridView;
    }
}
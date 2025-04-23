namespace ASFuelControl.Windows.UI.SettingForms
{
    partial class ChangeLogForm
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
            Telerik.WinControls.UI.GridViewTextBoxColumn gridViewTextBoxColumn3 = new Telerik.WinControls.UI.GridViewTextBoxColumn();
            Telerik.WinControls.UI.GridViewTextBoxColumn gridViewTextBoxColumn4 = new Telerik.WinControls.UI.GridViewTextBoxColumn();
            Telerik.WinControls.UI.GridViewTextBoxColumn gridViewTextBoxColumn5 = new Telerik.WinControls.UI.GridViewTextBoxColumn();
            Telerik.WinControls.UI.GridViewTextBoxColumn gridViewTextBoxColumn6 = new Telerik.WinControls.UI.GridViewTextBoxColumn();
            Telerik.WinControls.UI.GridViewTextBoxColumn gridViewTextBoxColumn7 = new Telerik.WinControls.UI.GridViewTextBoxColumn();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ChangeLogForm));
            this.changeLogBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.changeLogRadGridView = new Telerik.WinControls.UI.RadGridView();
            this.panel1 = new System.Windows.Forms.Panel();
            this.radDropDownList2 = new Telerik.WinControls.UI.RadDropDownList();
            this.radLabel4 = new Telerik.WinControls.UI.RadLabel();
            this.radDropDownList1 = new Telerik.WinControls.UI.RadDropDownList();
            this.radLabel3 = new Telerik.WinControls.UI.RadLabel();
            this.radLabel2 = new Telerik.WinControls.UI.RadLabel();
            this.radLabel1 = new Telerik.WinControls.UI.RadLabel();
            this.radDateTimePicker2 = new Telerik.WinControls.UI.RadDateTimePicker();
            this.radDateTimePicker1 = new Telerik.WinControls.UI.RadDateTimePicker();
            this.radButton1 = new Telerik.WinControls.UI.RadButton();
            ((System.ComponentModel.ISupportInitialize)(this.changeLogBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.changeLogRadGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.changeLogRadGridView.MasterTemplate)).BeginInit();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.radDropDownList2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel4)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDropDownList1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDateTimePicker2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDateTimePicker1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();
            // 
            // changeLogBindingSource
            // 
            this.changeLogBindingSource.DataSource = typeof(ASFuelControl.Data.ChangeLog);
            // 
            // changeLogRadGridView
            // 
            this.changeLogRadGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.changeLogRadGridView.Location = new System.Drawing.Point(0, 42);
            // 
            // changeLogRadGridView
            // 
            this.changeLogRadGridView.MasterTemplate.AllowAddNewRow = false;
            this.changeLogRadGridView.MasterTemplate.AllowDeleteRow = false;
            this.changeLogRadGridView.MasterTemplate.AllowEditRow = false;
            this.changeLogRadGridView.MasterTemplate.AutoGenerateColumns = false;
            gridViewDateTimeColumn1.CustomFormat = "{0:dd/MM/yyyy HH:mm}";
            gridViewDateTimeColumn1.FieldName = "DateTimeStamp";
            gridViewDateTimeColumn1.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            gridViewDateTimeColumn1.FormatString = "{0:dd/MM/yyyy HH:mm}";
            gridViewDateTimeColumn1.HeaderText = "Ημερομηνία / Ώρα";
            gridViewDateTimeColumn1.IsAutoGenerated = true;
            gridViewDateTimeColumn1.Name = "DateTimeStamp";
            gridViewDateTimeColumn1.Width = 120;
            gridViewTextBoxColumn1.FieldName = "TableName";
            gridViewTextBoxColumn1.HeaderText = "Πίνακας";
            gridViewTextBoxColumn1.IsAutoGenerated = true;
            gridViewTextBoxColumn1.Name = "TableName";
            gridViewTextBoxColumn1.Width = 120;
            gridViewTextBoxColumn2.FieldName = "ColumnName";
            gridViewTextBoxColumn2.HeaderText = "Πεδίο";
            gridViewTextBoxColumn2.IsAutoGenerated = true;
            gridViewTextBoxColumn2.Name = "ColumnName";
            gridViewTextBoxColumn2.Width = 120;
            gridViewTextBoxColumn3.FieldName = "AdditionalDescription";
            gridViewTextBoxColumn3.HeaderText = "Προσθετες Πληροφορίες";
            gridViewTextBoxColumn3.IsAutoGenerated = true;
            gridViewTextBoxColumn3.Name = "AdditionalDescription";
            gridViewTextBoxColumn3.Width = 150;
            gridViewTextBoxColumn4.FieldName = "OldValue";
            gridViewTextBoxColumn4.HeaderText = "Παλιά Τιμή";
            gridViewTextBoxColumn4.IsAutoGenerated = true;
            gridViewTextBoxColumn4.Name = "OldValue";
            gridViewTextBoxColumn4.Width = 120;
            gridViewTextBoxColumn5.FieldName = "NewValue";
            gridViewTextBoxColumn5.HeaderText = "Νέα Τιμή";
            gridViewTextBoxColumn5.IsAutoGenerated = true;
            gridViewTextBoxColumn5.Name = "NewValue";
            gridViewTextBoxColumn5.Width = 120;
            gridViewTextBoxColumn6.FieldName = "ApplicationUserName";
            gridViewTextBoxColumn6.HeaderText = "Χρήστης";
            gridViewTextBoxColumn6.IsAutoGenerated = true;
            gridViewTextBoxColumn6.Name = "ApplicationUserName";
            gridViewTextBoxColumn6.Width = 80;
            gridViewTextBoxColumn7.FieldName = "PrimaryKey";
            gridViewTextBoxColumn7.HeaderText = "PrimaryKey";
            gridViewTextBoxColumn7.Name = "PrimaryKey";
            gridViewTextBoxColumn7.Width = 130;
            this.changeLogRadGridView.MasterTemplate.Columns.AddRange(new Telerik.WinControls.UI.GridViewDataColumn[] {
            gridViewDateTimeColumn1,
            gridViewTextBoxColumn1,
            gridViewTextBoxColumn2,
            gridViewTextBoxColumn3,
            gridViewTextBoxColumn4,
            gridViewTextBoxColumn5,
            gridViewTextBoxColumn6,
            gridViewTextBoxColumn7});
            this.changeLogRadGridView.MasterTemplate.DataSource = this.changeLogBindingSource;
            this.changeLogRadGridView.MasterTemplate.EnableAlternatingRowColor = true;
            this.changeLogRadGridView.Name = "changeLogRadGridView";
            this.changeLogRadGridView.Size = new System.Drawing.Size(1016, 528);
            this.changeLogRadGridView.TabIndex = 1;
            this.changeLogRadGridView.Text = "radGridView1";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.radButton1);
            this.panel1.Controls.Add(this.radDropDownList2);
            this.panel1.Controls.Add(this.radLabel4);
            this.panel1.Controls.Add(this.radDropDownList1);
            this.panel1.Controls.Add(this.radLabel3);
            this.panel1.Controls.Add(this.radLabel2);
            this.panel1.Controls.Add(this.radLabel1);
            this.panel1.Controls.Add(this.radDateTimePicker2);
            this.panel1.Controls.Add(this.radDateTimePicker1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1016, 42);
            this.panel1.TabIndex = 2;
            // 
            // radDropDownList2
            // 
            this.radDropDownList2.DropDownStyle = Telerik.WinControls.RadDropDownStyle.DropDownList;
            this.radDropDownList2.Location = new System.Drawing.Point(585, 11);
            this.radDropDownList2.Name = "radDropDownList2";
            this.radDropDownList2.Size = new System.Drawing.Size(155, 20);
            this.radDropDownList2.TabIndex = 13;
            this.radDropDownList2.Text = "radDropDownList2";
            this.radDropDownList2.SelectedIndexChanged += new Telerik.WinControls.UI.Data.PositionChangedEventHandler(this.FilterSelection_Changed);
            // 
            // radLabel4
            // 
            this.radLabel4.Location = new System.Drawing.Point(531, 11);
            this.radLabel4.Name = "radLabel4";
            this.radLabel4.Size = new System.Drawing.Size(50, 18);
            this.radLabel4.TabIndex = 12;
            this.radLabel4.Text = "Χρήστης";
            // 
            // radDropDownList1
            // 
            this.radDropDownList1.DropDownStyle = Telerik.WinControls.RadDropDownStyle.DropDownList;
            this.radDropDownList1.Location = new System.Drawing.Point(360, 11);
            this.radDropDownList1.Name = "radDropDownList1";
            this.radDropDownList1.Size = new System.Drawing.Size(155, 20);
            this.radDropDownList1.TabIndex = 11;
            this.radDropDownList1.Text = "radDropDownList1";
            this.radDropDownList1.SelectedIndexChanged += new Telerik.WinControls.UI.Data.PositionChangedEventHandler(this.FilterSelection_Changed);
            // 
            // radLabel3
            // 
            this.radLabel3.Location = new System.Drawing.Point(306, 11);
            this.radLabel3.Name = "radLabel3";
            this.radLabel3.Size = new System.Drawing.Size(48, 18);
            this.radLabel3.TabIndex = 10;
            this.radLabel3.Text = "Πίνακας";
            // 
            // radLabel2
            // 
            this.radLabel2.Location = new System.Drawing.Point(162, 12);
            this.radLabel2.Name = "radLabel2";
            this.radLabel2.Size = new System.Drawing.Size(26, 18);
            this.radLabel2.TabIndex = 9;
            this.radLabel2.Text = "Εώς";
            // 
            // radLabel1
            // 
            this.radLabel1.Location = new System.Drawing.Point(11, 12);
            this.radLabel1.Name = "radLabel1";
            this.radLabel1.Size = new System.Drawing.Size(27, 18);
            this.radLabel1.TabIndex = 8;
            this.radLabel1.Text = "Από";
            // 
            // radDateTimePicker2
            // 
            this.radDateTimePicker2.CustomFormat = "dd/MM/yyyy";
            this.radDateTimePicker2.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.radDateTimePicker2.Location = new System.Drawing.Point(194, 10);
            this.radDateTimePicker2.Name = "radDateTimePicker2";
            this.radDateTimePicker2.Size = new System.Drawing.Size(87, 20);
            this.radDateTimePicker2.TabIndex = 7;
            this.radDateTimePicker2.TabStop = false;
            this.radDateTimePicker2.Text = "08/01/2015";
            this.radDateTimePicker2.Value = new System.DateTime(2015, 1, 8, 10, 21, 15, 318);
            this.radDateTimePicker2.ValueChanged += new System.EventHandler(this.DateTimeFilter_Changed);
            // 
            // radDateTimePicker1
            // 
            this.radDateTimePicker1.CustomFormat = "dd/MM/yyyy";
            this.radDateTimePicker1.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.radDateTimePicker1.Location = new System.Drawing.Point(44, 11);
            this.radDateTimePicker1.Name = "radDateTimePicker1";
            this.radDateTimePicker1.Size = new System.Drawing.Size(87, 20);
            this.radDateTimePicker1.TabIndex = 6;
            this.radDateTimePicker1.TabStop = false;
            this.radDateTimePicker1.Text = "08/01/2015";
            this.radDateTimePicker1.Value = new System.DateTime(2015, 1, 8, 10, 21, 15, 318);
            this.radDateTimePicker1.ValueChanged += new System.EventHandler(this.DateTimeFilter_Changed);
            // 
            // radButton1
            // 
            this.radButton1.Location = new System.Drawing.Point(894, 9);
            this.radButton1.Name = "radButton1";
            this.radButton1.Size = new System.Drawing.Size(110, 24);
            this.radButton1.TabIndex = 14;
            this.radButton1.Text = "Καθαρισμός...";
            this.radButton1.Click += new System.EventHandler(this.radButton1_Click);
            // 
            // ChangeLogForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1016, 570);
            this.Controls.Add(this.changeLogRadGridView);
            this.Controls.Add(this.panel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(1024, 600);
            this.Name = "ChangeLogForm";
            // 
            // 
            // 
            this.RootElement.ApplyShapeToControl = true;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Αλλαγές...";
            ((System.ComponentModel.ISupportInitialize)(this.changeLogBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.changeLogRadGridView.MasterTemplate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.changeLogRadGridView)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.radDropDownList2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel4)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDropDownList1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDateTimePicker2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDateTimePicker1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.BindingSource changeLogBindingSource;
        private Telerik.WinControls.UI.RadGridView changeLogRadGridView;
        private System.Windows.Forms.Panel panel1;
        private Telerik.WinControls.UI.RadDropDownList radDropDownList2;
        private Telerik.WinControls.UI.RadLabel radLabel4;
        private Telerik.WinControls.UI.RadDropDownList radDropDownList1;
        private Telerik.WinControls.UI.RadLabel radLabel3;
        private Telerik.WinControls.UI.RadLabel radLabel2;
        private Telerik.WinControls.UI.RadLabel radLabel1;
        private Telerik.WinControls.UI.RadDateTimePicker radDateTimePicker2;
        private Telerik.WinControls.UI.RadDateTimePicker radDateTimePicker1;
        private Telerik.WinControls.UI.RadButton radButton1;
    }
}
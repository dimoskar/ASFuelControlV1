namespace ASFuelControl.Windows.UI.SettingForms
{
    partial class TankLevelHistory
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
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn2 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn3 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TankLevelHistory));
            this.panel1 = new System.Windows.Forms.Panel();
            this.radButton1 = new Telerik.WinControls.UI.RadButton();
            this.radDropDownList1 = new Telerik.WinControls.UI.RadDropDownList();
            this.radLabel3 = new Telerik.WinControls.UI.RadLabel();
            this.radLabel2 = new Telerik.WinControls.UI.RadLabel();
            this.radLabel1 = new Telerik.WinControls.UI.RadLabel();
            this.radDateTimePicker2 = new Telerik.WinControls.UI.RadDateTimePicker();
            this.radDateTimePicker1 = new Telerik.WinControls.UI.RadDateTimePicker();
            this.tankCheckBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.tankCheckRadGridView = new Telerik.WinControls.UI.RadGridView();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.radButton1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDropDownList1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDateTimePicker2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDateTimePicker1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tankCheckBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tankCheckRadGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tankCheckRadGridView.MasterTemplate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.radButton1);
            this.panel1.Controls.Add(this.radDropDownList1);
            this.panel1.Controls.Add(this.radLabel3);
            this.panel1.Controls.Add(this.radLabel2);
            this.panel1.Controls.Add(this.radLabel1);
            this.panel1.Controls.Add(this.radDateTimePicker2);
            this.panel1.Controls.Add(this.radDateTimePicker1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(2);
            this.panel1.Size = new System.Drawing.Size(737, 40);
            this.panel1.TabIndex = 0;
            // 
            // radButton1
            // 
            this.radButton1.Dock = System.Windows.Forms.DockStyle.Right;
            this.radButton1.Image = global::ASFuelControl.Windows.Properties.Resources.Search;
            this.radButton1.Location = new System.Drawing.Point(603, 2);
            this.radButton1.Name = "radButton1";
            this.radButton1.Size = new System.Drawing.Size(132, 36);
            this.radButton1.TabIndex = 12;
            this.radButton1.Text = "Εύρεση";
            this.radButton1.Click += new System.EventHandler(this.radButton1_Click);
            // 
            // radDropDownList1
            // 
            this.radDropDownList1.DropDownStyle = Telerik.WinControls.RadDropDownStyle.DropDownList;
            this.radDropDownList1.Location = new System.Drawing.Point(379, 10);
            this.radDropDownList1.Name = "radDropDownList1";
            this.radDropDownList1.Size = new System.Drawing.Size(125, 20);
            this.radDropDownList1.TabIndex = 11;
            this.radDropDownList1.Text = "radDropDownList1";
            this.radDropDownList1.SelectedIndexChanged += new Telerik.WinControls.UI.Data.PositionChangedEventHandler(this.radDropDownList1_SelectedIndexChanged);
            // 
            // radLabel3
            // 
            this.radLabel3.Location = new System.Drawing.Point(319, 10);
            this.radLabel3.Name = "radLabel3";
            this.radLabel3.Size = new System.Drawing.Size(54, 18);
            this.radLabel3.TabIndex = 10;
            this.radLabel3.Text = "Δεξαμενή";
            // 
            // radLabel2
            // 
            this.radLabel2.Location = new System.Drawing.Point(169, 12);
            this.radLabel2.Name = "radLabel2";
            this.radLabel2.Size = new System.Drawing.Size(26, 18);
            this.radLabel2.TabIndex = 9;
            this.radLabel2.Text = "Εώς";
            // 
            // radLabel1
            // 
            this.radLabel1.Location = new System.Drawing.Point(14, 12);
            this.radLabel1.Name = "radLabel1";
            this.radLabel1.Size = new System.Drawing.Size(27, 18);
            this.radLabel1.TabIndex = 8;
            this.radLabel1.Text = "Από";
            // 
            // radDateTimePicker2
            // 
            this.radDateTimePicker2.CustomFormat = "dd/MM/yyyy HH:mm";
            this.radDateTimePicker2.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.radDateTimePicker2.Location = new System.Drawing.Point(201, 10);
            this.radDateTimePicker2.Name = "radDateTimePicker2";
            this.radDateTimePicker2.Size = new System.Drawing.Size(109, 20);
            this.radDateTimePicker2.TabIndex = 7;
            this.radDateTimePicker2.TabStop = false;
            this.radDateTimePicker2.Text = "08/01/2015 10:21";
            this.radDateTimePicker2.Value = new System.DateTime(2015, 1, 8, 10, 21, 15, 318);
            // 
            // radDateTimePicker1
            // 
            this.radDateTimePicker1.CustomFormat = "dd/MM/yyyy HH:mm";
            this.radDateTimePicker1.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.radDateTimePicker1.Location = new System.Drawing.Point(47, 11);
            this.radDateTimePicker1.Name = "radDateTimePicker1";
            this.radDateTimePicker1.Size = new System.Drawing.Size(114, 20);
            this.radDateTimePicker1.TabIndex = 6;
            this.radDateTimePicker1.TabStop = false;
            this.radDateTimePicker1.Text = "08/01/2015 10:21";
            this.radDateTimePicker1.Value = new System.DateTime(2015, 1, 8, 10, 21, 15, 318);
            // 
            // tankCheckBindingSource
            // 
            this.tankCheckBindingSource.DataSource = typeof(ASFuelControl.Data.TankCheck);
            // 
            // tankCheckRadGridView
            // 
            this.tankCheckRadGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tankCheckRadGridView.Location = new System.Drawing.Point(0, 40);
            // 
            // tankCheckRadGridView
            // 
            this.tankCheckRadGridView.MasterTemplate.AllowAddNewRow = false;
            this.tankCheckRadGridView.MasterTemplate.AllowDeleteRow = false;
            this.tankCheckRadGridView.MasterTemplate.AllowEditRow = false;
            this.tankCheckRadGridView.MasterTemplate.AutoGenerateColumns = false;
            gridViewDateTimeColumn1.FieldName = "CheckDate";
            gridViewDateTimeColumn1.FormatString = "{0:dd/MM/yyyy HH:mm:ss}";
            gridViewDateTimeColumn1.HeaderText = "Ημερομηνία / Ώρα";
            gridViewDateTimeColumn1.IsAutoGenerated = true;
            gridViewDateTimeColumn1.Name = "CheckDate";
            gridViewDateTimeColumn1.Width = 150;
            gridViewTextBoxColumn1.FieldName = "Tank.Description";
            gridViewTextBoxColumn1.HeaderText = "Δεξαμενή";
            gridViewTextBoxColumn1.IsAutoGenerated = true;
            gridViewTextBoxColumn1.Name = "Tank";
            gridViewTextBoxColumn1.Width = 200;
            gridViewDecimalColumn1.FieldName = "TankLevel";
            gridViewDecimalColumn1.HeaderText = "Στάθμη";
            gridViewDecimalColumn1.IsAutoGenerated = true;
            gridViewDecimalColumn1.Name = "TankLevel";
            gridViewDecimalColumn1.Width = 80;
            gridViewDecimalColumn2.FormatString = "{0:N2}";
            gridViewDecimalColumn2.HeaderText = "Διαφορά mm";
            gridViewDecimalColumn2.Name = "DiffColumn";
            gridViewDecimalColumn2.Width = 80;
            gridViewDecimalColumn3.FormatString = "{0:N2}";
            gridViewDecimalColumn3.HeaderText = "Διαφορά Lt";
            gridViewDecimalColumn3.Name = "DiffVolColumn";
            gridViewDecimalColumn3.Width = 80;
            this.tankCheckRadGridView.MasterTemplate.Columns.AddRange(new Telerik.WinControls.UI.GridViewDataColumn[] {
            gridViewDateTimeColumn1,
            gridViewTextBoxColumn1,
            gridViewDecimalColumn1,
            gridViewDecimalColumn2,
            gridViewDecimalColumn3});
            this.tankCheckRadGridView.MasterTemplate.DataSource = this.tankCheckBindingSource;
            this.tankCheckRadGridView.Name = "tankCheckRadGridView";
            this.tankCheckRadGridView.ShowGroupPanel = false;
            this.tankCheckRadGridView.Size = new System.Drawing.Size(737, 439);
            this.tankCheckRadGridView.TabIndex = 2;
            this.tankCheckRadGridView.Text = "radGridView1";
            this.tankCheckRadGridView.CellFormatting += new Telerik.WinControls.UI.CellFormattingEventHandler(this.tankCheckRadGridView_CellFormatting);
            // 
            // TankLevelHistory
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(737, 479);
            this.Controls.Add(this.tankCheckRadGridView);
            this.Controls.Add(this.panel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "TankLevelHistory";
            // 
            // 
            // 
            this.RootElement.ApplyShapeToControl = true;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Καταγεραμμένες Στάθμες Δεξαμενών";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.radButton1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDropDownList1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDateTimePicker2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDateTimePicker1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tankCheckBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tankCheckRadGridView.MasterTemplate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tankCheckRadGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private Telerik.WinControls.UI.RadLabel radLabel2;
        private Telerik.WinControls.UI.RadLabel radLabel1;
        private Telerik.WinControls.UI.RadDateTimePicker radDateTimePicker2;
        private Telerik.WinControls.UI.RadDateTimePicker radDateTimePicker1;
        private System.Windows.Forms.BindingSource tankCheckBindingSource;
        private Telerik.WinControls.UI.RadGridView tankCheckRadGridView;
        private Telerik.WinControls.UI.RadDropDownList radDropDownList1;
        private Telerik.WinControls.UI.RadLabel radLabel3;
        private Telerik.WinControls.UI.RadButton radButton1;
    }
}
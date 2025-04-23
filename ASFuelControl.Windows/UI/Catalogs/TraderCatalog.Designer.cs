namespace ASFuelControl.Windows.UI.Catalogs
{
    partial class TraderCatalog
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
            this.components = new System.ComponentModel.Container();
            Telerik.WinControls.UI.GridViewComboBoxColumn gridViewComboBoxColumn2 = new Telerik.WinControls.UI.GridViewComboBoxColumn();
            Telerik.WinControls.UI.GridViewTextBoxColumn gridViewTextBoxColumn10 = new Telerik.WinControls.UI.GridViewTextBoxColumn();
            Telerik.WinControls.UI.GridViewTextBoxColumn gridViewTextBoxColumn11 = new Telerik.WinControls.UI.GridViewTextBoxColumn();
            Telerik.WinControls.UI.GridViewTextBoxColumn gridViewTextBoxColumn12 = new Telerik.WinControls.UI.GridViewTextBoxColumn();
            Telerik.WinControls.UI.GridViewTextBoxColumn gridViewTextBoxColumn13 = new Telerik.WinControls.UI.GridViewTextBoxColumn();
            Telerik.WinControls.UI.GridViewTextBoxColumn gridViewTextBoxColumn14 = new Telerik.WinControls.UI.GridViewTextBoxColumn();
            Telerik.WinControls.UI.GridViewTextBoxColumn gridViewTextBoxColumn15 = new Telerik.WinControls.UI.GridViewTextBoxColumn();
            Telerik.WinControls.UI.GridViewTextBoxColumn gridViewTextBoxColumn16 = new Telerik.WinControls.UI.GridViewTextBoxColumn();
            Telerik.WinControls.UI.GridViewTextBoxColumn gridViewTextBoxColumn17 = new Telerik.WinControls.UI.GridViewTextBoxColumn();
            Telerik.WinControls.UI.GridViewTextBoxColumn gridViewTextBoxColumn18 = new Telerik.WinControls.UI.GridViewTextBoxColumn();
            this.enumBinder1 = new Telerik.WinControls.UI.Data.EnumBinder();
            this.panel1 = new System.Windows.Forms.Panel();
            this.labelTitle = new System.Windows.Forms.Label();
            this.radGridView1 = new Telerik.WinControls.UI.RadGridView();
            this.traderViewModelBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.panel2 = new System.Windows.Forms.Panel();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.radButton1 = new Telerik.WinControls.UI.RadButton();
            this.radLabel2 = new Telerik.WinControls.UI.RadLabel();
            this.btnDelete = new Telerik.WinControls.UI.RadButton();
            this.radLabel1 = new Telerik.WinControls.UI.RadLabel();
            this.radTextBox1 = new Telerik.WinControls.UI.RadTextBox();
            this.panel3 = new System.Windows.Forms.Panel();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.radRadioButton2 = new Telerik.WinControls.UI.RadRadioButton();
            this.radRadioButton1 = new Telerik.WinControls.UI.RadRadioButton();
            this.radDropDownButton1 = new Telerik.WinControls.UI.RadDropDownButton();
            this.addCustomerMenuItem = new Telerik.WinControls.UI.RadMenuItem();
            this.addSuplierMenuItem = new Telerik.WinControls.UI.RadMenuItem();
            this.radMenuItem1 = new Telerik.WinControls.UI.RadMenuItem();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.radGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radGridView1.MasterTemplate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.traderViewModelBindingSource)).BeginInit();
            this.panel2.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.radButton1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.btnDelete)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radTextBox1)).BeginInit();
            this.panel3.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.radRadioButton2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radRadioButton1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDropDownButton1)).BeginInit();
            this.SuspendLayout();
            // 
            // enumBinder1
            // 
            this.enumBinder1.Source = typeof(ASFuelControl.Windows.ViewModels.EntityStateEnum);
            gridViewComboBoxColumn2.DataSource = this.enumBinder1;
            gridViewComboBoxColumn2.DisplayMember = "Description";
            gridViewComboBoxColumn2.ValueMember = "Value";
            this.enumBinder1.Target = gridViewComboBoxColumn2;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.labelTitle);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1102, 74);
            this.panel1.TabIndex = 2;
            // 
            // labelTitle
            // 
            this.labelTitle.BackColor = System.Drawing.Color.WhiteSmoke;
            this.labelTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelTitle.Font = new System.Drawing.Font("Segoe UI", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.labelTitle.Location = new System.Drawing.Point(0, 0);
            this.labelTitle.Name = "labelTitle";
            this.labelTitle.Size = new System.Drawing.Size(1102, 74);
            this.labelTitle.TabIndex = 5;
            this.labelTitle.Text = "Κατάλογος Πελατών";
            this.labelTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // radGridView1
            // 
            this.radGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.radGridView1.Location = new System.Drawing.Point(0, 114);
            // 
            // radGridView1
            // 
            this.radGridView1.MasterTemplate.AllowAddNewRow = false;
            this.radGridView1.MasterTemplate.AllowDeleteRow = false;
            this.radGridView1.MasterTemplate.AllowEditRow = false;
            this.radGridView1.MasterTemplate.AutoGenerateColumns = false;
            this.radGridView1.MasterTemplate.AutoSizeColumnsMode = Telerik.WinControls.UI.GridViewAutoSizeColumnsMode.Fill;
            gridViewTextBoxColumn10.FieldName = "Name";
            gridViewTextBoxColumn10.HeaderText = "Επωνυμία";
            gridViewTextBoxColumn10.IsAutoGenerated = true;
            gridViewTextBoxColumn10.Name = "Name";
            gridViewTextBoxColumn10.Width = 217;
            gridViewTextBoxColumn11.FieldName = "Address";
            gridViewTextBoxColumn11.HeaderText = "Διεύθυνση";
            gridViewTextBoxColumn11.IsAutoGenerated = true;
            gridViewTextBoxColumn11.Name = "Address";
            gridViewTextBoxColumn11.Width = 163;
            gridViewTextBoxColumn12.FieldName = "City";
            gridViewTextBoxColumn12.HeaderText = "Πόλη";
            gridViewTextBoxColumn12.IsAutoGenerated = true;
            gridViewTextBoxColumn12.Name = "City";
            gridViewTextBoxColumn12.Width = 109;
            gridViewTextBoxColumn13.AllowResize = false;
            gridViewTextBoxColumn13.FieldName = "Phone1";
            gridViewTextBoxColumn13.HeaderText = "Τηλέφωνο 1";
            gridViewTextBoxColumn13.IsAutoGenerated = true;
            gridViewTextBoxColumn13.Name = "Phone1";
            gridViewTextBoxColumn13.Width = 80;
            gridViewTextBoxColumn14.AllowResize = false;
            gridViewTextBoxColumn14.FieldName = "Phone2";
            gridViewTextBoxColumn14.HeaderText = "Τηλέφωνο 2";
            gridViewTextBoxColumn14.IsAutoGenerated = true;
            gridViewTextBoxColumn14.Name = "Phone2";
            gridViewTextBoxColumn14.Width = 80;
            gridViewTextBoxColumn15.AllowResize = false;
            gridViewTextBoxColumn15.FieldName = "TaxRegistrationNumber";
            gridViewTextBoxColumn15.HeaderText = "Α.Φ.Μ.";
            gridViewTextBoxColumn15.IsAutoGenerated = true;
            gridViewTextBoxColumn15.Name = "TaxRegistrationNumber";
            gridViewTextBoxColumn15.Width = 80;
            gridViewTextBoxColumn16.FieldName = "TaxRegistrationOffice";
            gridViewTextBoxColumn16.HeaderText = "Δ.Ο.Υ.";
            gridViewTextBoxColumn16.IsAutoGenerated = true;
            gridViewTextBoxColumn16.Name = "TaxRegistrationOffice";
            gridViewTextBoxColumn16.Width = 130;
            gridViewTextBoxColumn17.AllowResize = false;
            gridViewTextBoxColumn17.FieldName = "Email";
            gridViewTextBoxColumn17.HeaderText = "Email";
            gridViewTextBoxColumn17.IsAutoGenerated = true;
            gridViewTextBoxColumn17.Name = "Email";
            gridViewTextBoxColumn17.Width = 100;
            gridViewTextBoxColumn18.FieldName = "Occupation";
            gridViewTextBoxColumn18.HeaderText = "Επάγγελμα";
            gridViewTextBoxColumn18.IsAutoGenerated = true;
            gridViewTextBoxColumn18.Name = "Occupation";
            gridViewTextBoxColumn18.Width = 130;
            this.radGridView1.MasterTemplate.Columns.AddRange(new Telerik.WinControls.UI.GridViewDataColumn[] {
            gridViewTextBoxColumn10,
            gridViewTextBoxColumn11,
            gridViewTextBoxColumn12,
            gridViewTextBoxColumn13,
            gridViewTextBoxColumn14,
            gridViewTextBoxColumn15,
            gridViewTextBoxColumn16,
            gridViewTextBoxColumn17,
            gridViewTextBoxColumn18});
            this.radGridView1.MasterTemplate.DataSource = this.traderViewModelBindingSource;
            this.radGridView1.MasterTemplate.EnableGrouping = false;
            this.radGridView1.Name = "radGridView1";
            this.radGridView1.Size = new System.Drawing.Size(1102, 373);
            this.radGridView1.TabIndex = 3;
            this.radGridView1.Text = "radGridView1";
            this.radGridView1.CellDoubleClick += new Telerik.WinControls.UI.GridViewCellEventHandler(this.radGridView1_CellDoubleClick);
            // 
            // traderViewModelBindingSource
            // 
            this.traderViewModelBindingSource.DataSource = typeof(ASFuelControl.Windows.ViewModels.TraderCatalogViewModel);
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.tableLayoutPanel3);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel2.Location = new System.Drawing.Point(0, 74);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(1102, 40);
            this.panel2.TabIndex = 4;
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 7;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 250F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableLayoutPanel3.Controls.Add(this.radButton1, 8, 0);
            this.tableLayoutPanel3.Controls.Add(this.radLabel2, 4, 0);
            this.tableLayoutPanel3.Controls.Add(this.btnDelete, 1, 0);
            this.tableLayoutPanel3.Controls.Add(this.radLabel1, 2, 0);
            this.tableLayoutPanel3.Controls.Add(this.radTextBox1, 3, 0);
            this.tableLayoutPanel3.Controls.Add(this.panel3, 5, 0);
            this.tableLayoutPanel3.Controls.Add(this.radDropDownButton1, 0, 0);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 1;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(1102, 40);
            this.tableLayoutPanel3.TabIndex = 1;
            // 
            // radButton1
            // 
            this.radButton1.DisplayStyle = Telerik.WinControls.DisplayStyle.Image;
            this.radButton1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.radButton1.Image = global::ASFuelControl.Windows.Properties.Resources.Search;
            this.radButton1.ImageAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            this.radButton1.Location = new System.Drawing.Point(1062, 0);
            this.radButton1.Margin = new System.Windows.Forms.Padding(0);
            this.radButton1.Name = "radButton1";
            this.radButton1.Size = new System.Drawing.Size(40, 40);
            this.radButton1.TabIndex = 6;
            this.radButton1.Text = "radButton1";
            this.radButton1.UseCompatibleTextRendering = false;
            this.radButton1.Click += new System.EventHandler(this.radButton1_Click);
            // 
            // radLabel2
            // 
            this.radLabel2.AutoSize = false;
            this.radLabel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.radLabel2.Location = new System.Drawing.Point(665, 3);
            this.radLabel2.Name = "radLabel2";
            this.radLabel2.Size = new System.Drawing.Size(144, 34);
            this.radLabel2.TabIndex = 4;
            this.radLabel2.Text = "Τύπος Συναλλασσόμενου";
            this.radLabel2.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnDelete
            // 
            this.btnDelete.DisplayStyle = Telerik.WinControls.DisplayStyle.Image;
            this.btnDelete.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnDelete.Image = global::ASFuelControl.Windows.Properties.Resources.Delete;
            this.btnDelete.ImageAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            this.btnDelete.Location = new System.Drawing.Point(60, 0);
            this.btnDelete.Margin = new System.Windows.Forms.Padding(0);
            this.btnDelete.Name = "btnDelete";
            // 
            // 
            // 
            this.btnDelete.RootElement.UseDefaultDisabledPaint = true;
            this.btnDelete.Size = new System.Drawing.Size(40, 40);
            this.btnDelete.TabIndex = 1;
            this.btnDelete.Text = "radButton2";
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // radLabel1
            // 
            this.radLabel1.AutoSize = false;
            this.radLabel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.radLabel1.Location = new System.Drawing.Point(103, 3);
            this.radLabel1.Name = "radLabel1";
            this.radLabel1.Size = new System.Drawing.Size(64, 34);
            this.radLabel1.TabIndex = 2;
            this.radLabel1.Text = "Φίλτρο";
            this.radLabel1.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // radTextBox1
            // 
            this.radTextBox1.AutoSize = false;
            this.radTextBox1.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.traderViewModelBindingSource, "Filter", true));
            this.radTextBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.radTextBox1.Location = new System.Drawing.Point(173, 6);
            this.radTextBox1.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.radTextBox1.Name = "radTextBox1";
            this.radTextBox1.Size = new System.Drawing.Size(486, 28);
            this.radTextBox1.TabIndex = 3;
            this.radTextBox1.KeyUp += new System.Windows.Forms.KeyEventHandler(this.radTextBox1_KeyUp);
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.tableLayoutPanel1);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel3.Location = new System.Drawing.Point(815, 3);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(244, 34);
            this.panel3.TabIndex = 5;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.radRadioButton2, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.radRadioButton1, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(244, 34);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // radRadioButton2
            // 
            this.radRadioButton2.AutoSize = false;
            this.radRadioButton2.DataBindings.Add(new System.Windows.Forms.Binding("IsChecked", this.traderViewModelBindingSource, "SelectSuplier", true));
            this.radRadioButton2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.radRadioButton2.Location = new System.Drawing.Point(125, 3);
            this.radRadioButton2.Name = "radRadioButton2";
            this.radRadioButton2.Size = new System.Drawing.Size(116, 28);
            this.radRadioButton2.TabIndex = 1;
            this.radRadioButton2.Text = "Προμηθευτής";
            // 
            // radRadioButton1
            // 
            this.radRadioButton1.AutoSize = false;
            this.radRadioButton1.DataBindings.Add(new System.Windows.Forms.Binding("IsChecked", this.traderViewModelBindingSource, "SelectCustomer", true));
            this.radRadioButton1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.radRadioButton1.Location = new System.Drawing.Point(3, 3);
            this.radRadioButton1.Name = "radRadioButton1";
            this.radRadioButton1.Size = new System.Drawing.Size(116, 28);
            this.radRadioButton1.TabIndex = 0;
            this.radRadioButton1.Text = "Πελάτης";
            // 
            // radDropDownButton1
            // 
            this.radDropDownButton1.DisplayStyle = Telerik.WinControls.DisplayStyle.Image;
            this.radDropDownButton1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.radDropDownButton1.Image = global::ASFuelControl.Windows.Properties.Resources.Add;
            this.radDropDownButton1.ImageAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            this.radDropDownButton1.Items.AddRange(new Telerik.WinControls.RadItem[] {
            this.addCustomerMenuItem,
            this.addSuplierMenuItem,
            this.radMenuItem1});
            this.radDropDownButton1.Location = new System.Drawing.Point(0, 0);
            this.radDropDownButton1.Margin = new System.Windows.Forms.Padding(0);
            this.radDropDownButton1.Name = "radDropDownButton1";
            this.radDropDownButton1.Size = new System.Drawing.Size(60, 40);
            this.radDropDownButton1.TabIndex = 7;
            this.radDropDownButton1.Text = "radDropDownButton1";
            // 
            // addCustomerMenuItem
            // 
            this.addCustomerMenuItem.AccessibleDescription = "Προσθήκη Πελάτη";
            this.addCustomerMenuItem.AccessibleName = "Προσθήκη Πελάτη";
            this.addCustomerMenuItem.Name = "addCustomerMenuItem";
            this.addCustomerMenuItem.Text = "Προσθήκη Πελάτη";
            this.addCustomerMenuItem.Visibility = Telerik.WinControls.ElementVisibility.Visible;
            this.addCustomerMenuItem.Click += new System.EventHandler(this.addCustomerMenuItem_Click);
            // 
            // addSuplierMenuItem
            // 
            this.addSuplierMenuItem.AccessibleDescription = "Προσθήκη Προμηθευτή";
            this.addSuplierMenuItem.AccessibleName = "Προσθήκη Προμηθευτή";
            this.addSuplierMenuItem.Name = "addSuplierMenuItem";
            this.addSuplierMenuItem.Text = "Προσθήκη Προμηθευτή";
            this.addSuplierMenuItem.Visibility = Telerik.WinControls.ElementVisibility.Visible;
            this.addSuplierMenuItem.Click += new System.EventHandler(this.addSupplierMenuItem_Click);
            // 
            // radMenuItem1
            // 
            this.radMenuItem1.AccessibleDescription = "Προσθήκη μέσω ΓΓΠΣ";
            this.radMenuItem1.AccessibleName = "Προσθήκη μέσω ΓΓΠΣ";
            this.radMenuItem1.Name = "radMenuItem1";
            this.radMenuItem1.Text = "Προσθήκη μέσω ΓΓΠΣ";
            this.radMenuItem1.Visibility = Telerik.WinControls.ElementVisibility.Visible;
            this.radMenuItem1.Click += new System.EventHandler(this.radMenuItem1_Click);
            // 
            // TraderCatalog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.radGridView1);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Name = "TraderCatalog";
            this.Size = new System.Drawing.Size(1102, 487);
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.radGridView1.MasterTemplate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radGridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.traderViewModelBindingSource)).EndInit();
            this.panel2.ResumeLayout(false);
            this.tableLayoutPanel3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.radButton1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.btnDelete)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radTextBox1)).EndInit();
            this.panel3.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.radRadioButton2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radRadioButton1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDropDownButton1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label labelTitle;
        private Telerik.WinControls.UI.RadGridView radGridView1;
        private System.Windows.Forms.BindingSource traderViewModelBindingSource;
        private Telerik.WinControls.UI.Data.EnumBinder enumBinder1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private Telerik.WinControls.UI.RadLabel radLabel2;
        private Telerik.WinControls.UI.RadButton btnDelete;
        private Telerik.WinControls.UI.RadLabel radLabel1;
        private Telerik.WinControls.UI.RadTextBox radTextBox1;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private Telerik.WinControls.UI.RadRadioButton radRadioButton2;
        private Telerik.WinControls.UI.RadRadioButton radRadioButton1;
        private Telerik.WinControls.UI.RadButton radButton1;
        private Telerik.WinControls.UI.RadDropDownButton radDropDownButton1;
        private Telerik.WinControls.UI.RadMenuItem addCustomerMenuItem;
        private Telerik.WinControls.UI.RadMenuItem addSuplierMenuItem;
        private Telerik.WinControls.UI.RadMenuItem radMenuItem1;
    }
}

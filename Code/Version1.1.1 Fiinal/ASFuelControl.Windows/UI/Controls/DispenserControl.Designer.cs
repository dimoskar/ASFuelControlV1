namespace ASFuelControl.Windows.UI.Controls
{
    partial class DispenserControl
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
            Telerik.WinControls.UI.GridViewTextBoxColumn gridViewTextBoxColumn1 = new Telerik.WinControls.UI.GridViewTextBoxColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn1 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            Telerik.WinControls.UI.GridViewDecimalColumn gridViewDecimalColumn2 = new Telerik.WinControls.UI.GridViewDecimalColumn();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel11 = new System.Windows.Forms.Panel();
            this.labelFuelType = new System.Windows.Forms.Label();
            this.labelStatus = new System.Windows.Forms.Label();
            this.panel6 = new System.Windows.Forms.Panel();
            this.labelNozzleNumber = new System.Windows.Forms.Label();
            this.panel5 = new System.Windows.Forms.Panel();
            this.labelDispenserNumber = new System.Windows.Forms.Label();
            this.panel10 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.panel9 = new System.Windows.Forms.Panel();
            this.labelUnitPrice = new System.Windows.Forms.Label();
            this.labelUnitPriceDec = new System.Windows.Forms.Label();
            this.panel8 = new System.Windows.Forms.Panel();
            this.labelTotalVolume = new System.Windows.Forms.Label();
            this.labelTotalVolumeDec = new System.Windows.Forms.Label();
            this.panel7 = new System.Windows.Forms.Panel();
            this.labelTotalPriceDec = new System.Windows.Forms.Label();
            this.labelTotalPrice = new System.Windows.Forms.Label();
            this.panel3 = new System.Windows.Forms.Panel();
            this.dispenserSaleRadGridView = new Telerik.WinControls.UI.RadGridView();
            this.dispenserSaleBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.panel4 = new System.Windows.Forms.Panel();
            this.btnLiterCheck = new System.Windows.Forms.Panel();
            this.btnCustomer = new System.Windows.Forms.Panel();
            this.btnInvoices = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.panel6.SuspendLayout();
            this.panel5.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel9.SuspendLayout();
            this.panel8.SuspendLayout();
            this.panel7.SuspendLayout();
            this.panel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dispenserSaleRadGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dispenserSaleRadGridView.MasterTemplate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dispenserSaleBindingSource)).BeginInit();
            this.panel4.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.Gray;
            this.panel1.Controls.Add(this.panel11);
            this.panel1.Controls.Add(this.labelFuelType);
            this.panel1.Controls.Add(this.labelStatus);
            this.panel1.Controls.Add(this.panel6);
            this.panel1.Controls.Add(this.panel5);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(10, 10);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(385, 77);
            this.panel1.TabIndex = 0;
            // 
            // panel11
            // 
            this.panel11.BackgroundImage = global::ASFuelControl.Windows.Properties.Resources.Locked;
            this.panel11.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.panel11.Cursor = System.Windows.Forms.Cursors.Hand;
            this.panel11.Location = new System.Drawing.Point(345, 4);
            this.panel11.Name = "panel11";
            this.panel11.Size = new System.Drawing.Size(40, 32);
            this.panel11.TabIndex = 4;
            this.panel11.Click += new System.EventHandler(this.panel11_Click);
            // 
            // labelFuelType
            // 
            this.labelFuelType.BackColor = System.Drawing.Color.Transparent;
            this.labelFuelType.Font = new System.Drawing.Font("Tahoma", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelFuelType.ForeColor = System.Drawing.Color.White;
            this.labelFuelType.Location = new System.Drawing.Point(122, 46);
            this.labelFuelType.Name = "labelFuelType";
            this.labelFuelType.Size = new System.Drawing.Size(260, 31);
            this.labelFuelType.TabIndex = 3;
            this.labelFuelType.Text = "Πετρέλαιο Κίνησης";
            this.labelFuelType.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelStatus
            // 
            this.labelStatus.BackColor = System.Drawing.Color.Transparent;
            this.labelStatus.Font = new System.Drawing.Font("Tahoma", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelStatus.ForeColor = System.Drawing.Color.White;
            this.labelStatus.Location = new System.Drawing.Point(122, 0);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(225, 36);
            this.labelStatus.TabIndex = 2;
            this.labelStatus.Text = "Offline";
            this.labelStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panel6
            // 
            this.panel6.BackColor = System.Drawing.Color.Transparent;
            this.panel6.BackgroundImage = global::ASFuelControl.Windows.Properties.Resources.Fuel_Station;
            this.panel6.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.panel6.Controls.Add(this.labelNozzleNumber);
            this.panel6.Location = new System.Drawing.Point(42, 0);
            this.panel6.Name = "panel6";
            this.panel6.Size = new System.Drawing.Size(74, 77);
            this.panel6.TabIndex = 1;
            // 
            // labelNozzleNumber
            // 
            this.labelNozzleNumber.BackColor = System.Drawing.Color.Transparent;
            this.labelNozzleNumber.Font = new System.Drawing.Font("Quartz MS", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelNozzleNumber.ForeColor = System.Drawing.Color.Black;
            this.labelNozzleNumber.Location = new System.Drawing.Point(0, 0);
            this.labelNozzleNumber.Name = "labelNozzleNumber";
            this.labelNozzleNumber.Padding = new System.Windows.Forms.Padding(0, 15, 10, 0);
            this.labelNozzleNumber.Size = new System.Drawing.Size(74, 77);
            this.labelNozzleNumber.TabIndex = 4;
            this.labelNozzleNumber.Text = "1";
            this.labelNozzleNumber.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panel5
            // 
            this.panel5.BackColor = System.Drawing.Color.Transparent;
            this.panel5.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.panel5.Controls.Add(this.labelDispenserNumber);
            this.panel5.Controls.Add(this.panel10);
            this.panel5.Location = new System.Drawing.Point(0, 0);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(40, 77);
            this.panel5.TabIndex = 0;
            // 
            // labelDispenserNumber
            // 
            this.labelDispenserNumber.BackColor = System.Drawing.Color.Transparent;
            this.labelDispenserNumber.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelDispenserNumber.Font = new System.Drawing.Font("Quartz MS", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelDispenserNumber.ForeColor = System.Drawing.Color.White;
            this.labelDispenserNumber.Location = new System.Drawing.Point(0, 32);
            this.labelDispenserNumber.Name = "labelDispenserNumber";
            this.labelDispenserNumber.Size = new System.Drawing.Size(40, 45);
            this.labelDispenserNumber.TabIndex = 3;
            this.labelDispenserNumber.Text = "1";
            this.labelDispenserNumber.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panel10
            // 
            this.panel10.BackgroundImage = global::ASFuelControl.Windows.Properties.Resources.Information;
            this.panel10.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.panel10.Cursor = System.Windows.Forms.Cursors.Hand;
            this.panel10.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel10.Location = new System.Drawing.Point(0, 0);
            this.panel10.Name = "panel10";
            this.panel10.Size = new System.Drawing.Size(40, 32);
            this.panel10.TabIndex = 0;
            this.panel10.Click += new System.EventHandler(this.panel10_Click);
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.Color.White;
            this.panel2.Controls.Add(this.label9);
            this.panel2.Controls.Add(this.label8);
            this.panel2.Controls.Add(this.label7);
            this.panel2.Controls.Add(this.panel9);
            this.panel2.Controls.Add(this.panel8);
            this.panel2.Controls.Add(this.panel7);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel2.Location = new System.Drawing.Point(10, 87);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(385, 176);
            this.panel2.TabIndex = 1;
            // 
            // label9
            // 
            this.label9.BackColor = System.Drawing.Color.Transparent;
            this.label9.Font = new System.Drawing.Font("Tahoma", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.ForeColor = System.Drawing.Color.Black;
            this.label9.Location = new System.Drawing.Point(312, 124);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(73, 40);
            this.label9.TabIndex = 11;
            this.label9.Text = "€/Lt";
            // 
            // label8
            // 
            this.label8.BackColor = System.Drawing.Color.Transparent;
            this.label8.Font = new System.Drawing.Font("Tahoma", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.ForeColor = System.Drawing.Color.Black;
            this.label8.Location = new System.Drawing.Point(312, 66);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(54, 40);
            this.label8.TabIndex = 10;
            this.label8.Text = "Lt";
            // 
            // label7
            // 
            this.label7.BackColor = System.Drawing.Color.Transparent;
            this.label7.Font = new System.Drawing.Font("Tahoma", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.ForeColor = System.Drawing.Color.Black;
            this.label7.Location = new System.Drawing.Point(312, 7);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(54, 40);
            this.label7.TabIndex = 9;
            this.label7.Text = "€";
            // 
            // panel9
            // 
            this.panel9.BackgroundImage = global::ASFuelControl.Windows.Properties.Resources.Display;
            this.panel9.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.panel9.Controls.Add(this.labelUnitPrice);
            this.panel9.Controls.Add(this.labelUnitPriceDec);
            this.panel9.Location = new System.Drawing.Point(3, 116);
            this.panel9.Name = "panel9";
            this.panel9.Size = new System.Drawing.Size(303, 57);
            this.panel9.TabIndex = 8;
            // 
            // labelUnitPrice
            // 
            this.labelUnitPrice.BackColor = System.Drawing.Color.Transparent;
            this.labelUnitPrice.Font = new System.Drawing.Font("Quartz MS", 32F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelUnitPrice.ForeColor = System.Drawing.Color.White;
            this.labelUnitPrice.Location = new System.Drawing.Point(27, -2);
            this.labelUnitPrice.Name = "labelUnitPrice";
            this.labelUnitPrice.Size = new System.Drawing.Size(153, 48);
            this.labelUnitPrice.TabIndex = 4;
            this.labelUnitPrice.Text = "000";
            this.labelUnitPrice.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // labelUnitPriceDec
            // 
            this.labelUnitPriceDec.BackColor = System.Drawing.Color.Transparent;
            this.labelUnitPriceDec.Font = new System.Drawing.Font("Quartz MS", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelUnitPriceDec.ForeColor = System.Drawing.Color.White;
            this.labelUnitPriceDec.Location = new System.Drawing.Point(210, 8);
            this.labelUnitPriceDec.Name = "labelUnitPriceDec";
            this.labelUnitPriceDec.Size = new System.Drawing.Size(69, 40);
            this.labelUnitPriceDec.TabIndex = 3;
            this.labelUnitPriceDec.Text = "000";
            this.labelUnitPriceDec.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // panel8
            // 
            this.panel8.BackgroundImage = global::ASFuelControl.Windows.Properties.Resources.Display;
            this.panel8.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.panel8.Controls.Add(this.labelTotalVolume);
            this.panel8.Controls.Add(this.labelTotalVolumeDec);
            this.panel8.Location = new System.Drawing.Point(3, 58);
            this.panel8.Name = "panel8";
            this.panel8.Size = new System.Drawing.Size(303, 57);
            this.panel8.TabIndex = 7;
            // 
            // labelTotalVolume
            // 
            this.labelTotalVolume.BackColor = System.Drawing.Color.Transparent;
            this.labelTotalVolume.Font = new System.Drawing.Font("Quartz MS", 32F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelTotalVolume.ForeColor = System.Drawing.Color.White;
            this.labelTotalVolume.Location = new System.Drawing.Point(27, -1);
            this.labelTotalVolume.Name = "labelTotalVolume";
            this.labelTotalVolume.Size = new System.Drawing.Size(153, 48);
            this.labelTotalVolume.TabIndex = 3;
            this.labelTotalVolume.Text = "000";
            this.labelTotalVolume.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // labelTotalVolumeDec
            // 
            this.labelTotalVolumeDec.BackColor = System.Drawing.Color.Transparent;
            this.labelTotalVolumeDec.Font = new System.Drawing.Font("Quartz MS", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelTotalVolumeDec.ForeColor = System.Drawing.Color.White;
            this.labelTotalVolumeDec.Location = new System.Drawing.Point(210, 8);
            this.labelTotalVolumeDec.Name = "labelTotalVolumeDec";
            this.labelTotalVolumeDec.Size = new System.Drawing.Size(69, 40);
            this.labelTotalVolumeDec.TabIndex = 2;
            this.labelTotalVolumeDec.Text = "00";
            this.labelTotalVolumeDec.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // panel7
            // 
            this.panel7.BackgroundImage = global::ASFuelControl.Windows.Properties.Resources.Display;
            this.panel7.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.panel7.Controls.Add(this.labelTotalPriceDec);
            this.panel7.Controls.Add(this.labelTotalPrice);
            this.panel7.Location = new System.Drawing.Point(3, 0);
            this.panel7.Name = "panel7";
            this.panel7.Size = new System.Drawing.Size(303, 57);
            this.panel7.TabIndex = 6;
            // 
            // labelTotalPriceDec
            // 
            this.labelTotalPriceDec.BackColor = System.Drawing.Color.Transparent;
            this.labelTotalPriceDec.Font = new System.Drawing.Font("Quartz MS", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelTotalPriceDec.ForeColor = System.Drawing.Color.White;
            this.labelTotalPriceDec.Location = new System.Drawing.Point(210, 7);
            this.labelTotalPriceDec.Name = "labelTotalPriceDec";
            this.labelTotalPriceDec.Size = new System.Drawing.Size(69, 40);
            this.labelTotalPriceDec.TabIndex = 1;
            this.labelTotalPriceDec.Text = "00";
            this.labelTotalPriceDec.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // labelTotalPrice
            // 
            this.labelTotalPrice.BackColor = System.Drawing.Color.Transparent;
            this.labelTotalPrice.Font = new System.Drawing.Font("Quartz MS", 32F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelTotalPrice.ForeColor = System.Drawing.Color.White;
            this.labelTotalPrice.Location = new System.Drawing.Point(27, -1);
            this.labelTotalPrice.Name = "labelTotalPrice";
            this.labelTotalPrice.Size = new System.Drawing.Size(153, 48);
            this.labelTotalPrice.TabIndex = 0;
            this.labelTotalPrice.Text = "000";
            this.labelTotalPrice.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // panel3
            // 
            this.panel3.BackColor = System.Drawing.Color.White;
            this.panel3.Controls.Add(this.dispenserSaleRadGridView);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel3.Location = new System.Drawing.Point(10, 263);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(385, 114);
            this.panel3.TabIndex = 2;
            // 
            // dispenserSaleRadGridView
            // 
            this.dispenserSaleRadGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dispenserSaleRadGridView.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.dispenserSaleRadGridView.Location = new System.Drawing.Point(0, 0);
            // 
            // dispenserSaleRadGridView
            // 
            this.dispenserSaleRadGridView.MasterTemplate.AllowAddNewRow = false;
            this.dispenserSaleRadGridView.MasterTemplate.AllowDeleteRow = false;
            this.dispenserSaleRadGridView.MasterTemplate.AutoGenerateColumns = false;
            this.dispenserSaleRadGridView.MasterTemplate.AutoSizeColumnsMode = Telerik.WinControls.UI.GridViewAutoSizeColumnsMode.Fill;
            gridViewTextBoxColumn1.FieldName = "FuelDescription";
            gridViewTextBoxColumn1.HeaderText = "FuelDescription";
            gridViewTextBoxColumn1.IsAutoGenerated = true;
            gridViewTextBoxColumn1.Name = "FuelDescription";
            gridViewTextBoxColumn1.Width = 193;
            gridViewDecimalColumn1.FieldName = "TotalVolume";
            gridViewDecimalColumn1.FormatString = "{0:N2} Lt";
            gridViewDecimalColumn1.HeaderText = "TotalVolume";
            gridViewDecimalColumn1.IsAutoGenerated = true;
            gridViewDecimalColumn1.Name = "TotalVolume";
            gridViewDecimalColumn1.Width = 96;
            gridViewDecimalColumn2.FieldName = "TotalPrice";
            gridViewDecimalColumn2.FormatString = "{0:N2} €";
            gridViewDecimalColumn2.HeaderText = "TotalPrice";
            gridViewDecimalColumn2.IsAutoGenerated = true;
            gridViewDecimalColumn2.Name = "TotalPrice";
            gridViewDecimalColumn2.Width = 97;
            this.dispenserSaleRadGridView.MasterTemplate.Columns.AddRange(new Telerik.WinControls.UI.GridViewDataColumn[] {
            gridViewTextBoxColumn1,
            gridViewDecimalColumn1,
            gridViewDecimalColumn2});
            this.dispenserSaleRadGridView.MasterTemplate.DataSource = this.dispenserSaleBindingSource;
            this.dispenserSaleRadGridView.MasterTemplate.EnableSorting = false;
            this.dispenserSaleRadGridView.MasterTemplate.HorizontalScrollState = Telerik.WinControls.UI.ScrollState.AlwaysHide;
            this.dispenserSaleRadGridView.MasterTemplate.ShowColumnHeaders = false;
            this.dispenserSaleRadGridView.MasterTemplate.ShowRowHeaderColumn = false;
            this.dispenserSaleRadGridView.MasterTemplate.VerticalScrollState = Telerik.WinControls.UI.ScrollState.AlwaysHide;
            this.dispenserSaleRadGridView.Name = "dispenserSaleRadGridView";
            this.dispenserSaleRadGridView.ReadOnly = true;
            this.dispenserSaleRadGridView.ShowGroupPanel = false;
            this.dispenserSaleRadGridView.Size = new System.Drawing.Size(385, 114);
            this.dispenserSaleRadGridView.TabIndex = 0;
            this.dispenserSaleRadGridView.Text = "radGridView1";
            // 
            // dispenserSaleBindingSource
            // 
            this.dispenserSaleBindingSource.DataSource = typeof(ASFuelControl.VirtualDevices.DispenserSale);
            // 
            // panel4
            // 
            this.panel4.Controls.Add(this.btnLiterCheck);
            this.panel4.Controls.Add(this.btnCustomer);
            this.panel4.Controls.Add(this.btnInvoices);
            this.panel4.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel4.Location = new System.Drawing.Point(10, 377);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(385, 73);
            this.panel4.TabIndex = 3;
            // 
            // btnLiterCheck
            // 
            this.btnLiterCheck.BackgroundImage = global::ASFuelControl.Windows.Properties.Resources.Bottle;
            this.btnLiterCheck.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.btnLiterCheck.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.btnLiterCheck.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnLiterCheck.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnLiterCheck.Location = new System.Drawing.Point(128, 0);
            this.btnLiterCheck.Name = "btnLiterCheck";
            this.btnLiterCheck.Size = new System.Drawing.Size(129, 73);
            this.btnLiterCheck.TabIndex = 1;
            this.btnLiterCheck.Click += new System.EventHandler(this.btnLiterCheck_Click);
            this.btnLiterCheck.MouseLeave += new System.EventHandler(this.btnInvoices_MouseLeave);
            this.btnLiterCheck.MouseMove += new System.Windows.Forms.MouseEventHandler(this.btnInvoices_MouseMove);
            // 
            // btnCustomer
            // 
            this.btnCustomer.BackgroundImage = global::ASFuelControl.Windows.Properties.Resources.Customer;
            this.btnCustomer.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.btnCustomer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.btnCustomer.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCustomer.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnCustomer.Location = new System.Drawing.Point(257, 0);
            this.btnCustomer.Name = "btnCustomer";
            this.btnCustomer.Size = new System.Drawing.Size(128, 73);
            this.btnCustomer.TabIndex = 2;
            this.btnCustomer.Click += new System.EventHandler(this.btnCustomer_Click);
            this.btnCustomer.MouseLeave += new System.EventHandler(this.btnInvoices_MouseLeave);
            this.btnCustomer.MouseMove += new System.Windows.Forms.MouseEventHandler(this.btnInvoices_MouseMove);
            // 
            // btnInvoices
            // 
            this.btnInvoices.BackgroundImage = global::ASFuelControl.Windows.Properties.Resources.Invoice_100;
            this.btnInvoices.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.btnInvoices.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.btnInvoices.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnInvoices.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnInvoices.Location = new System.Drawing.Point(0, 0);
            this.btnInvoices.Name = "btnInvoices";
            this.btnInvoices.Size = new System.Drawing.Size(128, 73);
            this.btnInvoices.TabIndex = 0;
            this.btnInvoices.Click += new System.EventHandler(this.btnInvoices_Click);
            this.btnInvoices.MouseLeave += new System.EventHandler(this.btnInvoices_MouseLeave);
            this.btnInvoices.MouseMove += new System.Windows.Forms.MouseEventHandler(this.btnInvoices_MouseMove);
            // 
            // DispenserControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel4);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.DoubleBuffered = true;
            this.Name = "DispenserControl";
            this.Padding = new System.Windows.Forms.Padding(10);
            this.Size = new System.Drawing.Size(405, 460);
            this.panel1.ResumeLayout(false);
            this.panel6.ResumeLayout(false);
            this.panel5.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel9.ResumeLayout(false);
            this.panel8.ResumeLayout(false);
            this.panel7.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dispenserSaleRadGridView.MasterTemplate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dispenserSaleRadGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dispenserSaleBindingSource)).EndInit();
            this.panel4.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Panel panel6;
        private System.Windows.Forms.Panel panel7;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Panel panel9;
        private System.Windows.Forms.Label labelUnitPriceDec;
        private System.Windows.Forms.Panel panel8;
        private System.Windows.Forms.Label labelTotalVolumeDec;
        private System.Windows.Forms.Label labelTotalPriceDec;
        private System.Windows.Forms.Label labelTotalPrice;
        private System.Windows.Forms.Label labelUnitPrice;
        private System.Windows.Forms.Label labelTotalVolume;
        private System.Windows.Forms.Label labelFuelType;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.Label labelDispenserNumber;
        private System.Windows.Forms.Panel btnLiterCheck;
        private System.Windows.Forms.Panel btnCustomer;
        private System.Windows.Forms.Panel btnInvoices;
        private Telerik.WinControls.UI.RadGridView dispenserSaleRadGridView;
        private System.Windows.Forms.BindingSource dispenserSaleBindingSource;
        private System.Windows.Forms.Label labelNozzleNumber;
        private System.Windows.Forms.Panel panel10;
        private System.Windows.Forms.Panel panel11;
    }
}

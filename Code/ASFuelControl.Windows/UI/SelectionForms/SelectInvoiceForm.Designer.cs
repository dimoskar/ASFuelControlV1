namespace ASFuelControl.Windows.UI.SelectionForms
{
    partial class SelectInvoiceForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SelectInvoiceForm));
            this.radPageView1 = new Telerik.WinControls.UI.RadPageView();
            this.radPageViewPage1 = new Telerik.WinControls.UI.RadPageViewPage();
            this.selectInvoiceControl1 = new ASFuelControl.Windows.UI.SelectionForms.SelectInvoiceControl();
            this.radPageViewPage2 = new Telerik.WinControls.UI.RadPageViewPage();
            this.selectInvoiceControl2 = new ASFuelControl.Windows.UI.SelectionForms.SelectInvoiceControl();
            this.radPageViewPage3 = new Telerik.WinControls.UI.RadPageViewPage();
            this.selectInvoiceControl3 = new ASFuelControl.Windows.UI.SelectionForms.SelectInvoiceControl();
            this.invoiceLineBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.panel1 = new System.Windows.Forms.Panel();
            this.radButton2 = new Telerik.WinControls.UI.RadButton();
            this.panel2 = new System.Windows.Forms.Panel();
            this.radDateTimePicker1 = new Telerik.WinControls.UI.RadDateTimePicker();
            this.radLabel1 = new Telerik.WinControls.UI.RadLabel();
            this.radLabel2 = new Telerik.WinControls.UI.RadLabel();
            this.radDateTimePicker2 = new Telerik.WinControls.UI.RadDateTimePicker();
            this.radButton1 = new Telerik.WinControls.UI.RadButton();
            ((System.ComponentModel.ISupportInitialize)(this.radPageView1)).BeginInit();
            this.radPageView1.SuspendLayout();
            this.radPageViewPage1.SuspendLayout();
            this.radPageViewPage2.SuspendLayout();
            this.radPageViewPage3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.invoiceLineBindingSource)).BeginInit();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.radButton2)).BeginInit();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.radDateTimePicker1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDateTimePicker2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();
            // 
            // radPageView1
            // 
            this.radPageView1.Controls.Add(this.radPageViewPage1);
            this.radPageView1.Controls.Add(this.radPageViewPage2);
            this.radPageView1.Controls.Add(this.radPageViewPage3);
            this.radPageView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.radPageView1.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radPageView1.Location = new System.Drawing.Point(0, 47);
            this.radPageView1.Name = "radPageView1";
            this.radPageView1.SelectedPage = this.radPageViewPage1;
            this.radPageView1.Size = new System.Drawing.Size(963, 357);
            this.radPageView1.TabIndex = 0;
            this.radPageView1.SelectedPageChanged += new System.EventHandler(this.radPageView1_SelectedPageChanged);
            ((Telerik.WinControls.UI.RadPageViewStripElement)(this.radPageView1.GetChildAt(0))).StripButtons = Telerik.WinControls.UI.StripViewButtons.None;
            // 
            // radPageViewPage1
            // 
            this.radPageViewPage1.Controls.Add(this.selectInvoiceControl1);
            this.radPageViewPage1.Location = new System.Drawing.Point(10, 40);
            this.radPageViewPage1.Name = "radPageViewPage1";
            this.radPageViewPage1.Size = new System.Drawing.Size(942, 306);
            this.radPageViewPage1.Text = "Δελτία Παραλαβής";
            // 
            // selectInvoiceControl1
            // 
            this.selectInvoiceControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.selectInvoiceControl1.FillingMode = ASFuelControl.Windows.UI.SelectionForms.SelectInvoiceControl.FillingModeEnum.Delivery;
            this.selectInvoiceControl1.FuelTypeId = new System.Guid("00000000-0000-0000-0000-000000000000");
            this.selectInvoiceControl1.Location = new System.Drawing.Point(0, 0);
            this.selectInvoiceControl1.Name = "selectInvoiceControl1";
            this.selectInvoiceControl1.Size = new System.Drawing.Size(942, 306);
            this.selectInvoiceControl1.TabIndex = 0;
            this.selectInvoiceControl1.TankId = new System.Guid("00000000-0000-0000-0000-000000000000");
            // 
            // radPageViewPage2
            // 
            this.radPageViewPage2.Controls.Add(this.selectInvoiceControl2);
            this.radPageViewPage2.Location = new System.Drawing.Point(10, 40);
            this.radPageViewPage2.Name = "radPageViewPage2";
            this.radPageViewPage2.Size = new System.Drawing.Size(942, 316);
            this.radPageViewPage2.Text = "Λιτρομετρήσεις";
            // 
            // selectInvoiceControl2
            // 
            this.selectInvoiceControl2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.selectInvoiceControl2.FillingMode = ASFuelControl.Windows.UI.SelectionForms.SelectInvoiceControl.FillingModeEnum.LiterCheck;
            this.selectInvoiceControl2.FuelTypeId = new System.Guid("00000000-0000-0000-0000-000000000000");
            this.selectInvoiceControl2.Location = new System.Drawing.Point(0, 0);
            this.selectInvoiceControl2.Name = "selectInvoiceControl2";
            this.selectInvoiceControl2.Size = new System.Drawing.Size(942, 316);
            this.selectInvoiceControl2.TabIndex = 1;
            this.selectInvoiceControl2.TankId = new System.Guid("00000000-0000-0000-0000-000000000000");
            // 
            // radPageViewPage3
            // 
            this.radPageViewPage3.Controls.Add(this.selectInvoiceControl3);
            this.radPageViewPage3.Location = new System.Drawing.Point(10, 40);
            this.radPageViewPage3.Name = "radPageViewPage3";
            this.radPageViewPage3.Size = new System.Drawing.Size(942, 316);
            this.radPageViewPage3.Text = "Δελτία Επιστροφής";
            // 
            // selectInvoiceControl3
            // 
            this.selectInvoiceControl3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.selectInvoiceControl3.FillingMode = ASFuelControl.Windows.UI.SelectionForms.SelectInvoiceControl.FillingModeEnum.Return;
            this.selectInvoiceControl3.FuelTypeId = new System.Guid("00000000-0000-0000-0000-000000000000");
            this.selectInvoiceControl3.Location = new System.Drawing.Point(0, 0);
            this.selectInvoiceControl3.Name = "selectInvoiceControl3";
            this.selectInvoiceControl3.Size = new System.Drawing.Size(942, 316);
            this.selectInvoiceControl3.TabIndex = 1;
            this.selectInvoiceControl3.TankId = new System.Guid("00000000-0000-0000-0000-000000000000");
            // 
            // invoiceLineBindingSource
            // 
            this.invoiceLineBindingSource.DataSource = typeof(ASFuelControl.Data.InvoiceLine);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.radButton2);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 404);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(963, 45);
            this.panel1.TabIndex = 1;
            // 
            // radButton2
            // 
            this.radButton2.Dock = System.Windows.Forms.DockStyle.Right;
            this.radButton2.Image = global::ASFuelControl.Windows.Properties.Resources.Delete;
            this.radButton2.Location = new System.Drawing.Point(860, 0);
            this.radButton2.Name = "radButton2";
            this.radButton2.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.radButton2.Size = new System.Drawing.Size(103, 45);
            this.radButton2.TabIndex = 3;
            this.radButton2.Text = "Άκυρο";
            this.radButton2.TextAlignment = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.radButton1);
            this.panel2.Controls.Add(this.radLabel2);
            this.panel2.Controls.Add(this.radDateTimePicker2);
            this.panel2.Controls.Add(this.radLabel1);
            this.panel2.Controls.Add(this.radDateTimePicker1);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(963, 47);
            this.panel2.TabIndex = 2;
            // 
            // radDateTimePicker1
            // 
            this.radDateTimePicker1.CustomFormat = "dd/MM/yyyy";
            this.radDateTimePicker1.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.radDateTimePicker1.Location = new System.Drawing.Point(51, 12);
            this.radDateTimePicker1.Name = "radDateTimePicker1";
            this.radDateTimePicker1.Size = new System.Drawing.Size(95, 20);
            this.radDateTimePicker1.TabIndex = 0;
            this.radDateTimePicker1.TabStop = false;
            this.radDateTimePicker1.Text = "07/10/2014";
            this.radDateTimePicker1.Value = new System.DateTime(2014, 10, 7, 18, 34, 30, 304);
            // 
            // radLabel1
            // 
            this.radLabel1.Location = new System.Drawing.Point(13, 13);
            this.radLabel1.Name = "radLabel1";
            this.radLabel1.Size = new System.Drawing.Size(27, 18);
            this.radLabel1.TabIndex = 1;
            this.radLabel1.Text = "Από";
            // 
            // radLabel2
            // 
            this.radLabel2.Location = new System.Drawing.Point(173, 13);
            this.radLabel2.Name = "radLabel2";
            this.radLabel2.Size = new System.Drawing.Size(26, 18);
            this.radLabel2.TabIndex = 3;
            this.radLabel2.Text = "Εώς";
            // 
            // radDateTimePicker2
            // 
            this.radDateTimePicker2.CustomFormat = "dd/MM/yyyy";
            this.radDateTimePicker2.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.radDateTimePicker2.Location = new System.Drawing.Point(211, 12);
            this.radDateTimePicker2.Name = "radDateTimePicker2";
            this.radDateTimePicker2.Size = new System.Drawing.Size(95, 20);
            this.radDateTimePicker2.TabIndex = 2;
            this.radDateTimePicker2.TabStop = false;
            this.radDateTimePicker2.Text = "07/10/2014";
            this.radDateTimePicker2.Value = new System.DateTime(2014, 10, 7, 18, 34, 30, 304);
            // 
            // radButton1
            // 
            this.radButton1.Image = global::ASFuelControl.Windows.Properties.Resources.Search;
            this.radButton1.Location = new System.Drawing.Point(315, 5);
            this.radButton1.Name = "radButton1";
            this.radButton1.Size = new System.Drawing.Size(37, 34);
            this.radButton1.TabIndex = 4;
            this.radButton1.Click += new System.EventHandler(this.radButton1_Click);
            // 
            // SelectInvoiceForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(963, 449);
            this.Controls.Add(this.radPageView1);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.panel2);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "SelectInvoiceForm";
            // 
            // 
            // 
            this.RootElement.ApplyShapeToControl = true;
            this.Text = "Επιλογή Παραστατικού";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SelectInvoiceForm_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.radPageView1)).EndInit();
            this.radPageView1.ResumeLayout(false);
            this.radPageViewPage1.ResumeLayout(false);
            this.radPageViewPage2.ResumeLayout(false);
            this.radPageViewPage3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.invoiceLineBindingSource)).EndInit();
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.radButton2)).EndInit();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.radDateTimePicker1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDateTimePicker2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private Telerik.WinControls.UI.RadPageView radPageView1;
        private Telerik.WinControls.UI.RadPageViewPage radPageViewPage1;
        private Telerik.WinControls.UI.RadPageViewPage radPageViewPage2;
        private Telerik.WinControls.UI.RadPageViewPage radPageViewPage3;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.BindingSource invoiceLineBindingSource;
        private Telerik.WinControls.UI.RadButton radButton2;
        private SelectInvoiceControl selectInvoiceControl1;
        private SelectInvoiceControl selectInvoiceControl2;
        private SelectInvoiceControl selectInvoiceControl3;
        private System.Windows.Forms.Panel panel2;
        private Telerik.WinControls.UI.RadButton radButton1;
        private Telerik.WinControls.UI.RadLabel radLabel2;
        private Telerik.WinControls.UI.RadDateTimePicker radDateTimePicker2;
        private Telerik.WinControls.UI.RadLabel radLabel1;
        private Telerik.WinControls.UI.RadDateTimePicker radDateTimePicker1;
    }
}
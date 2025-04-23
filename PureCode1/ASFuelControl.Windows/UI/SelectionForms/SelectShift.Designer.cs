namespace ASFuelControl.Windows.UI.SelectionForms
{
    partial class SelectShift
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
            Telerik.WinControls.UI.GridViewDateTimeColumn gridViewDateTimeColumn2 = new Telerik.WinControls.UI.GridViewDateTimeColumn();
            Telerik.WinControls.UI.GridViewTextBoxColumn gridViewTextBoxColumn1 = new Telerik.WinControls.UI.GridViewTextBoxColumn();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SelectShift));
            this.panel1 = new System.Windows.Forms.Panel();
            this.radLabel2 = new Telerik.WinControls.UI.RadLabel();
            this.radDateTimePicker2 = new Telerik.WinControls.UI.RadDateTimePicker();
            this.radLabel1 = new Telerik.WinControls.UI.RadLabel();
            this.radDateTimePicker1 = new Telerik.WinControls.UI.RadDateTimePicker();
            this.shiftBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.shiftRadGridView = new Telerik.WinControls.UI.RadGridView();
            this.radButton1 = new Telerik.WinControls.UI.RadButton();
            this.radButton2 = new Telerik.WinControls.UI.RadButton();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDateTimePicker2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDateTimePicker1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.shiftBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.shiftRadGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.shiftRadGridView.MasterTemplate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.radLabel2);
            this.panel1.Controls.Add(this.radDateTimePicker2);
            this.panel1.Controls.Add(this.radLabel1);
            this.panel1.Controls.Add(this.radDateTimePicker1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(491, 31);
            this.panel1.TabIndex = 0;
            // 
            // radLabel2
            // 
            this.radLabel2.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radLabel2.Location = new System.Drawing.Point(232, 4);
            this.radLabel2.Name = "radLabel2";
            this.radLabel2.Size = new System.Drawing.Size(31, 21);
            this.radLabel2.TabIndex = 3;
            this.radLabel2.Text = "Έως";
            this.radLabel2.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // radDateTimePicker2
            // 
            this.radDateTimePicker2.CustomFormat = "dddd dd/MM/yyyy";
            this.radDateTimePicker2.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radDateTimePicker2.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.radDateTimePicker2.Location = new System.Drawing.Point(265, 3);
            this.radDateTimePicker2.Name = "radDateTimePicker2";
            this.radDateTimePicker2.Size = new System.Drawing.Size(174, 23);
            this.radDateTimePicker2.TabIndex = 2;
            this.radDateTimePicker2.TabStop = false;
            this.radDateTimePicker2.Text = "Σάββατο 21/06/2014";
            this.radDateTimePicker2.Value = new System.DateTime(2014, 6, 21, 14, 12, 49, 57);
            // 
            // radLabel1
            // 
            this.radLabel1.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radLabel1.Location = new System.Drawing.Point(4, 4);
            this.radLabel1.Name = "radLabel1";
            this.radLabel1.Size = new System.Drawing.Size(32, 21);
            this.radLabel1.TabIndex = 1;
            this.radLabel1.Text = "Από";
            this.radLabel1.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // radDateTimePicker1
            // 
            this.radDateTimePicker1.CustomFormat = "dddd dd/MM/yyyy";
            this.radDateTimePicker1.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radDateTimePicker1.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.radDateTimePicker1.Location = new System.Drawing.Point(37, 3);
            this.radDateTimePicker1.Name = "radDateTimePicker1";
            this.radDateTimePicker1.Size = new System.Drawing.Size(177, 23);
            this.radDateTimePicker1.TabIndex = 0;
            this.radDateTimePicker1.TabStop = false;
            this.radDateTimePicker1.Text = "Σάββατο 21/06/2014";
            this.radDateTimePicker1.Value = new System.DateTime(2014, 6, 21, 14, 12, 49, 57);
            // 
            // shiftBindingSource
            // 
            this.shiftBindingSource.DataSource = typeof(ASFuelControl.Data.Shift);
            // 
            // shiftRadGridView
            // 
            this.shiftRadGridView.Location = new System.Drawing.Point(4, 37);
            // 
            // shiftRadGridView
            // 
            this.shiftRadGridView.MasterTemplate.AllowAddNewRow = false;
            this.shiftRadGridView.MasterTemplate.AutoGenerateColumns = false;
            gridViewDateTimeColumn1.FieldName = "ShiftBegin";
            gridViewDateTimeColumn1.FormatString = "{0:dd/MM/yyyy HH:mm}";
            gridViewDateTimeColumn1.HeaderText = "Εναρξη";
            gridViewDateTimeColumn1.IsAutoGenerated = true;
            gridViewDateTimeColumn1.Name = "ShiftBegin";
            gridViewDateTimeColumn1.Width = 150;
            gridViewDateTimeColumn2.DataType = typeof(System.Nullable<System.DateTime>);
            gridViewDateTimeColumn2.FieldName = "ShiftEnd";
            gridViewDateTimeColumn2.FormatString = "{0:dd/MM/yyyy HH:mm}";
            gridViewDateTimeColumn2.HeaderText = "Λήξη";
            gridViewDateTimeColumn2.IsAutoGenerated = true;
            gridViewDateTimeColumn2.Name = "ShiftEnd";
            gridViewDateTimeColumn2.Width = 150;
            gridViewTextBoxColumn1.FieldName = "ApplicationUser.UserName";
            gridViewTextBoxColumn1.HeaderText = "Χειριστής";
            gridViewTextBoxColumn1.IsAutoGenerated = true;
            gridViewTextBoxColumn1.Name = "ApplicationUser";
            gridViewTextBoxColumn1.Width = 150;
            this.shiftRadGridView.MasterTemplate.Columns.AddRange(new Telerik.WinControls.UI.GridViewDataColumn[] {
            gridViewDateTimeColumn1,
            gridViewDateTimeColumn2,
            gridViewTextBoxColumn1});
            this.shiftRadGridView.MasterTemplate.DataSource = this.shiftBindingSource;
            this.shiftRadGridView.Name = "shiftRadGridView";
            this.shiftRadGridView.ReadOnly = true;
            this.shiftRadGridView.Size = new System.Drawing.Size(485, 345);
            this.shiftRadGridView.TabIndex = 2;
            this.shiftRadGridView.Text = "radGridView1";
            // 
            // radButton1
            // 
            this.radButton1.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radButton1.Location = new System.Drawing.Point(4, 388);
            this.radButton1.Name = "radButton1";
            this.radButton1.Size = new System.Drawing.Size(119, 24);
            this.radButton1.TabIndex = 3;
            this.radButton1.Text = "Επιλογή";
            this.radButton1.Click += new System.EventHandler(this.radButton1_Click);
            // 
            // radButton2
            // 
            this.radButton2.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radButton2.Location = new System.Drawing.Point(370, 388);
            this.radButton2.Name = "radButton2";
            this.radButton2.Size = new System.Drawing.Size(119, 24);
            this.radButton2.TabIndex = 4;
            this.radButton2.Text = "Άκυρο";
            this.radButton2.Click += new System.EventHandler(this.radButton2_Click);
            // 
            // SelectShift
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(491, 416);
            this.ControlBox = false;
            this.Controls.Add(this.radButton2);
            this.Controls.Add(this.radButton1);
            this.Controls.Add(this.shiftRadGridView);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "SelectShift";
            // 
            // 
            // 
            this.RootElement.ApplyShapeToControl = true;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Επιλογή Βάρδιας";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDateTimePicker2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDateTimePicker1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.shiftBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.shiftRadGridView.MasterTemplate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.shiftRadGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private Telerik.WinControls.UI.RadDateTimePicker radDateTimePicker1;
        private Telerik.WinControls.UI.RadLabel radLabel2;
        private Telerik.WinControls.UI.RadDateTimePicker radDateTimePicker2;
        private Telerik.WinControls.UI.RadLabel radLabel1;
        private System.Windows.Forms.BindingSource shiftBindingSource;
        private Telerik.WinControls.UI.RadGridView shiftRadGridView;
        private Telerik.WinControls.UI.RadButton radButton1;
        private Telerik.WinControls.UI.RadButton radButton2;
    }
}
namespace ASFuelControl.Windows.UI.SelectionForms
{
    partial class SelectionBaseForm
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
            this.btnSearch = new Telerik.WinControls.UI.RadButton();
            this.dataRadGridView = new Telerik.WinControls.UI.RadGridView();
            this.radLabel1 = new Telerik.WinControls.UI.RadLabel();
            this.radTextBox1 = new Telerik.WinControls.UI.RadTextBox();
            this.btnCancel = new Telerik.WinControls.UI.RadButton();
            this.btnSelect = new Telerik.WinControls.UI.RadButton();
            this.radButton1 = new Telerik.WinControls.UI.RadButton();
            ((System.ComponentModel.ISupportInitialize)(this.btnSearch)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataRadGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataRadGridView.MasterTemplate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radTextBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.btnCancel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.btnSelect)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();
            // 
            // btnSearch
            // 
            this.btnSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSearch.DisplayStyle = Telerik.WinControls.DisplayStyle.Image;
            this.btnSearch.Image = global::ASFuelControl.Windows.Properties.Resources.Search_small;
            this.btnSearch.ImageAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            this.btnSearch.Location = new System.Drawing.Point(500, 9);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(26, 24);
            this.btnSearch.TabIndex = 30;
            this.btnSearch.Text = "Εύρεση";
            this.btnSearch.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
            // 
            // dataRadGridView
            // 
            this.dataRadGridView.Location = new System.Drawing.Point(12, 39);
            // 
            // dataRadGridView
            // 
            this.dataRadGridView.MasterTemplate.AllowAddNewRow = false;
            this.dataRadGridView.MasterTemplate.AutoGenerateColumns = false;
            this.dataRadGridView.MasterTemplate.EnableAlternatingRowColor = true;
            this.dataRadGridView.Name = "dataRadGridView";
            this.dataRadGridView.ReadOnly = true;
            this.dataRadGridView.ShowGroupPanel = false;
            this.dataRadGridView.Size = new System.Drawing.Size(514, 306);
            this.dataRadGridView.TabIndex = 1;
            this.dataRadGridView.Text = "radGridView1";
            this.dataRadGridView.CellDoubleClick += new Telerik.WinControls.UI.GridViewCellEventHandler(this.dataRadGridView_CellDoubleClick);
            this.dataRadGridView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.dataRadGridView_KeyDown);
            // 
            // radLabel1
            // 
            this.radLabel1.Location = new System.Drawing.Point(12, 12);
            this.radLabel1.Name = "radLabel1";
            this.radLabel1.Size = new System.Drawing.Size(42, 18);
            this.radLabel1.TabIndex = 28;
            this.radLabel1.Text = "Φίλτρο";
            // 
            // radTextBox1
            // 
            this.radTextBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.radTextBox1.Location = new System.Drawing.Point(73, 11);
            this.radTextBox1.Name = "radTextBox1";
            this.radTextBox1.Size = new System.Drawing.Size(421, 20);
            this.radTextBox1.TabIndex = 0;
            this.radTextBox1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.radTextBox1_KeyDown);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.DisplayStyle = Telerik.WinControls.DisplayStyle.Text;
            this.btnCancel.Location = new System.Drawing.Point(416, 351);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(110, 31);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "Άκυρο";
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnSelect
            // 
            this.btnSelect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnSelect.DisplayStyle = Telerik.WinControls.DisplayStyle.Text;
            this.btnSelect.Location = new System.Drawing.Point(12, 351);
            this.btnSelect.Name = "btnSelect";
            this.btnSelect.Size = new System.Drawing.Size(110, 31);
            this.btnSelect.TabIndex = 2;
            this.btnSelect.Text = "Επιλογή";
            this.btnSelect.Click += new System.EventHandler(this.btnSelect_Click);
            // 
            // radButton1
            // 
            this.radButton1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.radButton1.Image = global::ASFuelControl.Windows.Properties.Resources.Add24;
            this.radButton1.Location = new System.Drawing.Point(128, 351);
            this.radButton1.Name = "radButton1";
            this.radButton1.Padding = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.radButton1.Size = new System.Drawing.Size(94, 31);
            this.radButton1.TabIndex = 31;
            this.radButton1.Text = "Προσθήκη";
            this.radButton1.TextAlignment = System.Drawing.ContentAlignment.MiddleRight;
            this.radButton1.Visible = false;
            // 
            // SelectionBaseForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(538, 387);
            this.Controls.Add(this.radButton1);
            this.Controls.Add(this.btnSearch);
            this.Controls.Add(this.dataRadGridView);
            this.Controls.Add(this.radLabel1);
            this.Controls.Add(this.radTextBox1);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSelect);
            this.Name = "SelectionBaseForm";
            // 
            // 
            // 
            this.RootElement.ApplyShapeToControl = true;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "SelectionBaseForm";
            this.Resize += new System.EventHandler(this.SelectionBaseForm_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.btnSearch)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataRadGridView.MasterTemplate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataRadGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radTextBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.btnCancel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.btnSelect)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        protected Telerik.WinControls.UI.RadButton btnSearch;
        private Telerik.WinControls.UI.RadLabel radLabel1;
        protected Telerik.WinControls.UI.RadTextBox radTextBox1;
        protected Telerik.WinControls.UI.RadButton btnCancel;
        protected Telerik.WinControls.UI.RadButton btnSelect;
        public Telerik.WinControls.UI.RadGridView dataRadGridView;
        protected Telerik.WinControls.UI.RadButton radButton1;
    }
}
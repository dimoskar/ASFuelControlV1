namespace ASFuelControl.Windows.UI.SelectionForms
{
    partial class SelectCompanyForm
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
            this.radListView1 = new Telerik.WinControls.UI.RadListView();
            this.radButton1 = new Telerik.WinControls.UI.RadButton();
            this.radButton2 = new Telerik.WinControls.UI.RadButton();
            this.oilCompanyBindingSource = new System.Windows.Forms.BindingSource(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.radListView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.oilCompanyBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();
            // 
            // radListView1
            // 
            this.radListView1.AllowColumnReorder = false;
            this.radListView1.AllowColumnResize = false;
            this.radListView1.AllowEdit = false;
            this.radListView1.AllowRemove = false;
            this.radListView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.radListView1.DataSource = this.oilCompanyBindingSource;
            this.radListView1.DisplayMember = "Name";
            this.radListView1.FullRowSelect = false;
            this.radListView1.ItemSize = new System.Drawing.Size(120, 120);
            this.radListView1.ItemSpacing = 20;
            this.radListView1.Location = new System.Drawing.Point(12, 12);
            this.radListView1.Name = "radListView1";
            this.radListView1.SelectLastAddedItem = false;
            this.radListView1.ShowColumnHeaders = false;
            this.radListView1.Size = new System.Drawing.Size(662, 411);
            this.radListView1.TabIndex = 1;
            this.radListView1.Text = "radListView1";
            this.radListView1.ValueMember = "OilCompanyId";
            this.radListView1.ViewType = Telerik.WinControls.UI.ListViewType.IconsView;
            this.radListView1.VisualItemFormatting += new Telerik.WinControls.UI.ListViewVisualItemEventHandler(this.radListView1_VisualItemFormatting);
            this.radListView1.ItemDataBound += new Telerik.WinControls.UI.ListViewItemEventHandler(this.radListView1_ItemDataBound);
            // 
            // radButton1
            // 
            this.radButton1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.radButton1.Location = new System.Drawing.Point(12, 429);
            this.radButton1.Name = "radButton1";
            this.radButton1.Size = new System.Drawing.Size(110, 24);
            this.radButton1.TabIndex = 0;
            this.radButton1.Text = "Επιλογή";
            this.radButton1.Click += new System.EventHandler(this.radButton1_Click);
            // 
            // radButton2
            // 
            this.radButton2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.radButton2.Location = new System.Drawing.Point(564, 429);
            this.radButton2.Name = "radButton2";
            this.radButton2.Size = new System.Drawing.Size(110, 24);
            this.radButton2.TabIndex = 2;
            this.radButton2.Text = "Άκυρο";
            this.radButton2.Click += new System.EventHandler(this.radButton2_Click);
            // 
            // oilCompanyBindingSource
            // 
            this.oilCompanyBindingSource.DataSource = typeof(ASFuelControl.Data.OilCompany);
            // 
            // SelectCompanyForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(686, 465);
            this.Controls.Add(this.radButton2);
            this.Controls.Add(this.radButton1);
            this.Controls.Add(this.radListView1);
            this.Name = "SelectCompanyForm";
            // 
            // 
            // 
            this.RootElement.ApplyShapeToControl = true;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Επιλογή Εταιρείας...";
            ((System.ComponentModel.ISupportInitialize)(this.radListView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.oilCompanyBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private Telerik.WinControls.UI.RadListView radListView1;
        private Telerik.WinControls.UI.RadButton radButton1;
        private Telerik.WinControls.UI.RadButton radButton2;
        private System.Windows.Forms.BindingSource oilCompanyBindingSource;
    }
}
namespace ASFuelControl.Windows.UI.SelectionForms
{
    partial class SelectController
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
            this.radButton2 = new Telerik.WinControls.UI.RadButton();
            this.radButton1 = new Telerik.WinControls.UI.RadButton();
            this.radListControl1 = new Telerik.WinControls.UI.RadListControl();
            this.communicationControllerBindingSource = new System.Windows.Forms.BindingSource(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.radButton2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radListControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.communicationControllerBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();
            // 
            // radButton2
            // 
            this.radButton2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.radButton2.Location = new System.Drawing.Point(220, 202);
            this.radButton2.Name = "radButton2";
            this.radButton2.Size = new System.Drawing.Size(110, 24);
            this.radButton2.TabIndex = 9;
            this.radButton2.Text = "Άκυρο";
            this.radButton2.Click += new System.EventHandler(this.radButton2_Click);
            // 
            // radButton1
            // 
            this.radButton1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.radButton1.Location = new System.Drawing.Point(12, 202);
            this.radButton1.Name = "radButton1";
            this.radButton1.Size = new System.Drawing.Size(110, 24);
            this.radButton1.TabIndex = 8;
            this.radButton1.Text = "Επιλογή";
            this.radButton1.Click += new System.EventHandler(this.radButton1_Click);
            // 
            // radListControl1
            // 
            this.radListControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.radListControl1.DataSource = this.communicationControllerBindingSource;
            this.radListControl1.Location = new System.Drawing.Point(12, 12);
            this.radListControl1.Name = "radListControl1";
            this.radListControl1.SelectionMode = System.Windows.Forms.SelectionMode.MultiSimple;
            this.radListControl1.Size = new System.Drawing.Size(318, 184);
            this.radListControl1.TabIndex = 10;
            this.radListControl1.Text = "radListControl1";
            this.radListControl1.SelectedItemsChanged += new Telerik.WinControls.Data.NotifyCollectionChangedEventHandler(this.radListControl1_SelectedItemsChanged);
            // 
            // communicationControllerBindingSource
            // 
            this.communicationControllerBindingSource.DataSource = typeof(ASFuelControl.Data.CommunicationController);
            // 
            // SelectController
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(342, 238);
            this.Controls.Add(this.radListControl1);
            this.Controls.Add(this.radButton2);
            this.Controls.Add(this.radButton1);
            this.MinimumSize = new System.Drawing.Size(350, 268);
            this.Name = "SelectController";
            // 
            // 
            // 
            this.RootElement.ApplyShapeToControl = true;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Επιλογή Ελεγκτή...";
            ((System.ComponentModel.ISupportInitialize)(this.radButton2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radListControl1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.communicationControllerBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private Telerik.WinControls.UI.RadButton radButton2;
        private Telerik.WinControls.UI.RadButton radButton1;
        private Telerik.WinControls.UI.RadListControl radListControl1;
        private System.Windows.Forms.BindingSource communicationControllerBindingSource;
    }
}
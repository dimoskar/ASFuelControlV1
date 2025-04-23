namespace ASFuelControl.Windows.UI.SelectionForms
{
    partial class TraderSelectForm
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
            this.radButton2 = new Telerik.WinControls.UI.RadButton();
            this.radButton3 = new Telerik.WinControls.UI.RadButton();
            ((System.ComponentModel.ISupportInitialize)(this.theSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.btnSearch)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radTextBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.btnCancel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.btnSelect)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();
            // 
            // btnSearch
            // 
            this.btnSearch.Location = new System.Drawing.Point(601, 9);
            // 
            // radTextBox1
            // 
            this.radTextBox1.Size = new System.Drawing.Size(522, 20);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(517, 373);
            // 
            // btnSelect
            // 
            this.btnSelect.Location = new System.Drawing.Point(12, 373);
            // 
            // radButton1
            // 
            this.radButton1.Location = new System.Drawing.Point(382, 373);
            this.radButton1.Size = new System.Drawing.Size(40, 31);
            this.radButton1.Click += new System.EventHandler(this.radButton1_Click);
            // 
            // radButton2
            // 
            this.radButton2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.radButton2.Image = global::ASFuelControl.Windows.Properties.Resources.Add24;
            this.radButton2.Location = new System.Drawing.Point(128, 373);
            this.radButton2.Name = "radButton2";
            this.radButton2.Padding = new System.Windows.Forms.Padding(5, 0, 25, 0);
            this.radButton2.Size = new System.Drawing.Size(105, 31);
            this.radButton2.TabIndex = 32;
            this.radButton2.Text = "Πελάτη";
            this.radButton2.TextAlignment = System.Drawing.ContentAlignment.MiddleRight;
            this.radButton2.Click += new System.EventHandler(this.radButton2_Click);
            // 
            // radButton3
            // 
            this.radButton3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.radButton3.Image = global::ASFuelControl.Windows.Properties.Resources.Add24;
            this.radButton3.Location = new System.Drawing.Point(239, 373);
            this.radButton3.Name = "radButton3";
            this.radButton3.Padding = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.radButton3.Size = new System.Drawing.Size(111, 31);
            this.radButton3.TabIndex = 33;
            this.radButton3.Text = "Προμηθευτή";
            this.radButton3.TextAlignment = System.Drawing.ContentAlignment.MiddleRight;
            this.radButton3.Click += new System.EventHandler(this.radButton3_Click);
            // 
            // TraderSelectForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(639, 409);
            this.Controls.Add(this.radButton3);
            this.Controls.Add(this.radButton2);
            this.Name = "TraderSelectForm";
            // 
            // 
            // 
            this.RootElement.ApplyShapeToControl = true;
            this.Text = "TraderSelectForm";
            this.Controls.SetChildIndex(this.btnSelect, 0);
            this.Controls.SetChildIndex(this.btnCancel, 0);
            this.Controls.SetChildIndex(this.radTextBox1, 0);
            this.Controls.SetChildIndex(this.btnSearch, 0);
            this.Controls.SetChildIndex(this.radButton1, 0);
            this.Controls.SetChildIndex(this.radButton2, 0);
            this.Controls.SetChildIndex(this.radButton3, 0);
            ((System.ComponentModel.ISupportInitialize)(this.theSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.btnSearch)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radTextBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.btnCancel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.btnSelect)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Telerik.WinControls.UI.RadButton radButton2;
        private Telerik.WinControls.UI.RadButton radButton3;
    }
}
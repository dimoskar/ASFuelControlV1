namespace ASFuelControl.Windows.UI.Controls
{
    partial class TankControlSmall
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
            this.radToggleButton1 = new Telerik.WinControls.UI.RadToggleButton();
            this.radLabel1 = new Telerik.WinControls.UI.RadLabel();
            this.radLabel2 = new Telerik.WinControls.UI.RadLabel();
            ((System.ComponentModel.ISupportInitialize)(this.radToggleButton1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel2)).BeginInit();
            this.SuspendLayout();
            // 
            // radToggleButton1
            // 
            this.radToggleButton1.Image = global::ASFuelControl.Windows.Properties.Resources.Tank_48;
            this.radToggleButton1.ImageAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            this.radToggleButton1.Location = new System.Drawing.Point(3, 3);
            this.radToggleButton1.Name = "radToggleButton1";
            this.radToggleButton1.Size = new System.Drawing.Size(64, 64);
            this.radToggleButton1.TabIndex = 0;
            // 
            // radLabel1
            // 
            this.radLabel1.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radLabel1.Location = new System.Drawing.Point(74, 4);
            this.radLabel1.Name = "radLabel1";
            this.radLabel1.Size = new System.Drawing.Size(93, 30);
            this.radLabel1.TabIndex = 1;
            this.radLabel1.Text = "radLabel1";
            // 
            // radLabel2
            // 
            this.radLabel2.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radLabel2.Location = new System.Drawing.Point(74, 37);
            this.radLabel2.Name = "radLabel2";
            this.radLabel2.Size = new System.Drawing.Size(93, 30);
            this.radLabel2.TabIndex = 2;
            this.radLabel2.Text = "radLabel2";
            // 
            // TankControlSmall
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.radLabel2);
            this.Controls.Add(this.radLabel1);
            this.Controls.Add(this.radToggleButton1);
            this.Name = "TankControlSmall";
            this.Size = new System.Drawing.Size(427, 71);
            ((System.ComponentModel.ISupportInitialize)(this.radToggleButton1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Telerik.WinControls.UI.RadToggleButton radToggleButton1;
        private Telerik.WinControls.UI.RadLabel radLabel1;
        private Telerik.WinControls.UI.RadLabel radLabel2;
    }
}

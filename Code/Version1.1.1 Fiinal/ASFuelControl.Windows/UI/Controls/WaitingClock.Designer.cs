namespace ASFuelControl.Windows.UI.Controls
{
    partial class WaitingClock
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
            this.radTextBox1 = new Telerik.WinControls.UI.RadTextBox();
            this.radProgressBar1 = new Telerik.WinControls.UI.RadProgressBar();
            ((System.ComponentModel.ISupportInitialize)(this.radTextBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radProgressBar1)).BeginInit();
            this.SuspendLayout();
            // 
            // radTextBox1
            // 
            this.radTextBox1.AutoSize = false;
            this.radTextBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.radTextBox1.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radTextBox1.Location = new System.Drawing.Point(0, 0);
            this.radTextBox1.Name = "radTextBox1";
            this.radTextBox1.Size = new System.Drawing.Size(545, 122);
            this.radTextBox1.TabIndex = 0;
            this.radTextBox1.Text = "00:00:00";
            this.radTextBox1.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // radProgressBar1
            // 
            this.radProgressBar1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.radProgressBar1.Location = new System.Drawing.Point(0, 122);
            this.radProgressBar1.Name = "radProgressBar1";
            this.radProgressBar1.Size = new System.Drawing.Size(545, 10);
            this.radProgressBar1.TabIndex = 1;
            // 
            // WaitingClock
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.radTextBox1);
            this.Controls.Add(this.radProgressBar1);
            this.Name = "WaitingClock";
            this.Size = new System.Drawing.Size(545, 132);
            ((System.ComponentModel.ISupportInitialize)(this.radTextBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radProgressBar1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private Telerik.WinControls.UI.RadTextBox radTextBox1;
        private Telerik.WinControls.UI.RadProgressBar radProgressBar1;
    }
}

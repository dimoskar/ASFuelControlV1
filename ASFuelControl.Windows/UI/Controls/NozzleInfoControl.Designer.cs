namespace ASFuelControl.Windows.UI.Controls
{
    partial class NozzleInfoControl
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
            this.nozzleDescriptionLabel = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.nozzleNumberLabel = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.nozzleTotalsLabel = new System.Windows.Forms.Label();
            this.panel3 = new System.Windows.Forms.Panel();
            this.labelSerial = new System.Windows.Forms.Label();
            this.panel4 = new System.Windows.Forms.Panel();
            this.panel2.SuspendLayout();
            this.panel4.SuspendLayout();
            this.SuspendLayout();
            // 
            // nozzleDescriptionLabel
            // 
            this.nozzleDescriptionLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nozzleDescriptionLabel.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.nozzleDescriptionLabel.Location = new System.Drawing.Point(0, 0);
            this.nozzleDescriptionLabel.Name = "nozzleDescriptionLabel";
            this.nozzleDescriptionLabel.Size = new System.Drawing.Size(438, 34);
            this.nozzleDescriptionLabel.TabIndex = 2;
            this.nozzleDescriptionLabel.Text = "Diesel";
            this.nozzleDescriptionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.panel4);
            this.panel2.Controls.Add(this.nozzleNumberLabel);
            this.panel2.Controls.Add(this.panel1);
            this.panel2.Controls.Add(this.nozzleTotalsLabel);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(632, 60);
            this.panel2.TabIndex = 4;
            // 
            // nozzleNumberLabel
            // 
            this.nozzleNumberLabel.Dock = System.Windows.Forms.DockStyle.Left;
            this.nozzleNumberLabel.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.nozzleNumberLabel.Location = new System.Drawing.Point(41, 0);
            this.nozzleNumberLabel.Name = "nozzleNumberLabel";
            this.nozzleNumberLabel.Size = new System.Drawing.Size(43, 60);
            this.nozzleNumberLabel.TabIndex = 3;
            this.nozzleNumberLabel.Text = "1";
            this.nozzleNumberLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panel1
            // 
            this.panel1.BackgroundImage = global::ASFuelControl.Windows.Properties.Resources.Nozzle;
            this.panel1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.panel1.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(41, 60);
            this.panel1.TabIndex = 2;
            // 
            // nozzleTotalsLabel
            // 
            this.nozzleTotalsLabel.Dock = System.Windows.Forms.DockStyle.Right;
            this.nozzleTotalsLabel.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.nozzleTotalsLabel.Location = new System.Drawing.Point(522, 0);
            this.nozzleTotalsLabel.Name = "nozzleTotalsLabel";
            this.nozzleTotalsLabel.Size = new System.Drawing.Size(110, 60);
            this.nozzleTotalsLabel.TabIndex = 4;
            this.nozzleTotalsLabel.Text = "1";
            this.nozzleTotalsLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // panel3
            // 
            this.panel3.BackgroundImage = global::ASFuelControl.Windows.Properties.Resources.Border;
            this.panel3.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.panel3.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel3.Location = new System.Drawing.Point(0, 55);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(632, 5);
            this.panel3.TabIndex = 5;
            // 
            // labelSerial
            // 
            this.labelSerial.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.labelSerial.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.labelSerial.Location = new System.Drawing.Point(0, 34);
            this.labelSerial.Name = "labelSerial";
            this.labelSerial.Size = new System.Drawing.Size(438, 26);
            this.labelSerial.TabIndex = 5;
            this.labelSerial.Text = "Diesel";
            this.labelSerial.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panel4
            // 
            this.panel4.Controls.Add(this.nozzleDescriptionLabel);
            this.panel4.Controls.Add(this.labelSerial);
            this.panel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel4.Location = new System.Drawing.Point(84, 0);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(438, 60);
            this.panel4.TabIndex = 6;
            // 
            // NozzleInfoControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel2);
            this.DoubleBuffered = true;
            this.Name = "NozzleInfoControl";
            this.Size = new System.Drawing.Size(632, 60);
            this.panel2.ResumeLayout(false);
            this.panel4.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label nozzleDescriptionLabel;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label nozzleNumberLabel;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label nozzleTotalsLabel;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Label labelSerial;
    }
}

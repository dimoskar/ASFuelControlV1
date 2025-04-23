namespace ASFuelControl.Windows.UI.Forms
{
    partial class DispenserInfoForm
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel4 = new System.Windows.Forms.Panel();
            this.dispenserNameLab = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.radButton1 = new Telerik.WinControls.UI.RadButton();
            this.panel3 = new System.Windows.Forms.Panel();
            this.panelNozzles = new System.Windows.Forms.Panel();
            this.panel5 = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.radButton1)).BeginInit();
            this.panel3.SuspendLayout();
            this.panel5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.Gray;
            this.panel1.Controls.Add(this.panel4);
            this.panel1.Controls.Add(this.dispenserNameLab);
            this.panel1.Controls.Add(this.panel2);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(5);
            this.panel1.Size = new System.Drawing.Size(507, 77);
            this.panel1.TabIndex = 0;
            // 
            // panel4
            // 
            this.panel4.BackgroundImage = global::ASFuelControl.Windows.Properties.Resources.Reset;
            this.panel4.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.panel4.Cursor = System.Windows.Forms.Cursors.Hand;
            this.panel4.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel4.Location = new System.Drawing.Point(418, 5);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(84, 67);
            this.panel4.TabIndex = 2;
            this.panel4.Click += new System.EventHandler(this.panel4_Click);
            // 
            // dispenserNameLab
            // 
            this.dispenserNameLab.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dispenserNameLab.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.dispenserNameLab.ForeColor = System.Drawing.Color.White;
            this.dispenserNameLab.Location = new System.Drawing.Point(89, 5);
            this.dispenserNameLab.Name = "dispenserNameLab";
            this.dispenserNameLab.Padding = new System.Windows.Forms.Padding(5, 0, 0, 0);
            this.dispenserNameLab.Size = new System.Drawing.Size(413, 67);
            this.dispenserNameLab.TabIndex = 1;
            this.dispenserNameLab.Text = "label1";
            this.dispenserNameLab.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panel2
            // 
            this.panel2.BackgroundImage = global::ASFuelControl.Windows.Properties.Resources.Fuel_Station;
            this.panel2.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.panel2.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel2.Location = new System.Drawing.Point(5, 5);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(84, 67);
            this.panel2.TabIndex = 0;
            // 
            // radButton1
            // 
            this.radButton1.Location = new System.Drawing.Point(197, 6);
            this.radButton1.Name = "radButton1";
            this.radButton1.Size = new System.Drawing.Size(110, 24);
            this.radButton1.TabIndex = 1;
            this.radButton1.Text = "Κλείσιμο";
            this.radButton1.Click += new System.EventHandler(this.radButton1_Click);
            // 
            // panel3
            // 
            this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel3.Controls.Add(this.panelNozzles);
            this.panel3.Controls.Add(this.panel5);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel3.Location = new System.Drawing.Point(0, 77);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(507, 369);
            this.panel3.TabIndex = 2;
            // 
            // panelNozzles
            // 
            this.panelNozzles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelNozzles.Location = new System.Drawing.Point(0, 0);
            this.panelNozzles.Name = "panelNozzles";
            this.panelNozzles.Size = new System.Drawing.Size(505, 326);
            this.panelNozzles.TabIndex = 2;
            // 
            // panel5
            // 
            this.panel5.Controls.Add(this.radButton1);
            this.panel5.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel5.Location = new System.Drawing.Point(0, 326);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(505, 41);
            this.panel5.TabIndex = 3;
            // 
            // DispenserInfoForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(507, 446);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "DispenserInfoForm";
            // 
            // 
            // 
            this.RootElement.ApplyShapeToControl = true;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "DispenserInfoForm";
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.radButton1)).EndInit();
            this.panel3.ResumeLayout(false);
            this.panel5.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private Telerik.WinControls.UI.RadButton radButton1;
        private System.Windows.Forms.Label dispenserNameLab;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Panel panelNozzles;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.Panel panel4;
    }
}
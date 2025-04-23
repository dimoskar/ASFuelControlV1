namespace ASFuelControl.Windows.UI.Controls
{
    partial class LcdUpDown
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.lcdDisplay1 = new ASFuelControl.Windows.UI.Controls.LcdDisplay();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackgroundImage = global::ASFuelControl.Windows.Properties.Resources.Down;
            this.panel1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.panel1.Cursor = System.Windows.Forms.Cursors.Hand;
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 350);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(577, 50);
            this.panel1.TabIndex = 1;
            this.panel1.Click += new System.EventHandler(this.panel1_Click);
            // 
            // panel2
            // 
            this.panel2.BackgroundImage = global::ASFuelControl.Windows.Properties.Resources.Up;
            this.panel2.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.panel2.Cursor = System.Windows.Forms.Cursors.Hand;
            this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(577, 50);
            this.panel2.TabIndex = 2;
            this.panel2.Click += new System.EventHandler(this.panel2_Click);
            // 
            // lcdDisplay1
            // 
            this.lcdDisplay1.BaseColor = System.Drawing.Color.Blue;
            this.lcdDisplay1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lcdDisplay1.ForeColor = System.Drawing.Color.White;
            this.lcdDisplay1.LabelText = "label1";
            this.lcdDisplay1.Location = new System.Drawing.Point(0, 50);
            this.lcdDisplay1.Name = "lcdDisplay1";
            this.lcdDisplay1.Size = new System.Drawing.Size(577, 300);
            this.lcdDisplay1.TabIndex = 0;
            // 
            // LcdUpDown
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lcdDisplay1);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Name = "LcdUpDown";
            this.Size = new System.Drawing.Size(577, 400);
            this.ResumeLayout(false);

        }

        #endregion

        private LcdDisplay lcdDisplay1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
    }
}

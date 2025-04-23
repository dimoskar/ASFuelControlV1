namespace ASFuelControl.Windows.UI.Forms
{
    partial class PumpDebugForm
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
            if(disposing && (components != null))
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
            this.StatusBtn = new System.Windows.Forms.Button();
            this.TotalsBtn = new System.Windows.Forms.Button();
            this.DisplayBtn = new System.Windows.Forms.Button();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // StatusBtn
            // 
            this.StatusBtn.Location = new System.Drawing.Point(12, 3);
            this.StatusBtn.Name = "StatusBtn";
            this.StatusBtn.Size = new System.Drawing.Size(126, 29);
            this.StatusBtn.TabIndex = 0;
            this.StatusBtn.Text = "Status";
            this.StatusBtn.UseVisualStyleBackColor = true;
            // 
            // TotalsBtn
            // 
            this.TotalsBtn.Location = new System.Drawing.Point(144, 3);
            this.TotalsBtn.Name = "TotalsBtn";
            this.TotalsBtn.Size = new System.Drawing.Size(126, 29);
            this.TotalsBtn.TabIndex = 1;
            this.TotalsBtn.Text = "Totals";
            this.TotalsBtn.UseVisualStyleBackColor = true;
            // 
            // DisplayBtn
            // 
            this.DisplayBtn.Location = new System.Drawing.Point(276, 3);
            this.DisplayBtn.Name = "DisplayBtn";
            this.DisplayBtn.Size = new System.Drawing.Size(126, 29);
            this.DisplayBtn.TabIndex = 2;
            this.DisplayBtn.Text = "Display";
            this.DisplayBtn.UseVisualStyleBackColor = true;
            // 
            // richTextBox1
            // 
            this.richTextBox1.Location = new System.Drawing.Point(10, 38);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(520, 363);
            this.richTextBox1.TabIndex = 3;
            this.richTextBox1.Text = "";
            // 
            // PumpDebugForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(542, 413);
            this.Controls.Add(this.richTextBox1);
            this.Controls.Add(this.DisplayBtn);
            this.Controls.Add(this.TotalsBtn);
            this.Controls.Add(this.StatusBtn);
            this.Name = "PumpDebugForm";
            this.Text = "PumpDebugForm";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button StatusBtn;
        private System.Windows.Forms.Button TotalsBtn;
        private System.Windows.Forms.Button DisplayBtn;
        private System.Windows.Forms.RichTextBox richTextBox1;
    }
}
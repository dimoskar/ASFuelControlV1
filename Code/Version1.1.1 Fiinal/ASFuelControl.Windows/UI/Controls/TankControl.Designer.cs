namespace ASFuelControl.Windows.UI.Controls
{
    partial class TankControl
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
            this.labelFuelTypeDesc = new System.Windows.Forms.Label();
            this.labelNumber = new System.Windows.Forms.Label();
            this.tankLinearGauge2 = new ASFuelControl.Windows.UI.Controls.TankLinearGauge();
            this.tankLinearGauge1 = new ASFuelControl.Windows.UI.Controls.TankLinearGauge();
            this.tankGauge1 = new ASFuelControl.Windows.UI.Controls.TankGauge();
            this.panel2 = new System.Windows.Forms.Panel();
            this.btnFillingCancel = new System.Windows.Forms.Panel();
            this.btnFillingEnd = new System.Windows.Forms.Panel();
            this.btnFillingStart = new System.Windows.Forms.Panel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.labelStatus = new System.Windows.Forms.Label();
            this.labelAveragePrice = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.labelVolumeNormalized = new System.Windows.Forms.Label();
            this.labelVolume = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.Transparent;
            this.panel1.Controls.Add(this.labelFuelTypeDesc);
            this.panel1.Controls.Add(this.labelNumber);
            this.panel1.Controls.Add(this.tankLinearGauge2);
            this.panel1.Controls.Add(this.tankLinearGauge1);
            this.panel1.Controls.Add(this.tankGauge1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(385, 280);
            this.panel1.TabIndex = 0;
            // 
            // labelFuelTypeDesc
            // 
            this.labelFuelTypeDesc.BackColor = System.Drawing.Color.Black;
            this.labelFuelTypeDesc.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.labelFuelTypeDesc.Location = new System.Drawing.Point(0, 1);
            this.labelFuelTypeDesc.Name = "labelFuelTypeDesc";
            this.labelFuelTypeDesc.Size = new System.Drawing.Size(331, 45);
            this.labelFuelTypeDesc.TabIndex = 12;
            this.labelFuelTypeDesc.Text = "1";
            this.labelFuelTypeDesc.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelNumber
            // 
            this.labelNumber.BackColor = System.Drawing.Color.Black;
            this.labelNumber.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.labelNumber.Location = new System.Drawing.Point(329, 1);
            this.labelNumber.Name = "labelNumber";
            this.labelNumber.Size = new System.Drawing.Size(56, 45);
            this.labelNumber.TabIndex = 10;
            this.labelNumber.Text = "1";
            this.labelNumber.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // tankLinearGauge2
            // 
            this.tankLinearGauge2.BaseColor = System.Drawing.Color.Empty;
            this.tankLinearGauge2.CurrentLevel = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.tankLinearGauge2.Location = new System.Drawing.Point(91, 48);
            this.tankLinearGauge2.Name = "tankLinearGauge2";
            this.tankLinearGauge2.Orientation = ASFuelControl.Windows.UI.Controls.GaugeOrientationEnum.Vertical;
            this.tankLinearGauge2.PercentageValue = 58F;
            this.tankLinearGauge2.Size = new System.Drawing.Size(44, 229);
            this.tankLinearGauge2.TabIndex = 8;
            // 
            // tankLinearGauge1
            // 
            this.tankLinearGauge1.BaseColor = System.Drawing.Color.Empty;
            this.tankLinearGauge1.CurrentLevel = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.tankLinearGauge1.Location = new System.Drawing.Point(21, 48);
            this.tankLinearGauge1.Name = "tankLinearGauge1";
            this.tankLinearGauge1.Orientation = ASFuelControl.Windows.UI.Controls.GaugeOrientationEnum.Vertical;
            this.tankLinearGauge1.PercentageValue = 58F;
            this.tankLinearGauge1.Size = new System.Drawing.Size(44, 229);
            this.tankLinearGauge1.TabIndex = 7;
            // 
            // tankGauge1
            // 
            this.tankGauge1.BaseColor = System.Drawing.Color.Empty;
            this.tankGauge1.Location = new System.Drawing.Point(128, 48);
            this.tankGauge1.Name = "tankGauge1";
            this.tankGauge1.PercentageValue = 0F;
            this.tankGauge1.Size = new System.Drawing.Size(235, 235);
            this.tankGauge1.StateColor = System.Drawing.Color.Empty;
            this.tankGauge1.TabIndex = 6;
            this.tankGauge1.Temperature = new decimal(new int[] {
            0,
            0,
            0,
            0});
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.btnFillingCancel);
            this.panel2.Controls.Add(this.btnFillingEnd);
            this.panel2.Controls.Add(this.btnFillingStart);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel2.Location = new System.Drawing.Point(0, 403);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(385, 70);
            this.panel2.TabIndex = 1;
            // 
            // btnFillingCancel
            // 
            this.btnFillingCancel.BackColor = System.Drawing.SystemColors.Control;
            this.btnFillingCancel.BackgroundImage = global::ASFuelControl.Windows.Properties.Resources.FillingCancel;
            this.btnFillingCancel.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.btnFillingCancel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.btnFillingCancel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnFillingCancel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnFillingCancel.Location = new System.Drawing.Point(128, 0);
            this.btnFillingCancel.Name = "btnFillingCancel";
            this.btnFillingCancel.Size = new System.Drawing.Size(129, 70);
            this.btnFillingCancel.TabIndex = 4;
            this.btnFillingCancel.EnabledChanged += new System.EventHandler(this.btnEnabled_Changed);
            this.btnFillingCancel.Click += new System.EventHandler(this.btnFillingCancel_Click);
            // 
            // btnFillingEnd
            // 
            this.btnFillingEnd.BackColor = System.Drawing.SystemColors.Control;
            this.btnFillingEnd.BackgroundImage = global::ASFuelControl.Windows.Properties.Resources.FillingEnd;
            this.btnFillingEnd.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.btnFillingEnd.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.btnFillingEnd.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnFillingEnd.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnFillingEnd.Location = new System.Drawing.Point(257, 0);
            this.btnFillingEnd.Name = "btnFillingEnd";
            this.btnFillingEnd.Size = new System.Drawing.Size(128, 70);
            this.btnFillingEnd.TabIndex = 5;
            this.btnFillingEnd.EnabledChanged += new System.EventHandler(this.btnEnabled_Changed);
            this.btnFillingEnd.Click += new System.EventHandler(this.btnFillingEnd_Click);
            // 
            // btnFillingStart
            // 
            this.btnFillingStart.BackColor = System.Drawing.SystemColors.Control;
            this.btnFillingStart.BackgroundImage = global::ASFuelControl.Windows.Properties.Resources.FillingStart;
            this.btnFillingStart.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.btnFillingStart.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.btnFillingStart.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnFillingStart.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnFillingStart.Location = new System.Drawing.Point(0, 0);
            this.btnFillingStart.Name = "btnFillingStart";
            this.btnFillingStart.Size = new System.Drawing.Size(128, 70);
            this.btnFillingStart.TabIndex = 3;
            this.btnFillingStart.EnabledChanged += new System.EventHandler(this.btnEnabled_Changed);
            this.btnFillingStart.Click += new System.EventHandler(this.btnFillingStart_Click);
            // 
            // panel3
            // 
            this.panel3.BackColor = System.Drawing.Color.Teal;
            this.panel3.Controls.Add(this.label3);
            this.panel3.Controls.Add(this.labelStatus);
            this.panel3.Controls.Add(this.labelAveragePrice);
            this.panel3.Controls.Add(this.label6);
            this.panel3.Controls.Add(this.labelVolumeNormalized);
            this.panel3.Controls.Add(this.labelVolume);
            this.panel3.Controls.Add(this.label2);
            this.panel3.Controls.Add(this.label1);
            this.panel3.Controls.Add(this.panel1);
            this.panel3.Controls.Add(this.panel2);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel3.ForeColor = System.Drawing.Color.White;
            this.panel3.Location = new System.Drawing.Point(10, 10);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(385, 473);
            this.panel3.TabIndex = 3;
            this.panel3.BackColorChanged += new System.EventHandler(this.panel3_BackColorChanged);
            // 
            // labelStatus
            // 
            this.labelStatus.BackColor = System.Drawing.Color.Navy;
            this.labelStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.labelStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.labelStatus.Location = new System.Drawing.Point(0, 374);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(385, 29);
            this.labelStatus.TabIndex = 8;
            this.labelStatus.Text = "Σε αναμονή";
            this.labelStatus.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // labelAveragePrice
            // 
            this.labelAveragePrice.BackColor = System.Drawing.Color.Transparent;
            this.labelAveragePrice.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.labelAveragePrice.Location = new System.Drawing.Point(257, 313);
            this.labelAveragePrice.Name = "labelAveragePrice";
            this.labelAveragePrice.Size = new System.Drawing.Size(128, 25);
            this.labelAveragePrice.TabIndex = 7;
            this.labelAveragePrice.Text = "0,000 €";
            this.labelAveragePrice.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // label6
            // 
            this.label6.BackColor = System.Drawing.Color.Transparent;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.label6.Location = new System.Drawing.Point(257, 286);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(128, 25);
            this.label6.TabIndex = 6;
            this.label6.Text = "Μέση Τιμή";
            this.label6.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // labelVolumeNormalized
            // 
            this.labelVolumeNormalized.BackColor = System.Drawing.Color.Transparent;
            this.labelVolumeNormalized.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.labelVolumeNormalized.Location = new System.Drawing.Point(128, 313);
            this.labelVolumeNormalized.Name = "labelVolumeNormalized";
            this.labelVolumeNormalized.Size = new System.Drawing.Size(129, 25);
            this.labelVolumeNormalized.TabIndex = 5;
            this.labelVolumeNormalized.Text = "0,00 lt";
            this.labelVolumeNormalized.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // labelVolume
            // 
            this.labelVolume.BackColor = System.Drawing.Color.Transparent;
            this.labelVolume.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.labelVolume.Location = new System.Drawing.Point(0, 313);
            this.labelVolume.Name = "labelVolume";
            this.labelVolume.Size = new System.Drawing.Size(128, 25);
            this.labelVolume.TabIndex = 4;
            this.labelVolume.Text = "0,00 lt";
            this.labelVolume.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // label2
            // 
            this.label2.BackColor = System.Drawing.Color.Transparent;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.label2.Location = new System.Drawing.Point(128, 286);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(129, 25);
            this.label2.TabIndex = 3;
            this.label2.Text = "Όγκος 15ο";
            this.label2.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.label1.Location = new System.Drawing.Point(0, 286);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(128, 23);
            this.label1.TabIndex = 2;
            this.label1.Text = "Όγκος";
            this.label1.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // label3
            // 
            this.label3.BackColor = System.Drawing.Color.Black;
            this.label3.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.label3.Location = new System.Drawing.Point(0, 345);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(385, 29);
            this.label3.TabIndex = 9;
            this.label3.Text = "Διαθέσιμος Όγκος";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // TankControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.panel3);
            this.DoubleBuffered = true;
            this.Name = "TankControl";
            this.Padding = new System.Windows.Forms.Padding(10);
            this.Size = new System.Drawing.Size(405, 493);
            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private TankGauge tankGauge1;
        private System.Windows.Forms.Panel panel3;
        private TankLinearGauge tankLinearGauge1;
        private TankLinearGauge tankLinearGauge2;
        private System.Windows.Forms.Label labelAveragePrice;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label labelVolumeNormalized;
        private System.Windows.Forms.Label labelVolume;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.Label labelNumber;
        private System.Windows.Forms.Label labelFuelTypeDesc;
        private System.Windows.Forms.Panel btnFillingCancel;
        private System.Windows.Forms.Panel btnFillingEnd;
        private System.Windows.Forms.Panel btnFillingStart;
        private System.Windows.Forms.Label label3;
    }
}

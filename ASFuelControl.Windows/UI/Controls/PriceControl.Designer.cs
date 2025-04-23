namespace ASFuelControl.Windows.UI.Controls
{
    partial class PriceControl
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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.Label baseDensityLabel;
            System.Windows.Forms.Label thermalCoeficientLabel;
            System.Windows.Forms.Label label1;
            System.Windows.Forms.Label excludeBalanceLabel;
            this.nameLabel = new System.Windows.Forms.Label();
            this.fuelTypeBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.baseDensityRadSpinEditor = new Telerik.WinControls.UI.RadSpinEditor();
            this.thermalCoeficientRadSpinEditor = new Telerik.WinControls.UI.RadSpinEditor();
            this.panel1 = new System.Windows.Forms.Panel();
            this.int1 = new ASFuelControl.Windows.UI.Controls.LcdUpDown();
            this.lcdUpDown6 = new ASFuelControl.Windows.UI.Controls.LcdUpDown();
            this.int2 = new ASFuelControl.Windows.UI.Controls.LcdUpDown();
            this.decimal3 = new ASFuelControl.Windows.UI.Controls.LcdUpDown();
            this.decimal1 = new ASFuelControl.Windows.UI.Controls.LcdUpDown();
            this.decimal2 = new ASFuelControl.Windows.UI.Controls.LcdUpDown();
            this.radCheckBox1 = new Telerik.WinControls.UI.RadCheckBox();
            this.excludeBalanceLabelCbx = new Telerik.WinControls.UI.RadCheckBox();
            baseDensityLabel = new System.Windows.Forms.Label();
            thermalCoeficientLabel = new System.Windows.Forms.Label();
            label1 = new System.Windows.Forms.Label();
            excludeBalanceLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.fuelTypeBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.baseDensityRadSpinEditor)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.thermalCoeficientRadSpinEditor)).BeginInit();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.radCheckBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.excludeBalanceLabelCbx)).BeginInit();
            this.SuspendLayout();
            // 
            // baseDensityLabel
            // 
            baseDensityLabel.AutoSize = true;
            baseDensityLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            baseDensityLabel.Location = new System.Drawing.Point(10, 62);
            baseDensityLabel.Name = "baseDensityLabel";
            baseDensityLabel.Size = new System.Drawing.Size(75, 16);
            baseDensityLabel.TabIndex = 2;
            baseDensityLabel.Text = "Πυκνότητα";
            // 
            // thermalCoeficientLabel
            // 
            thermalCoeficientLabel.AutoSize = true;
            thermalCoeficientLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            thermalCoeficientLabel.Location = new System.Drawing.Point(10, 88);
            thermalCoeficientLabel.Name = "thermalCoeficientLabel";
            thermalCoeficientLabel.Size = new System.Drawing.Size(127, 16);
            thermalCoeficientLabel.TabIndex = 12;
            thermalCoeficientLabel.Text = "Θερμ. Συντελεστής";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            label1.Location = new System.Drawing.Point(10, 112);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(159, 16);
            label1.TabIndex = 20;
            label1.Text = "Υποστιρίζει Αρ. Παροχής";
            // 
            // excludeBalanceLabel
            // 
            excludeBalanceLabel.AutoSize = true;
            excludeBalanceLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            excludeBalanceLabel.Location = new System.Drawing.Point(10, 136);
            excludeBalanceLabel.Name = "excludeBalanceLabel";
            excludeBalanceLabel.Size = new System.Drawing.Size(146, 16);
            excludeBalanceLabel.TabIndex = 22;
            excludeBalanceLabel.Text = "Εξαίρεση από Ισοζύγιο";
            // 
            // nameLabel
            // 
            this.nameLabel.BackColor = System.Drawing.Color.MidnightBlue;
            this.nameLabel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.nameLabel.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.fuelTypeBindingSource, "Name", true));
            this.nameLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.nameLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.nameLabel.ForeColor = System.Drawing.Color.White;
            this.nameLabel.Location = new System.Drawing.Point(0, 0);
            this.nameLabel.Name = "nameLabel";
            this.nameLabel.Size = new System.Drawing.Size(309, 53);
            this.nameLabel.TabIndex = 10;
            this.nameLabel.Text = "Ονομασία";
            this.nameLabel.Click += new System.EventHandler(this.nameLabel_Click);
            // 
            // fuelTypeBindingSource
            // 
            this.fuelTypeBindingSource.DataSource = typeof(ASFuelControl.Data.FuelType);
            // 
            // baseDensityRadSpinEditor
            // 
            this.baseDensityRadSpinEditor.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.fuelTypeBindingSource, "BaseDensity", true));
            this.baseDensityRadSpinEditor.DecimalPlaces = 2;
            this.baseDensityRadSpinEditor.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.baseDensityRadSpinEditor.Location = new System.Drawing.Point(193, 60);
            this.baseDensityRadSpinEditor.Maximum = new decimal(new int[] {
            2000,
            0,
            0,
            0});
            this.baseDensityRadSpinEditor.Name = "baseDensityRadSpinEditor";
            this.baseDensityRadSpinEditor.Size = new System.Drawing.Size(100, 20);
            this.baseDensityRadSpinEditor.TabIndex = 3;
            this.baseDensityRadSpinEditor.TabStop = false;
            this.baseDensityRadSpinEditor.TextAlignment = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // thermalCoeficientRadSpinEditor
            // 
            this.thermalCoeficientRadSpinEditor.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.fuelTypeBindingSource, "ThermalCoeficient", true));
            this.thermalCoeficientRadSpinEditor.DecimalPlaces = 5;
            this.thermalCoeficientRadSpinEditor.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.thermalCoeficientRadSpinEditor.Location = new System.Drawing.Point(193, 86);
            this.thermalCoeficientRadSpinEditor.Name = "thermalCoeficientRadSpinEditor";
            this.thermalCoeficientRadSpinEditor.ReadOnly = true;
            this.thermalCoeficientRadSpinEditor.Size = new System.Drawing.Size(100, 20);
            this.thermalCoeficientRadSpinEditor.TabIndex = 13;
            this.thermalCoeficientRadSpinEditor.TabStop = false;
            this.thermalCoeficientRadSpinEditor.TextAlignment = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.MidnightBlue;
            this.panel1.Controls.Add(this.int1);
            this.panel1.Controls.Add(this.lcdUpDown6);
            this.panel1.Controls.Add(this.int2);
            this.panel1.Controls.Add(this.decimal3);
            this.panel1.Controls.Add(this.decimal1);
            this.panel1.Controls.Add(this.decimal2);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 168);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(309, 76);
            this.panel1.TabIndex = 19;
            // 
            // int1
            // 
            this.int1.BaseColor = System.Drawing.Color.MidnightBlue;
            this.int1.Digit = 0;
            this.int1.Font = new System.Drawing.Font("Microsoft Sans Serif", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.int1.IsSeparator = false;
            this.int1.Location = new System.Drawing.Point(11, 0);
            this.int1.Margin = new System.Windows.Forms.Padding(11, 10, 11, 10);
            this.int1.Name = "int1";
            this.int1.Size = new System.Drawing.Size(51, 74);
            this.int1.TabIndex = 0;
            // 
            // lcdUpDown6
            // 
            this.lcdUpDown6.BaseColor = System.Drawing.Color.MidnightBlue;
            this.lcdUpDown6.Digit = 0;
            this.lcdUpDown6.Font = new System.Drawing.Font("Microsoft Sans Serif", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lcdUpDown6.IsSeparator = true;
            this.lcdUpDown6.Location = new System.Drawing.Point(113, 0);
            this.lcdUpDown6.Margin = new System.Windows.Forms.Padding(11, 10, 11, 10);
            this.lcdUpDown6.Name = "lcdUpDown6";
            this.lcdUpDown6.Size = new System.Drawing.Size(27, 74);
            this.lcdUpDown6.TabIndex = 18;
            // 
            // int2
            // 
            this.int2.BaseColor = System.Drawing.Color.MidnightBlue;
            this.int2.Digit = 0;
            this.int2.Font = new System.Drawing.Font("Microsoft Sans Serif", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.int2.IsSeparator = false;
            this.int2.Location = new System.Drawing.Point(62, 0);
            this.int2.Margin = new System.Windows.Forms.Padding(11, 10, 11, 10);
            this.int2.Name = "int2";
            this.int2.Size = new System.Drawing.Size(51, 74);
            this.int2.TabIndex = 14;
            // 
            // decimal3
            // 
            this.decimal3.BaseColor = System.Drawing.Color.MidnightBlue;
            this.decimal3.Digit = 0;
            this.decimal3.Font = new System.Drawing.Font("Microsoft Sans Serif", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.decimal3.IsSeparator = false;
            this.decimal3.Location = new System.Drawing.Point(242, 0);
            this.decimal3.Margin = new System.Windows.Forms.Padding(11, 10, 11, 10);
            this.decimal3.Name = "decimal3";
            this.decimal3.Size = new System.Drawing.Size(51, 74);
            this.decimal3.TabIndex = 17;
            // 
            // decimal1
            // 
            this.decimal1.BaseColor = System.Drawing.Color.MidnightBlue;
            this.decimal1.Digit = 0;
            this.decimal1.Font = new System.Drawing.Font("Microsoft Sans Serif", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.decimal1.IsSeparator = false;
            this.decimal1.Location = new System.Drawing.Point(140, 0);
            this.decimal1.Margin = new System.Windows.Forms.Padding(11, 10, 11, 10);
            this.decimal1.Name = "decimal1";
            this.decimal1.Size = new System.Drawing.Size(51, 74);
            this.decimal1.TabIndex = 15;
            // 
            // decimal2
            // 
            this.decimal2.BaseColor = System.Drawing.Color.MidnightBlue;
            this.decimal2.Digit = 0;
            this.decimal2.Font = new System.Drawing.Font("Microsoft Sans Serif", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.decimal2.IsSeparator = false;
            this.decimal2.Location = new System.Drawing.Point(191, 0);
            this.decimal2.Margin = new System.Windows.Forms.Padding(11, 10, 11, 10);
            this.decimal2.Name = "decimal2";
            this.decimal2.Size = new System.Drawing.Size(51, 74);
            this.decimal2.TabIndex = 16;
            // 
            // radCheckBox1
            // 
            this.radCheckBox1.DataBindings.Add(new System.Windows.Forms.Binding("IsChecked", this.fuelTypeBindingSource, "SupportsSupplyNumber", true));
            this.radCheckBox1.Location = new System.Drawing.Point(193, 113);
            this.radCheckBox1.Name = "radCheckBox1";
            this.radCheckBox1.Size = new System.Drawing.Size(15, 15);
            this.radCheckBox1.TabIndex = 21;
            // 
            // excludeBalanceLabelCbx
            // 
            this.excludeBalanceLabelCbx.DataBindings.Add(new System.Windows.Forms.Binding("IsChecked", this.fuelTypeBindingSource, "BalanceExclusion", true));
            this.excludeBalanceLabelCbx.Location = new System.Drawing.Point(193, 137);
            this.excludeBalanceLabelCbx.Name = "excludeBalanceLabelCbx";
            this.excludeBalanceLabelCbx.Size = new System.Drawing.Size(15, 15);
            this.excludeBalanceLabelCbx.TabIndex = 23;
            // 
            // PriceControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this.excludeBalanceLabelCbx);
            this.Controls.Add(excludeBalanceLabel);
            this.Controls.Add(this.radCheckBox1);
            this.Controls.Add(label1);
            this.Controls.Add(this.panel1);
            this.Controls.Add(baseDensityLabel);
            this.Controls.Add(this.baseDensityRadSpinEditor);
            this.Controls.Add(this.nameLabel);
            this.Controls.Add(thermalCoeficientLabel);
            this.Controls.Add(this.thermalCoeficientRadSpinEditor);
            this.Name = "PriceControl";
            this.Size = new System.Drawing.Size(309, 244);
            ((System.ComponentModel.ISupportInitialize)(this.fuelTypeBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.baseDensityRadSpinEditor)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.thermalCoeficientRadSpinEditor)).EndInit();
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.radCheckBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.excludeBalanceLabelCbx)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private LcdUpDown int1;
        private System.Windows.Forms.BindingSource fuelTypeBindingSource;
        private Telerik.WinControls.UI.RadSpinEditor baseDensityRadSpinEditor;
        private Telerik.WinControls.UI.RadSpinEditor thermalCoeficientRadSpinEditor;
        private LcdUpDown int2;
        private LcdUpDown decimal1;
        private LcdUpDown decimal3;
        private LcdUpDown decimal2;
        private LcdUpDown lcdUpDown6;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label nameLabel;
        private Telerik.WinControls.UI.RadCheckBox radCheckBox1;
        private Telerik.WinControls.UI.RadCheckBox excludeBalanceLabelCbx;
    }
}

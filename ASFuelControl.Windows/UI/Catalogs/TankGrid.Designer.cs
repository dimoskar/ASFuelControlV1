namespace ASFuelControl.Windows.UI.Catalogs
{
    partial class TankGrid
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
            this.labelTitle = new System.Windows.Forms.Label();
            this.radButton2 = new Telerik.WinControls.UI.RadButton();
            this.waitingClock1 = new ASFuelControl.Windows.UI.Controls.WaitingClock();
            this.radButton1 = new Telerik.WinControls.UI.RadButton();
            this.panel3 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.panel5 = new System.Windows.Forms.Panel();
            this.radLabel2 = new Telerik.WinControls.UI.RadLabel();
            this.radButton3 = new Telerik.WinControls.UI.RadButton();
            this.radButton4 = new Telerik.WinControls.UI.RadButton();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.radButton2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton1)).BeginInit();
            this.panel3.SuspendLayout();
            this.panel5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton4)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.Gainsboro;
            this.panel1.Controls.Add(this.panel2);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(972, 74);
            this.panel1.TabIndex = 0;
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.Color.Gainsboro;
            this.panel2.Controls.Add(this.labelTitle);
            this.panel2.Controls.Add(this.radButton2);
            this.panel2.Controls.Add(this.waitingClock1);
            this.panel2.Controls.Add(this.radButton1);
            this.panel2.Controls.Add(this.panel3);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(972, 74);
            this.panel2.TabIndex = 0;
            // 
            // labelTitle
            // 
            this.labelTitle.BackColor = System.Drawing.SystemColors.Control;
            this.labelTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelTitle.Font = new System.Drawing.Font("Segoe UI", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.labelTitle.Location = new System.Drawing.Point(186, 0);
            this.labelTitle.Name = "labelTitle";
            this.labelTitle.Size = new System.Drawing.Size(349, 74);
            this.labelTitle.TabIndex = 5;
            this.labelTitle.Text = "Δεξαμενές";
            this.labelTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // radButton2
            // 
            this.radButton2.Dock = System.Windows.Forms.DockStyle.Left;
            this.radButton2.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radButton2.Image = global::ASFuelControl.Windows.Properties.Resources.Tank_48;
            this.radButton2.Location = new System.Drawing.Point(0, 0);
            this.radButton2.Name = "radButton2";
            this.radButton2.Size = new System.Drawing.Size(186, 74);
            this.radButton2.TabIndex = 8;
            this.radButton2.Text = "Παραλαβή / Εξαγωγή";
            this.radButton2.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.radButton2.TextWrap = true;
            this.radButton2.Visible = false;
            this.radButton2.Click += new System.EventHandler(this.radButton2_Click);
            // 
            // waitingClock1
            // 
            this.waitingClock1.Dock = System.Windows.Forms.DockStyle.Right;
            this.waitingClock1.Location = new System.Drawing.Point(535, 0);
            this.waitingClock1.Name = "waitingClock1";
            this.waitingClock1.Size = new System.Drawing.Size(141, 74);
            this.waitingClock1.TabIndex = 7;
            this.waitingClock1.Visible = false;
            // 
            // radButton1
            // 
            this.radButton1.Dock = System.Windows.Forms.DockStyle.Right;
            this.radButton1.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radButton1.Image = global::ASFuelControl.Windows.Properties.Resources.Invoice;
            this.radButton1.Location = new System.Drawing.Point(676, 0);
            this.radButton1.Name = "radButton1";
            this.radButton1.Size = new System.Drawing.Size(271, 74);
            this.radButton1.TabIndex = 6;
            this.radButton1.Text = "Παραστατικό  Παραλαβής/Εξαγωγής";
            this.radButton1.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.radButton1.TextWrap = true;
            this.radButton1.Click += new System.EventHandler(this.radButton1_Click);
            // 
            // panel3
            // 
            this.panel3.BackColor = System.Drawing.SystemColors.Control;
            this.panel3.Controls.Add(this.label1);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel3.Location = new System.Drawing.Point(947, 0);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(25, 74);
            this.panel3.TabIndex = 7;
            // 
            // label1
            // 
            this.label1.Cursor = System.Windows.Forms.Cursors.Hand;
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(25, 27);
            this.label1.TabIndex = 1;
            this.label1.Text = "X";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 4;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 74);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(972, 381);
            this.tableLayoutPanel1.TabIndex = 2;
            this.tableLayoutPanel1.Resize += new System.EventHandler(this.tableLayoutPanel1_Resize);
            // 
            // panel5
            // 
            this.panel5.BackColor = System.Drawing.SystemColors.ControlDark;
            this.panel5.Controls.Add(this.radLabel2);
            this.panel5.Controls.Add(this.radButton3);
            this.panel5.Controls.Add(this.radButton4);
            this.panel5.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel5.Location = new System.Drawing.Point(0, 455);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(972, 42);
            this.panel5.TabIndex = 6;
            // 
            // radLabel2
            // 
            this.radLabel2.AutoSize = false;
            this.radLabel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.radLabel2.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radLabel2.Location = new System.Drawing.Point(54, 0);
            this.radLabel2.Name = "radLabel2";
            this.radLabel2.Size = new System.Drawing.Size(864, 42);
            this.radLabel2.TabIndex = 2;
            this.radLabel2.Text = "Σελίδα {0} από {1}";
            this.radLabel2.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            this.radLabel2.TextWrap = false;
            // 
            // radButton3
            // 
            this.radButton3.DisplayStyle = Telerik.WinControls.DisplayStyle.Image;
            this.radButton3.Dock = System.Windows.Forms.DockStyle.Right;
            this.radButton3.Image = global::ASFuelControl.Windows.Properties.Resources.NextPage;
            this.radButton3.ImageAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            this.radButton3.Location = new System.Drawing.Point(918, 0);
            this.radButton3.Name = "radButton3";
            this.radButton3.Size = new System.Drawing.Size(54, 42);
            this.radButton3.TabIndex = 1;
            this.radButton3.Text = "radButton3";
            this.radButton3.Click += new System.EventHandler(this.radButton3_Click);
            // 
            // radButton4
            // 
            this.radButton4.DisplayStyle = Telerik.WinControls.DisplayStyle.Image;
            this.radButton4.Dock = System.Windows.Forms.DockStyle.Left;
            this.radButton4.Image = global::ASFuelControl.Windows.Properties.Resources.PreviousPage;
            this.radButton4.ImageAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            this.radButton4.Location = new System.Drawing.Point(0, 0);
            this.radButton4.Name = "radButton4";
            this.radButton4.Size = new System.Drawing.Size(54, 42);
            this.radButton4.TabIndex = 0;
            this.radButton4.Text = "radButton4";
            this.radButton4.Click += new System.EventHandler(this.radButton4_Click);
            // 
            // TankGrid
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.panel5);
            this.Controls.Add(this.panel1);
            this.Name = "TankGrid";
            this.Size = new System.Drawing.Size(972, 497);
            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.radButton2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton1)).EndInit();
            this.panel3.ResumeLayout(false);
            this.panel5.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.radLabel2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton4)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private Telerik.WinControls.UI.RadButton radButton1;
        private System.Windows.Forms.Label labelTitle;
        private Controls.WaitingClock waitingClock1;
        private Telerik.WinControls.UI.RadButton radButton2;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Panel panel5;
        private Telerik.WinControls.UI.RadLabel radLabel2;
        private Telerik.WinControls.UI.RadButton radButton3;
        private Telerik.WinControls.UI.RadButton radButton4;
    }
}

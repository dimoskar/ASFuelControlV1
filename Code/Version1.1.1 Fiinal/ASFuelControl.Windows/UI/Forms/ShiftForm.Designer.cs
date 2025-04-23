namespace ASFuelControl.Windows.UI.Forms
{
    partial class ShiftForm
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
            this.panel4 = new System.Windows.Forms.Panel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.radLabel1 = new Telerik.WinControls.UI.RadLabel();
            this.label2 = new System.Windows.Forms.Label();
            this.PasswordText = new Telerik.WinControls.UI.RadTextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.radDropDownList1 = new Telerik.WinControls.UI.RadDropDownList();
            this.label3 = new System.Windows.Forms.Label();
            this.radSpinEditor1 = new Telerik.WinControls.UI.RadSpinEditor();
            this.label4 = new System.Windows.Forms.Label();
            this.radButton1 = new Telerik.WinControls.UI.RadButton();
            this.panelClose = new System.Windows.Forms.Panel();
            this.panelOpen = new System.Windows.Forms.Panel();
            this.label5 = new System.Windows.Forms.Label();
            this.radButton2 = new Telerik.WinControls.UI.RadButton();
            this.PasswordTextOpen = new Telerik.WinControls.UI.RadTextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.radButton3 = new Telerik.WinControls.UI.RadButton();
            this.radButton4 = new Telerik.WinControls.UI.RadButton();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PasswordText)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDropDownList1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radSpinEditor1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton1)).BeginInit();
            this.panelClose.SuspendLayout();
            this.panelOpen.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.radButton2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PasswordTextOpen)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton4)).BeginInit();
            this.SuspendLayout();
            // 
            // panel4
            // 
            this.panel4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(233)))), ((int)(((byte)(82)))), ((int)(((byte)(65)))));
            this.panel4.BackgroundImage = global::ASFuelControl.Windows.Properties.Resources.MainLogo;
            this.panel4.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.panel4.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel4.Location = new System.Drawing.Point(0, 0);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(81, 214);
            this.panel4.TabIndex = 7;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(233)))), ((int)(((byte)(82)))), ((int)(((byte)(65)))));
            this.panel1.Controls.Add(this.radLabel1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 214);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(725, 19);
            this.panel1.TabIndex = 8;
            // 
            // radLabel1
            // 
            this.radLabel1.ForeColor = System.Drawing.Color.White;
            this.radLabel1.Location = new System.Drawing.Point(101, 0);
            this.radLabel1.Name = "radLabel1";
            this.radLabel1.Size = new System.Drawing.Size(180, 18);
            this.radLabel1.TabIndex = 0;
            this.radLabel1.Text = "Copyright 2014 ΑραμπατζηService";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.label2.ForeColor = System.Drawing.Color.White;
            this.label2.Location = new System.Drawing.Point(13, 36);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(56, 16);
            this.label2.TabIndex = 10;
            this.label2.Text = "Κωδικός";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // PasswordText
            // 
            this.PasswordText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.PasswordText.Location = new System.Drawing.Point(126, 34);
            this.PasswordText.Name = "PasswordText";
            this.PasswordText.PasswordChar = '*';
            this.PasswordText.Size = new System.Drawing.Size(153, 20);
            this.PasswordText.TabIndex = 9;
            this.PasswordText.KeyUp += new System.Windows.Forms.KeyEventHandler(this.PasswordText_KeyUp);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(123, 10);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(60, 16);
            this.label1.TabIndex = 11;
            this.label1.Text = "Χρήστης";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // radDropDownList1
            // 
            this.radDropDownList1.DropDownStyle = Telerik.WinControls.RadDropDownStyle.DropDownList;
            this.radDropDownList1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            this.radDropDownList1.Location = new System.Drawing.Point(126, 8);
            this.radDropDownList1.Name = "radDropDownList1";
            this.radDropDownList1.Size = new System.Drawing.Size(153, 20);
            this.radDropDownList1.TabIndex = 13;
            this.radDropDownList1.Text = "radDropDownList1";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.label3.ForeColor = System.Drawing.Color.White;
            this.label3.Location = new System.Drawing.Point(13, 10);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(96, 16);
            this.label3.TabIndex = 12;
            this.label3.Text = "Όνομα Χρήστη";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // radSpinEditor1
            // 
            this.radSpinEditor1.DecimalPlaces = 2;
            this.radSpinEditor1.Font = new System.Drawing.Font("Segoe UI", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radSpinEditor1.Location = new System.Drawing.Point(126, 84);
            this.radSpinEditor1.Maximum = new decimal(new int[] {
            999999999,
            0,
            0,
            0});
            this.radSpinEditor1.Name = "radSpinEditor1";
            this.radSpinEditor1.ReadOnly = true;
            this.radSpinEditor1.Size = new System.Drawing.Size(153, 33);
            this.radSpinEditor1.TabIndex = 14;
            this.radSpinEditor1.TabStop = false;
            this.radSpinEditor1.TextAlignment = System.Windows.Forms.HorizontalAlignment.Center;
            this.radSpinEditor1.Value = new decimal(new int[] {
            125874,
            0,
            0,
            0});
            this.radSpinEditor1.Visible = false;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.label4.ForeColor = System.Drawing.Color.White;
            this.label4.Location = new System.Drawing.Point(11, 88);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(63, 25);
            this.label4.TabIndex = 15;
            this.label4.Text = "Ποσό";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.label4.Visible = false;
            // 
            // radButton1
            // 
            this.radButton1.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radButton1.Location = new System.Drawing.Point(16, 162);
            this.radButton1.Name = "radButton1";
            this.radButton1.Size = new System.Drawing.Size(184, 24);
            this.radButton1.TabIndex = 16;
            this.radButton1.Text = "Κλείσιμο Βάρδιας";
            this.radButton1.Visible = false;
            this.radButton1.Click += new System.EventHandler(this.radButton1_Click);
            // 
            // panelClose
            // 
            this.panelClose.Controls.Add(this.radButton3);
            this.panelClose.Controls.Add(this.label3);
            this.panelClose.Controls.Add(this.radButton1);
            this.panelClose.Controls.Add(this.PasswordText);
            this.panelClose.Controls.Add(this.label4);
            this.panelClose.Controls.Add(this.label2);
            this.panelClose.Controls.Add(this.radSpinEditor1);
            this.panelClose.Controls.Add(this.label1);
            this.panelClose.Dock = System.Windows.Forms.DockStyle.Left;
            this.panelClose.Location = new System.Drawing.Point(81, 0);
            this.panelClose.Name = "panelClose";
            this.panelClose.Size = new System.Drawing.Size(295, 214);
            this.panelClose.TabIndex = 17;
            // 
            // panelOpen
            // 
            this.panelOpen.Controls.Add(this.radButton4);
            this.panelOpen.Controls.Add(this.label5);
            this.panelOpen.Controls.Add(this.radButton2);
            this.panelOpen.Controls.Add(this.PasswordTextOpen);
            this.panelOpen.Controls.Add(this.label7);
            this.panelOpen.Controls.Add(this.radDropDownList1);
            this.panelOpen.Dock = System.Windows.Forms.DockStyle.Left;
            this.panelOpen.Location = new System.Drawing.Point(376, 0);
            this.panelOpen.Name = "panelOpen";
            this.panelOpen.Size = new System.Drawing.Size(295, 214);
            this.panelOpen.TabIndex = 18;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.label5.ForeColor = System.Drawing.Color.White;
            this.label5.Location = new System.Drawing.Point(13, 10);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(96, 16);
            this.label5.TabIndex = 12;
            this.label5.Text = "Όνομα Χρήστη";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // radButton2
            // 
            this.radButton2.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radButton2.Location = new System.Drawing.Point(16, 162);
            this.radButton2.Name = "radButton2";
            this.radButton2.Size = new System.Drawing.Size(184, 24);
            this.radButton2.TabIndex = 16;
            this.radButton2.Text = "Άνοιγμα Βάρδιας";
            this.radButton2.Visible = false;
            this.radButton2.Click += new System.EventHandler(this.radButton2_Click);
            // 
            // PasswordTextOpen
            // 
            this.PasswordTextOpen.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.PasswordTextOpen.Location = new System.Drawing.Point(126, 34);
            this.PasswordTextOpen.Name = "PasswordTextOpen";
            this.PasswordTextOpen.PasswordChar = '*';
            this.PasswordTextOpen.Size = new System.Drawing.Size(153, 20);
            this.PasswordTextOpen.TabIndex = 9;
            this.PasswordTextOpen.KeyUp += new System.Windows.Forms.KeyEventHandler(this.PasswordTextOpen_KeyUp);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.label7.ForeColor = System.Drawing.Color.White;
            this.label7.Location = new System.Drawing.Point(13, 36);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(56, 16);
            this.label7.TabIndex = 10;
            this.label7.Text = "Κωδικός";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // radButton3
            // 
            this.radButton3.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radButton3.Location = new System.Drawing.Point(206, 162);
            this.radButton3.Name = "radButton3";
            this.radButton3.Size = new System.Drawing.Size(73, 24);
            this.radButton3.TabIndex = 17;
            this.radButton3.Text = "Άκυρο";
            this.radButton3.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // radButton4
            // 
            this.radButton4.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radButton4.Location = new System.Drawing.Point(206, 162);
            this.radButton4.Name = "radButton4";
            this.radButton4.Size = new System.Drawing.Size(73, 24);
            this.radButton4.TabIndex = 18;
            this.radButton4.Text = "Άκυρο";
            this.radButton4.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // ShiftForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(233)))), ((int)(((byte)(82)))), ((int)(((byte)(65)))));
            this.ClientSize = new System.Drawing.Size(725, 233);
            this.Controls.Add(this.panelOpen);
            this.Controls.Add(this.panelClose);
            this.Controls.Add(this.panel4);
            this.Controls.Add(this.panel1);
            this.Name = "ShiftForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ShiftForm";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.radLabel1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PasswordText)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDropDownList1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radSpinEditor1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton1)).EndInit();
            this.panelClose.ResumeLayout(false);
            this.panelClose.PerformLayout();
            this.panelOpen.ResumeLayout(false);
            this.panelOpen.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.radButton2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PasswordTextOpen)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton4)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Panel panel1;
        private Telerik.WinControls.UI.RadLabel radLabel1;
        private System.Windows.Forms.Label label2;
        private Telerik.WinControls.UI.RadTextBox PasswordText;
        private System.Windows.Forms.Label label1;
        private Telerik.WinControls.UI.RadDropDownList radDropDownList1;
        private System.Windows.Forms.Label label3;
        private Telerik.WinControls.UI.RadSpinEditor radSpinEditor1;
        private System.Windows.Forms.Label label4;
        private Telerik.WinControls.UI.RadButton radButton1;
        private System.Windows.Forms.Panel panelClose;
        private System.Windows.Forms.Panel panelOpen;
        private System.Windows.Forms.Label label5;
        private Telerik.WinControls.UI.RadButton radButton2;
        private Telerik.WinControls.UI.RadTextBox PasswordTextOpen;
        private System.Windows.Forms.Label label7;
        private Telerik.WinControls.UI.RadButton radButton3;
        private Telerik.WinControls.UI.RadButton radButton4;
    }
}
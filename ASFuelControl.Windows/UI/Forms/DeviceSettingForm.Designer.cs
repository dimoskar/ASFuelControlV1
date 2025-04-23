namespace ASFuelControl.Windows.UI.Forms
{
    partial class DeviceSettingForm
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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.Label descriptionLabel;
            System.Windows.Forms.Label deviceTypeLabel;
            System.Windows.Forms.Label settingValueLabel;
            this.deviceSettingBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.descriptionRadTextBox = new Telerik.WinControls.UI.RadTextBox();
            this.settingValueRadTextBox = new Telerik.WinControls.UI.RadTextBox();
            this.radButton1 = new Telerik.WinControls.UI.RadButton();
            this.radButton2 = new Telerik.WinControls.UI.RadButton();
            this.radDropDownList1 = new Telerik.WinControls.UI.RadDropDownList();
            descriptionLabel = new System.Windows.Forms.Label();
            deviceTypeLabel = new System.Windows.Forms.Label();
            settingValueLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.deviceSettingBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.descriptionRadTextBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.settingValueRadTextBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDropDownList1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();
            // 
            // descriptionLabel
            // 
            descriptionLabel.AutoSize = true;
            descriptionLabel.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            descriptionLabel.Location = new System.Drawing.Point(11, 12);
            descriptionLabel.Name = "descriptionLabel";
            descriptionLabel.Size = new System.Drawing.Size(73, 17);
            descriptionLabel.TabIndex = 1;
            descriptionLabel.Text = "Περιγραφή";
            // 
            // deviceTypeLabel
            // 
            deviceTypeLabel.AutoSize = true;
            deviceTypeLabel.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            deviceTypeLabel.Location = new System.Drawing.Point(11, 44);
            deviceTypeLabel.Name = "deviceTypeLabel";
            deviceTypeLabel.Size = new System.Drawing.Size(98, 17);
            deviceTypeLabel.TabIndex = 3;
            deviceTypeLabel.Text = "Είδος Συσκευής";
            // 
            // settingValueLabel
            // 
            settingValueLabel.AutoSize = true;
            settingValueLabel.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            settingValueLabel.Location = new System.Drawing.Point(11, 72);
            settingValueLabel.Name = "settingValueLabel";
            settingValueLabel.Size = new System.Drawing.Size(32, 17);
            settingValueLabel.TabIndex = 7;
            settingValueLabel.Text = "Τιμή";
            // 
            // deviceSettingBindingSource
            // 
            this.deviceSettingBindingSource.DataSource = typeof(ASFuelControl.Data.DeviceSetting);
            // 
            // descriptionRadTextBox
            // 
            this.descriptionRadTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.deviceSettingBindingSource, "Description", true));
            this.descriptionRadTextBox.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.descriptionRadTextBox.Location = new System.Drawing.Point(115, 12);
            this.descriptionRadTextBox.Name = "descriptionRadTextBox";
            this.descriptionRadTextBox.Size = new System.Drawing.Size(315, 23);
            this.descriptionRadTextBox.TabIndex = 2;
            // 
            // settingValueRadTextBox
            // 
            this.settingValueRadTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.deviceSettingBindingSource, "SettingValue", true));
            this.settingValueRadTextBox.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.settingValueRadTextBox.Location = new System.Drawing.Point(115, 72);
            this.settingValueRadTextBox.Name = "settingValueRadTextBox";
            this.settingValueRadTextBox.Size = new System.Drawing.Size(315, 23);
            this.settingValueRadTextBox.TabIndex = 8;
            // 
            // radButton1
            // 
            this.radButton1.Image = global::ASFuelControl.Windows.Properties.Resources.Save;
            this.radButton1.Location = new System.Drawing.Point(14, 111);
            this.radButton1.Name = "radButton1";
            this.radButton1.Padding = new System.Windows.Forms.Padding(5, 0, 0, 0);
            this.radButton1.Size = new System.Drawing.Size(134, 44);
            this.radButton1.TabIndex = 9;
            this.radButton1.Text = "Αποθήκευση";
            this.radButton1.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.radButton1.Click += new System.EventHandler(this.radButton1_Click);
            // 
            // radButton2
            // 
            this.radButton2.Image = global::ASFuelControl.Windows.Properties.Resources.Cancel;
            this.radButton2.Location = new System.Drawing.Point(296, 111);
            this.radButton2.Name = "radButton2";
            this.radButton2.Padding = new System.Windows.Forms.Padding(5, 0, 0, 0);
            this.radButton2.Size = new System.Drawing.Size(134, 44);
            this.radButton2.TabIndex = 10;
            this.radButton2.Text = "Άκυρο";
            this.radButton2.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.radButton2.Click += new System.EventHandler(this.radButton2_Click);
            // 
            // radDropDownList1
            // 
            this.radDropDownList1.DataBindings.Add(new System.Windows.Forms.Binding("SelectedValue", this.deviceSettingBindingSource, "DeviceType", true));
            this.radDropDownList1.DropDownStyle = Telerik.WinControls.RadDropDownStyle.DropDownList;
            this.radDropDownList1.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radDropDownList1.Location = new System.Drawing.Point(115, 41);
            this.radDropDownList1.Name = "radDropDownList1";
            this.radDropDownList1.Size = new System.Drawing.Size(315, 23);
            this.radDropDownList1.TabIndex = 11;
            this.radDropDownList1.Text = "radDropDownList1";
            // 
            // DeviceSettingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(446, 164);
            this.Controls.Add(this.radDropDownList1);
            this.Controls.Add(this.radButton2);
            this.Controls.Add(this.radButton1);
            this.Controls.Add(descriptionLabel);
            this.Controls.Add(this.descriptionRadTextBox);
            this.Controls.Add(deviceTypeLabel);
            this.Controls.Add(settingValueLabel);
            this.Controls.Add(this.settingValueRadTextBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "DeviceSettingForm";
            // 
            // 
            // 
            this.RootElement.ApplyShapeToControl = true;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Σειριακός Συσκευής";
            ((System.ComponentModel.ISupportInitialize)(this.deviceSettingBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.descriptionRadTextBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.settingValueRadTextBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDropDownList1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.BindingSource deviceSettingBindingSource;
        private Telerik.WinControls.UI.RadTextBox descriptionRadTextBox;
        private Telerik.WinControls.UI.RadTextBox settingValueRadTextBox;
        private Telerik.WinControls.UI.RadButton radButton1;
        private Telerik.WinControls.UI.RadButton radButton2;
        private Telerik.WinControls.UI.RadDropDownList radDropDownList1;
    }
}
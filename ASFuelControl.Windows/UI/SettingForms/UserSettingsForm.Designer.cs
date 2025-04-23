namespace ASFuelControl.Windows.UI.SettingForms
{
    partial class UserSettingsForm
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
            System.Windows.Forms.Label passwordLabel;
            System.Windows.Forms.Label userLevelLabel;
            System.Windows.Forms.Label userNameLabel;
            Telerik.WinControls.UI.GridViewDateTimeColumn gridViewDateTimeColumn1 = new Telerik.WinControls.UI.GridViewDateTimeColumn();
            Telerik.WinControls.UI.GridViewDateTimeColumn gridViewDateTimeColumn2 = new Telerik.WinControls.UI.GridViewDateTimeColumn();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UserSettingsForm));
            this.radListView1 = new Telerik.WinControls.UI.RadListView();
            this.applicationUserBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnDelete = new Telerik.WinControls.UI.RadButton();
            this.btnSave = new Telerik.WinControls.UI.RadButton();
            this.btnAdd = new Telerik.WinControls.UI.RadButton();
            this.passwordRadTextBox = new Telerik.WinControls.UI.RadTextBox();
            this.userNameRadTextBox = new Telerik.WinControls.UI.RadTextBox();
            this.radDropDownList1 = new Telerik.WinControls.UI.RadDropDownList();
            this.applicationUserLoggonsBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.shiftsBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.shiftsRadGridView = new Telerik.WinControls.UI.RadGridView();
            passwordLabel = new System.Windows.Forms.Label();
            userLevelLabel = new System.Windows.Forms.Label();
            userNameLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.radListView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.applicationUserBindingSource)).BeginInit();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.btnDelete)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.btnSave)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.btnAdd)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.passwordRadTextBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.userNameRadTextBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDropDownList1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.applicationUserLoggonsBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.shiftsBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.shiftsRadGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.shiftsRadGridView.MasterTemplate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();
            // 
            // passwordLabel
            // 
            passwordLabel.AutoSize = true;
            passwordLabel.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            passwordLabel.Location = new System.Drawing.Point(232, 80);
            passwordLabel.Name = "passwordLabel";
            passwordLabel.Size = new System.Drawing.Size(67, 17);
            passwordLabel.TabIndex = 8;
            passwordLabel.Text = "Password:";
            // 
            // userLevelLabel
            // 
            userLevelLabel.AutoSize = true;
            userLevelLabel.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            userLevelLabel.Location = new System.Drawing.Point(232, 109);
            userLevelLabel.Name = "userLevelLabel";
            userLevelLabel.Size = new System.Drawing.Size(71, 17);
            userLevelLabel.TabIndex = 10;
            userLevelLabel.Text = "User Level:";
            // 
            // userNameLabel
            // 
            userNameLabel.AutoSize = true;
            userNameLabel.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            userNameLabel.Location = new System.Drawing.Point(232, 51);
            userNameLabel.Name = "userNameLabel";
            userNameLabel.Size = new System.Drawing.Size(77, 17);
            userNameLabel.TabIndex = 12;
            userNameLabel.Text = "User Name:";
            // 
            // radListView1
            // 
            this.radListView1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.radListView1.DataSource = this.applicationUserBindingSource;
            this.radListView1.DisplayMember = "UserName";
            this.radListView1.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radListView1.Location = new System.Drawing.Point(1, 48);
            this.radListView1.Name = "radListView1";
            this.radListView1.Size = new System.Drawing.Size(224, 414);
            this.radListView1.TabIndex = 0;
            this.radListView1.Text = "radListView1";
            this.radListView1.ValueMember = "ApplicationUserId";
            // 
            // applicationUserBindingSource
            // 
            this.applicationUserBindingSource.DataSource = typeof(ASFuelControl.Data.ApplicationUser);
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.btnDelete);
            this.panel1.Controls.Add(this.btnSave);
            this.panel1.Controls.Add(this.btnAdd);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(632, 42);
            this.panel1.TabIndex = 3;
            // 
            // btnDelete
            // 
            this.btnDelete.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnDelete.Image = global::ASFuelControl.Windows.Properties.Resources.Delete;
            this.btnDelete.Location = new System.Drawing.Point(141, 0);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(141, 40);
            this.btnDelete.TabIndex = 2;
            this.btnDelete.Text = "Διαγραφή";
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // btnSave
            // 
            this.btnSave.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnSave.Image = global::ASFuelControl.Windows.Properties.Resources.Save;
            this.btnSave.Location = new System.Drawing.Point(489, 0);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(141, 40);
            this.btnSave.TabIndex = 0;
            this.btnSave.Text = "Αποθήκευση";
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnAdd
            // 
            this.btnAdd.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnAdd.Image = global::ASFuelControl.Windows.Properties.Resources.Add;
            this.btnAdd.Location = new System.Drawing.Point(0, 0);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(141, 40);
            this.btnAdd.TabIndex = 3;
            this.btnAdd.Text = "Προσθήκη";
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // passwordRadTextBox
            // 
            this.passwordRadTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.applicationUserBindingSource, "PasswordEnc", true));
            this.passwordRadTextBox.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.passwordRadTextBox.Location = new System.Drawing.Point(346, 77);
            this.passwordRadTextBox.Name = "passwordRadTextBox";
            this.passwordRadTextBox.PasswordChar = '*';
            this.passwordRadTextBox.Size = new System.Drawing.Size(100, 23);
            this.passwordRadTextBox.TabIndex = 9;
            // 
            // userNameRadTextBox
            // 
            this.userNameRadTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.applicationUserBindingSource, "UserName", true));
            this.userNameRadTextBox.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.userNameRadTextBox.Location = new System.Drawing.Point(346, 48);
            this.userNameRadTextBox.Name = "userNameRadTextBox";
            this.userNameRadTextBox.Size = new System.Drawing.Size(284, 23);
            this.userNameRadTextBox.TabIndex = 13;
            // 
            // radDropDownList1
            // 
            this.radDropDownList1.DropDownStyle = Telerik.WinControls.RadDropDownStyle.DropDownList;
            this.radDropDownList1.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.radDropDownList1.Location = new System.Drawing.Point(346, 106);
            this.radDropDownList1.Name = "radDropDownList1";
            this.radDropDownList1.Size = new System.Drawing.Size(284, 23);
            this.radDropDownList1.TabIndex = 14;
            this.radDropDownList1.Text = "radDropDownList1";
            // 
            // applicationUserLoggonsBindingSource
            // 
            this.applicationUserLoggonsBindingSource.DataMember = "ApplicationUserLoggons";
            this.applicationUserLoggonsBindingSource.DataSource = this.applicationUserBindingSource;
            // 
            // shiftsBindingSource
            // 
            this.shiftsBindingSource.DataMember = "Shifts";
            this.shiftsBindingSource.DataSource = this.applicationUserBindingSource;
            this.shiftsBindingSource.Sort = "ShiftBegin";
            // 
            // shiftsRadGridView
            // 
            this.shiftsRadGridView.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.shiftsRadGridView.Location = new System.Drawing.Point(231, 135);
            // 
            // shiftsRadGridView
            // 
            this.shiftsRadGridView.MasterTemplate.AllowAddNewRow = false;
            this.shiftsRadGridView.MasterTemplate.AllowDeleteRow = false;
            this.shiftsRadGridView.MasterTemplate.AutoGenerateColumns = false;
            gridViewDateTimeColumn1.FieldName = "ShiftBegin";
            gridViewDateTimeColumn1.FormatString = "{0:dd/MM/yyyy HH:mm}";
            gridViewDateTimeColumn1.HeaderText = "Έναξη Βάρδιας";
            gridViewDateTimeColumn1.IsAutoGenerated = true;
            gridViewDateTimeColumn1.Name = "ShiftBegin";
            gridViewDateTimeColumn1.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            gridViewDateTimeColumn1.Width = 150;
            gridViewDateTimeColumn2.DataType = typeof(System.Nullable<System.DateTime>);
            gridViewDateTimeColumn2.FieldName = "ShiftEnd";
            gridViewDateTimeColumn2.FormatString = "{0:dd/MM/yyyy HH:mm}";
            gridViewDateTimeColumn2.HeaderText = "Λήξη Βάρδιας";
            gridViewDateTimeColumn2.IsAutoGenerated = true;
            gridViewDateTimeColumn2.Name = "ShiftEnd";
            gridViewDateTimeColumn2.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            gridViewDateTimeColumn2.Width = 150;
            this.shiftsRadGridView.MasterTemplate.Columns.AddRange(new Telerik.WinControls.UI.GridViewDataColumn[] {
            gridViewDateTimeColumn1,
            gridViewDateTimeColumn2});
            this.shiftsRadGridView.MasterTemplate.DataSource = this.shiftsBindingSource;
            this.shiftsRadGridView.Name = "shiftsRadGridView";
            this.shiftsRadGridView.ReadOnly = true;
            this.shiftsRadGridView.ShowGroupPanel = false;
            this.shiftsRadGridView.Size = new System.Drawing.Size(399, 327);
            this.shiftsRadGridView.TabIndex = 14;
            this.shiftsRadGridView.Text = "radGridView1";
            // 
            // UserSettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(632, 463);
            this.Controls.Add(this.shiftsRadGridView);
            this.Controls.Add(this.radDropDownList1);
            this.Controls.Add(passwordLabel);
            this.Controls.Add(this.passwordRadTextBox);
            this.Controls.Add(userLevelLabel);
            this.Controls.Add(userNameLabel);
            this.Controls.Add(this.userNameRadTextBox);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.radListView1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "UserSettingsForm";
            // 
            // 
            // 
            this.RootElement.ApplyShapeToControl = true;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Χρήστες Εφαρμογής";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.UserSettingsForm_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.radListView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.applicationUserBindingSource)).EndInit();
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.btnDelete)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.btnSave)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.btnAdd)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.passwordRadTextBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.userNameRadTextBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radDropDownList1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.applicationUserLoggonsBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.shiftsBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.shiftsRadGridView.MasterTemplate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.shiftsRadGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Telerik.WinControls.UI.RadListView radListView1;
        private System.Windows.Forms.Panel panel1;
        private Telerik.WinControls.UI.RadButton btnDelete;
        private Telerik.WinControls.UI.RadButton btnSave;
        private System.Windows.Forms.BindingSource applicationUserBindingSource;
        private Telerik.WinControls.UI.RadTextBox passwordRadTextBox;
        private Telerik.WinControls.UI.RadTextBox userNameRadTextBox;
        private Telerik.WinControls.UI.RadDropDownList radDropDownList1;
        private System.Windows.Forms.BindingSource applicationUserLoggonsBindingSource;
        private System.Windows.Forms.BindingSource shiftsBindingSource;
        private Telerik.WinControls.UI.RadGridView shiftsRadGridView;
        private Telerik.WinControls.UI.RadButton btnAdd;
    }
}
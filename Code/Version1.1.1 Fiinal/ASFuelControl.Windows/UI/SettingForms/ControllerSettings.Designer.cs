namespace ASFuelControl.Windows.UI.SettingForms
{
    partial class ControllerSettings
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
            System.Windows.Forms.Label communicationPortLabel;
            System.Windows.Forms.Label communicationProtocolLabel;
            System.Windows.Forms.Label controllerAssemblyLabel;
            System.Windows.Forms.Label nameLabel;
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ControllerSettings));
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnDelete = new Telerik.WinControls.UI.RadButton();
            this.radButton1 = new Telerik.WinControls.UI.RadButton();
            this.btnAdd = new Telerik.WinControls.UI.RadButton();
            this.radSplitContainer1 = new Telerik.WinControls.UI.RadSplitContainer();
            this.splitPanel1 = new Telerik.WinControls.UI.SplitPanel();
            this.radListControl1 = new Telerik.WinControls.UI.RadListControl();
            this.splitPanel2 = new Telerik.WinControls.UI.SplitPanel();
            this.portDropDown = new Telerik.WinControls.UI.RadDropDownList();
            this.communicationControllerBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.communicationProtocolRadDropDownList = new Telerik.WinControls.UI.RadDropDownList();
            this.controllerAssemblyRadTextBox = new Telerik.WinControls.UI.RadTextBox();
            this.nameRadTextBox = new Telerik.WinControls.UI.RadTextBox();
            communicationPortLabel = new System.Windows.Forms.Label();
            communicationProtocolLabel = new System.Windows.Forms.Label();
            controllerAssemblyLabel = new System.Windows.Forms.Label();
            nameLabel = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.btnDelete)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.btnAdd)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radSplitContainer1)).BeginInit();
            this.radSplitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitPanel1)).BeginInit();
            this.splitPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.radListControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitPanel2)).BeginInit();
            this.splitPanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.portDropDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.communicationControllerBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.communicationProtocolRadDropDownList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.controllerAssemblyRadTextBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nameRadTextBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();
            // 
            // communicationPortLabel
            // 
            communicationPortLabel.AutoSize = true;
            communicationPortLabel.Location = new System.Drawing.Point(14, 42);
            communicationPortLabel.Name = "communicationPortLabel";
            communicationPortLabel.Size = new System.Drawing.Size(107, 13);
            communicationPortLabel.TabIndex = 2;
            communicationPortLabel.Text = "Θύρα Επικοινωνίας";
            // 
            // communicationProtocolLabel
            // 
            communicationProtocolLabel.AutoSize = true;
            communicationProtocolLabel.Location = new System.Drawing.Point(14, 67);
            communicationProtocolLabel.Name = "communicationProtocolLabel";
            communicationProtocolLabel.Size = new System.Drawing.Size(109, 13);
            communicationProtocolLabel.TabIndex = 4;
            communicationProtocolLabel.Text = "Τύπος Επικοινωνίας";
            // 
            // controllerAssemblyLabel
            // 
            controllerAssemblyLabel.AutoSize = true;
            controllerAssemblyLabel.Location = new System.Drawing.Point(14, 94);
            controllerAssemblyLabel.Name = "controllerAssemblyLabel";
            controllerAssemblyLabel.Size = new System.Drawing.Size(65, 13);
            controllerAssemblyLabel.TabIndex = 6;
            controllerAssemblyLabel.Text = "Βιβλιοθήκη";
            // 
            // nameLabel
            // 
            nameLabel.AutoSize = true;
            nameLabel.Location = new System.Drawing.Point(14, 16);
            nameLabel.Name = "nameLabel";
            nameLabel.Size = new System.Drawing.Size(60, 13);
            nameLabel.TabIndex = 10;
            nameLabel.Text = "Ονομασία";
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.btnDelete);
            this.panel1.Controls.Add(this.radButton1);
            this.panel1.Controls.Add(this.btnAdd);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(657, 42);
            this.panel1.TabIndex = 4;
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
            // radButton1
            // 
            this.radButton1.Dock = System.Windows.Forms.DockStyle.Right;
            this.radButton1.Enabled = false;
            this.radButton1.Image = global::ASFuelControl.Windows.Properties.Resources.Save;
            this.radButton1.Location = new System.Drawing.Point(514, 0);
            this.radButton1.Name = "radButton1";
            this.radButton1.Size = new System.Drawing.Size(141, 40);
            this.radButton1.TabIndex = 0;
            this.radButton1.Text = "Αποθήκευση";
            this.radButton1.Click += new System.EventHandler(this.radButton1_Click);
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
            // radSplitContainer1
            // 
            this.radSplitContainer1.Controls.Add(this.splitPanel1);
            this.radSplitContainer1.Controls.Add(this.splitPanel2);
            this.radSplitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.radSplitContainer1.Location = new System.Drawing.Point(0, 42);
            this.radSplitContainer1.Name = "radSplitContainer1";
            // 
            // 
            // 
            this.radSplitContainer1.RootElement.MinSize = new System.Drawing.Size(25, 25);
            this.radSplitContainer1.Size = new System.Drawing.Size(657, 328);
            this.radSplitContainer1.SplitterWidth = 4;
            this.radSplitContainer1.TabIndex = 5;
            this.radSplitContainer1.TabStop = false;
            this.radSplitContainer1.Text = "radSplitContainer1";
            // 
            // splitPanel1
            // 
            this.splitPanel1.Controls.Add(this.radListControl1);
            this.splitPanel1.Location = new System.Drawing.Point(0, 0);
            this.splitPanel1.Name = "splitPanel1";
            // 
            // 
            // 
            this.splitPanel1.RootElement.MinSize = new System.Drawing.Size(25, 25);
            this.splitPanel1.Size = new System.Drawing.Size(250, 328);
            this.splitPanel1.SizeInfo.AbsoluteSize = new System.Drawing.Size(250, 200);
            this.splitPanel1.SizeInfo.SizeMode = Telerik.WinControls.UI.Docking.SplitPanelSizeMode.Absolute;
            this.splitPanel1.TabIndex = 0;
            this.splitPanel1.TabStop = false;
            this.splitPanel1.Text = "splitPanel1";
            // 
            // radListControl1
            // 
            this.radListControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.radListControl1.Location = new System.Drawing.Point(0, 0);
            this.radListControl1.Name = "radListControl1";
            this.radListControl1.Size = new System.Drawing.Size(250, 328);
            this.radListControl1.TabIndex = 0;
            this.radListControl1.Text = "radListControl1";
            // 
            // splitPanel2
            // 
            this.splitPanel2.Controls.Add(this.portDropDown);
            this.splitPanel2.Controls.Add(this.communicationProtocolRadDropDownList);
            this.splitPanel2.Controls.Add(communicationPortLabel);
            this.splitPanel2.Controls.Add(communicationProtocolLabel);
            this.splitPanel2.Controls.Add(controllerAssemblyLabel);
            this.splitPanel2.Controls.Add(this.controllerAssemblyRadTextBox);
            this.splitPanel2.Controls.Add(nameLabel);
            this.splitPanel2.Controls.Add(this.nameRadTextBox);
            this.splitPanel2.Location = new System.Drawing.Point(254, 0);
            this.splitPanel2.Name = "splitPanel2";
            // 
            // 
            // 
            this.splitPanel2.RootElement.MinSize = new System.Drawing.Size(25, 25);
            this.splitPanel2.Size = new System.Drawing.Size(403, 328);
            this.splitPanel2.TabIndex = 1;
            this.splitPanel2.TabStop = false;
            this.splitPanel2.Text = "splitPanel2";
            // 
            // portDropDown
            // 
            this.portDropDown.DataBindings.Add(new System.Windows.Forms.Binding("SelectedValue", this.communicationControllerBindingSource, "CommunicationPort", true));
            this.portDropDown.DropDownStyle = Telerik.WinControls.RadDropDownStyle.DropDownList;
            this.portDropDown.Location = new System.Drawing.Point(138, 38);
            this.portDropDown.Name = "portDropDown";
            this.portDropDown.Size = new System.Drawing.Size(213, 20);
            this.portDropDown.TabIndex = 13;
            this.portDropDown.Text = "radDropDownList1";
            // 
            // communicationControllerBindingSource
            // 
            this.communicationControllerBindingSource.DataSource = typeof(ASFuelControl.Data.CommunicationController);
            // 
            // communicationProtocolRadDropDownList
            // 
            this.communicationProtocolRadDropDownList.DataBindings.Add(new System.Windows.Forms.Binding("SelectedValue", this.communicationControllerBindingSource, "CommunicationProtocol", true));
            this.communicationProtocolRadDropDownList.DropDownStyle = Telerik.WinControls.RadDropDownStyle.DropDownList;
            this.communicationProtocolRadDropDownList.Location = new System.Drawing.Point(138, 64);
            this.communicationProtocolRadDropDownList.Name = "communicationProtocolRadDropDownList";
            this.communicationProtocolRadDropDownList.Size = new System.Drawing.Size(213, 20);
            this.communicationProtocolRadDropDownList.TabIndex = 12;
            this.communicationProtocolRadDropDownList.Text = "radDropDownList1";
            // 
            // controllerAssemblyRadTextBox
            // 
            this.controllerAssemblyRadTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.controllerAssemblyRadTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.communicationControllerBindingSource, "ControllerAssembly", true));
            this.controllerAssemblyRadTextBox.Location = new System.Drawing.Point(138, 90);
            this.controllerAssemblyRadTextBox.Name = "controllerAssemblyRadTextBox";
            this.controllerAssemblyRadTextBox.Size = new System.Drawing.Size(213, 20);
            this.controllerAssemblyRadTextBox.TabIndex = 7;
            // 
            // nameRadTextBox
            // 
            this.nameRadTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.nameRadTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.communicationControllerBindingSource, "Name", true));
            this.nameRadTextBox.Location = new System.Drawing.Point(138, 12);
            this.nameRadTextBox.Name = "nameRadTextBox";
            this.nameRadTextBox.Size = new System.Drawing.Size(212, 20);
            this.nameRadTextBox.TabIndex = 11;
            // 
            // ControllerSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(657, 370);
            this.Controls.Add(this.radSplitContainer1);
            this.Controls.Add(this.panel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(665, 400);
            this.Name = "ControllerSettings";
            // 
            // 
            // 
            this.RootElement.ApplyShapeToControl = true;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Ρυθμίσεις Ελεγκτών";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ControllerSettings_FormClosing);
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.btnDelete)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radButton1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.btnAdd)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radSplitContainer1)).EndInit();
            this.radSplitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitPanel1)).EndInit();
            this.splitPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.radListControl1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitPanel2)).EndInit();
            this.splitPanel2.ResumeLayout(false);
            this.splitPanel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.portDropDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.communicationControllerBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.communicationProtocolRadDropDownList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.controllerAssemblyRadTextBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nameRadTextBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private Telerik.WinControls.UI.RadButton btnDelete;
        private Telerik.WinControls.UI.RadButton radButton1;
        private Telerik.WinControls.UI.RadSplitContainer radSplitContainer1;
        private Telerik.WinControls.UI.SplitPanel splitPanel1;
        private Telerik.WinControls.UI.RadListControl radListControl1;
        private Telerik.WinControls.UI.SplitPanel splitPanel2;
        private System.Windows.Forms.BindingSource communicationControllerBindingSource;
        private Telerik.WinControls.UI.RadTextBox controllerAssemblyRadTextBox;
        private Telerik.WinControls.UI.RadTextBox nameRadTextBox;
        private Telerik.WinControls.UI.RadDropDownList communicationProtocolRadDropDownList;
        private Telerik.WinControls.UI.RadButton btnAdd;
        private Telerik.WinControls.UI.RadDropDownList portDropDown;
    }
}
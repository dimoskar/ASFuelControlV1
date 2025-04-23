using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.SelectionForms
{
    public partial class SelectInvoiceForm : RadForm
    {
        private Guid tankId;
        private Guid fuelTypeId;

        public Guid TankId
        {
            set
            {
                this.tankId = value;
                this.selectInvoiceControl1.TankId = this.tankId;
                this.selectInvoiceControl2.TankId = this.tankId;
                this.selectInvoiceControl3.TankId = this.tankId;
            }
            get
            {
                return this.tankId;
            }
        }

        public Guid FuelTypeId
        {
            set
            {
                this.fuelTypeId = value;
                this.selectInvoiceControl1.FuelTypeId = this.fuelTypeId;
                this.selectInvoiceControl2.FuelTypeId = this.fuelTypeId;
                this.selectInvoiceControl3.FuelTypeId = this.fuelTypeId;
            }
            get
            {
                return this.fuelTypeId;
            }
        }

        public Guid SelectedInvoiceLineId { private set; get; }

        public decimal InvoicedVolume { set; get; }

        public SelectInvoiceControl.FillingModeEnum SelectionFillingMode { private set; get; }

        public SelectInvoiceForm()
        {
            InitializeComponent();
            this.Load += new EventHandler(SelectInvoiceForm_Load);
            this.selectInvoiceControl1.SelectionClicked += new EventHandler<SelectionClickedArgs>(selectInvoiceControl_SelectionClicked);
            this.selectInvoiceControl2.SelectionClicked += new EventHandler<SelectionClickedArgs>(selectInvoiceControl_SelectionClicked);
            this.selectInvoiceControl3.SelectionClicked += new EventHandler<SelectionClickedArgs>(selectInvoiceControl_SelectionClicked);

            this.radDateTimePicker1.Value = DateTime.Today.AddDays(-1);
            this.radDateTimePicker2.Value = DateTime.Today;

            //this.selectInvoiceControl1.DateFrom = this.radDateTimePicker1.Value;
            //this.selectInvoiceControl2.DateFrom = this.radDateTimePicker1.Value;
            //this.selectInvoiceControl3.DateFrom = this.radDateTimePicker1.Value;

            //this.selectInvoiceControl1.DateTo = this.radDateTimePicker2.Value;
            //this.selectInvoiceControl2.DateTo = this.radDateTimePicker2.Value;
            //this.selectInvoiceControl3.DateTo = this.radDateTimePicker2.Value;
        }

        private void LoadData()
        {
            if (this.radPageView1.SelectedPage == this.radPageViewPage1 &&
                (this.selectInvoiceControl1.DateFrom != this.radDateTimePicker1.Value || this.selectInvoiceControl1.DateTo != this.radDateTimePicker2.Value))
            {
                this.selectInvoiceControl1.DateFrom = this.radDateTimePicker1.Value;
                this.selectInvoiceControl1.DateTo = this.radDateTimePicker2.Value;
                this.selectInvoiceControl1.LoadData();
            }
            else if (this.radPageView1.SelectedPage == this.radPageViewPage2 &&
                (this.selectInvoiceControl2.DateFrom != this.radDateTimePicker1.Value || this.selectInvoiceControl2.DateTo != this.radDateTimePicker2.Value))
            {
                this.selectInvoiceControl2.DateFrom = this.radDateTimePicker1.Value;
                this.selectInvoiceControl2.DateTo = this.radDateTimePicker2.Value;
                this.selectInvoiceControl2.LoadData();
            }
            else if (this.radPageView1.SelectedPage == this.radPageViewPage3 &&
                (this.selectInvoiceControl3.DateFrom != this.radDateTimePicker1.Value || this.selectInvoiceControl3.DateTo != this.radDateTimePicker2.Value))
            {
                this.selectInvoiceControl3.DateFrom = this.radDateTimePicker1.Value;
                this.selectInvoiceControl3.DateTo = this.radDateTimePicker2.Value;
                this.selectInvoiceControl3.LoadData();
            }
        }

        void selectInvoiceControl_SelectionClicked(object sender, SelectionClickedArgs e)
        {
            this.SelectedInvoiceLineId = e.SelectedInvoiceLineId;
            this.InvoicedVolume = e.Volume;
            SelectInvoiceControl sic = sender as SelectInvoiceControl;
            this.SelectionFillingMode = sic.FillingMode;
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        void SelectInvoiceForm_Load(object sender, EventArgs e)
        {
            this.selectInvoiceControl1.Initialized = true;
            this.selectInvoiceControl2.Initialized = true;
            this.selectInvoiceControl3.Initialized = true;

            //this.selectInvoiceControl1.LoadData();
            //this.selectInvoiceControl2.LoadData();
            //this.selectInvoiceControl3.LoadData();
        }

        private void radPageView1_SelectedPageChanged(object sender, EventArgs e)
        {
            this.LoadData();
        }

        private void SelectInvoiceForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.SelectedInvoiceLineId != null)
                return;
            
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            this.LoadData();
        }
    }
}

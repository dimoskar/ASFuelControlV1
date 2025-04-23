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
    public partial class SelectPrinterForm : RadForm
    {
        public string SelectedPrinter { set; get; }

        public SelectPrinterForm()
        {
            InitializeComponent();
            List<string> printers = new List<string>();
            foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
            {
                printers.Add(printer);
            }
            this.radListControl1.DataSource = printers.OrderBy(p => p).Select(p => new { Name = p, Description = p });
            this.radListControl1.DisplayMember = "Description";
            this.radListControl1.ValueMember = "Name";
        }

        private void SelectPrinterForm_Load(object sender, EventArgs e)
        {

        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            this.SelectedPrinter = this.radListControl1.SelectedValue.ToString();
            if(this.SelectedPrinter == null || SelectedPrinter.Length == 0)
                this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            else
                this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Close();
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Close();
        }
    }
}

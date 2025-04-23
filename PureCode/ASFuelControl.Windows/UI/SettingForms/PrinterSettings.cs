using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.SettingForms
{
    public partial class PrinterSettings : RadForm
    {
        Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
        Data.Implementation.OptionHandler optionHandler;

        List<TaxDeviceType> taxDevices = new List<TaxDeviceType>();
        List<TaxDeviceType> printerSettings = new List<TaxDeviceType>();

        public PrinterSettings()
        {
            InitializeComponent();

            this.optionHandler = Data.Implementation.OptionHandler.Instance;
            this.database = optionHandler.Database;


            taxDevices.Add(new TaxDeviceType() { Name = "SignA", Description = "Φορολογικός Μηχανισμός Τύπου Α" });
            taxDevices.Add(new TaxDeviceType() { Name = "Samtec", Description = "Φορολογικός Μηχανισμός Τύπου B" });
            this.taxDeviceType.DataSource = taxDevices;
            this.taxDeviceType.DisplayMember = "Description";
            this.taxDeviceType.ValueMember = "Name";
            this.taxDeviceType.SelectedValue = optionHandler.GetOptionOrAdd("DefaultTaxDevice", "Samtec");

            printerSettings.Add(new TaxDeviceType() { Name = "A5", Description = "Α5 Οριζόντια" });
            printerSettings.Add(new TaxDeviceType() { Name = "A4", Description = "A4 Κατακόρυφα" });
            this.radDropDownList1.DataSource = printerSettings;
            this.radDropDownList1.DisplayMember = "Description";
            this.radDropDownList1.ValueMember = "Name";
            this.radDropDownList1.SelectedValue = optionHandler.GetOptionOrAdd("InvoicePageSetting", "A4");

            this.taxLine.Text = optionHandler.GetOptionOrAdd("SingLine", "");
            this.tailLines.Value = int.Parse(optionHandler.GetOptionOrAdd("TailInvoiceLine", "5"));
            if (this.taxDeviceType.SelectedValue == "SignA")
            {
                this.outFolder.Text = optionHandler.GetOptionOrAdd("SignA_SignFolder", "C:\\ASFuelControl\\Sign");
                this.signFolder.Visible = false;
                this.radLabel4.Visible = false;
                this.radButton3.Visible = false;
            }
            else
            {
                this.outFolder.Text = optionHandler.GetOptionOrAdd("Samtec_SignFolder", "C:\\ASFuelControl\\Sign");
                this.signFolder.Text = optionHandler.GetOptionOrAdd("Samtec_OutFolder", "C:\\ASFuelControl\\Sign\\Out");
                this.signFolder.Visible = true;
                this.radLabel4.Visible = true;
                this.radButton3.Visible = true;
            }

            this.printOnPrinterCheck.Checked = optionHandler.GetBoolOption("PrintOnPrinter", false);
            this.chkPrintBarcode.Checked = optionHandler.GetBoolOption("PrintInvoiceBarcode", false);
            this.chkWinPrint.Checked = optionHandler.GetBoolOption("WindowsInvoicePrint", false);
            this.txtBarcodeTemplate.Text = optionHandler.GetOptionOrAdd("BarCodePattern", "[FuelType.EnumeratorValue:2][UnitPrice:4][Volume:8][TotalPrice:8][Invoice.Number:9]");
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                dlg.SelectedPath = System.Environment.CurrentDirectory;
                DialogResult res = dlg.ShowDialog();
                if (res != System.Windows.Forms.DialogResult.OK)
                    return;
                this.outFolder.Text = dlg.SelectedPath;
            }
        }

        private void taxDeviceType_SelectedIndexChanged(object sender, Telerik.WinControls.UI.Data.PositionChangedEventArgs e)
        {
            this.radButton2.Enabled = true;
            string devType = this.taxDeviceType.SelectedValue.ToString();
            if (this.taxDeviceType.SelectedValue == "SignA")
            {
                this.outFolder.Text = optionHandler.GetOptionOrAdd("SignA_SignFolder", "C:\\ASFuelControl\\Sign");
                this.signFolder.Visible = false;
                this.radLabel4.Visible = false;
                this.radButton3.Visible = false;
            }
            else
            {
                this.outFolder.Text = optionHandler.GetOptionOrAdd("Samtec_SignFolder", "C:\\ASFuelControl\\Sign");
                this.signFolder.Text = optionHandler.GetOptionOrAdd("Samtec_SignFolder", "C:\\ASFuelControl\\Sign\\Out");
                this.signFolder.Visible = true;
                this.radLabel4.Visible = true;
                this.radButton3.Visible = true;
            }
        }

        private void taxLine_TextChanged(object sender, EventArgs e)
        {
            this.radButton2.Enabled = true;
        }

        private void tailLines_ValueChanged(object sender, EventArgs e)
        {
            this.radButton2.Enabled = true;
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            string devType = this.taxDeviceType.SelectedValue.ToString();
            optionHandler.SetOption(this.database, "DefaultTaxDevice", devType);

            string invoicePageSetting = this.radDropDownList1.SelectedValue.ToString();
            optionHandler.SetOption(this.database, "InvoicePageSetting", invoicePageSetting);

            string singLine = this.taxLine.Text;
            optionHandler.SetOption(this.database, "SingLine", singLine);

            int tailLinesNr = (int)this.tailLines.Value;
            optionHandler.SetOption(this.database, "TailInvoiceLine", tailLinesNr.ToString());


            if (devType == "SignA")
                optionHandler.SetOption(this.database, "SignA_SignFolder", this.outFolder.Text);
            else
            {
                optionHandler.SetOption(this.database, "Samtec_SignFolder", this.outFolder.Text);
                optionHandler.SetOption(this.database, "Samtec_OutFolder", this.signFolder.Text);
            }

            optionHandler.SetOption(this.database, "PrintOnPrinter", this.printOnPrinterCheck.Checked);
            optionHandler.SetOption(this.database, "PrintInvoiceBarcode", this.chkPrintBarcode.Checked);
            optionHandler.SetOption(this.database, "WindowsInvoicePrint", this.chkWinPrint.Checked);
            optionHandler.SetOption(this.database, "BarCodePattern", this.txtBarcodeTemplate.Text);

            this.radButton2.Enabled = false;

            Data.Implementation.OptionHandler.Instance.LoadOptions();
        }

        private void radButton3_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                dlg.SelectedPath = System.Environment.CurrentDirectory;
                DialogResult res = dlg.ShowDialog();
                if (res != System.Windows.Forms.DialogResult.OK)
                    return;
                this.signFolder.Text = dlg.SelectedPath;
            }
        }

        private void radDropDownList1_SelectedIndexChanged(object sender, Telerik.WinControls.UI.Data.PositionChangedEventArgs e)
        {
            this.radButton2.Enabled = true;
            string devType = this.taxDeviceType.SelectedValue.ToString();
        }
    }

    public class TaxDeviceType
    {
        public string Name { set; get; }
        public string Description { set; get; }
    }
}

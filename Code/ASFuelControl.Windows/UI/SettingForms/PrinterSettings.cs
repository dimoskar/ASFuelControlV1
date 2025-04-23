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
    }

    public class TaxDeviceType
    {
        public string Name { set; get; }
        public string Description { set; get; }
    }
}

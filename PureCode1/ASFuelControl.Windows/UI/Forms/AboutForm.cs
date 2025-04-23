using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.Forms
{
    public partial class AboutForm : RadForm
    {
        public AboutForm()
        {
            InitializeComponent();
            this.crcLabel.Text = Program.ApplicationCRC;
            this.labSerial.Text = Data.Implementation.OptionHandler.Instance.GetOption("SerialNumber");

            this.labelCustomer.Text = Data.Implementation.OptionHandler.Instance.GetOption("CompanyName");
            this.labelCustomerContact.Text = Data.Implementation.OptionHandler.Instance.GetOption("CompanyPhone");
            this.labelAddress.Text = Data.Implementation.OptionHandler.Instance.GetOption("CompanyAddress") + "-" + Data.Implementation.OptionHandler.Instance.GetOption("CompanyCity");
            this.labelTaxInfo.Text = Data.Implementation.OptionHandler.Instance.GetOption("CompanyTIN") + " / " + Data.Implementation.OptionHandler.Instance.GetOption("CompanyTaxOffice");

            this.versionLab.Text = "v" + Program.MainVersion.ToString() + "." + Program.SubVersion.ToString();
        }

        private void radButton1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void radButton2_Click(object sender, EventArgs e)
        {
            string txt = Program.ApplicationCRC;
            txt = txt + "\r\n" + Data.Implementation.OptionHandler.Instance.GetOption("SerialNumber");
            txt = txt + "\r\n" + Data.Implementation.OptionHandler.Instance.GetOption("CompanyName");
            txt = txt + "\r\n" + Data.Implementation.OptionHandler.Instance.GetOption("CompanyPhone");
            txt = txt + "\r\n" + Data.Implementation.OptionHandler.Instance.GetOption("CompanyAddress") + "-" + Data.Implementation.OptionHandler.Instance.GetOption("CompanyCity");
            txt = txt + "\r\n" + Data.Implementation.OptionHandler.Instance.GetOption("CompanyTIN") + " / " + Data.Implementation.OptionHandler.Instance.GetOption("CompanyTaxOffice");

            txt = txt + "\r\n" + "v" + Program.MainVersion.ToString() + "." + Program.SubVersion.ToString();

            Clipboard.SetText(txt);
        }
    }
}

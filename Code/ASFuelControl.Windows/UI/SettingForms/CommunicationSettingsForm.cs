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
    public partial class CommunicationSettingsForm : RadForm
    {
        public CommunicationSettingsForm()
        {
            InitializeComponent();
            this.LoadData();
        }

        private void LoadData()
        {
            this.textEmails.Text = Data.Implementation.OptionHandler.Instance.GetOption("MailRecievers");
            this.textSMS.Text = Data.Implementation.OptionHandler.Instance.GetOption("SmsAccount");
            this.textCell.Text = Data.Implementation.OptionHandler.Instance.GetOption("SmsCellPhone");
            this.cbxAlarm.IsChecked = Data.Implementation.OptionHandler.Instance.GetBoolOption("SmsSendAlarm", false);
            this.cbxChangePrice.IsChecked = Data.Implementation.OptionHandler.Instance.GetBoolOption("SmsSendPriceChange", false);
            this.cbxDelivery.IsChecked = Data.Implementation.OptionHandler.Instance.GetBoolOption("SmsSendDelivery", false);
        }

        private void SaveData()
        {
            Data.Implementation.OptionHandler.Instance.SetOption("MailRecievers", this.textEmails.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("SmsAccount", this.textSMS.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("SmsCellPhone", this.textCell.Text);

            Data.Implementation.OptionHandler.Instance.SetOption("SmsSendAlarm", this.cbxAlarm.IsChecked);
            Data.Implementation.OptionHandler.Instance.SetOption("SmsSendPriceChange", this.cbxChangePrice.IsChecked);
            Data.Implementation.OptionHandler.Instance.SetOption("SmsSendDelivery", this.cbxDelivery.IsChecked);
        }

        private void radButton4_Click(object sender, EventArgs e)
        {
            this.SaveData();
            this.Close();
        }

        private void radButton5_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}

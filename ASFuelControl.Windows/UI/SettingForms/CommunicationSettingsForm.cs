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
            this.sendEmailTextBox.Text = Data.Implementation.OptionHandler.Instance.GetOption("SendEmail");
            this.sendEmailPasswordTextBox.Text = Data.Implementation.OptionHandler.Instance.GetOption("SendEmailPassword");
            this.cbxChangePrice.IsChecked = Data.Implementation.OptionHandler.Instance.GetBoolOption("SmsSendPriceChange", false);
            this.cbxDelivery.IsChecked = Data.Implementation.OptionHandler.Instance.GetBoolOption("SmsSendDelivery", false);
            this.sslCheckBox.Checked = Data.Implementation.OptionHandler.Instance.GetBoolOption("EmailSSL", false);
            this.smtpPortEditor.Value = Data.Implementation.OptionHandler.Instance.GetIntOption("SMTPPort", 25);
            this.mailServerTextBox.Text = Data.Implementation.OptionHandler.Instance.GetOption("OutGoingMailServer");
            this.chkShift.IsChecked = Data.Implementation.OptionHandler.Instance.GetBoolOption("MailSendShift", false);

        }

        private void SaveData()
        {
            Data.Implementation.OptionHandler.Instance.SetOption("MailRecievers", this.textEmails.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("SmsAccount", this.textSMS.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("SmsCellPhone", this.textCell.Text);

            Data.Implementation.OptionHandler.Instance.SetOption("SmsSendAlarm", this.cbxAlarm.IsChecked);
            Data.Implementation.OptionHandler.Instance.SetOption("SmsSendPriceChange", this.cbxChangePrice.IsChecked);
            Data.Implementation.OptionHandler.Instance.SetOption("SmsSendDelivery", this.cbxDelivery.IsChecked);

            Data.Implementation.OptionHandler.Instance.SetOption("SendEmail", this.sendEmailTextBox.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("SendEmailPassword", this.sendEmailPasswordTextBox.Text);

            Data.Implementation.OptionHandler.Instance.SetOption("OutGoingMailServer", this.mailServerTextBox.Text);
            Data.Implementation.OptionHandler.Instance.SetOption("EmailSSL", this.sslCheckBox.Checked);
            Data.Implementation.OptionHandler.Instance.SetOption("SMTPPort", (int)this.smtpPortEditor.Value);

            Data.Implementation.OptionHandler.Instance.SetOption("MailSendShift", this.chkShift.IsChecked);

            Threads.MailSender.Instance.SMTPPort = (int)this.smtpPortEditor.Value;
            Threads.MailSender.Instance.EMailSender = this.sendEmailTextBox.Text;
            Threads.MailSender.Instance.OutgoingServer = this.mailServerTextBox.Text;
            Threads.MailSender.Instance.SSLEnabled = this.sslCheckBox.Checked;
            Threads.MailSender.Instance.MailSenderPassword = this.sendEmailPasswordTextBox.Text;
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

        private void radButton1_Click(object sender, EventArgs e)
        {
            Threads.MailSender.Instance.SMTPPort = (int)this.smtpPortEditor.Value;
            Threads.MailSender.Instance.EMailSender = this.sendEmailTextBox.Text;
            Threads.MailSender.Instance.OutgoingServer = this.mailServerTextBox.Text;
            Threads.MailSender.Instance.SSLEnabled = this.sslCheckBox.Checked;
            Threads.MailSender.Instance.MailSenderPassword = this.sendEmailPasswordTextBox.Text;

            Threads.MailSender.Instance.SendMail(new List<string>().ToArray(), "TEST", "TEST");
        }
    }
}

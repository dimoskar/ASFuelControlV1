using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Mail;

namespace ASFuelControl.Windows.Threads
{
    /// <summary>
    /// Class providing functionality for sending e-mails and SMS's.
    /// </summary>
    public class MailSender
    {
        private static MailSender instance;

        public static MailSender Instance
        {
            get 
            {
                if (instance == null)
                    instance = new MailSender();
                return instance;
            }
        }


        private string[] recievers;
        string mailAddresses = "";
        string cellPhone = "";
        string smsAcount = "";
        string userName = "";
        string password = "";
        System.Threading.Thread th;
        
        public MailSender()
        {
            try
            {
                this.cellPhone = Data.Implementation.OptionHandler.Instance.GetOption("SmsCellPhone");
                this.smsAcount = Data.Implementation.OptionHandler.Instance.GetOption("SmsAccount");
                if (cellPhone.Length == 10 && cellPhone.StartsWith("69"))
                    cellPhone = "30" + cellPhone;
                if (!(smsAcount == null || cellPhone == null || smsAcount == "" || cellPhone == ""))
                {
                    string[] creds = AESEncryption.Decrypt(this.smsAcount, "Exedron").Split(';');
                    if (creds.Length == 2)
                    {
                        userName = creds[0];
                        password = creds[1];
                    }
                }

                this.mailAddresses = Data.Implementation.OptionHandler.Instance.GetOption("MailRecievers");
                if (mailAddresses == null)
                    return;
                recievers = this.mailAddresses.Split(';');
            }
            catch { }
        }

        /// <summary>
        /// Sends email with attached files to mailAddresses
        /// </summary>
        /// <param name="fileNames"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        public void SendMail(string[] fileNames, string subject, string body)
        {
            try
            {
                if (recievers.Length == 0)
                    return;
                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress("asfuelcontrol@gmail.com");
                    foreach (string toAddress in recievers)
                        mail.To.Add(toAddress);

                    mail.Subject = subject;
                    mail.Body = body;
                    mail.IsBodyHtml = false;

                    foreach (string fName in fileNames)
                    {
                        mail.Attachments.Add(new Attachment(fName));
                    }

                    using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                    {
                        smtp.Credentials = new NetworkCredential("asfuelcontrol@gmail.com", "@r@mp@t2#$.AS");
                        smtp.EnableSsl = true;
                        smtp.Port = 587;
                        smtp.Send(mail);
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Sends an Sms to cellPhone
        /// </summary>
        /// <param name="smsText"></param>
        public void SendSms(string smsText)
        {
            try
            {
                if (smsAcount == null || smsAcount == "")
                    return;
                if (cellPhone == null || cellPhone == "")
                    return;
                SmsService.Service1Client client = new SmsService.Service1Client();
                SmsService.BalanceReturn ret = client.GetBalance(userName, password);
                if(ret.Balance > 0)
                    client.SendSms(userName, password, smsText, cellPhone);
                client.Close();
            }
            catch
            {
            }
        }
    }
}

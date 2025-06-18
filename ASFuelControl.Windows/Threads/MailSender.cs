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
        string emailSender = "";
        string emailPassword = "";
        int smtpPort = 0;
        string outgoingServer = "";
        bool ssl = false;
        System.Threading.Thread th;

        public string EMailSender
        {
            set { this.emailSender = value; }
            get { return this.emailSender; }
        }

        public string MailSenderPassword
        {
            set { this.emailPassword = value; }
            get { return this.emailPassword; }
        }

        public bool SSLEnabled
        {
            set { this.ssl = value; }
            get { return this.ssl; }
        }

        public string OutgoingServer
        {
            set { this.outgoingServer = value; }
            get { return this.outgoingServer; }
        }

        public int SMTPPort
        {
            set { this.smtpPort = value; }
            get { return this.smtpPort; }
        }

        public MailSender()
        {
            try
            {
                this.cellPhone = Data.Implementation.OptionHandler.Instance.GetOption("SmsCellPhone");
                this.smsAcount = Data.Implementation.OptionHandler.Instance.GetOption("SmsAccount");
                this.emailSender = Data.Implementation.OptionHandler.Instance.GetOption("SendEmail");
                this.emailPassword = Data.Implementation.OptionHandler.Instance.GetOption("SendEmailPassword");
                this.smtpPort = Data.Implementation.OptionHandler.Instance.GetIntOption("SMTPPort", 25);
                this.outgoingServer = Data.Implementation.OptionHandler.Instance.GetOption("OutGoingMailServer");
                this.ssl = Data.Implementation.OptionHandler.Instance.GetBoolOption("EmailSSL", false);

                if (cellPhone != null &&  cellPhone.Length == 10 && cellPhone.StartsWith("69"))
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
                if (this.emailSender == null || this.emailSender == "")
                    return;
                if (this.emailPassword == null || this.emailPassword == "")
                    return;
                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress(this.emailSender);
                    foreach (string toAddress in recievers)
                        mail.To.Add(toAddress);

                    mail.Subject = subject;
                    mail.Body = body;
                    mail.IsBodyHtml = false;

                    foreach (string fName in fileNames)
                    {
                        mail.Attachments.Add(new Attachment(fName));
                    }

                    using (SmtpClient smtp = new SmtpClient(this.outgoingServer, this.smtpPort))
                    {
                        smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials = new NetworkCredential(this.emailSender, this.emailPassword);
                        smtp.EnableSsl = this.ssl;
                        smtp.Port = this.smtpPort;
                        
                        smtp.Send(mail);
                    }
                }
            }
            catch(Exception ex)
            {
                Logging.Logger.Instance.LogToFile("Send e-Mail", ex);
            }
        }

        /// <summary>
        /// Sends email with attached files to mailAddresses
        /// </summary>
        /// <param name="fileNames"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        public void SendMail(byte[] attachment, string attachmentName, string subject, string body)
        {
            try
            {
                if (recievers.Length == 0)
                    return;
                if (this.emailSender == null || this.emailSender == "")
                    return;
                if (this.emailPassword == null || this.emailPassword == "")
                    return;
                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress(this.emailSender);
                    foreach (string toAddress in recievers)
                        mail.To.Add(toAddress);

                    mail.Subject = subject;
                    mail.Body = body;
                    mail.IsBodyHtml = false;

                    System.IO.MemoryStream ms = new System.IO.MemoryStream(attachment);
                    System.Net.Mime.ContentType ct = new System.Net.Mime.ContentType(System.Net.Mime.MediaTypeNames.Application.Pdf);
                    System.Net.Mail.Attachment attach = new System.Net.Mail.Attachment(ms, ct);
                    attach.ContentDisposition.FileName = attachmentName;
                    mail.Attachments.Add(attach);
                    using (SmtpClient smtp = new SmtpClient(this.outgoingServer, this.smtpPort))
                    {
                        smtp.Credentials = new NetworkCredential(this.emailSender, this.emailPassword);
                        smtp.EnableSsl = this.ssl;
                        smtp.Port = this.smtpPort;
                        smtp.Send(mail);
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Logger.Instance.LogToFile("Send e-Mail", ex);
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

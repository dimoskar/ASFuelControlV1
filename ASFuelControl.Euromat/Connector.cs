using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.Euromat
{
    public class Transaction : IDisposable
    {
        public event EventHandler TransactionCompleted;
        public event EventHandler TransactionDenied;
        public event EventHandler TransactionAllowed;

        private static List<EuromatNRParameters> nrParameters = new List<EuromatNRParameters>();

        SimpleTCP.SimpleTcpClient client = new SimpleTCP.SimpleTcpClient();
        private TransactionStateEnum transState = TransactionStateEnum.Pending;
        private int invNumber = 0;

        public int FuelPoint { set; get; }
        public int Nozzle { set; get; }
        public RateTypeEnum RateType { set; get; }
        public TranTypeEnum TranType { set; get; }
        public string TransactionId { set; get; }
        public int RecieveTimeOut { set; get; } = 5000;
        public DateTime InvoiceDate { set; get; }
        public int InvoiceNumber
        {
            set
            {
                this.invNumber = value;
            }
            get { return this.invNumber; }
        }
        public DateTime EuromatDate { set; get; }
        public int EuromatInvoiceNumber { set; get; }
        public Decimal UnitPrice { set; get; }
        public Decimal Amount { set; get; }
        public Decimal Volume { set; get; }
        public string IPAddress { set; get; }
        public int Port { set; get; }
        public string PlateNumber { set; get; }
        public string FuelTypeCode{ set; get; }
        public string FuelTypeDescription { set; get; }
        public string Odometer { set; get; }

        public TransactionStateEnum TransactionState
        {
            set
            {
                if (value == this.transState)
                    return;
                this.transState = value;
                if (this.transState == TransactionStateEnum.TransDataRecieved && this.TransactionCompleted != null)
                    this.TransactionCompleted(this, new EventArgs());
                else if (this.transState == TransactionStateEnum.DtRecieved && this.TransactionDenied != null)
                    this.TransactionDenied(this, new EventArgs());
                else if (this.transState == TransactionStateEnum.CtRecieved && this.TransactionAllowed != null)
                    this.TransactionAllowed(this, new EventArgs());
                else if (this.transState == TransactionStateEnum.NzCloseAckRecieved && this.Volume == 0)
                {
                    this.SendFt(0, 0, 0);
                }
            }
            get { return this.transState; }
        }
        public bool AwaitingAck
        {
            get
            {
                return this.TransactionState == TransactionStateEnum.FtAck || this.TransactionState == TransactionStateEnum.NzOpenAck ||
                  this.TransactionState == TransactionStateEnum.NzCloseAck;
            }
        }

        public void Dispose()
        {
            this.client.Dispose();
        }

        public bool SentNzUp()
        {
            try
            {
                client.Connect(this.IPAddress, this.Port);
                client.DataReceived += Client_DataReceived;
                client.TcpClient.ReceiveTimeout = 45000;
                client.Delimiter = (byte)0;
                string cmd = string.Format("nz{0:00}{1}", this.FuelPoint, this.Nozzle);
                LogAction("Send NZ UP", cmd);
                client.WriteLine(cmd);
                this.TransactionState = TransactionStateEnum.NzOpenAck;
                int i = 0;
                while (this.TransactionState != TransactionStateEnum.NzOpenAckRecieved && this.TransactionState != TransactionStateEnum.CtRecieved)
                {
                    System.Threading.Thread.Sleep(250);
                    i++;
                    if (i > 5)
                        break;
                }
                if (this.TransactionState != TransactionStateEnum.NzOpenAckRecieved)
                    return false;
                return true;
                //if (msg != null && msg.MessageString != null && msg.MessageString.Length > 0)
                //{
                //    if (msg.MessageString.Contains("AK"))
                //    {
                //        this.TransactionState = TransactionStateEnum.NzOpenAckRecieved;
                //        LogAction("Recieved NZ UP Answer", msg.MessageString);
                //        return true;
                //    }
                //    else
                //    {
                //        LogAction("Recieved NZ UP Answer Error", msg.MessageString);
                //    }
                //}
                //return false;
            }
            catch (Exception ex)
            {
                LogAction("SentNzUp::Exception", ex);
                return false;
            }
        }

        public bool SentNzDown()
        {
            client.Connect(this.IPAddress, this.Port);
            client.Delimiter = (byte)0;
            client.DataReceived += Client_DataReceived;
            client.TcpClient.ReceiveTimeout = 1000;
            string cmd = string.Format("nz{0:00}0", this.FuelPoint, this.Nozzle);
            LogAction("Send NZ DOWN", cmd);
            client.WriteLine(cmd);
            this.TransactionState = TransactionStateEnum.NzCloseAck;
            int i = 0;
            while (this.TransactionState == TransactionStateEnum.NzCloseAck)
            {
                System.Threading.Thread.Sleep(250);
                i++;
                if (i > 5)
                    break;
            }
            if (this.TransactionState != TransactionStateEnum.NzCloseAckRecieved)
                return false;
            return true;
        }

        public bool SendFt(decimal up, decimal amount, decimal volume)
        {
            try
            {
                client.Connect(this.IPAddress, this.Port);
                client.StringEncoder = ASCIIEncoding.ASCII;
                client.TcpClient.ReceiveTimeout = 1000;
                string cmd = string.Format("ft{0:00}0{1}{2}{3:N3}|{4:N2}|{5:N2}", this.FuelPoint, this.Nozzle, (int)this.RateType, up, volume, amount);
                cmd = cmd.Replace(",", ".");
                LogAction("Send FT", cmd);
                this.UnitPrice = up;
                this.Volume = volume;
                this.Amount = amount;
                client.WriteLine(cmd);
                this.TransactionState = TransactionStateEnum.FtSent;
                int i = 0;
                while (this.TransactionState != TransactionStateEnum.FtAckRecieved)
                {
                    System.Threading.Thread.Sleep(250);
                    i++;
                    if (i > 20)
                        break;
                }
                if (this.TransactionState != TransactionStateEnum.FtAckRecieved)
                    return false;
                return true;

                //if (up == 0 && volume == 0 && amount == 0)
                //{
                //    SimpleTCP.Message msg = client.WriteLineAndGetReply(cmd, TimeSpan.FromSeconds(30));
                //    if (msg != null && msg.MessageString != null && msg.MessageString.Length > 0)
                //    {
                //        LogAction("FT Return", msg.MessageString);
                //        if (msg.MessageString.Contains("AK"))
                //        {
                //            this.TransactionState = TransactionStateEnum.TransDataRecieved;
                //            LogAction("Recieved FT Answer", msg.MessageString);
                //            return true;
                //        }
                //        else
                //            LogAction("Recieved FT Answer Error", msg.MessageString);
                //    }
                //}
                //else
                //{
                //    client.DataReceived += Client_DataReceived;
                //    client.TcpClient.ReceiveTimeout = 10000;
                //    SimpleTCP.Message msg = client.WriteLineAndGetReply(cmd, TimeSpan.FromSeconds(30));
                //    if (msg != null && msg.MessageString != null && msg.MessageString.Length > 0)
                //    {
                //        LogAction("FT Return", msg.MessageString);
                //        if (msg.MessageString.Contains("AK"))
                //        {
                //            LogAction("Recieved FT Answer", msg.MessageString);
                //            this.TransactionState = TransactionStateEnum.FtAckRecieved;
                //            this.ParseTransactionData(msg.MessageString);
                //            return true;
                //        }
                //        else
                //            LogAction("Recieved FT Answer Error", msg.MessageString);
                //    }
                //}
                //return false;
            }
            catch(Exception ex)
            {
                LogAction("SendFt::Exception", ex);
                return false;
            }
        }

        public bool SendNR()
        {
            if (client.TcpClient != null && client.TcpClient.Connected)
                client.Disconnect();
            client.Connect(this.IPAddress, this.Port + 2);
            client.StringEncoder = ASCIIEncoding.ASCII;
            client.TcpClient.ReceiveTimeout = 1000;
            string time = this.EuromatDate.ToString("HH:mm");
            string dt = this.EuromatDate.ToString("dd/MM/yy");
            string data = string.Format("{0:N2}|{1:N2}|{2}|{3}|{4}|{5}|{6}|{7}|{8}|{9:N3}|{10}|{11:00}\r\n", 
                this.Amount, 
                this.Volume, 
                this.PlateNumber, 
                time, 
                dt, 
                this.FuelTypeDescription, 
                this.FuelTypeCode, 
                this.TransactionId, 
                this.Odometer, 
                this.UnitPrice, 
                this.InvoiceNumber, 
                this.FuelPoint);
            string cmd = string.Format("NR:{0}", data);
            cmd = cmd.Replace(",", ".");
            LogAction("Send NR", cmd);
            client.WriteLine(cmd);
            this.TransactionState = TransactionStateEnum.NrSent;
            int i = 0;
            while (this.TransactionState != TransactionStateEnum.NrAckRecieved)
            {
                System.Threading.Thread.Sleep(250);
                i++;
                if (i > 20)
                    break;
            }
            this.client.Disconnect();
            this.client.Connect(this.IPAddress, this.Port);
            if (this.TransactionState != TransactionStateEnum.NrAckRecieved)
                return false;
            return true;
        }

        private void ParseCT(string answer)
        {
            try
            {
                string asw = answer.Substring(answer.IndexOf("CT") + 2);
                string fpStr = new string(asw.Take(2).ToArray());
                byte nzByte = (byte)asw.Skip(2).Take(1).First();
                if (this.FuelPoint != int.Parse(fpStr))
                {
                    return;
                }
                this.TranType = (TranTypeEnum)int.Parse(new string(asw.Skip(3).Take(1).ToArray()));
                this.RateType = (RateTypeEnum)int.Parse(new string(asw.Skip(4).Take(1).ToArray()));
                string rest = new string(asw.Skip(5).ToArray());
                string[] parts = rest.Split('|');
                int nzc = parts.Length / 2;
                this.TransactionId = parts[(nzc * 2)].Replace("_", "");
                this.TransactionState = TransactionStateEnum.CtRecieved;
            }
            catch(Exception ex)
            {
                LogAction("ParseCT::Exception", ex);
            }
        }

        private void ParseDT(string answer)
        {
            string asw = answer.Substring(answer.IndexOf("DT") + 2);
            string fpStr = new string(asw.Take(2).ToArray());
            byte nzByte = (byte)asw.Skip(2).Take(1).First();
            if (this.FuelPoint != int.Parse(fpStr))
            {
                return;
            }
            this.TransactionState = TransactionStateEnum.DtRecieved;
        }

        private void ParseTransactionData(string answer)
        {
            if (answer.Contains("AK OK"))
                answer = answer.Replace("AK OK", "");
            try
            {
                string price = new string(answer.Take(9).ToArray()).Replace(" ", "");
                string volume = new string(answer.Skip(11).Take(9).ToArray()).Replace(" ", "");

                string plates = new string(answer.Skip(23).Take(10).ToArray()).Trim();

                string nz = new string(answer.Skip(34).Take(1).ToArray()).Replace(" ", "");
                string fp = new string(answer.Skip(38).Take(2).ToArray()).Replace(" ", "");
                string trnr = new string(answer.Skip(45).Take(6).ToArray()).Replace(" ", "");
                string time = new string(answer.Skip(52).Take(5).ToArray()).Replace(" ", "");
                string dt = new string(answer.Skip(58).Take(8).ToArray()).Replace(" ", "");
                string ftd = new string(answer.Skip(67).Take(10).ToArray()).Replace(" ", "");
                string ftt = new string(answer.Skip(78).Take(4).ToArray()).Replace(" ", "");
                string id = new string(answer.Skip(100).Take(19).ToArray()).Replace(" ", "");
                string odometer = new string(answer.Skip(135).Take(8).ToArray()).Replace(" ", "");
                string up = new string(answer.Skip(153).Take(7).ToArray()).Replace(" ", "");
                if (odometer == "")
                    odometer = "0";

                //if (this.FuelPoint != int.Parse(fp))
                //{
                //    return;
                //}
                this.Odometer = odometer;
                this.PlateNumber = plates;
                this.FuelTypeCode = ftt;
                this.FuelTypeDescription = ftd;

                //if (id != trans.TransactionId)
                //    throw (new EuromatException("Transaction ID Mismatch", EuromatException.EuromatErrorCode.TransactionIdMismatch))
                CultureInfo provider = CultureInfo.InvariantCulture;
                string dateString = dt + " " + time;
                this.EuromatDate = DateTime.ParseExact(dateString, "dd/MM/yy HH:mm", provider);
                this.TransactionState = TransactionStateEnum.TransDataRecieved;

                if (invNumber == 0)
                    return;
                this.SendNR();

                //EuromatNRParameters parameters = new EuromatNRParameters()
                //{
                //    Odometer = odometer,
                //    PlateNumber = plates,
                //    FuelTypeCode = ftt,
                //    FuelTypeDescription = ftd,
                //    EuromatDate = this.EuromatDate,
                //    TransactionId = id
                //};
                //nrParameters.Add(parameters);
            }
            catch(Exception ex)
            {
                LogAction("ParseTransactionData::Exception", ex);
            }
        }

        private void LogAction(string caption, string message)
        {
            if (System.IO.File.Exists("Euromat.txt"))
            {
                System.IO.File.AppendAllText("Euromat.txt", "===============" + caption + "===============\r\n");
                string msg = string.Format("{0:dd/MM/yyyy HH:mm:ss.fff}\t{1}\r\n\r\n", DateTime.Now, message);
                System.IO.File.AppendAllText("Euromat.txt", msg);
            }
        }

        private void LogAction(string caption, Exception ex)
        {
            if (System.IO.File.Exists("Euromat.txt"))
            {
                System.IO.File.AppendAllText("Euromat.txt", "===============" + caption + "===============\r\n");
                string msg = string.Format("\r\n{0:dd/MM/yyyy HH:mm:ss.fff}\t{1}\r\n", DateTime.Now, ex.Message + "\r\n\r\n" + ex.StackTrace);
                System.IO.File.AppendAllText("Euromat.txt", msg);
            }
        }

        private string lastMessage = "";

        private void Client_DataReceived(object sender, SimpleTCP.Message e)
        {
            if (e != null && e.MessageString != null && e.MessageString.Length > 0)
            {
                if (e.MessageString == lastMessage)
                    return;
                lastMessage = e.MessageString;
                LogAction("Client_DataReceived", e.MessageString);

                if (this.TransactionState == TransactionStateEnum.NzOpenAck && e.MessageString.Contains("AK"))
                {
                    this.TransactionState = TransactionStateEnum.NzOpenAckRecieved;
                    LogAction(TransactionStateEnum.NzOpenAckRecieved.ToString(), e.MessageString);
                }
                else if (this.TransactionState == TransactionStateEnum.NzCloseAck && e.MessageString.Contains("AK"))
                {
                    this.TransactionState = TransactionStateEnum.NzCloseAckRecieved;
                    LogAction(TransactionStateEnum.NzCloseAckRecieved.ToString(), e.MessageString);
                }
                else if (this.TransactionState == TransactionStateEnum.FtSent && e.MessageString.Contains("AK"))
                {
                    this.TransactionState = TransactionStateEnum.FtAckRecieved;
                    LogAction(TransactionStateEnum.FtSent.ToString(), e.MessageString);
                }
                else if ((this.TransactionState == TransactionStateEnum.NzOpenAck || this.TransactionState == TransactionStateEnum.NzOpenAckRecieved || this.TransactionState == TransactionStateEnum.NzCloseAck) && e.MessageString.Contains("CT"))
                {
                    this.ParseCT(e.MessageString);
                    LogAction(TransactionStateEnum.NzOpenAckRecieved.ToString(), e.MessageString);
                }
                else if (this.TransactionState == TransactionStateEnum.NzOpenAck || this.TransactionState == TransactionStateEnum.NzOpenAckRecieved && e.MessageString.Contains("DT"))
                {
                    this.ParseDT(e.MessageString);
                    LogAction(TransactionStateEnum.NzOpenAckRecieved.ToString(), e.MessageString);
                }
                else if ((this.TransactionState == TransactionStateEnum.FtAckRecieved|| this.TransactionState == TransactionStateEnum.FtSent || this.TransactionState == TransactionStateEnum.TransDataRecieved) &&  
                    (e.MessageString.Contains(this.TransactionId) || e.MessageString.Contains("**")))
                {
                    this.ParseTransactionData(e.MessageString);
                    LogAction(this.TransactionState.ToString(), e.MessageString);
                }
                else if (this.TransactionState == TransactionStateEnum.NrSent && e.MessageString.Contains("AK"))
                {
                    this.TransactionState = TransactionStateEnum.NrAckRecieved;
                    LogAction(this.TransactionState.ToString(), e.MessageString);
                }
                else
                    LogAction("Not in valid state", this.TransactionState.ToString() + " :: " + e.MessageString);
            }
        }
    }

    public class EuromatNRParameters
    {
        public string Odometer { set; get; }
        public string PlateNumber { set; get; }
        public string FuelTypeCode { set; get; }
        public string FuelTypeDescription { set; get; }
        public DateTime EuromatDate { set; get; }
        public string TransactionId { set; get; }
    }

    public enum TransactionStateEnum
    {
        Pending,
        NzOpenAck,
        NzOpenAckRecieved,
        NzCloseAck,
        NzCloseAckRecieved,
        CtRecieved,
        DtRecieved,
        FtSent,
        FtAck,
        FtAckRecieved,
        NrSent, 
        NrAckRecieved,
        TransDataRecieved
    }

    public enum RateTypeEnum
    {
        Cash = 1,
        Credit = 2,
        Both = 3
    }

    public enum TranTypeEnum
    {
        Euromat = 1,
        Attendant = 3
    }
}

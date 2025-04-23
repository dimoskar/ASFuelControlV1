using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace ASFuelControl.Common
{
    public class EuromatClient
    {
        public event EventHandler<EuromatAuthorizeEventArgs> EuromatAuthorize;

        List<string> openNozzles = new List<string>();
        Dictionary<string, RateTypeEnum> openRates = new Dictionary<string, RateTypeEnum>();

        List<EuromatTransaction> opentransactions = new List<EuromatTransaction>();

        private bool hasOpenThread { set; get; }

        public string ServerIp { set; get; }
        public int ServrerPort { set; get; }


        public void PumpNotification(int pumpNumber, int nozzlIndex)
        {
            EuromatTransaction trans = new EuromatTransaction();
            trans.PumpNumber = pumpNumber;
            trans.NozzleIndex = nozzlIndex;
            trans.ServerIp = this.ServerIp;
            trans.ServrerPort = this.ServrerPort;
            trans.EuromatAuthorize += new EventHandler(trans_EuromatAuthorize);
            this.opentransactions.Add(trans);
            trans.NozzleOpened();
            
        }

        void trans_EuromatAuthorize(object sender, EventArgs e)
        {
            EuromatTransaction trans = sender as EuromatTransaction;
            this.EuromatAuthorize(this, new EuromatAuthorizeEventArgs() { Transaction = trans });
        }

        public void NozzleClosed(int pumpNumber)
        {
            EuromatTransaction trans =  this.opentransactions.Where(t => t.PumpNumber == pumpNumber).FirstOrDefault();
            if (trans == null)
                return;
            trans.NozzleClosed();
        }

        public void TransactionCompleted(int pumpNumber, int nozzlIndex, decimal pricePerLiter, decimal totalAmount, decimal volume)
        {
            EuromatTransaction trans = this.opentransactions.Where(t => t.PumpNumber == pumpNumber).FirstOrDefault();
            if (trans == null)
                return;
            trans.TransactionClosed(pricePerLiter, totalAmount, volume);
        }

        //public void PumpNotification(int pumpNumber, int nozzlIndex)
        //{
        //    string str = "nz";
        //    str = str + string.Format("{0:00}", pumpNumber);
        //    if (nozzlIndex <= 0)
        //        str = str + "0";
        //    else
        //        str = str + (nozzlIndex).ToString();

        //    string ret = "";
        //    if (nozzlIndex > 0)
        //    {
        //        EuromatVariables vars = new EuromatVariables() { Data = str, WaitingForAnswer = true };
        //        vars.EuromatDispenserNumber = pumpNumber;
        //        System.Threading.Thread th = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(this.ThreadRun));
        //        th.Start(vars);
        //    }
        //}

        //public void TransactionCompleted(int pumpNumber, int nozzlIndex, decimal pricePerLiter, decimal totalAmount, decimal volume)
        //{
        //    if (nozzlIndex < 0)
        //        return;

        //    string strNz = "nz";
        //    strNz = strNz + string.Format("{0:00}", pumpNumber) + "0";

        //    EuromatVariables vars1 = new EuromatVariables() { Data = strNz, WaitingForAnswer = true };
        //    SendCommand(vars1, false);

        //    if (!this.openNozzles.Contains(strNz.Substring(2, 2)))
        //        return;
        //    string nzStr = strNz.Substring(2, 2);

        //    this.openNozzles.Remove(nzStr);
        //    RateTypeEnum rate = RateTypeEnum.Credit;
        //    if (this.openRates.ContainsKey(nzStr))
        //    {
        //        rate = this.openRates[nzStr];
        //        this.openRates.Remove(nzStr);
        //    }
        //    string str = "ft";
        //    str = str + string.Format("{0:00}", pumpNumber);
        //    str = str + "0" + nozzlIndex.ToString();
        //    //str = str + "2";
        //    str = str + ((int)rate).ToString();
        //    str = str + pricePerLiter.ToString("N3").Replace(",", ".") + "|" + totalAmount.ToString("N2").Replace(",", ".") + "|" + volume.ToString("N2").Replace(",", ".");

        //    EuromatVariables vars = new EuromatVariables() { Data = str, WaitingForAnswer = true };
        //    SendCommand(vars, true);
        //    //System.Threading.Thread th = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(this.ThreadRun));
        //    //th.Start(vars);
        //}

        //public void NozzleClosed(int pumpNumber)
        //{
        //    try
        //    {
        //        string str = "nz";
        //        str = str + string.Format("{0:00}", pumpNumber);
        //        str = str + "0";
        //        EuromatVariables var = new EuromatVariables();
        //        var.Data = str;
        //        var.WaitingForAnswer = true;
        //        this.SendCommand(var, false);
        //    }
        //    catch
        //    {
        //    }
        //}

        //private void SendCommand(EuromatVariables var, bool multiResponse)
        //{
        //    TcpClient server = new TcpClient();
        //    try
        //    {
        //        server.ReceiveTimeout = 15000;

        //        server.Connect(this.ServerIp, this.ServrerPort);
                
        //    }
        //    catch
        //    {
        //        var.Response = "ERROR";
        //    }
        //    server.Client.ReceiveTimeout = 1000;
        //    Byte[] buffer = System.Text.UTF8Encoding.ASCII.GetBytes(var.Data);
        //    NetworkStream nwStream = server.GetStream();
        //    nwStream.Write(buffer, 0, buffer.Length);
        //    System.IO.File.AppendAllText("Euromat.log", "SEND : " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + "    " + var.Data + "\r\n");
        //    nwStream.ReadTimeout = 15000;
        //    //server.Client.Send(buffer);

        //    if (!var.WaitingForAnswer)
        //    {
        //        nwStream.Close();
        //        var.Response = "";
        //        return;
        //    }
        //    try
        //    {
        //        byte[] bufferRecieve = new byte[server.Client.ReceiveBufferSize];

        //        //---read incoming stream---
        //        int bytesRead = nwStream.Read(bufferRecieve, 0, server.Client.ReceiveBufferSize);

        //        string ack =  Encoding.ASCII.GetString(bufferRecieve, 0, bytesRead);
        //        System.IO.File.AppendAllText("Euromat.log", "RECIEVE : " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + "    " + ack + "\r\n");
        //        if (multiResponse)
        //        {
        //            while (ack.StartsWith("AK"))
        //            {
        //                try
        //                {
        //                    bufferRecieve = new byte[server.Client.ReceiveBufferSize];
        //                    bytesRead = nwStream.Read(bufferRecieve, 0, server.Client.ReceiveBufferSize);
        //                    ack = Encoding.ASCII.GetString(bufferRecieve, 0, bytesRead);
        //                    System.IO.File.AppendAllText("Euromat.log", "RECIEVE : " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + "    " + ack + "\r\n");
        //                }
        //                catch
        //                {
        //                    System.IO.File.AppendAllText("Euromat.log", "TIMEOUT : " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + "\r\n");
        //                    break;
        //                }
        //            }
        //            //bufferRecieve = new byte[server.Client.ReceiveBufferSize];
        //            //bytesRead = nwStream.Read(bufferRecieve, 0, server.Client.ReceiveBufferSize);
        //        }
        //        //---convert the data received into a string---
        //        //string dataReceived = Encoding.ASCII.GetString(bufferRecieve, 0, bytesRead);
                
        //        //Console.WriteLine(dataReceived);
        //        var.Response = ack;
        //    }
        //    catch
        //    {
        //        var.Response = "ERROR";
        //    }
        //}

        //private void ThreadRun(object param)
        //{
        //    try
        //    {
        //        EuromatVariables var = param as EuromatVariables;
        //        this.SendCommand(var, true);
        //        if (var.Response != "" && var.Response != "ERROR")
        //        {
        //            if (var.Response.StartsWith("CT"))
        //            {
        //                string resp = var.Response.Substring(7);

        //                int nz = resp[4];
        //                var.RateType = (RateTypeEnum)int.Parse(var.Response[6].ToString());
        //                var.TransType = (TransTypeEnum)int.Parse(var.Response[5].ToString());
        //                List<EuromatNozzle> nozzles = new List<EuromatNozzle>();
        //                for (int i = 0; i < 8; i++)
        //                {
        //                    EuromatNozzle nozzle = new EuromatNozzle();
        //                    nozzle.RateType = var.RateType;
        //                    int foo = (int)Math.Pow(2, i);
        //                    if ((foo & nz) == nz)
        //                        nozzle.Enabled = false;
        //                    else
        //                        nozzle.Enabled = true;
        //                    nozzles.Add(nozzle);
        //                }
        //                var.Nozzles = nozzles.ToArray();

        //                string[] vals = resp.Split('|');
        //                var.TransactionId = vals[vals.Length - 1];
        //                if (var.RateType == RateTypeEnum.Both)
        //                {
        //                    for (int i = 0; i < vals.Length % 4; i++)
        //                    {
        //                        var.Nozzles[i].VolumeCash = decimal.Parse(vals[i * 4].Replace(".", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator));
        //                        var.Nozzles[i].AmountCash = decimal.Parse(vals[i * 4 + 1].Replace(".", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator));
        //                        var.Nozzles[i].VolumeCredit = decimal.Parse(vals[i * 4 + 2].Replace(".", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator));
        //                        var.Nozzles[i].AmountCredit = decimal.Parse(vals[i * 4 + 3].Replace(".", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator));
        //                    }
        //                }
        //                else
        //                {
        //                    for (int i = 0; i < vals.Length % 2; i++)
        //                    {
        //                        if (var.RateType == RateTypeEnum.Cash)
        //                        {
        //                            var.Nozzles[i].VolumeCash = decimal.Parse(vals[i * 2].Replace(".", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator));
        //                            var.Nozzles[i].AmountCash = decimal.Parse(vals[i * 2 + 1].Replace(".", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator));
        //                        }
        //                        else
        //                        {
        //                            var.Nozzles[i].VolumeCredit = decimal.Parse(vals[i * 2].Replace(".", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator));
        //                            var.Nozzles[i].AmountCredit = decimal.Parse(vals[i * 2 + 1].Replace(".", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator));
        //                        }
        //                    }
        //                }
        //                this.openNozzles.Add(var.Data.Substring(2, 2));
        //                openRates.Add(var.Data.Substring(2, 2), var.RateType);
        //                if (this.EuromatAuthorize != null)
        //                {
        //                    this.EuromatAuthorize(this, new EuromatAuthorizeEventArgs() { EuromatVars = var });
        //                }
        //            }

        //        }
        //    }
        //    catch (Exception ex)
        //    {
                
        //    }
        //    finally
        //    {
        //    }
        //}
    }

    public class EuromatTransaction
    {
        public event EventHandler EuromatAuthorize;

        public string ServerIp { set; get; }
        public int ServrerPort { set; get; }

        public int PumpNumber { set; get; }
        public int NozzleIndex { set; get; }
        public bool IsNozzleOpen { set; get; }
        public string TransactionId { set; get; }
        public RateTypeEnum RateType { set; get; }
        public TransTypeEnum TransType { set; get; }
        public decimal PricePerLiter { set; get; }
        public decimal TotalAmount { set; get; }
        public decimal Volume { set; get; }
        public decimal VolumeCash { set; get; }
        public decimal VolumeCredit { set; get; }
        public decimal AmountCash { set; get; }
        public decimal AmountCredit { set; get; }

        public void NozzleOpened()
        {
            
            if (this.NozzleIndex > 0)
            {
                System.Threading.Thread th = new System.Threading.Thread(new System.Threading.ThreadStart(this.ThreadOpenNozzleRun));
                th.Start();
            }
        }

        public void NozzleClosed()
        {
                System.Threading.Thread th = new System.Threading.Thread(new System.Threading.ThreadStart(this.ThreadCloseNozzleRun));
                th.Start();
        }

        public void TransactionClosed(decimal pricePerLiter, decimal totalAmount, decimal volume)
        {
            this.PricePerLiter = pricePerLiter;
            this.TotalAmount = totalAmount;
            this.Volume = volume;
            System.Threading.Thread th = new System.Threading.Thread(new System.Threading.ThreadStart(this.ThreadTransactionCloseRun));
            th.Start();
        }

        private void SendOpenNozzleCommand()
        {
            if (this.IsNozzleOpen)
                return;
            TcpClient server = new TcpClient();
            try
            {
                server.ReceiveTimeout = 15000;

                server.Connect(this.ServerIp, this.ServrerPort);

            }
            catch
            {
                
            }

            string str = "nz";
            str = str + string.Format("{0:00}", this.PumpNumber);
            if (this.NozzleIndex <= 0)
                str = str + "0";
            else
                str = str + (this.NozzleIndex).ToString();

            server.Client.ReceiveTimeout = 1000;
            Byte[] buffer = System.Text.UTF8Encoding.ASCII.GetBytes(str);
            NetworkStream nwStream = server.GetStream();
            nwStream.Write(buffer, 0, buffer.Length);
            nwStream.ReadTimeout = 15000;
            //server.Client.Send(buffer);

            byte[] bufferRecieve = new byte[server.Client.ReceiveBufferSize];
            int bytesRead = nwStream.Read(bufferRecieve, 0, server.Client.ReceiveBufferSize);

            bufferRecieve = new byte[server.Client.ReceiveBufferSize];
            bytesRead = nwStream.Read(bufferRecieve, 0, server.Client.ReceiveBufferSize);
            string ack = Encoding.ASCII.GetString(bufferRecieve, 0, bytesRead);
            if (ack.StartsWith("AK" + string.Format("{0:00}", this.PumpNumber)))
                this.IsNozzleOpen = true;

            bufferRecieve = new byte[server.Client.ReceiveBufferSize];
            bytesRead = nwStream.Read(bufferRecieve, 0, server.Client.ReceiveBufferSize);
            ack = Encoding.ASCII.GetString(bufferRecieve, 0, bytesRead);

            if (ack.StartsWith("CT"))
            {
                string resp = ack.Substring(7);

                int nz = resp[4];
                this.RateType = (RateTypeEnum)int.Parse(ack[6].ToString());
                this.TransType = (TransTypeEnum)int.Parse(ack[5].ToString());
                List<EuromatNozzle> nozzles = new List<EuromatNozzle>();
                for (int i = 0; i < 8; i++)
                {
                    EuromatNozzle nozzle = new EuromatNozzle();
                    nozzle.RateType = this.RateType;
                    int foo = (int)Math.Pow(2, i);
                    if ((foo & nz) == nz)
                        nozzle.Enabled = false;
                    else
                        nozzle.Enabled = true;
                    nozzles.Add(nozzle);
                }

                string[] vals = resp.Split('|');
                this.TransactionId = vals[vals.Length - 1];
                if (this.RateType == RateTypeEnum.Both)
                {
                    for (int i = 0; i < vals.Length % 4; i++)
                    {
                        this.VolumeCash = decimal.Parse(vals[i * 4].Replace(".", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator));
                        this.AmountCash = decimal.Parse(vals[i * 4 + 1].Replace(".", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator));
                        this.VolumeCredit = decimal.Parse(vals[i * 4 + 2].Replace(".", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator));
                        this.AmountCredit = decimal.Parse(vals[i * 4 + 3].Replace(".", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator));
                    }
                }
                else
                {
                    for (int i = 0; i < vals.Length % 2; i++)
                    {
                        if (this.RateType == RateTypeEnum.Cash)
                        {
                            this.VolumeCash = decimal.Parse(vals[i * 2].Replace(".", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator));
                            this.AmountCash = decimal.Parse(vals[i * 2 + 1].Replace(".", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator));
                        }
                        else
                        {
                            this.VolumeCredit = decimal.Parse(vals[i * 2].Replace(".", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator));
                            this.AmountCredit = decimal.Parse(vals[i * 2 + 1].Replace(".", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator));
                        }
                    }
                }
                
                if (this.EuromatAuthorize != null)
                {
                    this.EuromatAuthorize(this, new EventArgs());
                }
            }
        }

        private void SendCloseNozzleCommand()
        {
            if (!this.IsNozzleOpen)
                return;
            TcpClient server = new TcpClient();
            try
            {
                server.ReceiveTimeout = 15000;

                server.Connect(this.ServerIp, this.ServrerPort);

            }
            catch
            {
                
            }

            string str = "nz";
            str = str + string.Format("{0:00}", this.PumpNumber);
            str = str + "0";

            server.Client.ReceiveTimeout = 1000;
            Byte[] buffer = System.Text.UTF8Encoding.ASCII.GetBytes(str);
            NetworkStream nwStream = server.GetStream();
            nwStream.Write(buffer, 0, buffer.Length);
            
            nwStream.ReadTimeout = 15000;
            //server.Client.Send(buffer);

            byte[] bufferRecieve = new byte[server.Client.ReceiveBufferSize];
            int bytesRead = nwStream.Read(bufferRecieve, 0, server.Client.ReceiveBufferSize);

            bufferRecieve = new byte[server.Client.ReceiveBufferSize];
            bytesRead = nwStream.Read(bufferRecieve, 0, server.Client.ReceiveBufferSize);
            string ack = Encoding.ASCII.GetString(bufferRecieve, 0, bytesRead);
            if (ack.StartsWith("AK" + string.Format("{0:00}", this.PumpNumber)))
                this.IsNozzleOpen = false;
        }

        private void SendTransactionEndCommand()
        {
            
            TcpClient server = new TcpClient();
            try
            {
                server.ReceiveTimeout = 15000;
                server.Connect(this.ServerIp, this.ServrerPort);

            }
            catch
            {
                return;
            }

            if (this.IsNozzleOpen)
                SendCloseNozzleCommand();

            string str = "ft";
            str = str + string.Format("{0:00}", this.PumpNumber);
            str = str + "0" + this.NozzleIndex.ToString();
            //str = str + "2";
            str = str + ((int)this.RateType).ToString();
            str = str + this.PricePerLiter.ToString("N3").Replace(",", ".") + "|" + this.TotalAmount.ToString("N2").Replace(",", ".") + "|" + this.Volume.ToString("N2").Replace(",", ".");

            server.Client.ReceiveTimeout = 1000;
            Byte[] buffer = System.Text.UTF8Encoding.ASCII.GetBytes(str);
            NetworkStream nwStream = server.GetStream();
            nwStream.Write(buffer, 0, buffer.Length);
            nwStream.ReadTimeout = 15000;
            //server.Client.Send(buffer);

            try
            {
                byte[] bufferRecieve = new byte[server.Client.ReceiveBufferSize];

                //---read incoming stream---
                int bytesRead = nwStream.Read(bufferRecieve, 0, server.Client.ReceiveBufferSize);

                string ack = Encoding.ASCII.GetString(bufferRecieve, 0, bytesRead);
                System.IO.File.AppendAllText("Euromat.log", "RECIEVE : " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + "    " + ack + "\r\n");
                while (ack.StartsWith("AK"))
                {
                    try
                    {
                        bufferRecieve = new byte[server.Client.ReceiveBufferSize];
                        bytesRead = nwStream.Read(bufferRecieve, 0, server.Client.ReceiveBufferSize);
                        ack = Encoding.ASCII.GetString(bufferRecieve, 0, bytesRead);
                        System.IO.File.AppendAllText("Euromat.log", "RECIEVE : " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + "    " + ack + "\r\n");
                    }
                    catch
                    {
                        System.IO.File.AppendAllText("Euromat.log", "TIMEOUT : " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + "\r\n");
                        break;
                    }
                }

                
            }
            catch
            {
                
            }

        }

        private void ThreadOpenNozzleRun()
        {
            this.SendOpenNozzleCommand();
        }

        private void ThreadCloseNozzleRun()
        {
            this.SendCloseNozzleCommand();
        }

        private void ThreadTransactionCloseRun()
        {
            this.SendTransactionEndCommand();
        }
    }

    public enum RateTypeEnum
    {
        Cash = 1,
        Credit = 2,
        Both = 3
    }

    public enum TransTypeEnum
    {
        Euromat = 1,
        Attendant = 3
    }

    public class EuromatVariables
    {
        public string Data { set;get;}
        public bool WaitingForAnswer { set; get; }
        public string Response { set; get; }
        public EuromatNozzle[] Nozzles { set; get; }
        public string TransactionId { set; get; }
        public RateTypeEnum RateType { set; get; }
        public TransTypeEnum TransType { set; get; }
        public int EuromatDispenserNumber { set; get; }
    }

    public class EuromatNozzle
    {
        public RateTypeEnum RateType { set; get; }
        public decimal AmountCash { set; get; }
        public decimal VolumeCash { set; get; }
        public decimal AmountCredit { set; get; }
        public decimal VolumeCredit { set; get; }
        public bool Enabled { set; get; }
    }

    public class EuromatAuthorizeEventArgs : EventArgs
    {
        public EuromatTransaction Transaction { set; get; }
    }
}



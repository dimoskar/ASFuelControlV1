using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net.Sockets;

namespace EuromatTest
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
            EuromatTransaction trans = this.opentransactions.Where(t => t.PumpNumber == pumpNumber).FirstOrDefault();
            if (trans == null)
                return;
            trans.NozzleClosed();
        }

        //public void TransactionCompleted(Common.FuelPoint fp, int pumpNumber, int nozzlIndex, decimal pricePerLiter, decimal totalAmount, decimal volume)
        //{
        //    EuromatTransaction trans = this.opentransactions.Where(t => t.PumpNumber == pumpNumber).FirstOrDefault();
        //    if (trans == null)
        //        return;
        //    trans.TransactionClosed(fp, pricePerLiter, totalAmount, volume);
        //    this.opentransactions.Remove(trans);
        //}

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
        public bool IsNozzleClosed { set; get; }
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
        public string Plates { set; get; }
        public int Odometer { set; get; }
        public string KeyId { set; get; }
        public string TransactionNumber { set; get; }
        public string FuelTypeDescription { set; get; }
        public string FuelTypeCode { set; get; }
        public string TransactionDate { set; get; }
        public string TransactionTime { set; get; }

        public void NozzleOpened()
        {

            if (this.NozzleIndex > 0)
            {
                SendOpenNozzleCommand();
                //System.Threading.Thread th = new System.Threading.Thread(new System.Threading.ThreadStart(this.ThreadOpenNozzleRun));
                //th.Start();
            }
        }

        public void NozzleClosed()
        {
            SendCloseNozzleCommand();
            //System.Threading.Thread th = new System.Threading.Thread(new System.Threading.ThreadStart(this.ThreadCloseNozzleRun));
            //th.Start();
        }

        //public void TransactionClosed(Common.FuelPoint fp, decimal pricePerLiter, decimal totalAmount, decimal volume)
        //{
        //    this.PricePerLiter = pricePerLiter;
        //    this.TotalAmount = totalAmount;
        //    this.Volume = volume;
        //    SendTransactionEndCommand(fp);
        //    //System.Threading.Thread th = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(this.ThreadTransactionCloseRun));
        //    //th.Start(fp);
        //}

        bool openConnection = false;
        private bool SendOpenNozzleCommand()
        {
            if (openConnection)
                return false;
            IsNozzleClosed = false;
            TcpClient server = new TcpClient();
            NetworkStream nwStream = null;
            try
            {

                //if (this.IsNozzleOpen)
                //    return true;

                try
                {
                    server.ReceiveTimeout = 15000;
                    server.Connect(this.ServerIp, this.ServrerPort);

                }
                catch (Exception ex)
                {
                    LogToFile("Open Connection Exception", ex);
                    return false;
                }

                string str = "nz";
                str = str + string.Format("{0:00}", this.PumpNumber);
                if (this.NozzleIndex <= 0)
                    str = str + "0" + "\r\n";
                else
                    str = str + (this.NozzleIndex).ToString() + "\r\n";

                System.IO.File.AppendAllText("EuromatData.log", "SEND " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + str + "\r\n");

                server.Client.ReceiveTimeout = 4000;
                Byte[] buffer = System.Text.UTF8Encoding.ASCII.GetBytes(str);
                nwStream = server.GetStream();
                nwStream.Write(buffer, 0, buffer.Length);
                nwStream.ReadTimeout = 15000;
                //server.Client.Send(buffer);

                byte[] bufferRecieve = new byte[server.Client.ReceiveBufferSize];
                int bytesRead = nwStream.Read(bufferRecieve, 0, server.Client.ReceiveBufferSize);

                System.Threading.Thread.Sleep(100);
                if (nwStream.DataAvailable)
                {
                    bufferRecieve = new byte[server.Client.ReceiveBufferSize];
                    bytesRead = nwStream.Read(bufferRecieve, 0, server.Client.ReceiveBufferSize);
                }
                string ack = Encoding.ASCII.GetString(bufferRecieve, 0, bytesRead);
                System.IO.File.AppendAllText("EuromatData.log", "GET " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + ack + "\r\n");
                if (ack.StartsWith("AK" + string.Format("{0:00}", this.PumpNumber)))
                    this.IsNozzleOpen = true;

                System.Threading.Thread.Sleep(500);
                int sleep = 0;
                while (!nwStream.DataAvailable && sleep < 15000)
                {
                    System.Threading.Thread.Sleep(100);
                    sleep = sleep + 100;
                }
                if (nwStream.DataAvailable)
                {
                    bufferRecieve = new byte[server.Client.ReceiveBufferSize];
                    bytesRead = nwStream.Read(bufferRecieve, 0, server.Client.ReceiveBufferSize);
                    ack = Encoding.ASCII.GetString(bufferRecieve, 0, bytesRead);
                }

                if (ack.StartsWith("CT"))
                {

                    System.IO.File.AppendAllText("EuromatData.log", "GET " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + ack + "\r\n");
                    string resp = ack.Substring(7);

                    int nz = ack[4];
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
            catch (Exception ex)
            {

                LogToFile("Main SendOpenNozzleCommand Exception", ex);
                return false;
            }
            finally
            {
                try
                {
                    nwStream.Close();
                    nwStream.Dispose();
                    server.Close();
                }
                catch
                {
                }
                openConnection = false;
            }
            return true;
        }

        public void ReadCT(string ack)
        {
            if (ack.StartsWith("CT"))
            {

                //System.IO.File.AppendAllText("EuromatData.log", "GET " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + ack + "\r\n");
                string resp = ack.Substring(7);

                int nz = ack[4];
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
            }
        }

        private bool SendCloseNozzleCommand()
        {
            //if (openConnection)
            //    return false;
            //if (!this.IsNozzleOpen)
            //    return true;
            if (this.IsNozzleClosed)
                return true;
            TcpClient server = new TcpClient();
            NetworkStream nwStream = null;
            try
            {
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
                str = str + "0" + "\r\n";
                System.IO.File.AppendAllText("EuromatData.log", "SET " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + str + "\r\n");
                server.Client.ReceiveTimeout = 4000;
                Byte[] buffer = System.Text.UTF8Encoding.ASCII.GetBytes(str);
                nwStream = server.GetStream();
                nwStream.Write(buffer, 0, buffer.Length);

                nwStream.ReadTimeout = 15000;
                //server.Client.Send(buffer);

                byte[] bufferRecieve = new byte[server.Client.ReceiveBufferSize];
                int bytesRead = nwStream.Read(bufferRecieve, 0, server.Client.ReceiveBufferSize);
                System.Threading.Thread.Sleep(100);
                if (nwStream.DataAvailable)
                {
                    bufferRecieve = new byte[server.Client.ReceiveBufferSize];
                    bytesRead = nwStream.Read(bufferRecieve, 0, server.Client.ReceiveBufferSize);
                }
                string ack = Encoding.ASCII.GetString(bufferRecieve, 0, bytesRead);
                System.IO.File.AppendAllText("EuromatData.log", "GET " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + ack + "\r\n");
                if (ack.StartsWith("AK" + string.Format("{0:00}", this.PumpNumber)))
                    this.IsNozzleOpen = false;
            }
            catch (Exception ex)
            {
                LogToFile("SendCloseNozzleCommand", ex);
                return false;
            }
            finally
            {
                try
                {
                    nwStream.Close();
                    nwStream.Dispose();
                    server.Close();
                }
                catch
                {
                }
                openConnection = false;
                IsNozzleClosed = true;
            }
            return true;
        }

        public void CreateCommand()
        {
            this.IsNozzleClosed = false;
            SendCloseNozzleCommand();
            this.IsNozzleClosed = true;
            string str = "ft";
            str = str + string.Format("{0:00}", this.PumpNumber);
            str = str + "0" + this.NozzleIndex.ToString();
            //str = str + "2";
            //int rateType = (int)this.RateType;
            //if(rateType == 0)

            str = str + ((int)this.RateType).ToString();
            str = str + this.PricePerLiter.ToString("N3").Replace(",", ".") + "|" + this.TotalAmount.ToString("N2").Replace(",", ".") + "|" + this.Volume.ToString("N2").Replace(",", ".") + "\r\n";
            
            
        }

        private bool SendTransactionEndCommand()
        {
            if (openConnection)
                return false;
            openConnection = true;

            TcpClient server = new TcpClient();
            NetworkStream nwStream = null;
            try
            {
                try
                {
                    server.ReceiveTimeout = 15000;
                    server.Connect(this.ServerIp, this.ServrerPort);

                }
                catch
                {
                    return false;
                }

                //if (this.IsNozzleOpen)
                this.IsNozzleClosed = false;
                SendCloseNozzleCommand();
                this.IsNozzleClosed = true;
                string str = "ft";
                str = str + string.Format("{0:00}", this.PumpNumber);
                str = str + "0" + this.NozzleIndex.ToString();
                //str = str + "2";
                //int rateType = (int)this.RateType;
                //if(rateType == 0)

                str = str + ((int)this.RateType).ToString();
                str = str + this.PricePerLiter.ToString("N3").Replace(",", ".") + "|" + this.TotalAmount.ToString("N2").Replace(",", ".") + "|" + this.Volume.ToString("N2").Replace(",", ".") + "\r\n";
                System.IO.File.AppendAllText("EuromatData.log", "SET " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + str + "\r\n");
                server.Client.ReceiveTimeout = 4000;
                Byte[] buffer = System.Text.UTF8Encoding.ASCII.GetBytes(str);
                nwStream = server.GetStream();
                nwStream.Write(buffer, 0, buffer.Length);
                nwStream.ReadTimeout = 15000;
                //server.Client.Send(buffer);

                try
                {
                    byte[] bufferRecieve = new byte[server.Client.ReceiveBufferSize];

                    //---read incoming stream---
                    int bytesRead = nwStream.Read(bufferRecieve, 0, server.Client.ReceiveBufferSize);

                    string ack = Encoding.ASCII.GetString(bufferRecieve, 0, bytesRead);
                    System.IO.File.AppendAllText("EuromatData.log", "GET 1" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + ack + "\r\n");
                    System.IO.File.AppendAllText("Euromat.log", "RECIEVE : " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + "    " + ack + "\r\n");
                    //fp.EuromatTransactionParameters = ack;
                    while (ack.Length < 50)
                    {
                        try
                        {
                            bufferRecieve = new byte[server.Client.ReceiveBufferSize];
                            bytesRead = nwStream.Read(bufferRecieve, 0, server.Client.ReceiveBufferSize);
                            ack = Encoding.ASCII.GetString(bufferRecieve, 0, bytesRead);
                            System.IO.File.AppendAllText("EuromatData.log", "GET 2" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + ack + "\r\n");
                            //fp.EuromatTransactionParameters = ack;
                            System.IO.File.AppendAllText("Euromat.log", "RECIEVE : " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + "    " + ack + "\r\n");

                        }
                        catch
                        {
                            System.IO.File.AppendAllText("Euromat.log", "TIMEOUT : " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + "\r\n");
                            break;
                        }
                    }

                    this.ReadTransactionParameters(ack);
                }
                catch (Exception ex)
                {
                    System.IO.File.AppendAllText("Euromat.log", "Exception : " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + ex.Message + "\r\n");
                }
            }
            catch (Exception ex1)
            {
                System.IO.File.AppendAllText("Euromat.log", "Outter Exception : " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + ex1.Message + "\r\n");
                return false;
            }
            finally
            {
                try
                {
                    nwStream.Close();
                    nwStream.Dispose();
                    server.Close();
                }
                catch
                {
                }
                openConnection = false;
            }
            return true;
        }

        private void ThreadOpenNozzleRun()
        {
            while (true)
            {
                bool done = this.SendOpenNozzleCommand();
                if (done)
                    break;
            }
        }

        private void ThreadCloseNozzleRun()
        {
            while (true)
            {
                bool done = this.SendCloseNozzleCommand();
                if (done)
                    break;
            }
        }

        private void ThreadTransactionCloseRun(object fp)
        {
            //Common.FuelPoint fuelPoint = (Common.FuelPoint)fp;
            //while (true)
            //{
            //    bool done = this.SendTransactionEndCommand(fuelPoint);
            //    if (done)
            //        break;
            //}
        }

        private void ReadTransactionParameters(string response)
        {
            if (response.Length < 160)
                return;
            string price = new string(response.Take(9).ToArray()).Replace(" ", "");
            string volume = new string(response.Skip(11).Take(9).ToArray()).Replace(" ", "");
            string plates = new string(response.Skip(23).Take(10).ToArray()).Replace(" ", "");
            string nz = new string(response.Skip(34).Take(1).ToArray()).Replace(" ", "");
            string dp = new string(response.Skip(38).Take(2).ToArray()).Replace(" ", "");
            string trnr = new string(response.Skip(45).Take(6).ToArray()).Replace(" ", "");
            string time = new string(response.Skip(52).Take(5).ToArray()).Replace(" ", "");
            string dt = new string(response.Skip(58).Take(8).ToArray()).Replace(" ", "");
            string ftd = new string(response.Skip(67).Take(10).ToArray()).Replace(" ", "");
            string ftt = new string(response.Skip(78).Take(4).ToArray()).Replace(" ", "");
            string id = new string(response.Skip(100).Take(19).ToArray()).Replace(" ", "");
            string odometer = new string(response.Skip(135).Take(8).ToArray()).Replace(" ", "");
            string up = new string(response.Skip(153).Take(7).ToArray()).Replace(" ", "");
            if (odometer == "")
                odometer = "0";

            this.TransactionTime = time;
            this.TransactionDate = dt;
            this.KeyId = id;
            this.FuelTypeDescription = ftd;
            this.FuelTypeCode = ftt;
            int odo = 0;
            int.TryParse(odometer, out odo);
            this.Odometer = odo;
            this.TransactionNumber = trnr;
            this.Plates = plates;
        }

        public void LogToFile(string caption, Exception ex)
        {
            try
            {
                FileInfo fi = new FileInfo("EuromatException.Log");
                FileStream f = null;
                if (!fi.Exists)
                {
                    f = fi.Create();
                    f.Close();
                }
                string caption2 = string.Format("{0} :  {1:dd/MM/yyyy HH/mm/ss.fff}", caption, DateTime.Now);
                File.AppendAllText("EuromatException.Log", "=====================" + caption2 + "=====================\r\n");
                File.AppendAllText("EuromatException.Log", ex.Message + "\r\n");
                Exception parent = ex;
                while (parent.InnerException != null)
                {
                    File.AppendAllText("EuromatException.Log", "--------------------" + "InnerExeption" + "--------------------\r\n");
                    File.AppendAllText("EuromatException.Log", ex.Message + "\r\n");
                    parent = ex.InnerException;
                }

                File.AppendAllText("EuromatException.Log", "--------------------" + "Trace" + "--------------------\r\n");
                File.AppendAllText("EuromatException.Log", ex.StackTrace + "\r\n");

                File.AppendAllText("EuromatException.Log", "\r\n");
            }
            catch
            {
                if (System.IO.File.Exists("EuromatException.Log"))
                {
                    System.Threading.Thread.Sleep(500);
                    this.LogToFile(caption, ex);
                }
            }
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
        public string Data { set; get; }
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace ASFuelControl.FafnirDirect
{
    public class Connector
    {
        public event EventHandler DataUpdated;

        private System.IO.Ports.SerialPort serialPort = new System.IO.Ports.SerialPort();
        private System.Threading.Thread th;
        private List<ATGProbe> probes = new List<ATGProbe>();
        private bool isConnected = false;

        public ATGProbe[] Probes
        {
            get { return this.probes.ToArray(); }
        }

        public string CommunicationPort
        {
            set;
            get;
        }

        public bool IsConnected
        {
            get
            {
                return this.isConnected;
            }
        }

        public ATGProbe AddProbe(int addressId)
        {
            if (this.probes.Where(p => p.Address == addressId).Count() > 0)
                return null;
            ATGProbe probe = new ATGProbe();
            probe.Address = addressId;
            this.probes.Add(probe);
            Console.WriteLine(addressId);
            return probe;

        }

        public void Connect()
        {
            this.serialPort.PortName = this.CommunicationPort;
            //this.serialPort.DataReceived -= new System.IO.Ports.SerialDataReceivedEventHandler(serialPort_DataReceived);
            //this.serialPort.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(serialPort_DataReceived);
            this.serialPort.Open();
            this.serialPort.BaudRate = 1200;
            this.isConnected = true;
            th = new System.Threading.Thread(new System.Threading.ThreadStart(this.ThreadRun));
            th.Start();
        }

        public void DisConnect()
        {
            this.isConnected = false;
            this.serialPort.Close();
        }

        private bool errorOccured = false;

        private void ThreadRun()
        {
            while (this.IsConnected)
            {
                try
                {
                    if (this.errorOccured)
                    {
                        this.serialPort.Close();
                        this.serialPort.Open();
                        this.errorOccured = false;
                        Console.WriteLine("Fafnir CONTROLLER RESTARTED");
                    }
                    foreach (ATGProbe probe in this.probes)
                    {
                        string str = probe.Address.ToString();
                        byte[] bytes = null;
                        byte[] numArray = null;
                        if (str.Length == 5)
                        {
                            bytes = Encoding.ASCII.GetBytes(str);
                            byte[] numArray1 = new byte[] { 83, 0, 0, 0, 0, 0, 13, 10 };
                            numArray1[1] = bytes[0];
                            numArray1[2] = bytes[1];
                            numArray1[3] = bytes[2];
                            numArray1[4] = bytes[3];
                            numArray1[5] = bytes[4];
                            numArray = numArray1;
                        }
                        else if (str.Length == 6)
                        {
                            bytes = Encoding.ASCII.GetBytes(str);
                            byte[] numArray2 = new byte[] { 83, 0, 0, 0, 0, 0, 0, 13, 10 };
                            numArray2[1] = bytes[0];
                            numArray2[2] = bytes[1];
                            numArray2[3] = bytes[2];
                            numArray2[4] = bytes[3];
                            numArray2[5] = bytes[4];
                            numArray2[6] = bytes[5];
                            numArray = numArray2;
                        }
                        this.serialPort.Write(numArray, 0, (int)numArray.Length);
                        Thread.Sleep(400);
                        string str1 = "";
                        DateTime now = DateTime.Now;
                        while (true)
                        {
                            if (this.serialPort.BytesToRead > 0)
                            {
                                str1 = string.Concat(str1, this.serialPort.ReadExisting());
                                if (str1.Contains("\n\r"))
                                {
                                    break;
                                }
                            }
                            if (DateTime.Now.Subtract(now).TotalMilliseconds > 100)
                            {
                                break;
                            }
                        }
                        if (str1.Length > 0)
                        {
                            this.EvaluateResponse(str1);
                        }
                    }
                    Thread.Sleep(150);
                }
                catch (Exception exception)
                {
                    this.errorOccured = true;
                    Thread.Sleep(100);
                }
            }
            //while (this.IsConnected)
            //{
            //    try
            //    {
            //        if (errorOccured)
            //        {
            //            this.serialPort.Close();
            //            this.serialPort.Open();
            //            errorOccured = false;

            //            Console.WriteLine("Fafnir CONTROLLER RESTARTED");
            //        }

            //        foreach (ATGProbe probe in this.probes)
            //        {
            //            string idString = probe.Address.ToString();
            //            for (int k = 0; k < 5 - idString.Length; k++)
            //                idString += "0";
            //            byte[] idByte = Encoding.ASCII.GetBytes(idString);
            //            //S=083 055 054 050 054 053 CR=013 LF=010 
            //            byte[] status = new byte[] { 0x53, idByte[0], idByte[1], idByte[2], idByte[3], idByte[4], 0x0D, 0x0A };

            //            this.serialPort.Write(status, 0, status.Length);
            //            int waiting = 0;
            //            while (this.serialPort.BytesToRead < 37 && waiting < 300)
            //            {
            //                System.Threading.Thread.Sleep(50);
            //                waiting += 25;
            //            }

            //            string response = "";
            //            DateTime dtNow = DateTime.Now;
            //            while (true)
            //            {
            //                if (this.serialPort.BytesToRead > 0)
            //                {

            //                    response = response + serialPort.ReadExisting();
            //                    if (response.Contains("\n\r"))
            //                        break;
            //                }
            //                if (DateTime.Now.Subtract(dtNow).TotalMilliseconds > 100)
            //                    break;
            //            }
            //            if (response.Length > 0)
            //                EvaluateResponse(response);
            //        }


            //        System.Threading.Thread.Sleep(150);
            //    }
            //    catch (Exception ex)
            //    {
            //        errorOccured = true;
            //        System.Threading.Thread.Sleep(100);
            //    }
            //}
        }



        string buffer = "";

        Dictionary<ATGProbe, List<ProbeValues>> history = new Dictionary<ATGProbe, List<ProbeValues>>();

        private void EvaluateResponse(string response)
        {
            try
            {
                string str = response.Replace(" ", "");
                str = response.Replace("\n\r", "");
                response = response.Replace(":", "=");
                string[] strArrays = response.Split(new char[] { '=' });
                int num = Convert.ToInt32(strArrays[0].Replace(" ", ""));
                int num1 = Convert.ToInt32(strArrays[1].Replace(" ", ""));
                decimal num2 = Convert.ToInt32(strArrays[2].Replace(" ", ""));
                decimal num3 = Convert.ToDecimal(strArrays[3].Replace(" ", ""));
                decimal num4 = Convert.ToDecimal(strArrays[4].Replace(" ", ""));
                if (File.Exists("Fafnir.log"))
                {
                    string str1 = string.Concat(response, "\r\n");
                    string str2 = string.Format("Address : {0}, Status : {1}, Temp : {2}, FuelLevel : {3}, WaterLevel : {4}\r\n", new object[] { num, num1, num2, num3, num4 });
                    File.AppendAllText("Fafnir.log", str1);
                    File.AppendAllText("Fafnir.log", str2);
                    File.AppendAllText("Fafnir.log", "--------------------------------------------------------------------------------------");
                }
                ATGProbe aTGProbe = (
                    from p in this.probes
                    where p.Address == num
                    select p).FirstOrDefault<ATGProbe>();
                aTGProbe.FuelLevel = num3 / new decimal(1000);
                aTGProbe.WaterLevel = num4 / new decimal(1000);
                aTGProbe.Temperature = num2 / new decimal(1000);
                if (this.DataUpdated != null)
                {
                    this.DataUpdated(aTGProbe, new EventArgs());
                }
            }
            catch (Exception exception1)
            {
                Exception exception = exception1;
                if (File.Exists("Fafnir.log"))
                {
                    File.AppendAllText("Fafnir.log", exception.Message);
                }
            }
            //try
            //{
            //    string resp = response.Replace(@" ", "");
            //    resp = response.Replace("\n\r", "");
            //    response = response.Replace(":", "=");
            //    string[] parms = response.Split('=');

            //    int address = Convert.ToInt32(parms[0].Replace(" ", ""));
            //    int status = Convert.ToInt32(parms[1].Replace(" ", ""));
            //    decimal temp = Convert.ToInt32(parms[2].Replace(" ", ""));
            //    decimal flevel = Convert.ToDecimal(parms[3].Replace(" ", ""));
            //    decimal wlevel = Convert.ToDecimal(parms[4].Replace(" ", ""));

            //    if (System.IO.File.Exists("Fafnir.log"))
            //    {
            //        string input = response + "\r\n";
            //        string output = string.Format("Address : {0}, Status : {1}, Temp : {2}, FuelLevel : {3}, WaterLevel : {4}\r\n", address, status, temp, flevel, wlevel);

            //        System.IO.File.AppendAllText("Fafnir.log", input);
            //        System.IO.File.AppendAllText("Fafnir.log", output);
            //        System.IO.File.AppendAllText("Fafnir.log", "--------------------------------------------------------------------------------------");
            //    }

            //    ATGProbe probe = this.probes.Where(p => p.Address == address).FirstOrDefault();


            //    probe.FuelLevel = flevel / 1000;// history[probe].Average(p => p.FuelLevel);//Math.Round(flevel / 1000, 1, MidpointRounding.AwayFromZero);
            //    probe.WaterLevel = wlevel / 1000;// history[probe].Average(p => p.WaterLevel); //Math.Round(wlevel / 1000, 1, MidpointRounding.AwayFromZero);
            //    probe.Temperature = temp / 1000;// history[probe].Average(p => p.Temperature); //Math.Round(temp / 1000, 1, MidpointRounding.AwayFromZero);
            //    if (this.DataUpdated != null)
            //        this.DataUpdated(probe, new EventArgs());

            //}
            //catch (Exception ex)
            //{
            //    if (System.IO.File.Exists("Fafnir.log"))
            //    {
            //        System.IO.File.AppendAllText("Fafnir.log", ex.Message);
            //    }
            //}
        }

        void serialPort_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            if (this.IsConnected)
            {
                if (this.serialPort.BytesToRead != 0)
                {
                    this.buffer = string.Concat(this.buffer, this.serialPort.ReadExisting());
                    string[] strArrays = this.buffer.Split(new string[] { "\n\r" }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < (int)strArrays.Length; i++)
                    {
                        string str = strArrays[i];
                        string str1 = str.Replace(" ", "");
                        str1 = str.Replace("\n\r", "");
                        string[] strArrays1 = str1.Split(new char[] { '=' });
                        decimal num = new decimal();
                        decimal num1 = new decimal();
                        decimal num2 = new decimal();
                        int num3 = 0;
                        int num4 = 0;
                        if (strArrays1[0].Length == 5)
                        {
                            num4 = Convert.ToInt32(str.Substring(0, 5));
                            num2 = Convert.ToDecimal(str.Substring(9, 5));
                            num3 = Convert.ToInt32(str.Substring(6, 1));
                            if (str.Substring(5, 1) == ":")
                            {
                                num = Convert.ToDecimal(strArrays1[1]);
                                num1 = Convert.ToDecimal(strArrays1[2]);
                            }
                            else
                            {
                                num = Convert.ToDecimal(strArrays1[2]);
                                num1 = Convert.ToDecimal(strArrays1[3]);
                                string.Concat("\taddres:", num4.ToString(), "\t \n\\n\n\nStatus:");
                            }
                        }
                        else if (strArrays1[0].Length == 6)
                        {
                            num4 = Convert.ToInt32(str.Substring(0, 6));
                            num2 = Convert.ToDecimal(str.Substring(9, 5));
                            num3 = Convert.ToInt32(str.Substring(6, 1));
                            if (str.Substring(5, 1) == ":")
                            {
                                num = Convert.ToDecimal(strArrays1[1]);
                                num1 = Convert.ToDecimal(strArrays1[2]);
                            }
                            else
                            {
                                num = Convert.ToDecimal(strArrays1[2]);
                                num1 = Convert.ToDecimal(strArrays1[3]);
                                string.Concat("\taddres:", num4.ToString(), "\t \n\\n\n\nStatus:");
                            }
                        }
                        if (File.Exists("C:\\Fafnir.log"))
                        {
                            File.AppendAllText("C:\\Fafnir.log", string.Concat("\t", num.ToString(), "\r\n"));
                        }
                        ATGProbe aTGProbe = (
                            from p in this.probes
                            where p.Address == num4
                            select p).FirstOrDefault<ATGProbe>();
                        if (aTGProbe != null)
                        {
                            if (num3 == 0)
                            {
                                aTGProbe.FuelLevel = Math.Round(num / new decimal(1000), 1, MidpointRounding.AwayFromZero);
                                aTGProbe.WaterLevel = Math.Round(num1 / new decimal(1000), 1, MidpointRounding.AwayFromZero);
                                aTGProbe.Temperature = Math.Round(num2 / new decimal(1000), 1, MidpointRounding.AwayFromZero);
                                if (this.DataUpdated != null)
                                {
                                    this.DataUpdated(aTGProbe, new EventArgs());
                                }
                            }
                        }
                    }
                    int num5 = this.buffer.LastIndexOf("\n\r");
                    this.buffer = this.buffer.Substring(num5 + 1);
                }
            }

            //if (!this.IsConnected)
            //    return;
            //if (serialPort.BytesToRead == 0)
            //    return;
            //buffer = buffer + serialPort.ReadExisting();
            ////Console.WriteLine(buffer);
            //string[] responses = buffer.Split(new string[] { "\n\r" }, StringSplitOptions.RemoveEmptyEntries);
            //foreach (string response in responses)
            //{
            //    string resp = response.Replace(@" ", "");
            //    resp = response.Replace("\n\r", "");
            //    string[] parms = resp.Split('=');
            //    //if (parms.Length != 5)
            //    //return;

            //    decimal flevel = 0;
            //    decimal wlevel = 0;

            //    int address = Convert.ToInt32(response.Substring(0, 5));
            //    decimal temp = Convert.ToDecimal(response.Substring(9, 5));
            //    int status = Convert.ToInt32(response.Substring(6, 1));
            //    string plusOrCol = response.Substring(5, 1);
            //    if (plusOrCol != ":")
            //    {

            //        flevel = Convert.ToDecimal(parms[2]);
            //        wlevel = Convert.ToDecimal(parms[3]);
            //        string log = "\taddres:" + address.ToString() + "\t \n\\n\n\nStatus:";
            //    }
            //    else
            //    {
            //        flevel = Convert.ToDecimal(parms[1]);
            //        wlevel = Convert.ToDecimal(parms[2]);
            //    }
            //    if(File.Exists(@"C:\Fafnir.log"))
            //        File.AppendAllText(@"C:\Fafnir.log", "\t" + flevel.ToString() + "\r\n");

            //    ATGProbe probe = this.probes.Where(p => p.Address == address).FirstOrDefault();
            //    if (probe == null)
            //        continue;
            //    if (status == 0)
            //    {
            //        probe.FuelLevel = Math.Round(flevel / 1000, 1, MidpointRounding.AwayFromZero);
            //        probe.WaterLevel = Math.Round(wlevel / 1000, 1, MidpointRounding.AwayFromZero);
            //        probe.Temperature = Math.Round(temp / 1000, 1, MidpointRounding.AwayFromZero);
            //        if (this.DataUpdated != null)
            //            this.DataUpdated(probe, new EventArgs());
            //    }

            //}
            //int index = buffer.LastIndexOf("\n\r");
            //buffer = buffer.Substring(index + 1);
        }
    }

    public class ProbeValues
    {
        public decimal FuelLevel { set; get; }
        public decimal WaterLevel { set; get; }
        public decimal Temperature { set; get; }
    }

}

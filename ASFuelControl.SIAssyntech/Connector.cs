using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ASFuelControl.SIAssytech
{
    public class Connector
    {
        private SerialPort serialPort = new SerialPort();

        private Thread th;

        private List<ATGProbe> probes = new List<ATGProbe>();

        private bool isConnected = false;

        private bool errorOccured = false;

        private string buffer = "";

        public string CommunicationPort
        {
            get;
            set;
        }

        public bool IsConnected
        {
            get
            {
                return this.isConnected;
            }
        }

        public ATGProbe[] Probes
        {
            get
            {
                return this.probes.ToArray();
            }
        }

        public Connector()
        {
        }

        public ATGProbe AddProbe(int addressId)
        {
            ATGProbe aTGProbe;
            if ((
                from p in this.probes
                where p.Address == addressId
                select p).Count<ATGProbe>() <= 0)
            {
                ATGProbe aTGProbe1 = new ATGProbe()
                {
                    Address = addressId
                };
                this.probes.Add(aTGProbe1);
                aTGProbe = aTGProbe1;
            }
            else
            {
                aTGProbe = null;
            }
            return aTGProbe;
        }

        public void Connect()
        {
            this.serialPort.PortName = this.CommunicationPort;
            this.serialPort.DtrEnable = true;
            this.serialPort.Open();
            this.isConnected = true;
            this.th = new Thread(new ThreadStart(this.ThreadRun));
            this.th.Start();
        }

        public void DisConnect()
        {
            this.isConnected = false;
            this.serialPort.Close();
        }

        private void EvaluateResponse(string response)
        {
            string str = response.Replace("\n\r", "");
            string[] strArrays = str.Split(new char[] { '=' });
            if ((int)strArrays.Length == 6)
            {
                int num = int.Parse(strArrays[0]);
                int num1 = int.Parse(strArrays[1]);
                decimal num2 = int.Parse(strArrays[2]);
                decimal num3 = int.Parse(strArrays[3]);
                decimal num4 = int.Parse(strArrays[4]);
                ATGProbe aTGProbe = (
                    from p in this.probes
                    where p.Address == num
                    select p).FirstOrDefault<ATGProbe>();
                if (aTGProbe != null)
                {
                    if (num1 == 0)
                    {
                        aTGProbe.FuelLevel = num3 / new decimal(10);
                        aTGProbe.WaterLevel = num4 / new decimal(10);
                        aTGProbe.Temperature = num2 / new decimal(10);
                        if (this.DataUpdated != null)
                        {
                            this.DataUpdated(aTGProbe, new EventArgs());
                        }
                    }
                    else if (num1 == 1)
                    {
                        aTGProbe.FuelLevel = num3 / new decimal(10);
                        aTGProbe.WaterLevel = num4 / new decimal(10);
                        aTGProbe.Temperature = num2 / new decimal(10);
                        if (aTGProbe.FuelLevel != decimal.Zero)
                        {
                            if (this.DataUpdated != null)
                            {
                                this.DataUpdated(aTGProbe, new EventArgs());
                            }
                        }
                    }
                }
            }
        }

        private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (this.IsConnected)
            {
                if (this.serialPort.BytesToRead != 0)
                {
                    this.buffer = string.Concat(this.buffer, this.serialPort.ReadExisting());
                    string[] strArrays = this.buffer.Split(new string[] { "\n\r" }, StringSplitOptions.RemoveEmptyEntries);
                    int num = 0;
                    while (num < (int)strArrays.Length)
                    {
                        string str = strArrays[num].Replace("\n\r", "");
                        string[] strArrays1 = str.Split(new char[] { '=' });
                        if ((int)strArrays1.Length == 6)
                        {
                            int num1 = int.Parse(strArrays1[0]);
                            int num2 = int.Parse(strArrays1[1]);
                            decimal num3 = int.Parse(strArrays1[2]);
                            decimal num4 = int.Parse(strArrays1[3]);
                            decimal num5 = int.Parse(strArrays1[4]);
                            ATGProbe aTGProbe = (
                                from p in this.probes
                                where p.Address == num1
                                select p).FirstOrDefault<ATGProbe>();
                            if (aTGProbe != null)
                            {
                                if (num2 == 0)
                                {
                                    aTGProbe.FuelLevel = num4 / new decimal(10);
                                    aTGProbe.WaterLevel = num5 / new decimal(10);
                                    aTGProbe.Temperature = num3 / new decimal(10);
                                    if (this.DataUpdated != null)
                                    {
                                        this.DataUpdated(aTGProbe, new EventArgs());
                                    }
                                }
                                else if (num2 == 1)
                                {
                                    aTGProbe.FuelLevel = new decimal(10099, 0, 0, false, 2);
                                    aTGProbe.WaterLevel = new decimal(1099, 0, 0, false, 2);
                                    aTGProbe.Temperature = num3 / new decimal(10);
                                    if (this.DataUpdated != null)
                                    {
                                        this.DataUpdated(aTGProbe, new EventArgs());
                                    }
                                }
                            }
                            num++;
                        }
                        else
                        {
                            return;
                        }
                    }
                    int num6 = this.buffer.LastIndexOf("\n\r");
                    this.buffer = this.buffer.Substring(num6 + 1);
                }
            }
        }

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
                    }
                    int num = 0;
                    foreach (ATGProbe probe in this.probes)
                    {
                        SerialPort serialPort = this.serialPort;
                        int address = probe.Address;
                        serialPort.Write(string.Concat("M", address.ToString("0000"), "\n\r"));
                        Thread.Sleep(100);
                        num += 100;
                        string str = "";
                        DateTime now = DateTime.Now;
                        while (true)
                        {
                            if (this.serialPort.BytesToRead > 0)
                            {
                                str = string.Concat(str, this.serialPort.ReadExisting());
                                if (str.Contains("\n\r"))
                                {
                                    break;
                                }
                            }
                            if (DateTime.Now.Subtract(now).TotalMilliseconds > 100)
                            {
                                break;
                            }
                        }
                        if (str.Length > 0)
                        {
                            this.EvaluateResponse(str);
                        }
                    }
                    Thread.Sleep(1500 - num);
                }
                catch
                {
                    this.errorOccured = true;
                    Thread.Sleep(500);
                }
            }
        }

        public event EventHandler DataUpdated;
    }
}
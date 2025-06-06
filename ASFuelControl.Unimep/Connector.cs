using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;

namespace ASFuelControl.Unimep
{
    public class Connector
    {
        private class FuelHeightMesaurement
        {
            public int TankAddress { get; set; }

            public DateTime dtResponse { get; set; }

            public decimal FuelLevel { get; set; }

            public decimal Temperature { get; set; }
        }

        private SerialPort serialPort = new SerialPort();

        private Thread th;

        private List<ATGProbe> probes = new List<ATGProbe>();

        private bool isConnected;

        private bool errorOccured;

        private string buffer = "";

        private List<FuelHeightMesaurement> _fHeight = new List<FuelHeightMesaurement>();

        public string CommunicationPort { get; set; }

        public bool IsConnected => isConnected;

        public ATGProbe[] Probes => probes.ToArray();

        public event EventHandler DataUpdated;

        public ATGProbe AddProbe(int addressId)
        {
            if (probes.Where((ATGProbe p) => p.Address == addressId).Count() > 0)
            {
                return null;
            }
            ATGProbe aTGProbe = new ATGProbe
            {
                Address = addressId
            };
            probes.Add(aTGProbe);
            return aTGProbe;
        }

        public void Connect()
        {
            try
            {
                serialPort.PortName = CommunicationPort;
                serialPort.BaudRate = 9600;
                serialPort.Parity = Parity.Even;
                serialPort.Open();
                isConnected = true;
                th = new Thread(ThreadRun);
                th.Start();
            }
            catch
            {
            }
        }

        public void DisConnect()
        {
            isConnected = false;
            serialPort.Close();
        }

        private void EvaluateResponse(int addressProbe, string response)
        {
            ATGProbe aTGProbe = probes.Where((ATGProbe p) => p.Address == addressProbe).FirstOrDefault();
            if (response.Length >= 27 && aTGProbe != null)
            {
                string text = response.Substring(3, 7).Trim();
                string text2 = response.Substring(11, 7).Trim();
                string text3 = response.Substring(20, 5).Trim();
                aTGProbe.FuelLevel = decimal.Parse(text.Replace(".", ","));
                aTGProbe.WaterLevel = decimal.Parse(text2.Replace(".", ","));
                aTGProbe.Temperature = decimal.Parse(text3.Replace(".", ","));
                if (this.DataUpdated != null)
                {
                    this.DataUpdated(aTGProbe, new EventArgs());
                }
            }
        }

        private void ThreadRun()
        {
            while (IsConnected)
            {
                try
                {
                    if (errorOccured)
                    {
                        this.serialPort.Close();
                        this.serialPort.Open();
                        errorOccured = false;
                    }
                    foreach (ATGProbe probe in probes)
                    {
                        SerialPort serialPort = this.serialPort;
                        int address = probe.Address;
                        string text = "";
                        for (int i = 0; i < 2; i++)
                        {
                            text = "";
                            byte[] array = new byte[2]
                            {
                                (byte)address,
                                44
                            };
                            serialPort.Write(array, 0, array.Length);
                            Thread.Sleep(400);
                            
                            text += this.serialPort.ReadExisting();
                            string fName = "Unimep_" + address.ToString() + ".txt";
                            if (System.IO.File.Exists(fName))
                            {
                                Encoding wind1252 = Encoding.GetEncoding(1252);
                                //Encoding utf8 = Encoding.UTF8;
                                //byte[] wind1252Bytes = wind1252.GetBytes(text);
                                //byte[] utf8Bytes = Encoding.Convert(wind1252, utf8, wind1252Bytes);
                                //string utf8String = Encoding.ASCII.GetString(utf8Bytes);

                                //string encoding = this.serialPort.Encoding.CodePage.ToString() + " - " + this.serialPort.Encoding.WindowsCodePage.ToString();
                                System.IO.File.AppendAllText(fName, text + "\r\n", wind1252);
                            }
                        }
                        if (text.Length >= 27)
                        {
                            Trace.WriteLine(text);
                            EvaluateResponse(address, text);
                        }
                    }
                    Thread.Sleep(1000);
                }
                catch (Exception)
                {
                    errorOccured = true;
                    Thread.Sleep(500);
                }
            }
        }
    }
   
}

using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ASFuelControl.VeederRootTLS
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
        public string Mantissa(string value)
        {

            string hexString = value;
            uint num = uint.Parse(hexString, System.Globalization.NumberStyles.AllowHexSpecifier);

            byte[] floatVals = BitConverter.GetBytes(num);
            float f = BitConverter.ToSingle(floatVals, 0);
            return (f * 100).ToString();
        }
        private void EvaluateResponse(string response)
        {
            try
            {
                byte[] ba = Encoding.Default.GetBytes(response);
                string buffer = BitConverter.ToString(ba);
                string[] VR = buffer.Split('-');
                int length = response.Length;

                int ATG_id= Convert.ToInt32(Commands.ConvertHex(VR[6]));
                string Volume = Commands.ConvertHex(VR[50] + VR[51] + VR[52] + VR[53] + VR[54] + VR[55] + VR[56] + VR[57]);
                string Water = Commands.ConvertHex(VR[58] + VR[59] + VR[60] + VR[61] + VR[62] + VR[63] + VR[64] + VR[65]);
                string Temperature = Commands.ConvertHex(VR[66] + VR[67] + VR[68] + VR[69] + VR[70] + VR[71] + VR[72] + VR[73]);



                string str = response.Replace("\n\r", "");
                string[] strArrays = str.Split(new char[] { '=' });

                if (length == 89)
                {
                    int num = ATG_id;

                    decimal num2 = Convert.ToDecimal(Mantissa(Temperature));
                    decimal num3 = Convert.ToDecimal(Mantissa(Volume));
                    decimal num4 = Convert.ToDecimal(Mantissa(Water));
                    ATGProbe aTGProbe = (
                        from p in this.probes
                        where p.Address == num
                        select p).FirstOrDefault<ATGProbe>();
                    if (aTGProbe != null)
                    {

                        aTGProbe.FuelLevel = num3 / new decimal(100);
                        aTGProbe.WaterLevel = num4 / new decimal(100);
                        aTGProbe.Temperature = num2 / new decimal(100);
                        if (this.DataUpdated != null)
                        {
                            this.DataUpdated(aTGProbe, new EventArgs());
                        }
                    }

                }
                if (length < 88)
                {
                    int num = ATG_id;

                    
                    decimal num2 = 0;
                    decimal num3 = 0;
                    decimal num4 = 0;
                    ATGProbe aTGProbe = (
                        from p in this.probes
                        where p.Address == num
                        select p).FirstOrDefault<ATGProbe>();
                    if (aTGProbe != null)
                    {
                        aTGProbe.FuelLevel = num3;
                        aTGProbe.WaterLevel = num4;
                        aTGProbe.Temperature = num2;
                        if (this.DataUpdated != null)
                        {
                            this.DataUpdated(aTGProbe, new EventArgs());
                        }
                    }
                }
                response = "";
            }
            catch (Exception ex)
            {
                response = "";
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
                        string idString = probe.Address.ToString();
                        this.serialPort.Write(Commands.GetStatus(idString), 0, Commands.GetStatus(idString).Length);
                        
                        Thread.Sleep(500);
                        num = num + 100;
                        string str = "";
                        DateTime now = DateTime.Now;
                        while (true)
                        {
                            if (this.serialPort.BytesToRead > 0)
                            {
                                str = string.Concat(str, this.serialPort.ReadExisting());
                                if (str.Contains("\u0001"))
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

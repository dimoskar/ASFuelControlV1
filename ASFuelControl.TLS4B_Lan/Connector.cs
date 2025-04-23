using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Diagnostics;


namespace ASFuelControl.TLS4B_Lan
{
    public class Connector
    {
        public event EventHandler DataUpdated;

        //private SerialPort serialPort = new SerialPort();

        private Thread th;

        private List<ATGProbe> probes = new List<ATGProbe>();

        //private bool isConnected;

        private bool errorOccured;

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
                
                return tcp.IsConnected();
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
            this.th = new Thread(new ThreadStart(this.ThreadRun));
            this.th.Start();
        }

        public ATGProbe AddProbe(int addressId)
        {
            ATGProbe aTGProbe;
            if ((
                from p in this.probes
                where p.Address == addressId
                select p).Count<ATGProbe>() > 0)
            {
                aTGProbe = null;
            }
            else
            {
                ATGProbe aTGProbe1 = new ATGProbe()
                {
                    Address = addressId
                };
                this.probes.Add(aTGProbe1);
                aTGProbe = aTGProbe1;
            }
            return aTGProbe;
        }

        private TcpCom tcp;

        public void Connect()
        {
            try
            {
                tcp = new TcpCom("169.254.21.12", 35555);
                tcp.OnConnectEvent += new TcpCom.OnConnectEventHandler(OnConnect);
                tcp.OnDataRecievedEvent += new TcpCom.DataReceivedEventHandler(OnRecieved);
                tcp.Connect();

              
            }
            catch (Exception ex)
            {

            }

        }
        private void OnConnect(bool status)
        {
            try
            {

            }
            catch (Exception ex)
            {

            }
        }


        private void OnRecieved(string data)
        {
            try
            {
                string dat = data.Replace("\u0001","").Replace("\u0003","");

                if(dat.Length > 70)
                    this.EvaluateResponse(dat);

            }
            catch (Exception ex)
            {

            }
        }

        //private bool Close = false;

        public void DisConnect()
        {
            
            tcp.Disconnect();
            th.Abort();
        }

        private void EvaluateResponse(string response)
        {
            try
            {
                Trace.WriteLine(response);
                string[] strArrays = BitConverter.ToString(Encoding.Default.GetBytes(response)).Split(new char[] { '-' });
                int length = response.Length;
                int num = Convert.ToInt32(Commands.ConvertHex(strArrays[5]));
                string str = Commands.ConvertHex(string.Concat(new string[] { strArrays[49], strArrays[50], strArrays[51], strArrays[52], strArrays[53], strArrays[54], strArrays[55], strArrays[56] }));
                string str1 = Commands.ConvertHex(string.Concat(new string[] { strArrays[57], strArrays[58], strArrays[59], strArrays[60], strArrays[61], strArrays[62], strArrays[63], strArrays[64] }));
                string str2 = Commands.ConvertHex(string.Concat(new string[] { strArrays[65], strArrays[66], strArrays[67], strArrays[68], strArrays[69], strArrays[70], strArrays[71], strArrays[72] }));
                response.Replace("\n\r", "").Split(new char[] { '=' });
                if (length >= 87)
                {
                    int num1 = num;
                    decimal num2 = Convert.ToDecimal(this.Mantissa(str2));
                    decimal num3 = Convert.ToDecimal(this.Mantissa(str));
                    decimal num4 = Convert.ToDecimal(this.Mantissa(str1));
                    ATGProbe aTGProbe = (
                        from p in this.probes
                        where p.Address == num1
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
           
                response = "";
            }
            catch (Exception exception)
            {
                response = "";
            }
        }

        public string Mantissa(string value)
        {
            float single = BitConverter.ToSingle(BitConverter.GetBytes(uint.Parse(value, NumberStyles.AllowHexSpecifier)), 0) * 100f;
            return single.ToString();
        }

        private void ThreadRun()
        {
            try
            {
                while (true)
                {
                    try
                    {

                        while (IsConnected)
                        {
                            if (!IsConnected)
                                break;
                            foreach (ATGProbe probe in this.probes)
                            {
                                Console.WriteLine("Get Data From Probe " + probe.Address.ToString());
                                string str = probe.Address.ToString();
                                tcp.Write(Commands.GetStatus(str));
                                Thread.Sleep(1000);

                            }
                            Thread.Sleep(350);
                        }

                        while(!IsConnected)
                        {
                            Connect();
                            Thread.Sleep(2000);
                            Console.WriteLine("Try Connection");
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                    Thread.Sleep(250);
                }
            }

            catch
            {
                this.errorOccured = true;
                Thread.Sleep(500);
            }
        }   
    }


    public class Commands
    {
        public Commands()
        {
        }

        public static string ConvertHex(string hexString)
        {
            string str;
            try
            {
                string empty = string.Empty;
                for (int i = 0; i < hexString.Length; i += 2)
                {
                    string empty1 = string.Empty;
                    char chr = Convert.ToChar(Convert.ToUInt32(hexString.Substring(i, 2), 16));
                    empty = string.Concat(empty, chr.ToString());
                }
                str = empty;
            }
            catch (Exception exception)
            {
                return string.Empty;
            }
            return str;
        }

        public static byte[] GetStatus(string NozzleID)
        {
            int num = 48 + int.Parse(NozzleID);
            byte[] numArray = new byte[] { 1, 105, 50, 48, 49, 48, 0 };
            numArray[6] = (byte)num;
            return numArray;
        }
    }
}

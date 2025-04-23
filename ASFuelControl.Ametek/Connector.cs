using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Text.RegularExpressions;
namespace ASFuelControl.Ametec
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
            try
            {
                this.serialPort.PortName = this.CommunicationPort;
                this.serialPort.BaudRate = 9600;
                this.serialPort.DataReceived += new SerialDataReceivedEventHandler(this.DataIncome);
                this.serialPort.Open();
                this.isConnected = true;
                this.th = new Thread(new ThreadStart(this.ThreadRun));
                this.th.Start();
            }
            catch (Exception exception1)
            {
                Exception exception = exception1;
                Logger.Output(string.Concat(" Connect()", exception.ToString()), this.serialPort.PortName);
                this.errorOccured = true;
                Thread.Sleep(500);
            }
        }

        public void DisConnect()
        {
            this.isConnected = false;
            this.serialPort.Close();
        }
        private string DataIN;
        private List<AmetecData> AmetecDataList = new List<AmetecData>();
        private void AddDatatoAmetecList(int probeInt, string BufferData)
        {
            try
            {

                int CountList = AmetecDataList.Count;
                if (CountList < 1)
                {
                    AmetecDataList.Add(new AmetecData { ProbeId = probeInt, Response = BufferData });
                }

                else
                {
                    bool has = AmetecDataList.Any(x => x.ProbeId == probeInt);
                    if (has)
                    {
                        foreach (AmetecData dat in AmetecDataList)
                        {
                            if (dat.ProbeId == probeInt)
                                dat.Response = BufferData;
                        }
                    }
                    else
                    {
                        AmetecDataList.Add(new AmetecData { ProbeId = probeInt, Response = BufferData });
                    }

                }
            }
            catch(Exception exceptionAddToList)
            {
                Logger.Output(exceptionAddToList.ToString(), this.serialPort.PortName);
                this.errorOccured = true;
                Thread.Sleep(500);
            }
        }
        private string TakeDataFromAmetecList(int probeInt)
        {
            if (AmetecDataList.Any())
            {
                foreach (AmetecData dat in AmetecDataList)
                {
                    if (dat.ProbeId == probeInt)
                   
                        DataReturned = dat.Response;
                    
                }
                return DataReturned;
            }
            else
            {
                return "";
            }
            
        }
        private void DataIncome(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                while (this.serialPort.BytesToRead < 271)
                {
                    Thread.Sleep(30);
                }
                byte[] numArray = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(numArray, 0, this.serialPort.BytesToRead);
                string str = Encoding.Default.GetString(numArray);
                int num = int.Parse(str.Substring(0, 2));
                this.AddDatatoAmetecList(num, str);
                Logger.Output(string.Concat("DataIncome ", str), this.serialPort.PortName);
            }
            catch (Exception exception1)
            {
                Exception exception = exception1;
                Logger.Output(string.Concat("DataIncome ", exception.ToString()), this.serialPort.PortName);
            }
        }

        private void EvaluateResponse(string response)
        {
            try
            {
                if (!string.IsNullOrEmpty(response))
                {
                    response = response.Substring(0, 267);
                    decimal[] array = response.Substring(4).Replace('.', ',').Split(new char[] { '#' }).Select<string, decimal>(new Func<string, decimal>(decimal.Parse)).ToArray<decimal>();
                    decimal num = array[25];
                    decimal[] numArray = response.Substring(4).Replace('.', ',').Split(new char[] { '#' }).Select<string, decimal>(new Func<string, decimal>(decimal.Parse)).Skip<decimal>(26).Take<decimal>(5).ToArray<decimal>();
                    int num1 = int.Parse(response.Substring(0, 2));
                    decimal num2 = array.Take<decimal>(25).Average();
                    string str = num2.ToString("N4");
                    array[25].ToString("N4");
                    num2 = ((IEnumerable<decimal>)numArray).Average();
                    string str1 = num2.ToString("N2");
                    if (response.Length < 274)
                    {
                        int num3 = num1;
                        decimal num4 = Convert.ToDecimal(str1);
                        decimal num5 = decimal.Parse(str.Replace('.', ',')) * new decimal(254, 0, 0, false, 1);
                        decimal num6 = num * new decimal(254, 0, 0, false, 1);
                        ATGProbe aTGProbe = (
                            from p in this.probes
                            where p.Address == num3
                            select p).FirstOrDefault<ATGProbe>();
                        if (aTGProbe != null)
                        {
                            aTGProbe.FuelLevel = num5;
                            aTGProbe.WaterLevel = num6;
                            aTGProbe.Temperature = num4 / new decimal(10);
                            if (this.DataUpdated != null)
                            {
                                this.DataUpdated(aTGProbe, new EventArgs());
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.Output(exception.ToString(), this.serialPort.PortName);
                this.errorOccured = true;
                Thread.Sleep(500);
            }
        }


        string DataReturned;
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
                        string ValueBuf = TakeDataFromAmetecList(probe.Address);
                                              
                        if (!string.IsNullOrEmpty(ValueBuf))
                        {
                            this.EvaluateResponse(ValueBuf);
                            Logger.Output(ValueBuf, this.serialPort.PortName);
                        }
                    }
                    Thread.Sleep(350 - num);
                }
                catch(Exception ex)
                {
                    Logger.Output(ex.ToString(), this.serialPort.PortName);
                    this.errorOccured = true;
                    Thread.Sleep(500);
                }
            }
        }

        public event EventHandler DataUpdated;
    }
    public class AmetecData
    {
        public int ProbeId
        {
            get;
            set;
        }
        public string Response
        {
            get;
            set;
        }
    }
    public class Logger
    {
        public static void Output(string Data_Recieve, string SerialPort)
        {
            string fileName = "Ametec_" + SerialPort + ".txt";
            if (File.Exists(fileName))
            {
                using (StreamWriter writer = new StreamWriter(fileName, true, Encoding.UTF8))
                {
                    writer.Write("-->" + "RX" + "  " + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.fff") + "   <--- \r\n" + Data_Recieve.ToString() + "\r\n\r\n");
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
namespace ASFuelControl.StartItalianaXMT
{
    public class Connector
    {
        public event EventHandler DataUpdated;

        private System.IO.Ports.SerialPort serialPort = new System.IO.Ports.SerialPort();
        private System.Threading.Thread th;
        private List<ATGProbe> probes = new List<ATGProbe>();
        private bool isConnected = false;
        public Dictionary<string, int> StartItalianaTuple = new Dictionary<string, int>();
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
            if(this.probes.Where(p=>p.Address == addressId).Count() > 0)
                return null;
            ATGProbe probe = new ATGProbe();
            probe.Address = addressId;
            this.probes.Add(probe);
            return probe;
        }

        public void Connect()
        {
            this.serialPort.PortName = this.CommunicationPort;
            this.serialPort.DtrEnable = true;
            this.serialPort.Open();
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
                    if (errorOccured)
                    {
                        this.serialPort.Close();
                        this.serialPort.Open();
                        errorOccured = false;
                    }
                    int milsec = 0;
                    foreach (ATGProbe probe in this.probes)
                    {
                        this.serialPort.Write("M" + probe.Address.ToString() + "\n\r");
                        if (System.IO.File.Exists("xmt.log"))
                        {
                            System.IO.File.AppendAllText("xmt.log", "Response\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + "\t TX: " + "M" + probe.Address.ToString() + "\r\n");
                        }
                        System.Threading.Thread.Sleep(10);
                        milsec = milsec + 100;
                        string response = "";
                       
                        while(true)
                        {
                            int waiting = 0;
                            while (this.serialPort.BytesToRead < 30 && waiting < 200)
                            {
                                System.Threading.Thread.Sleep(25);
                                waiting += 20;
                            }
                            //if(this.serialPort.BytesToRead > 0)
                            //{
                            //    response = response + serialPort.ReadExisting();
                            //    if (response.Contains("\n\r"))
                            //        break;
                            //}
                            byte[] buffer = new byte[serialPort.BytesToRead];
                            serialPort.Read(buffer, 0, buffer.Length);
                            response = System.Text.Encoding.ASCII.GetString(buffer);                        
                            break;
                            
                        }
                        
                            EvaluateResponse(response);
                        
                        System.Threading.Thread.Sleep(25);
                    }
                    

                    System.Threading.Thread.Sleep(1500 - milsec);
                }
                catch
                {
                    errorOccured = true;
                    System.Threading.Thread.Sleep(500);
                }
            }
        }

        string buffer = "";
        private void AddTuple(string SerialNumber, int ErrorRecieve)
        {
            try
            {
                int CountDictionary = StartItalianaTuple.Count;
                if (CountDictionary > 0)
                {
                    foreach (var lst in StartItalianaTuple)
                    {
                        if (lst.Key == SerialNumber)
                        {
                            StartItalianaTuple.Remove(lst.Key);
                            StartItalianaTuple.Add(SerialNumber, ErrorRecieve);
                            break;
                        }
                    }
                    StartItalianaTuple.Add(SerialNumber, ErrorRecieve);
                }
                else
                    StartItalianaTuple.Add(SerialNumber, ErrorRecieve);
            }
            catch
            {

            }

        }
        private int NumErrors(string TankAddress)
        {
            int num = 0;
            var q = StartItalianaTuple.Where(p => p.Key == TankAddress).FirstOrDefault();
            num = q.Value;
            return num;
        }
        private void EvaluateResponse(string response)
        {
            if (response.Length > 0)
            {
                int idx = response.IndexOf("\n\r");
                if (idx >= 0)
                    response = response.Substring(0, idx);
            }
            string resp = response.Replace("\n\r", "");
            string[] parms = resp.Split('=');
            if (parms.Length != 6)
                return;
            int address = int.Parse(parms[0]);
            int status = int.Parse(parms[1]);
            decimal temp = int.Parse(parms[2]);
            decimal flevel = int.Parse(parms[3]);
            decimal wlevel = int.Parse(parms[4]);
            ATGProbe probe = this.probes.Where(p => p.Address == address).FirstOrDefault();
            int CountError = NumErrors(address.ToString());
            if (System.IO.File.Exists("xmt.log"))
            {
                System.IO.File.AppendAllText("xmt.log", "Response\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + "\t RX: " + response + "\t Error: " + CountError.ToString() + "\r\n");
            }
            if (probe == null)
            {
                return;
            }
            if(CountError >5)
            {
                return;
            }
            if ((int)resp.Length != 27)
            {
                AddTuple(address.ToString(), CountError + 1);
                probe.FuelLevel = probe.FuelLevel;
                probe.WaterLevel = probe.WaterLevel;
                probe.Temperature = probe.Temperature;
                if (this.DataUpdated != null)
                {
                    this.DataUpdated(probe, new EventArgs());
                }
            }
         
            if (status == 0 && CountError < 1)
            {
                AddTuple(address.ToString(), 0);
                probe.FuelLevel = flevel / 10;
                probe.WaterLevel = wlevel / 1;
                probe.Temperature = temp / 10;
                if (this.DataUpdated != null)
                    this.DataUpdated(probe, new EventArgs());
            }
            if (status == 1)            
                return;        
        }
       
    }
   
}

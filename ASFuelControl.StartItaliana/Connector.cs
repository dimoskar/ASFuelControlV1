using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.StartItaliana
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
            if(this.probes.Where(p=>p.Address == addressId).Count() > 0)
                return null;
            ATGProbe probe = new ATGProbe();
            probe.Address = addressId;
            this.probes.Add(probe);
            return probe;
        }

        public void Connect()
        {
            try
            {
                this.serialPort.PortName = this.CommunicationPort;
                this.serialPort.DtrEnable = true;
                //this.serialPort.DataReceived -= new System.IO.Ports.SerialDataReceivedEventHandler(serialPort_DataReceived);
                //this.serialPort.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(serialPort_DataReceived);
                this.serialPort.Open();
                this.isConnected = true;
                th = new System.Threading.Thread(new System.Threading.ThreadStart(this.ThreadRun));
                th.Start();
            }
            catch
            {

            }
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
                        System.Threading.Thread.Sleep(100);
                        milsec = milsec + 100;
                        string response = "";
                        DateTime dtNow = DateTime.Now;
                        while(true)
                        {
                            if (this.serialPort.BytesToRead > 0)
                            {
                                response = response + serialPort.ReadExisting();
                                dtNow = DateTime.Now;
                                if (response.Contains("\n\r"))
                                    break;
                                Console.WriteLine("{0:HH:mm:ss.fff} - DATA OK", DateTime.Now);
                            }
                            else
                            {
                                if (DateTime.Now.Subtract(dtNow).TotalMilliseconds > 500)
                                {
                                    //Console.WriteLine("{0:HH:mm:ss.fff} - NO DATA RETURNED", DateTime.Now);
                                    break;
                                }
                            }
                            System.Threading.Thread.Sleep(20); 
                        }

                        if (response.Length > 0)
                        {
                            int idx = response.IndexOf("\n\r");
                            if (idx >= 0)
                                response = response.Substring(0, idx);
                            EvaluateResponse(response);
                        }
                        else
                        {
                            Console.WriteLine("{0:HH:mm:ss.fff} - NO DATA RETURNED", DateTime.Now);
                        }
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

        private void EvaluateResponse(string response)
        {
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
            if (probe == null)
                return;
            /////////////////////////////////////////////////////////////////////////////
            //if(valid){
            /////////////////////////////////////////////////////////////////////////////
            
            if (status == 0)
            {
                probe.FuelLevel = flevel / 10;
                probe.WaterLevel = wlevel / 1;
                probe.Temperature = temp / 10;
                if (this.DataUpdated != null)
                    this.DataUpdated(probe, new EventArgs());
            }
            else if (status == 1)
            {
                probe.FuelLevel = flevel / 10;
                probe.WaterLevel = wlevel / 1;
                probe.Temperature = temp / 10;

                if (probe.FuelLevel == 0)
                    return;
                if (this.DataUpdated != null)
                    this.DataUpdated(probe, new EventArgs());
            }
        //}
        }
       
        void serialPort_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            if (!this.IsConnected)
                return;
            if (serialPort.BytesToRead == 0)
                return;
            buffer = buffer + serialPort.ReadExisting();
            //Console.WriteLine(buffer);
            string[] responses = buffer.Split(new string[] { "\n\r" },  StringSplitOptions.RemoveEmptyEntries);
            foreach (string response in responses)
            {
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
                if (probe == null)
                    continue;
                if (status == 0)
                {
                    probe.FuelLevel = flevel / 10;
                    probe.WaterLevel = wlevel / 1;
                    probe.Temperature = temp / 10;
                    if (this.DataUpdated != null)
                        this.DataUpdated(probe, new EventArgs());
                }
                else if (status == 1)
                {
                    probe.FuelLevel = (decimal)100.99;
                    probe.WaterLevel = (decimal)10.99;
                    probe.Temperature = temp / 10;
                    if (this.DataUpdated != null)
                        this.DataUpdated(probe, new EventArgs());
                }
            }
            int index = buffer.LastIndexOf("\n\r");
            buffer = buffer.Substring(index + 1);
        }
    }
   
}

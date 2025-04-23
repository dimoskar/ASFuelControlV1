using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.StartItalianaL
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
                        if (probe.Address.ToString().Length < 5)
                        {
                            this.serialPort.Write("M0" + probe.Address.ToString() + "\n\r");
                            if (System.IO.File.Exists("xmt.log"))
                            {
                                System.IO.File.AppendAllText("xmt.log", "Send\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + "\t TX: " + "M0" + probe.Address.ToString() + "\r\n");
                            }
                        }
                        else
                        {
                            this.serialPort.Write("M" + probe.Address.ToString() + "\n\r");
                            if (System.IO.File.Exists("xmt.log"))
                            {
                                System.IO.File.AppendAllText("xmt.log", "Send\t" + DateTime.Now.ToString("dd-MM HH:mm:ss.fff") + "\t TX: " + "M" + probe.Address.ToString() + "\r\n");
                            }
                        }
                      
                        System.Threading.Thread.Sleep(25);
                        milsec = milsec + 100;
                        string response = "";
                        DateTime dtNow = DateTime.Now;
                        while (true)
                        {
                            int waiting = 0;
                            while (this.serialPort.BytesToRead < 30 && waiting < 200)
                            {
                                System.Threading.Thread.Sleep(45);
                                waiting += 20;
                            }
                            byte[] buffer = new byte[serialPort.BytesToRead];
                            serialPort.Read(buffer, 0, buffer.Length);
                            
                            response = System.Text.Encoding.ASCII.GetString(buffer);

                            break;

                        }

                        EvaluateResponse(response);

                        System.Threading.Thread.Sleep(25);
                    }


                    System.Threading.Thread.Sleep(1000 - milsec);
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
            if (System.IO.File.Exists("xmt.log"))
            {
                string dt = DateTime.Now.ToString("dd-MM HH:mm:ss.fff");
                System.IO.File.AppendAllText("xmt.log", "Response\t" + dt + "\t RX: " + response  + "\r\n");
            }
            string resp = response.Replace("\n\r", "");
            string[] parms = resp.Split('=');

            if (parms.Length == 6)
            {
                //34983=0=+212=00984=0043=252

                int address = int.Parse(parms[0]);
                int status = int.Parse(parms[1]);
                decimal temp = int.Parse(parms[2]);
                decimal flevel = int.Parse(parms[3]);
                decimal wlevel = int.Parse(parms[4]);
                ATGProbe probe = this.probes.Where(p => p.Address == address).FirstOrDefault();
                if (probe == null)
                    return;
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
            }
            else if (parms.Length == 5)
            {
                //02792L0=+000=02981=3013=254
                string[] Address = parms[0].Split('L');

                int address = int.Parse(Address[0]);
                int status = int.Parse(Address[1]);
                decimal temp = int.Parse(parms[1]);
                decimal flevel = int.Parse(parms[2]);
                decimal wlevel = int.Parse(parms[3]);
                ATGProbe probe = this.probes.Where(p => p.Address == address).FirstOrDefault();
                if (probe == null)
                    return;
                if (status == 0)
                {
                    probe.FuelLevel = flevel / 1;
                    probe.WaterLevel = wlevel / 1;
                    probe.Temperature = temp / 10;
                    if (this.DataUpdated != null)
                        this.DataUpdated(probe, new EventArgs());
                }
                else if (status == 1)
                {
                    probe.FuelLevel = flevel / 1;
                    probe.WaterLevel = wlevel / 1;
                    probe.Temperature = temp / 10;

                    if (probe.FuelLevel == 0)
                        return;
                    if (this.DataUpdated != null)
                        this.DataUpdated(probe, new EventArgs());
                }
            }
            else
            { return; }
        }
       
        //void serialPort_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        //{
        //    if (!this.IsConnected)
        //        return;
        //    if (serialPort.BytesToRead == 0)
        //        return;
        //    buffer = buffer + serialPort.ReadExisting();

        //    string[] responses = buffer.Split(new string[] { "\n\r" },  StringSplitOptions.RemoveEmptyEntries);
        //    foreach (string response in responses)
        //    {
        //        string resp = response.Replace("\n\r", "");
        //        string[] parms = resp.Split('=');
        //        if (parms.Length != 6)
        //            return;
        //        int address = int.Parse(parms[0]);
        //        int status = int.Parse(parms[1]);
        //        decimal temp = int.Parse(parms[2]);
        //        decimal flevel = int.Parse(parms[3]);
        //        decimal wlevel = int.Parse(parms[4]);
        //        ATGProbe probe = this.probes.Where(p => p.Address == address).FirstOrDefault();
        //        if (probe == null)
        //            continue;
        //        if (status == 0)
        //        {
        //            probe.FuelLevel = flevel / 10;
        //            probe.WaterLevel = wlevel / 1;
        //            probe.Temperature = temp / 10;
        //            if (this.DataUpdated != null)
        //                this.DataUpdated(probe, new EventArgs());
        //        }
        //        else if (status == 1)
        //        {
        //            probe.FuelLevel = (decimal)100.99;
        //            probe.WaterLevel = (decimal)10.99;
        //            probe.Temperature = temp / 10;
        //            if (this.DataUpdated != null)
        //                this.DataUpdated(probe, new EventArgs());
        //        }
        //    }
        //    int index = buffer.LastIndexOf("\n\r");
        //    buffer = buffer.Substring(index + 1);
        //}
    }
   
}

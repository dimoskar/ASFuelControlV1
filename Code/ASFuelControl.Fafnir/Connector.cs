using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ASFuelControl.Fafnir
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
                    if (errorOccured)
                    {
                        this.serialPort.Close();
                        this.serialPort.Open();
                        errorOccured = false;

                        Console.WriteLine("Fafnir CONTROLLER RESTARTED");
                    }
                    int milsec = 0;
                    foreach (ATGProbe probe in this.probes)
                    {
                        string idString = probe.Address.ToString();
                        for(int k=0; k < 5 - idString.Length; k++)
                            idString += "0";
                        byte[] idByte = Encoding.ASCII.GetBytes(idString);
                        //S=083 055 054 050 054 053 CR=013 LF=010 
                        byte[] status = new byte[] { 0x53, idByte[0], idByte[1], idByte[2], idByte[3], idByte[4], 0x0D, 0x0A };

                        this.serialPort.Write(status, 0, status.Length);
                        
                        System.Threading.Thread.Sleep(800);
                        milsec = milsec + 100;
                        string response = "";
                        DateTime dtNow = DateTime.Now;
                        while (true)
                        {
                            if (this.serialPort.BytesToRead > 0)
                            {

                                response = response + serialPort.ReadExisting();
                                if (response.Contains("\n\r"))
                                    break;
                            }
                            if (DateTime.Now.Subtract(dtNow).TotalMilliseconds > 100)
                                break;
                        }
                        if (response.Length > 0)
                            EvaluateResponse(response);
                    }


                    System.Threading.Thread.Sleep(1500 - milsec);
                }
                catch
                {
                    errorOccured = true;
                    System.Threading.Thread.Sleep(100);
                }
            }
        }



        string buffer = "";

        private void EvaluateResponse(string response)
        {

            string resp = response.Replace(@" ", "");
            resp = response.Replace("\n\r", "");
            string[] parms = response.Split('=');
            //todo
            //if (parms.Length != 5)
            //return;
            //"76265=0:+19451=  64665=  23532= 39";

            decimal flevel = 0;
            decimal wlevel = 0;

            int address = Convert.ToInt32(response.Substring(0, 5));
            decimal temp = Convert.ToDecimal(response.Substring(9, 5));
            int status = Convert.ToInt32(response.Substring(6, 1));
            string plusOrCol = response.Substring(5, 1);
            if (plusOrCol != ":")
            {

                flevel = Convert.ToDecimal(parms[2]);
                wlevel = Convert.ToDecimal(parms[3]);
                //string log = "\taddres:" + address.ToString() + "\t \n\\n\n\nStatus:";
            }
            else
            {
                flevel = Convert.ToDecimal(parms[1]);
                wlevel = Convert.ToDecimal(parms[2]);
                //string log = "\taddres:" + address.ToString() + "\t \n\\n\n\nStatus:";
            }

            ATGProbe probe = this.probes.Where(p => p.Address == address).FirstOrDefault();

            if (probe == null)
                return;
            if (status == 0)
            {
                probe.FuelLevel = Math.Round(flevel / 1000, 1, MidpointRounding.AwayFromZero);
                probe.WaterLevel = Math.Round(wlevel / 1000, 1, MidpointRounding.AwayFromZero);
                probe.Temperature = Math.Round(temp / 1000, 1, MidpointRounding.AwayFromZero);
                if (this.DataUpdated != null)
                    this.DataUpdated(probe, new EventArgs());
            }
        }

        void serialPort_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            if (!this.IsConnected)
                return;
            if (serialPort.BytesToRead == 0)
                return;
            buffer = buffer + serialPort.ReadExisting();
            //Console.WriteLine(buffer);
            string[] responses = buffer.Split(new string[] { "\n\r" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string response in responses)
            {
                string resp = response.Replace(@" ", "");
                resp = response.Replace("\n\r", "");
                string[] parms = resp.Split('=');
                //if (parms.Length != 5)
                //return;

                decimal flevel = 0;
                decimal wlevel = 0;

                int address = Convert.ToInt32(response.Substring(0, 5));
                decimal temp = Convert.ToDecimal(response.Substring(9, 5));
                int status = Convert.ToInt32(response.Substring(6, 1));
                string plusOrCol = response.Substring(5, 1);
                if (plusOrCol != ":")
                {

                    flevel = Convert.ToDecimal(parms[2]);
                    wlevel = Convert.ToDecimal(parms[3]);
                    string log = "\taddres:" + address.ToString() + "\t \n\\n\n\nStatus:";
                }
                else
                {
                    flevel = Convert.ToDecimal(parms[1]);
                    wlevel = Convert.ToDecimal(parms[2]);
                }

                File.AppendAllText(@"C:\log.txt", "\t" + flevel.ToString());
                ATGProbe probe = this.probes.Where(p => p.Address == address).FirstOrDefault();
                if (probe == null)
                    continue;
                if (status == 0)
                {
                    probe.FuelLevel = Math.Round(flevel / 1000, 1, MidpointRounding.AwayFromZero);
                    probe.WaterLevel = Math.Round(wlevel / 1000, 1, MidpointRounding.AwayFromZero);
                    probe.Temperature = Math.Round(temp / 1000, 1, MidpointRounding.AwayFromZero);
                    if (this.DataUpdated != null)
                        this.DataUpdated(probe, new EventArgs());
                }

            }
            int index = buffer.LastIndexOf("\n\r");
            buffer = buffer.Substring(index + 1);
        }
    }
}

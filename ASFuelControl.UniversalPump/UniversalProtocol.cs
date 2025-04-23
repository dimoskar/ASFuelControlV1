using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.UniversalPump
{
    public class UniversalProtocol : Common.IFuelProtocol
    {
        private List<Common.FuelPoint> fuelPoints = new List<Common.FuelPoint>();
        private System.IO.Ports.SerialPort serialPort = new System.IO.Ports.SerialPort();
        private System.Threading.Thread th;
        private double speed;

        #region IFuelProtocol

        public event EventHandler<Common.FuelPointValuesArgs> DataChanged;

        public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;

        public event EventHandler<Common.FuelPointValuesArgs> DispenserStatusChanged;

        public bool IsConnected
        {
            get { return this.serialPort.IsOpen; }
        }

        public string CommunicationPort
        {
            set;
            get;
        }

        public void Connect()
        {
            try
            {
                this.serialPort.PortName = this.CommunicationPort;
                this.serialPort.BaudRate = 5787;
                this.speed = 1 / (Convert.ToDouble(serialPort.BaudRate) / Convert.ToDouble(8000));
                this.serialPort.Parity = System.IO.Ports.Parity.Even;
                this.serialPort.StopBits = System.IO.Ports.StopBits.One;
                this.serialPort.DataBits = 8;
                this.serialPort.Open();
                this.th = new System.Threading.Thread(new System.Threading.ThreadStart(this.ThreadRun));
                th.Start();
            }
            catch
            {
            }
        }

        public void Disconnect()
        {
            if (this.serialPort.IsOpen)
                this.serialPort.Close();
        }

        public void AddFuelPoint(Common.FuelPoint fp)
        {
            this.fuelPoints.Add(fp);
        }

        public void ClearFuelPoints()
        {
            this.fuelPoints.Clear();
        }

        public Common.FuelPoint[] FuelPoints
        {
            get
            {
                return this.fuelPoints.ToArray();
            }
            set
            {
                this.fuelPoints = new List<Common.FuelPoint>(value);
            }
        }

        public Common.DebugValues DebugStatusDialog(Common.FuelPoint fp)
        {
            throw new NotImplementedException();
        }

        #endregion

        private void ThreadRun()
        {
        }
    }
}

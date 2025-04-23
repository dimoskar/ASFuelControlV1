using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Windows.Threads
{
    public class FleetManagmentThread
    {
        public event EventHandler<FleetManagementDataRecievedArgs> DataRecieved;
        List<RousisRFID.Controller> controllers = new List<RousisRFID.Controller>();
        public Dictionary<RousisRFID.Controller, string> lastdataDict = new Dictionary<RousisRFID.Controller, string>();

        public void AddVontroller(Data.FleetManagmentCotroller controller)
        {
            if (controller.EnrollmentDevice)
                return;
            if (this.controllers.Where(kk=>kk.ComPort == controller.ComPort).Count() > 0)
                return;
            if ((controller.ComPort != null && controller.ComPort.StartsWith("COM")) || 
                (controller.ControlerType.HasValue && controller.ControlerType.Value == 0))
            {
                RousisRFID.Controller c = new RousisRFID.Controller();
                c.ComPort = controller.ComPort;
                c.Address = controller.DeviceIndex.HasValue ? controller.DeviceIndex.Value : 1;
                c.EnrollmentDevice = controller.EnrollmentDevice;
                this.controllers.Add(c);
            }
        }

        public void Start()
        {
            foreach(var c in this.controllers)
            {
                if (c != null)
                {
                    try
                    {
                        c.Connect();
                        System.Threading.Thread th = new System.Threading.Thread( new System.Threading.ParameterizedThreadStart(this.ControllerThreadRun));
                        th.Start(c);

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }

        public void Stop()
        {
            foreach (var c in this.controllers)
            {
                if (c != null)
                {
                    try
                    {
                        c.DisConnect();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }

        private void ControllerThreadRun(object c)
        {
            RousisRFID.Controller controller = c as RousisRFID.Controller;
            if (c == null)
                return;
            if (!controller.IsConnected)
                controller.Connect();
            DateTime start = DateTime.Now;
            string lastDisplay = "WELCOME";
            string lastDisplay2 = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            controller.SetDiplayLine1(lastDisplay);
            controller.SetDiplayLine2(lastDisplay2);
            while (controller.IsConnected)
            {
                string id = controller.GetLastId();
                if (id == "")
                {
                    int diff = (int)DateTime.Now.Subtract(start).TotalSeconds;
                    if (diff > 10 && lastDisplay != "WELCOME")
                    {
                        controller.SetDiplayLine1("WELCOME");
                        lastDisplay = "WELCOME";
                    }
                    string dtime = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                    if (lastDisplay2 != dtime)
                    {
                        controller.SetDiplayLine2(dtime);
                        lastDisplay2 = dtime;
                    }
                    continue;
                }
                
                
                if (controller.EnrollmentDevice)
                {
                    lastDisplay = id;
                    controller.LastScannedId = id;
                    controller.SetDiplayLine1(id);
                }
                else
                {
                    using (Data.DatabaseModel db = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
                    {
                        var veh = db.Vehicles.FirstOrDefault(v => v.CardId == id);
                        if(veh != null)
                        {
                            lastDisplay = Common.Helpers.GetLatinFromGreek(veh.PlateNumber.ToUpper());
                            controller.SetDiplayLine1(lastDisplay);
                            if (this.DataRecieved != null)
                                this.DataRecieved(controller, new FleetManagementDataRecievedArgs() { ComPort = controller.ComPort, Data = id, Date = DateTime.Now });
                        }
                    }
                }
                start = DateTime.Now;
                System.Threading.Thread.Sleep(100);
            }
        }

        //private void Comm_UpdateRfidData(object sender, RFID.RFIDEvent Data)
        //{
        //    ASFuelControl.RFID.Communication comm = sender as ASFuelControl.RFID.Communication;
        //    if (comm == null)
        //        return;
        //    if (this.DataRecieved != null)
        //        this.DataRecieved(this, new FleetManagementDataRecievedArgs() { ComPort = comm.IpTerminal, Data = Data.EnrollNumber.ToString(), Date = Data.EnrollDateTime });
        //}

        //void port_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        //{
        //    System.IO.Ports.SerialPort port = sender as System.IO.Ports.SerialPort;
        //    if (port == null)
        //        return;
        //    System.Threading.Thread.Sleep(100);
        //    string data = port.ReadExisting();
        //    if (!data.Contains("\r") && !data.Contains("\n"))
        //        return;
            
        //    string[] datacomps = data.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        //    if (datacomps.Length == 0)
        //        return;
        //    Console.WriteLine("DATA COMP : " + datacomps.Last());
        //    data = datacomps.Last().Replace("\r", "").Replace("\n", "");

        //    if (!this.lastdataDict.ContainsKey(port))
        //        this.lastdataDict.Add(port, "");
        //    string lastData = this.lastdataDict[port];
        //    if(lastData == data)
        //        return;
        //    this.lastdataDict[port] = data;
        //    Console.WriteLine("DATA : " + data);
        //    if (this.DataRecieved != null)
        //        this.DataRecieved(this, new FleetManagementDataRecievedArgs() { ComPort = port.PortName, Data = data });
        //}
    }

    public class FleetManagementDataRecievedArgs : EventArgs
    {
        public string ComPort { set; get; }
        public string Data { set; get; }
        public DateTime Date { set; get; }
    }

    public class EnrollData
    {
        public string EnrollId { set; get; }
        public DateTime EnrollDate { set; get; }
    }
}

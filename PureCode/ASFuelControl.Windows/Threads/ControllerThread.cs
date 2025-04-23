using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASFuelControl.VirtualDevices;
using System.Collections.Concurrent;
using ASFuelControl.Logging;
using System.Xml.Serialization;
using System.IO;

namespace ASFuelControl.Windows.Threads
{
    /// <summary>
    /// Class provides functionality for connection of fuelpump an tank workflows.
    /// </summary>
    public class ControllerThread
    {
        #region events

        public event EventHandler<ASFuelControl.WorkFlow.SaleCompletedArgs> SaleAvaliable;
        public event EventHandler TankFillingAvaliable;
        public event EventHandler RefreshAlerts;
        public event EventHandler<TankValuesEventArgs> TankValuesAvaliable;
        public event EventHandler<DispenserValuesEventArgs> DispenserValuesAvaliable;

        public event EventHandler<WorkFlow.AlarmRaisedEventArgs> AlarmRaised;
        public event EventHandler CheckInvoices;
        public event EventHandler<QueryResolveAlarmArgs> QueryResolveAlarm;
        public event EventHandler<WorkFlow.QueryAlarmResolvedArgs> QueryAlarmResolved;
        public event EventHandler<FuelPump.QueryTotalsUpdateArgs> QueryTotalsUpdate;
        public event EventHandler UpdateNozzleValues;
        public event EventHandler<FuelPump.NozzleStatusChangedArgs> UpdateNozzleStatus;
        public event EventHandler QueryPrices;
        public event EventHandler<PrintInvoiceArgs> PrintInvoice;
        public event EventHandler PrintAlarms;
        public event EventHandler<TimerStartEventArgs> QueryStartTimer;

        public event EventHandler ControllerConnectionFailed;
        public event EventHandler ControllerConnectionSuccess;

        #endregion

        #region private variables
        ConcurrentQueue<Common.Sales.SaleData> salesToProcess = new ConcurrentQueue<Common.Sales.SaleData>();
        ConcurrentQueue<Common.Sales.TankFillingData> fillingsToProcess = new ConcurrentQueue<Common.Sales.TankFillingData>();

        private bool haltThread = false;
        private System.Threading.Thread th;
        private DateTime lastTankCheck = DateTime.Now.AddMinutes(-1000);
        private List<ASFuelControl.WorkFlow.IFuelPumpWorkFlow> fpWorkFlows = new List<ASFuelControl.WorkFlow.IFuelPumpWorkFlow>();
        private List<Tank.TankWorkFlow> tankWorkFlows = new List<Tank.TankWorkFlow>();
        private string outFolder = "";
        private string signFolder = "";
        private string defaultTaxDevice = "";
        List<System.Threading.Thread> threads = new List<System.Threading.Thread>(); 

        #endregion

        #region public properties

        /// <summary>
        /// Interval for tankcheck 
        /// </summary>
        public int TankCheckInterval { set; get; }

        //public Common.IController Controller { set; get; }

        /// <summary>
        /// Flag to signal tha the threads should be stopped
        /// </summary>
        public bool HaltThread 
        {
            get { return this.haltThread; }
            set { this.haltThread = value; }
        }

        /// <summary>
        /// Flag for signal a price change made from user
        /// </summary>
        public bool PriceChanged { set; get; }

        #endregion

        public ControllerThread()
        {
            this.TankCheckInterval = 1440 * 60 * 1000;
            this.outFolder = Data.Implementation.OptionHandler.Instance.GetOption("Samtec_OutFolder");
            this.defaultTaxDevice = Data.Implementation.OptionHandler.Instance.GetOption("DefaultTaxDevice");
            if (this.defaultTaxDevice == "Samtec")
            {
                this.signFolder = Data.Implementation.OptionHandler.Instance.GetOption("Samtec_SignFolder");
                this.outFolder = Data.Implementation.OptionHandler.Instance.GetOption("Samtec_OutFolder");
            }
            else if (this.defaultTaxDevice == "SignA")
            {
                this.signFolder = Data.Implementation.OptionHandler.Instance.GetOption("SignA_SignFolder");
            }
        }

        #region public methods

        /// <summary>
        /// Gets the next sale avaliable in the salesToProcess Queue
        /// </summary>
        /// <returns></returns>
        public Common.Sales.SaleData GetNextSale()
        {
            if (this.salesToProcess.Count == 0)
                return null;
            Common.Sales.SaleData sale = null;
            if (this.salesToProcess.TryPeek(out sale))
                return sale;
            return null;
        }

        /// <summary>
        /// Add a sale to the salesToProcess Queue
        /// </summary>
        /// <param name="sale"></param>
        public void AddSale(Common.Sales.SaleData sale)
        {
            this.salesToProcess.Enqueue(sale);
        }

        /// <summary>
        /// Removes a sale from the salesToProcess Queue
        /// </summary>
        public void RenoveSale()
        {
            Common.Sales.SaleData sale;
            this.salesToProcess.TryDequeue(out sale);
        }

        /// <summary>
        /// Returns the next avaliable TankFilling from the fillingsToProcess Queue
        /// </summary>
        /// <returns></returns>
        public Common.Sales.TankFillingData GetNextFilling()
        {
            if (this.fillingsToProcess.Count == 0)
                return null;
            Common.Sales.TankFillingData filling = null;
            if (this.fillingsToProcess.TryDequeue(out filling))
                return filling;
            return null;
        }

        /// <summary>
        /// Creates an Virtual Dispenser and the Workflow attached to this dispenser
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="dispenserId"></param>
        /// <param name="channel"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        public VirtualDevices.VirtualDispenser AddDispenserWorkFlow(Common.IController controller, Guid dispenserId, int channel, int address)
        {   
            VirtualDevices.VirtualDispenser dispenser = new VirtualDispenser();
            dispenser.Controller = controller;
            dispenser.ChannelId = channel;
            dispenser.AddressId = address;
            dispenser.DispenserId = dispenserId;
            FuelPump.FuelPumpWorkFlow workFlow = new FuelPump.FuelPumpWorkFlow(dispenser);
            if(controller.GetType() == typeof(ASFuelControl.EMR3.EmrController))
            {
                dispenser.ManualyStart = true;
            }
            workFlow.Controller = controller;
            //workFlow.AlarmRaised+=new EventHandler<WorkFlow.AlarmRaisedEventArgs>(workFlow_AlarmRaised);
            workFlow.SaleCompleted += new EventHandler<WorkFlow.SaleCompletedArgs>(workFlow_SaleCompleted);
            workFlow.QueryTotalsUpdate += new EventHandler<FuelPump.QueryTotalsUpdateArgs>(workFlow_QueryTotalsUpdate);
            workFlow.NozzleStatusChanged += new EventHandler<FuelPump.NozzleStatusChangedArgs>(workFlow_NozzleStatusChanged);
            workFlow.QueryStationLocked += new System.ComponentModel.CancelEventHandler(workFlow_QueryStationLocked);
            this.fpWorkFlows.Add(workFlow);

            return dispenser;
        }
        // VirtualNozzle vNozzle = this.controllerThread.AddNozzle(dispenser.DispenserId, nozzle.NozzleId, nozzle.OrderId, nozzle.OfficialNozzleNumber, nozzle.NozzleIndex.Value, nozzle.NozzleState);
        
        /// <summary>
        /// Attaches an Virtual Nozle to the Virtual Dispenser identified by dispenserId
        /// </summary>
        /// <param name="dispenserId"></param>
        /// <param name="nozzleId"></param>
        /// <param name="orderIndex"></param>
        /// <param name="number"></param>
        /// <param name="index"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public VirtualDevices.VirtualNozzle AddNozzle(Guid dispenserId, Guid nozzleId, int orderIndex, int number, int index, int status)
        {
            VirtualDevices.VirtualDispenser dispenser = this.fpWorkFlows.Select(f => f.Dispenser).Where(d => d.DispenserId == dispenserId).FirstOrDefault();
            if (dispenser == null)
                return null;
            List<VirtualNozzle> nozzles = new List<VirtualNozzle>(dispenser.Nozzles);
            VirtualNozzle nozzle = new VirtualNozzle();
            nozzle.NozzleNumber = orderIndex;
            nozzle.NozzleOfficialNumber = number;
            nozzle.NozzleId = nozzleId;
            nozzle.NozzleIndex = index;
            nozzle.Status = (Common.Enumerators.NozzleStateEnum)status;
            nozzles.Add(nozzle);
            dispenser.Nozzles = nozzles.ToArray();
            nozzle.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(nozzle_PropertyChanged);
            return nozzle;
        }

        /// <summary>
        /// Creates an Virtual Tank and the Workflow attached to this tank
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="dispenserId"></param>
        /// <param name="channel"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        public VirtualDevices.VirtualTank AddTankWorkFlow(Common.IController controller, Guid tankId, int channel, int address, Common.Enumerators.TankStatusEnum status, Common.Enumerators.TankStatusEnum prevStatus)
        {
            VirtualDevices.VirtualTank tank = new VirtualTank();
            tank.ChannelId = channel;
            tank.AddressId = address;
            tank.TankId = tankId;
            tank.TankStatus = status;
            tank.PreviousStatus = prevStatus;
            

            Tank.TankWorkFlow workFlow = new Tank.TankWorkFlow(tank);
            workFlow.FillingCompleted += new EventHandler<Tank.TankFillingEventArgs>(workFlow_FillingCompleted);
            //workFlow.AlarmRaised += new EventHandler<WorkFlow.AlarmRaisedEventArgs>(workFlow_AlarmRaised);
            workFlow.Controller = controller;
            //workFlow.QueryAlarmResolved += new EventHandler<WorkFlow.QueryAlarmResolvedArgs>(workFlow_QueryAlarmResolved);
            this.tankWorkFlows.Add(workFlow);
            return tank;
        }

        /// <summary>
        /// Defines an connection between an Tank and a Nozzle
        /// </summary>
        /// <param name="tankId"></param>
        /// <param name="nozzleId"></param>
        public void AddTankNozzle(Guid tankId, Guid nozzleId)
        {
            List<VirtualNozzle> nozzles = this.fpWorkFlows.Select(f => f.Dispenser).SelectMany(d => d.Nozzles).Distinct().ToList();
            VirtualNozzle nozzle = nozzles.Where(n => n.NozzleId == nozzleId).FirstOrDefault();
            VirtualTank tank = this.tankWorkFlows.Select(t => t.Tank).Where(t => t.TankId == tankId).FirstOrDefault();
            if (tank == null || nozzle == null)
                return;
            List<VirtualNozzle> tankNozzles = tank.ConnectedNozzles.ToList();
            List<VirtualTank> nozzleTanks = nozzle.ConnectedTanks.ToList();
            if (!tankNozzles.Contains(nozzle))
            {
                tankNozzles.Add(nozzle);
                tank.ConnectedNozzles = tankNozzles.ToArray();
            }
            if (!nozzleTanks.Contains(tank))
            {
                nozzleTanks.Add(tank);
                nozzle.ConnectedTanks = nozzleTanks.ToArray();
            }
        }

        /// <summary>
        /// Returns all Virtual Tanks of the System
        /// </summary>
        /// <returns></returns>
        public VirtualTank[] GetTanks()
        {
            return this.tankWorkFlows.Select(t => t.Tank).OrderBy(t => t.TankNumber).ToArray();
        }

        /// <summary>
        /// Returns all Virtual Dispenser of the System
        /// </summary>
        /// <returns></returns>
        public VirtualDispenser[] GetDispensers()
        {
            return this.fpWorkFlows.Select(f => f.Dispenser).OrderBy(d => d.DispenserNumber).ToArray();
        }

        /// <summary>
        /// Gets the virtual Tank identified by tankId
        /// </summary>
        /// <param name="tankId"></param>
        /// <returns></returns>
        public VirtualTank GetTank(Guid tankId)
        {
            return this.tankWorkFlows.Select(t => t.Tank).Where(t => t.TankId == tankId).FirstOrDefault();
        }

        /// <summary>
        /// Gets the virtual Noozle identified by nozzleId
        /// </summary>
        /// <param name="tankId"></param>
        /// <returns></returns>
        public VirtualNozzle GetNozzle(Guid nozzleId)
        {
            return this.fpWorkFlows.SelectMany(t => t.Dispenser.Nozzles).Where(n => n.NozzleId == nozzleId).FirstOrDefault();
        }

        /// <summary>
        /// returns the Controller for the VirtualDevice vDevice
        /// </summary>
        /// <param name="vDevice"></param>
        /// <returns></returns>
        public Common.IController GetController(VirtualDevice vDevice)
        {
            if (vDevice.GetType() == typeof(VirtualTank))
            {
                VirtualTank vTank = vDevice as VirtualTank;
                return this.tankWorkFlows.Where(t => t.Tank.TankId == vTank.TankId).Select(t => t.Controller).FirstOrDefault();
            }
            else if (vDevice.GetType() == typeof(VirtualDispenser))
            {
                VirtualDispenser vDisp = vDevice as VirtualDispenser;
                return this.fpWorkFlows.Where(t => t.Dispenser.DispenserId == vDisp.DispenserId).Select(t => t.Controller).FirstOrDefault();
            }
            else if (vDevice.GetType() == typeof(VirtualNozzle))
            {
                VirtualNozzle vNozzle = vDevice as VirtualNozzle;
                VirtualDispenser vDisp = vNozzle.ParentDispenser;
                return this.fpWorkFlows.Where(t => t.Dispenser.DispenserId == vDisp.DispenserId).Select(t => t.Controller).FirstOrDefault();
            }
            return null;
        }

        /// <summary>
        /// Starts all controllers
        /// </summary>
        public void StartControllers()
        {
            haltThread = false;
            //List<Common.IController> controllers = this.fpWorkFlows.Select(f => f.Controller).Union(this.tankWorkFlows.Select(t => t.Controller)).Distinct().ToList();
            //foreach (Common.IController controller in controllers)
            //{
            //    try
            //    {
            //        controller.Connect();
            //    }
            //    catch
            //    {
            //    }
            //}

            this.th = new System.Threading.Thread(new System.Threading.ThreadStart(this.ThreadStart));
            this.th.Start();
        }

        /// <summary>
        /// Stops all controllers
        /// </summary>
        public void StopControllers()
        {
            try
            {
                List<Common.IController> controllers = this.fpWorkFlows.Select(f => f.Controller).Union(this.tankWorkFlows.Select(t => t.Controller)).Distinct().ToList();
                foreach (Common.IController controller in controllers)
                {
                    if (controller == null)
                        continue;
                    controller.DisConnect();
                }
            }
            catch
            {
            }
            haltThread = true;
        }

        #endregion

        //private void CheckForInvoices()
        //{
        //    try
        //    {
        //        if (outFolder != null && outFolder != "")
        //        {
        //            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(this.outFolder);
        //            if (!dir.Exists)
        //                return;
        //            System.IO.FileInfo[] files = dir.GetFiles("*.out");
        //            foreach (System.IO.FileInfo file in files)
        //            {
        //                string id = file.Name.Replace(".out", "");

        //                Guid invoiceId;
        //                if (!Guid.TryParse(id, out invoiceId))
        //                {
        //                    string originalFile = file.Name.Replace(".out", ".txt");
        //                    string str = System.IO.File.ReadAllText(file.FullName);
        //                    int signIndex = str.IndexOf(",");
        //                    if (signIndex < 0)
        //                        return;

        //                    str = str.Substring(7, signIndex - 6);

        //                    originalFile = this.signFolder + "\\" + originalFile;
        //                    System.IO.FileInfo origFileInfo = new System.IO.FileInfo(originalFile);
        //                    if (!origFileInfo.Exists)
        //                        continue;

        //                    System.IO.File.AppendAllText(originalFile, "\r\n" + str);
        //                    System.IO.DirectoryInfo dirInfo = new System.IO.DirectoryInfo(dir.Parent.Parent.FullName + "\\SingnedDocuments");
        //                    if (!dirInfo.Exists)
        //                        dirInfo.Create();
        //                    string newFileName = dirInfo.FullName + "\\" + file.Name.Replace(".out", ".txt");
        //                    if(System.IO.File.Exists(newFileName))
        //                        continue;
        //                    origFileInfo.MoveTo(newFileName);
        //                    file.Delete();
        //                    continue;
        //                }
        //                if (this.PrintInvoice != null)
        //                {
        //                    string str = System.IO.File.ReadAllText(file.FullName);
        //                    int signIndex = str.IndexOf(",");
        //                    if (signIndex < 0)
        //                        return;

        //                    string signStr = str.Substring(7, signIndex - 6);
        //                    if (signStr == null || signStr == "")
        //                    {
                                
        //                        signStr = str;
        //                    }
        //                    PrintInvoiceArgs args = new PrintInvoiceArgs(invoiceId, signStr);
        //                    this.PrintInvoice(this, args);
        //                    if (args.Competed)
        //                        file.Delete();
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Windows.Forms.MessageBox.Show(ex.Message, "Σφάλμα Εκτυπώσεων");
        //    }
        //}

        //private void CheckForAlarms()
        //{
        //    if (outFolder != null && outFolder != "")
        //    {
        //        System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(this.outFolder);
        //        if (!dir.Exists)
        //            return;

        //    }
        //}

        /// <summary>
        /// Initialization thead for each controller of fuelpoints or tanks. This thread is executing until the controller is connected. If no connection possible an message appears on the main form.
        /// </summary>
        /// <param name="parameter"></param>
        private void ControllerConnectionThread(object parameter)
        {
            Common.IController controller = parameter as Common.IController;
            while (!haltThread)
            {
                if (controller == null)
                {
                    System.Threading.Thread.Sleep(500);
                    continue;
                }
                if (!controller.IsConnected)
                {
                    try
                    {
                        controller.Connect();
                        if (!controller.IsConnected)
                        {
                            if (this.ControllerConnectionFailed != null)
                                this.ControllerConnectionFailed(controller, new EventArgs());
                            {
                                System.Threading.Thread.Sleep(500);
                                continue;
                            }
                        }
                    }
                    catch
                    {
                        if (this.ControllerConnectionFailed != null)
                            this.ControllerConnectionFailed(controller, new EventArgs());
                        {
                            System.Threading.Thread.Sleep(500);
                            continue;
                        }
                    }

                }
                if (this.ControllerConnectionSuccess != null)
                {
                    this.ControllerConnectionSuccess(controller, new EventArgs());
                    System.Threading.Thread.Sleep(5000);
                }
                else
                    System.Threading.Thread.Sleep(500);
            }
        }

        /// <summary>
        /// Initialization thead for each controller of fuelpoints. This thread is executing until all fuelpoints are connected and initialized.
        /// </summary>
        /// <param name="parameter"></param>
        private void InitializeThread(object parameter)
        {
            DispenserControllerClass dispClass = parameter as DispenserControllerClass;
            
            bool initialized = false;
            while (!initialized && !haltThread)
            {
                if (dispClass.Controller == null || !dispClass.Controller.IsConnected )
                {
                    System.Threading.Thread.Sleep(500);
                    continue;
                }
                initialized = true;
                lock (dispClass.Controller)
                {
                    foreach (VirtualNozzle nz in dispClass.Dispenser.Nozzles)
                    {
                        nz.TotalVolumeCounter = dispClass.Controller.GetNozzleTotalizer(dispClass.Dispenser.ChannelId, dispClass.Dispenser.AddressId, nz.NozzleNumber);
                        if (nz.TotalVolumeCounter < 0)
                        {
                            initialized = false;
                            break;
                        }
                    }
                }
                System.Threading.Thread.Sleep(500);
            }
            dispClass.Dispenser.Initialized = true;
        }

        /// <summary>
        /// Main thread of controlling all tank and fuelpoint workflows
        /// </summary>
        private void ThreadStart()
        {
            #region MainThread
            try
            {

                VirtualTank[] tanks = this.GetTanks();
                VirtualDispenser[] dispensers = this.GetDispensers();
                List<Common.IController> controllers = this.fpWorkFlows.Select(f => f.Controller).Union(this.tankWorkFlows.Select(t => t.Controller)).Distinct().ToList();

                foreach(ASFuelControl.Tank.TankWorkFlow workFlow in this.tankWorkFlows)
                {
                    workFlow.IsInitialized = true;
                    workFlow.Process.IsInitialized = true;
                    if(workFlow.Controller != null)
                        workFlow.Controller.AddAtg(workFlow.Tank.ChannelId, workFlow.Tank.AddressId);

                    workFlow.Tank.LastValuesUpdate = DateTime.Now;// DateTime.Now.Subtract(TimeSpan.FromSeconds(29));
                    workFlow.ProcessStateChanged -= new EventHandler(workFlow_ProcessStateChanged);
                    workFlow.ProcessStateChanged += new EventHandler(workFlow_ProcessStateChanged);
                    if(workFlow.Tank.CurrentFuelLevel > 0 && workFlow.Tank.LastFuelHeight == 0 && workFlow.Tank.FuelTypeDescription != "" && workFlow.Tank.PriceAverage == 0)
                    {
                        Common.Sales.TankFillingData tdata = new Common.Sales.TankFillingData();
                        tdata.TankId = workFlow.Tank.TankId;
                        Common.TankValues tvalues = new Common.TankValues();

                        tvalues.FuelHeight = workFlow.Tank.CurrentFuelLevel;
                        tvalues.CurrentTemperatur = workFlow.Tank.CurrentTemperature;
                        tvalues.WaterHeight = workFlow.Tank.CurrentWaterLevel;

                        tdata.Values = tvalues;
                        this.fillingsToProcess.Enqueue(tdata);
                        //LOGME
                        //System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
                        if(this.TankFillingAvaliable != null)
                            this.TankFillingAvaliable(this, new EventArgs());
                        //sw.Stop();
                        //TimeSpan elapsedTime = sw.Elapsed;
                        //if(elapsedTime.Seconds > 4)
                        //{
                        //    System.IO.File.AppendAllText(System.Environment.CurrentDirectory + "\\ThreadController.txt", string.Format("\n{1} this.TankFillingAvaliable(this, new EventArgs()); Start/Stop Elapsed time {0}", elapsedTime.ToString(), System.DateTime.Now));
                        //}
                    }
                }
                foreach(ASFuelControl.FuelPump.FuelPumpWorkFlow workFlow in this.fpWorkFlows)
                {
                    workFlow.IsInitialized = true;
                    workFlow.Process.IsInitialized = true;
                    if(workFlow.Controller != null && workFlow.Dispenser.IsValid)
                    {
                        workFlow.Controller.AddFuelPoint(workFlow.Dispenser.ChannelId, workFlow.Dispenser.AddressId, workFlow.Dispenser.Nozzles.Length, workFlow.Dispenser.DecimalPlaces,
                            workFlow.Dispenser.VolumeDecimalPlaces, workFlow.Dispenser.UnitPriceDecimalPlaces, workFlow.Dispenser.TotalDecimalPlaces);


                        if (workFlow.Controller.EuromatEnabled)
                        {
                            workFlow.Controller.SetEuromatDispenserNumber(workFlow.Dispenser.ChannelId, workFlow.Dispenser.AddressId, workFlow.Dispenser.EuromatNumber);
                        }
                        foreach(VirtualNozzle nz in workFlow.Dispenser.Nozzles)
                        {
                            workFlow.Controller.SetNozzleIndex  (workFlow.Dispenser.ChannelId, workFlow.Dispenser.AddressId, nz.NozzleNumber,nz.NozzleIndex);
                            workFlow.Controller.SetNozzlePrice(workFlow.Dispenser.ChannelId, workFlow.Dispenser.AddressId, nz.NozzleNumber, nz.CurrentSaleUnitPrice);
                        }
                    }
                }

                //List<Common.IController> controllers = this.fpWorkFlows.Select(f => f.Controller).Union(this.tankWorkFlows.Select(t => t.Controller)).Distinct().ToList();
                foreach(Common.IController controller in controllers)
                {
                    try
                    {
                        controller.Connect();
                    }
                    catch(Exception ex)
                    {
                        Logging.Logger.Instance.LogToFile("Connect Controller", ex);
                    }
                }

                //Threads for checking Controller state
                foreach (Common.IController controller in controllers)
                {
                    System.Threading.Thread thc = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(this.ControllerConnectionThread));
                    thc.Start(controller);
                    threads.Add(thc);
                }
                //Threads for check totals
                foreach (ASFuelControl.WorkFlow.IFuelPumpWorkFlow workFlow in this.fpWorkFlows)
                {
                    if (!workFlow.Dispenser.IsValid)
                        continue;
                    System.Threading.Thread th = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(this.InitializeThread));
                    th.Start(new DispenserControllerClass() { Dispenser = workFlow.Dispenser, Controller = workFlow.Controller });
                }

                AlertHandler.Instance.Tanks = this.GetTanks();
                AlertHandler.Instance.Dispensers = this.GetDispensers();
                AlertHandler.Instance.WireUpEvents();
                AlertHandler.Instance.AddAlertsFromDatabase();
                while(!haltThread)
                {
                    try
                    {
                        bool isTankCheck = false;
                        DateTime dt = DateTime.Now;
                        int interval = this.TankCheckInterval;

                        if (this.PriceChanged)
                        {
                            if (this.QueryPrices != null)
                                this.QueryPrices(this, new EventArgs());
                            this.PriceChanged = false;
                        }

                        if ((int)dt.TimeOfDay.TotalMinutes % interval == 0 && dt.Subtract(this.lastTankCheck).TotalMinutes > interval)
                        {
                            isTankCheck = true;
                            this.lastTankCheck = dt;
                        }
                        #region foreach TankWorkFlow
                        foreach (Common.IController controller in controllers)
                        {
                            if (controller == null)
                            {
                                System.Threading.Thread.Sleep(200);
                                continue;
                            }

                            foreach (ASFuelControl.Tank.TankWorkFlow workFlow in this.tankWorkFlows)
                            {
                                if (workFlow.Controller != controller)
                                {
                                    continue;
                                }
                                VirtualTank tank = workFlow.Tank;
                                //AlertHandler.CheckForTankAlerts(tank);
                                if (workFlow.Tank.IsVirtualTank)
                                {
                                    if (DateTime.Now.Subtract(tank.LastValuesUpdate).TotalSeconds > 1)
                                    {
                                        Common.TankValues values = new Common.TankValues();
                                        values.FuelHeight = workFlow.Tank.CurrentFuelLevel;
                                        values.CurrentTemperatur = workFlow.Tank.CurrentTemperature;
                                        values.WaterHeight = workFlow.Tank.CurrentWaterLevel;
                                        tank.TankValues = values;
                                        tank.LastValuesUpdate = DateTime.Now;
                                        workFlow.SetValues(values);
                                    }
                                }
                                else
                                {
                                    if (!controller.IsConnected)
                                    {
                                        if (tank.TankStatus != Common.Enumerators.TankStatusEnum.Offline && DateTime.Now.Subtract(tank.LastValuesUpdate).TotalSeconds > 45)
                                        {
                                            tank.TankStatus = Common.Enumerators.TankStatusEnum.Offline;
                                            tank.HasChanges = true;
                                            if (this.TankValuesAvaliable != null)
                                                this.TankValuesAvaliable(this, new TankValuesEventArgs(tank.TankId, null));
                                        }
                                        continue;
                                    }
                                    Common.TankValues values = controller.GetTankValues(tank.ChannelId, tank.AddressId);
                                    if (values == null)
                                    {

                                        if (DateTime.Now.Subtract(tank.LastValuesUpdate).TotalSeconds > 18)
                                        {
                                            tank.TankStatus = Common.Enumerators.TankStatusEnum.Offline;
                                            tank.HasChanges = true;
                                            if (this.TankValuesAvaliable != null)
                                                this.TankValuesAvaliable(this, new TankValuesEventArgs(tank.TankId, values));
                                        }
                                    }
                                    else
                                    {
                                        tank.LastValuesUpdate = DateTime.Now;
                                        tank.TankValues = values;
                                        if (tank.TankStatus == Common.Enumerators.TankStatusEnum.Offline)
                                            tank.TankStatus = Common.Enumerators.TankStatusEnum.Idle;
                                        if (this.TankValuesAvaliable != null)
                                            this.TankValuesAvaliable(this, new TankValuesEventArgs(tank.TankId, values));
                                        workFlow.SetValues(values);
                                    }

                                }
                                workFlow.ValidateTransitions();

                                tank.HasChanges = false;
                            }
                        #endregion
                        }
                        #region foreach fpWorkFlow
                        foreach (Common.IController controller in controllers)
                        {
                            if (controller == null)
                            {
                                System.Threading.Thread.Sleep(200);
                                continue;
                            }

                            foreach (ASFuelControl.WorkFlow.IFuelPumpWorkFlow workFlow in this.fpWorkFlows)
                            {
                                if (workFlow.Controller != controller)
                                    continue;
                                VirtualDispenser dispenser = workFlow.Dispenser;
                                if (!dispenser.IsValid)
                                    continue;

                                
                                if (!controller.IsConnected)
                                {
                                    if (this.salesToProcess.Count > 0)
                                    {
                                        for (int i = 0; i < this.salesToProcess.Count; i++)
                                        {
                                            if (this.SaleAvaliable != null)
                                                this.SaleAvaliable(this, new WorkFlow.SaleCompletedArgs(new Common.Sales.SaleData()));
                                        }
                                    }
                                }

                                if (Program.CurrentShiftId == Guid.Empty)
                                {
                                    
                                    continue;
                                }
                                if (this.salesToProcess.Count > 0)
                                {
                                    for (int i = 0; i < this.salesToProcess.Count; i++)
                                    {
                                        if (this.SaleAvaliable != null)
                                            this.SaleAvaliable(this, new WorkFlow.SaleCompletedArgs(new Common.Sales.SaleData()));
                                    }
                                }
                                workFlow.CheckOpenSales();
                                Common.FuelPointValues values = controller.GetDispenserValues(dispenser.ChannelId, dispenser.AddressId);
                                foreach (VirtualNozzle nz in dispenser.Nozzles)
                                {
                                    Common.Sales.SaleData nzSale = controller.GetSale(dispenser.ChannelId, dispenser.AddressId, nz.NozzleNumber);
                                    if (nzSale == null)
                                    {
                                        //if(values != null && values.Status == Common.Enumerators.FuelPointStatusEnum.Idle && dispenser.Status == Common.Enumerators.FuelPointStatusEnum.Offline)
                                        decimal tot = controller.GetNozzleTotalizer(dispenser.ChannelId, dispenser.AddressId, nz.NozzleNumber);
                                        if (tot != nz.TotalVolumeCounter)
                                        {
                                            nz.TotalVolumeCounter = tot;
                                            Console.WriteLine("TOTALS UPDATED : " + tot.ToString());
                                        }
                                        if (dispenser.Status == Common.Enumerators.FuelPointStatusEnum.Idle && DateTime.Now.Subtract(nz.LastIdleTime).TotalSeconds > 10)
                                        {
                                            if (nz.HasOpenSales)
                                                nz.HasOpenSales = false;
                                        }
                                        continue;
                                    }
                                    nzSale.NozzleId = nz.NozzleId;
                                    nzSale.CheckSale(dispenser.TotalDecimalPlaces);
                                    workFlow.SetSale(nzSale);

                                    if (this.QueryTotalsUpdate != null && nzSale.TotalVolume > 0)
                                    {
                                        this.QueryTotalsUpdate(this, new FuelPump.QueryTotalsUpdateArgs()
                                        {
                                            NozzleId = nz.NozzleId,
                                            TotalVolumeCounter = nzSale.TotalizerEnd
                                        });
                                    }
                                }

                                //AlertHandler.CheckForDispenserAlerts(dispenser);

                                //Common.Sales.SaleData sale = null;
                                if (values == null)
                                {
                                    if (dispenser.LastValues != null)
                                    {
                                        if (DateTime.Now.Subtract(dispenser.LastValuesUpdate).TotalSeconds > 5)
                                        {
                                            //Common.FuelPointValues vals = new Common.FuelPointValues();
                                            //vals.Status = Common.Enumerators.FuelPointStatusEnum.Offline;
                                            //vals.ActiveNozzle = -1;
                                            //workFlow.SetValues(vals);
                                        }
                                        else
                                        {

                                            if (dispenser.LastValuesId != dispenser.LastValues.ID || DateTime.Now.Subtract(dispenser.LastValues.AssignDateTime).TotalSeconds > 3)
                                            {
                                                workFlow.SetValues(dispenser.LastValues);
                                                if (values != null)
                                                    dispenser.LastValuesId = values.ID;
                                                dispenser.HasChanges = true;
                                                dispenser.LastValues.AssignDateTime = DateTime.Now;

                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    dispenser.LastValues = values;
                                    dispenser.LastValuesUpdate = DateTime.Now;
                                    if (values.ActiveNozzle >= 0 && (values.Status == Common.Enumerators.FuelPointStatusEnum.Idle || values.Status == Common.Enumerators.FuelPointStatusEnum.Offline))
                                    {
                                        values.ActiveNozzle = -1;
                                    }

                                    workFlow.SetValues(values);
                                    dispenser.LastValuesId = values.ID;
                                    dispenser.HasChanges = true;

                                    System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
                                    if (this.DispenserValuesAvaliable != null)
                                        this.DispenserValuesAvaliable(this, new DispenserValuesEventArgs(dispenser.DispenserId, values));
                                    sw.Stop();
                                }
                                workFlow.ValidateTransitions();
                                foreach (VirtualNozzle nz in workFlow.Dispenser.Nozzles)
                                {
                                    if (nz.StatusChanged)
                                    {
                                        if (this.UpdateNozzleStatus != null)
                                            this.UpdateNozzleStatus(this, new FuelPump.NozzleStatusChangedArgs()
                                            {
                                                NozzleId = nz.NozzleId,
                                                NozzleStatus = nz.Status
                                            });
                                        nz.StatusChanged = false;
                                    }
                                }
                                dispenser.UpdateUI = true;
                                dispenser.UpdateUI = false;
                            }
                        #endregion
                        }
                        //if (this.QueryResolveAlarm != null)
                        //{
                        //    this.QueryResolveAlarm(this, new QueryResolveAlarmArgs());
                        //}
                        System.Threading.Thread.Sleep(200);
                        if (this.RefreshAlerts != null)
                            this.RefreshAlerts(this, new EventArgs());
                    }
                    catch
                    {

                    }
                    finally
                    {
                        try
                        {
                            AlertHandler.Instance.CheckForAlerts();

                            //foreach (VirtualTank vt in tanks)
                            //    AlertHandler.Instance.CheckForTankAlerts(vt);

                            //foreach (VirtualDispenser vd in dispensers)
                            //    AlertHandler.Instance.CheckForDispenserAlerts(vd);

                            //if (this.RefreshAlerts != null)
                            //    this.RefreshAlerts(this, new EventArgs());

                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch(Exception ex)
            {

            }
            #endregion
            System.GC.Collect();
        }

        #region events

        void nozzle_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            VirtualNozzle nozzle = sender as VirtualNozzle;
            if (this.UpdateNozzleStatus != null)
                this.UpdateNozzleStatus(this, new FuelPump.NozzleStatusChangedArgs() { NozzleId = nozzle.NozzleId, NozzleStatus = nozzle.Status });
        }

        void workFlow_NozzleStatusChanged(object sender, FuelPump.NozzleStatusChangedArgs e)
        {
            if (this.UpdateNozzleStatus != null)
                this.UpdateNozzleStatus(this, e);
        }

        void workFlow_ProcessStateChanged(object sender, EventArgs e)
        {
            Tank.TankWorkFlow workFlow = sender as Tank.TankWorkFlow;
            if (workFlow.Process.CurrentState.Name == "Waiting" && workFlow.Process.PreviousState.Name != "Waiting")
            {
                if (this.QueryStartTimer != null)
                    this.QueryStartTimer(this, new TimerStartEventArgs(workFlow.CurrentWaitingTime));
            }
        }

        void workFlow_SaleCompleted(object sender, WorkFlow.SaleCompletedArgs e)
        {
            this.salesToProcess.Enqueue(e.Sale);
            if (SaleAvaliable != null)
                this.SaleAvaliable(this, e);
        }

        void workFlow_FillingCompleted(object sender, Tank.TankFillingEventArgs e)
        {
            this.fillingsToProcess.Enqueue(e.Data);
            if (this.TankFillingAvaliable != null)
                this.TankFillingAvaliable(this, new EventArgs());
        }

        void workFlow_QueryTotalsUpdate(object sender, FuelPump.QueryTotalsUpdateArgs e)
        {
            if (this.QueryTotalsUpdate != null)
                this.QueryTotalsUpdate(this, e);
        }

        void workFlow_QueryStationLocked(object sender, System.ComponentModel.CancelEventArgs e)
        {
            VirtualTank[] tanks = this.GetTanks();
            foreach (VirtualTank tank in tanks)
            {
                if (tank.TankStatus == Common.Enumerators.TankStatusEnum.Filling && !tank.IsLiterCheck)
                    e.Cancel = true;
                if (tank.TankStatus == Common.Enumerators.TankStatusEnum.FillingInit && !tank.IsLiterCheck)
                    e.Cancel = true;
                if (tank.TankStatus == Common.Enumerators.TankStatusEnum.FuelExtraction)
                    e.Cancel = true;
                if (tank.TankStatus == Common.Enumerators.TankStatusEnum.FuelExtractionInit)
                    e.Cancel = true;
                if (tank.TankStatus == Common.Enumerators.TankStatusEnum.Waiting && !tank.IsLiterCheck)
                    e.Cancel = true;
                if (e.Cancel)
                    return;
            }
        }

        #endregion
    }
    //public static class SerializeAnObject
    //{
    //    public static string SerializeObject<T>(this T toSerialize)
    //    {
    //        XmlSerializer xmlSerializer = new XmlSerializer(toSerialize.GetType());
    //        StringWriter textWriter = new StringWriter();

    //        xmlSerializer.Serialize(textWriter, toSerialize);
    //        return textWriter.ToString();
    //    }
    //}
    public class TankValuesEventArgs : EventArgs
    {
        public Guid TankId { set; get; }
        public Common.TankValues Values { set; get; }

        public TankValuesEventArgs(Guid tankId, Common.TankValues values)
        {
            this.TankId = tankId;
            this.Values = values;
        }
    }

    public class DispenserValuesEventArgs : EventArgs
    {
        public Guid DispenserId { set; get; }
        public Common.FuelPointValues Values { set; get; }

        public DispenserValuesEventArgs(Guid dispenserId, Common.FuelPointValues values)
        {   
            this.DispenserId = dispenserId;
            this.Values = values;
           
           
        }
    }

    public class PrintInvoiceArgs : EventArgs
    {
        public Guid InvoiceId { set; get; }
        public bool Competed { set; get; }
        public string SignString { set; get; }

        public PrintInvoiceArgs(Guid id, string signString)
        {
            this.InvoiceId = id;
            this.SignString = signString;
        }
    }

    public class TimerStartEventArgs : EventArgs
    {
        public TimeSpan WaitingTime { set; get; }

        public TimerStartEventArgs(TimeSpan ts)
        {
            this.WaitingTime = ts;
        }
    }

    internal class DispenserControllerClass
    {
        public VirtualDispenser Dispenser { set; get; }
        public Common.IController Controller { set; get; }
    }
}

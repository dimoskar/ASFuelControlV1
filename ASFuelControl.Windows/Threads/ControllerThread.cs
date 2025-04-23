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

        public event EventHandler<WorkFlow.AlarmRaisedEventArgs> AlarmRaised;

        public event EventHandler CheckInvoices;

        public event EventHandler ControllerConnectionFailed;

        public event EventHandler ControllerConnectionSuccess;

        public event EventHandler<DispenserValuesEventArgs> DispenserValuesAvaliable;

        public event EventHandler OTPStatusChanged;

        public event EventHandler PrintAlarms;

        public event EventHandler<PrintInvoiceArgs> PrintInvoice;

        public event EventHandler<WorkFlow.QueryAlarmResolvedArgs> QueryAlarmResolved;

        public event EventHandler QueryPrices;

        public event EventHandler<QueryResolveAlarmArgs> QueryResolveAlarm;

        public event EventHandler<TimerStartEventArgs> QueryStartTimer;

        public event EventHandler<FuelPump.QueryTotalsUpdateArgs> QueryTotalsUpdate;

        //public event EventHandler RefreshAlerts;
        public event EventHandler ReadFleetData;

        public event EventHandler<ASFuelControl.WorkFlow.SaleCompletedArgs> SaleAvaliable;
        public event EventHandler TankFillingAvaliable;
        public event EventHandler<TankValuesEventArgs> TankValuesAvaliable;
        public event EventHandler<FuelPump.NozzleStatusChangedArgs> UpdateNozzleStatus;

        //public event EventHandler UpdateNozzleValues;
        #endregion

        #region private variables
        private string defaultTaxDevice = "";
        ConcurrentQueue<Common.Sales.TankFillingData> fillingsToProcess = new ConcurrentQueue<Common.Sales.TankFillingData>();
        private List<ASFuelControl.WorkFlow.IFuelPumpWorkFlow> fpWorkFlows = new List<ASFuelControl.WorkFlow.IFuelPumpWorkFlow>();
        private bool haltThread = false;
        private DateTime lastTankCheck = DateTime.Now.AddMinutes(-1000);
        List<OTPConsoleController> OTPConsoles = new List<OTPConsoleController>();
        private string outFolder = "";
        ConcurrentDictionary<Guid, Common.Sales.SaleData> salesToProcess = new ConcurrentDictionary<Guid, Common.Sales.SaleData>();
        private string signFolder = "";
        private List<Tank.TankWorkFlow> tankWorkFlows = new List<Tank.TankWorkFlow>();
        private System.Threading.Thread th;
        List<System.Threading.Thread> threads = new List<System.Threading.Thread>();
        #endregion

        #region public properties

        /// <summary>
        /// Flag to signal tha the threads should be stopped
        /// </summary>
        public bool HaltThread
        {
            get { return this.haltThread; }
            set { this.haltThread = value; }
        }

        public bool OTPStatus
        {
            get
            {
                foreach(OTPConsoleController otp in this.OTPConsoles)
                {
                    if (otp.OPTStatus)
                        return true;
                }
                return false;
            }
        }

        //public Common.IController Controller { set; get; }
        /// <summary>
        /// Flag for signal a price change made from user
        /// </summary>
        public bool PriceChanged { set; get; }

        /// <summary>
        /// Interval for tankcheck 
        /// </summary>
        public int TankCheckInterval { set; get; }
        #endregion

        #region Public Constructors

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

        #endregion Public Constructors

        #region public methods

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
            if (controller.GetType() == typeof(ASFuelControl.EMR3.EmrController))
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
        public VirtualDevices.VirtualNozzle AddNozzle(Guid dispenserId, Guid nozzleId, int orderIndex, int number, int index, int status, int socket)
        {
            VirtualDevices.VirtualDispenser dispenser = this.fpWorkFlows.Select(f => f.Dispenser).Where(d => d.DispenserId == dispenserId).FirstOrDefault();
            if (dispenser == null)
                return null;
            List<VirtualNozzle> nozzles = new List<VirtualNozzle>(dispenser.Nozzles);
            VirtualNozzle nozzle = new VirtualNozzle();
            nozzle.NozzleNumber = orderIndex;
            nozzle.NozzleOfficialNumber = number;
            nozzle.NozzleId = nozzleId;
            nozzle.NozzleIndex = orderIndex;
            nozzle.NozzleSocket = socket;
            nozzle.Status = (Common.Enumerators.NozzleStateEnum)status;
            nozzles.Add(nozzle);
            dispenser.Nozzles = nozzles.ToArray();
            nozzle.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(nozzle_PropertyChanged);
            return nozzle;
        }

        public void AddOTPConsole(int vendingMachineId, string servip, int servport, string clip, int clport)
        {
            OTPConsoleController otpCon = new OTPConsoleController(servip, servport, clip, clport) { VendingMachineId = vendingMachineId };
            this.OTPConsoles.Add(otpCon);
            otpCon.OTPStatusChanged += OtpCon_OTPStatusChanged;


        }

        public void AddOTPDispenser(int vendingMachineId, VirtualDispenser dispenser, Guid[] nozzles)
        {
            OTPConsoleController otp = this.OTPConsoles.Where(c => c.VendingMachineId == vendingMachineId).FirstOrDefault();
            if (otp == null)
                return;
            otp.AddDispenser(dispenser, nozzles);
        }

        /// <summary>
        /// Add a sale to the salesToProcess Queue
        /// </summary>
        /// <param name="sale"></param>
        public void AddSale(Common.Sales.SaleData sale)
        {
            this.salesToProcess.TryAdd(sale.SalesId, sale);
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

        // VirtualNozzle vNozzle = this.controllerThread.AddNozzle(dispenser.DispenserId, nozzle.NozzleId, nozzle.OrderId, nozzle.OfficialNozzleNumber, nozzle.NozzleIndex.Value, nozzle.NozzleState);
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

        public void ClearOTPConsoles()
        {
            foreach (OTPConsoleController otp in this.OTPConsoles)
            {
                otp.CloseServer();
            }
            this.OTPConsoles.Clear();
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
        /// Returns all Virtual Dispenser of the System
        /// </summary>
        /// <returns></returns>
        public VirtualDispenser[] GetDispensers()
        {
            return this.fpWorkFlows.Select(f => f.Dispenser).OrderBy(d => d.DispenserNumber).ToArray();
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
        /// Gets the next sale avaliable in the salesToProcess Queue
        /// </summary>
        /// <returns></returns>
        public Common.Sales.SaleData GetNextSale()
        {
            if (this.salesToProcess.Count == 0)
                return null;
            Common.Sales.SaleData sale = this.salesToProcess.Values.FirstOrDefault();
            if (sale == null)
                return null; 
            if (this.salesToProcess.TryGetValue(sale.SalesId, out sale))
            {
                return sale;
            }
            return null;
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

        public OTPConsoleController[] GetOTPControlers()
        {
            return this.OTPConsoles.ToArray();
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
        /// Returns all Virtual Tanks of the System
        /// </summary>
        /// <returns></returns>
        public VirtualTank[] GetTanks()
        {
            return this.tankWorkFlows.Select(t => t.Tank).OrderBy(t => t.TankNumber).ToArray();
        }

        /// <summary>
        /// Removes a sale from the salesToProcess Queue
        /// </summary>
        public void RemoveSale(Guid saleId)
        {
            Common.Sales.SaleData sale = null;
            this.salesToProcess.TryRemove(saleId, out sale);
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

        private void OtpCon_OTPStatusChanged(object sender, EventArgs e)
        {
            if (this.OTPStatusChanged != null)
                this.OTPStatusChanged(this, new EventArgs());
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

        #region Private Methods

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
                    for(int i=0; i < dispClass.Dispenser.Nozzles.Length; i++)
                    {
                        VirtualNozzle nz = dispClass.Dispenser.Nozzles[i];
                        nz.TotalVolumeCounter = dispClass.Controller.GetNozzleTotalizer(dispClass.Dispenser.ChannelId, dispClass.Dispenser.AddressId, i + 1);
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

                foreach (ASFuelControl.Tank.TankWorkFlow workFlow in this.tankWorkFlows)
                {
                    workFlow.IsInitialized = true;
                    workFlow.Process.IsInitialized = true;
                    if (workFlow.Controller != null)
                        workFlow.Controller.AddAtg(workFlow.Tank.ChannelId, workFlow.Tank.AddressId);

                    workFlow.Tank.LastValuesUpdate = DateTime.Now;// DateTime.Now.Subtract(TimeSpan.FromSeconds(29));
                    workFlow.ProcessStateChanged -= new EventHandler(workFlow_ProcessStateChanged);
                    workFlow.ProcessStateChanged += new EventHandler(workFlow_ProcessStateChanged);
                    if (workFlow.Tank.CurrentFuelLevel >= workFlow.Tank.MinHeight && workFlow.Tank.LastFuelHeight == 0 && workFlow.Tank.FuelTypeDescription != "" && workFlow.Tank.PriceAverage == 0)
                    {
                        Common.Sales.TankFillingData tdata = new Common.Sales.TankFillingData();
                        tdata.TankId = workFlow.Tank.TankId;
                        Common.TankValues tvalues = new Common.TankValues();

                        tvalues.FuelHeight = workFlow.Tank.CurrentFuelLevel;
                        tvalues.CurrentTemperatur = workFlow.Tank.CurrentTemperature;
                        tvalues.WaterHeight = workFlow.Tank.CurrentWaterLevel;
                        tdata.StartValues = new Common.TankValues() { FuelHeight = workFlow.Tank.FillingStartTankLevel };
                        tdata.EndValues = tvalues;
                        this.fillingsToProcess.Enqueue(tdata);
                        //LOGME
                        //System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
                        if (this.TankFillingAvaliable != null)
                            this.TankFillingAvaliable(this, new EventArgs());
                        //sw.Stop();
                        //TimeSpan elapsedTime = sw.Elapsed;
                        //if(elapsedTime.Seconds > 4)
                        //{
                        //    System.IO.File.AppendAllText(System.Environment.CurrentDirectory + "\\ThreadController.txt", string.Format("\n{1} this.TankFillingAvaliable(this, new EventArgs()); Start/Stop Elapsed time {0}", elapsedTime.ToString(), System.DateTime.Now));
                        //}
                    }
                }
                Common.EuromatThread.Instance.ClearFuelPumps();
                foreach (ASFuelControl.FuelPump.FuelPumpWorkFlow workFlow in this.fpWorkFlows)
                {
                    workFlow.IsInitialized = true;
                    workFlow.Process.IsInitialized = true;
                    if (workFlow.Controller != null && workFlow.Dispenser.IsValid)
                    {
                        workFlow.Controller.AddFuelPoint(workFlow.Dispenser.ChannelId, workFlow.Dispenser.AddressId, workFlow.Dispenser.Nozzles.Length, workFlow.Dispenser.DecimalPlaces,
                            workFlow.Dispenser.VolumeDecimalPlaces, workFlow.Dispenser.UnitPriceDecimalPlaces, workFlow.Dispenser.TotalDecimalPlaces);


                        if (workFlow.Controller.EuromatEnabled && workFlow.Dispenser.EuromatNumber > 0)
                        {
                            workFlow.Controller.SetEuromatDispenserNumber(workFlow.Dispenser.ChannelId, workFlow.Dispenser.AddressId, workFlow.Dispenser.EuromatNumber, workFlow.Controller.EuromatIp, workFlow.Controller.EuromatPort);
                            //Common.EuromatThread.Instance.InitilizePump(workFlow.Controller, workFlow.Dispenser.ChannelId, workFlow.Dispenser.AddressId, workFlow.Dispenser.EuromatNumber);
                        }
                        var q = workFlow.Dispenser.Nozzles.OrderBy(n => n.NozzleNumber).ToArray();

                        for(int i=1; i  <= q.Length; i++)
                        {
                            VirtualNozzle nz = q[i - 1];
                            workFlow.Controller.SetNozzleIndex(workFlow.Dispenser.ChannelId, workFlow.Dispenser.AddressId, i, nz.NozzleIndex);
                            workFlow.Controller.SetNozzleSocket(workFlow.Dispenser.ChannelId, workFlow.Dispenser.AddressId, i, nz.NozzleSocket);
                            workFlow.Controller.SetNozzlePrice(workFlow.Dispenser.ChannelId, workFlow.Dispenser.AddressId, i, nz.CurrentSaleUnitPrice);
                        }
                        //foreach (VirtualNozzle nz in q)
                        //{
                        //    workFlow.Controller.SetNozzleIndex(workFlow.Dispenser.ChannelId, workFlow.Dispenser.AddressId, nz.NozzleNumber, nz.NozzleIndex);
                        //    workFlow.Controller.SetNozzlePrice(workFlow.Dispenser.ChannelId, workFlow.Dispenser.AddressId, nz.NozzleNumber, nz.CurrentSaleUnitPrice);
                        //}
                        workFlow.QueryFleetData -= new EventHandler(workFlow_QueryFleetData);
                        workFlow.QueryFleetData += new EventHandler(workFlow_QueryFleetData);

                    }
                }
                //List<Common.IController> controllers = this.fpWorkFlows.Select(f => f.Controller).Union(this.tankWorkFlows.Select(t => t.Controller)).Distinct().ToList();
                foreach (Common.IController controller in controllers)
                {
                    try
                    {
                        controller.Connect();
                    }
                    catch (Exception ex)
                    {
                        Common.Logger.Instance.Error("Could not connect to Controller in Port: {value1}", controller.CommunicationPort, "", 2);
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

                //AlertHandler.Instance.Tanks = this.GetTanks();
                //AlertHandler.Instance.Dispensers = this.GetDispensers();
                //AlertHandler.Instance.WireUpEvents();
                //AlertHandler.Instance.AddAlertsFromDatabase();
                int igc = 0;
                int alertIndex = 0;
                while (!haltThread)
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
                                Logging.Logger.Instance.LogToGivenFile("ThreadFlow.log", "Tank: " + tank.SerialNumber + "  Start");
                                //AlertHandler.CheckForTankAlerts(tank);
                                if (workFlow.Tank.IsVirtualTank)
                                {
                                    if (DateTime.Now.Subtract(tank.LastValuesUpdate).TotalSeconds > 1)
                                    {
                                        Common.TankValues values = new Common.TankValues();
                                        values.FuelHeight = workFlow.Tank.CurrentFuelLevel - workFlow.Tank.FuelOffset;
                                        values.CurrentTemperatur = workFlow.Tank.CurrentTemperature;
                                        values.WaterHeight = workFlow.Tank.CurrentWaterLevel;
                                        tank.TankValues = values;
                                        tank.LastValuesUpdate = DateTime.Now;
                                        //if (this.TankValuesAvaliable != null)
                                        //    this.TankValuesAvaliable(this, new TankValuesEventArgs(tank.TankId, values));
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
                                        Logging.Logger.Instance.LogToGivenFile("ThreadFlow.log", "Tank: " + tank.SerialNumber + "  End");
                                        continue;
                                    }
                                    Common.TankValues values = controller.GetTankValues(tank.ChannelId, tank.AddressId);
                                    if (values == null)
                                    {

                                        if (DateTime.Now.Subtract(tank.LastValuesUpdate).TotalSeconds > 10)
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
                                        {
                                            if (tank.PreviousStatus != Common.Enumerators.TankStatusEnum.Offline)
                                                tank.TankStatus = tank.PreviousStatus;// Common.Enumerators.TankStatusEnum.Idle;
                                            else
                                                tank.TankStatus = Common.Enumerators.TankStatusEnum.Idle;

                                        }
                                        if (this.TankValuesAvaliable != null)
                                            this.TankValuesAvaliable(this, new TankValuesEventArgs(tank.TankId, values));
                                        workFlow.SetValues(values);
                                    }

                                }
                                workFlow.ValidateTransitions();

                                tank.HasChanges = false;
                                Logging.Logger.Instance.LogToGivenFile("ThreadFlow.log", "Tank: " + tank.SerialNumber + "  End");
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
                                Logging.Logger.Instance.LogToGivenFile("ThreadFlow.log", "Dispenser: " + dispenser.SerialNumber + "  Start");
                                if (!dispenser.IsValid)
                                {
                                    Logging.Logger.Instance.LogToGivenFile("ThreadFlow.log", "Dispenser: " + dispenser.SerialNumber + "  End");
                                    continue;
                                }
                                if (!controller.IsConnected)
                                {
                                    //if (this.salesToProcess.Count > 0)
                                    //{
                                    //    for (int i = 0; i < this.salesToProcess.Count; i++)
                                    //    {
                                    //        if (this.SaleAvaliable != null)
                                    //            this.SaleAvaliable(this, new WorkFlow.SaleCompletedArgs(new Common.Sales.SaleData()));
                                    //    }
                                    //}
                                }
                                Common.Sales.SaleData manualSale = this.GetNextSale();
                                if(manualSale != null)
                                {
                                    if (!manualSale.SaleProcessed)
                                    {
                                        if (this.SaleAvaliable != null)
                                            this.SaleAvaliable(this, new WorkFlow.SaleCompletedArgs(manualSale));
                                        manualSale.SaleProcessed = true;
                                    }
                                    this.RemoveSale(manualSale.SalesId);
                                }
                                workFlow.CurrentShiftId = Program.CurrentShiftId;

                                if (dispenser.Reset)
                                {
                                    controller.SetDispenserStatus(dispenser.ChannelId, dispenser.AddressId, Common.Enumerators.FuelPointStatusEnum.Offline);
                                    dispenser.Reset = false;
                                }
                                bool otpEnabled = this.OTPStatus;// Data.Implementation.OptionHandler.Instance.GetBoolOption("[OTPEnabled]", false);
                                if (otpEnabled)
                                {
                                    controller.SetOtpEnabled(dispenser.ChannelId, dispenser.AddressId, true);
                                    foreach (OTPConsoleController otp in this.OTPConsoles)
                                    {
                                        if (dispenser.CurrentVendingMachineSale == null)
                                        {
                                            otp.GetNextSale(dispenser);
                                            if (dispenser.CurrentVendingMachineSale != null)
                                            {
                                                controller.SetPresetAmount(dispenser.ChannelId, dispenser.AddressId, dispenser.CurrentVendingMachineSale.PresetAmount);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    controller.SetOtpEnabled(dispenser.ChannelId, dispenser.AddressId, false);
                                    dispenser.CurrentVendingMachineSale = null;
                                    controller.SetPresetAmount(dispenser.ChannelId, dispenser.AddressId, -1);
                                }
                                
                                //if (dispenser.DeviceLocked && dispenser.Status == Common.Enumerators.FuelPointStatusEnum.Work)
                                //    controller.HaltDispenser(dispenser.ChannelId, dispenser.AddressId);

                                //if (this.salesToProcess.Count > 0)
                                //{
                                //    for (int i = 0; i < this.salesToProcess.Count; i++)
                                //    {
                                //        if (this.SaleAvaliable != null)
                                //            this.SaleAvaliable(this, new WorkFlow.SaleCompletedArgs(new Common.Sales.SaleData()));
                                //    }
                                //}
                                //workFlow.CheckOpenSales();
                                Common.FuelPointValues values = controller.GetDispenserValues(dispenser.ChannelId, dispenser.AddressId);
                                
                                if (values != null && values.Sale != null )
                                {
                                    Logger.Instance.LogToFile("Values With Sale:" + controller.CommunicationPort, values);
                                    Logger.Instance.LogToFile("Controller Thread Sale", values.Sale);
                                    if (System.IO.File.Exists("ControllerThread.log"))
                                    {
                                        System.IO.File.AppendAllText("ControllerThread.log", "Values found");
                                    }
                                    if (values.Sale.TotalVolume <= 0)
                                    {
                                        if (values.Sale.DisplayVolume > 0)
                                        {
                                            System.IO.File.AppendAllText("EmptySales.log", string.Format("Display:{0} TotalStart:{1} TotalStart:{2}\r\n", values.Sale.DisplayVolume, values.Sale.TotalizerStart, values.Sale.TotalizerEnd));
                                        }
                                    }
                                    else
                                    {
                                        Common.Sales.SaleData sale = values.Sale;

                                        VirtualNozzle saleNozzle = dispenser.Nozzles.Where(n => n.NozzleNumber == values.ActiveNozzle).FirstOrDefault();
                                        if (saleNozzle != null)
                                        {
                                            saleNozzle.TotalVolumeCounter = sale.TotalizerEnd;
                                            //sale.EuromatParameters = controller.GetEuromatParameters(dispenser.ChannelId, dispenser.AddressId);
                                            sale.EuromatTransaction = controller.GetEuromatTransaction(dispenser.ChannelId, dispenser.AddressId);
                                            sale.NozzleId = saleNozzle.NozzleId;
                                            sale.EuromatPumpNumber = dispenser.EuromatNumber;
                                            //sale.CheckSale(dispenser.TotalDecimalPlaces);
                                            saleNozzle.HasChanges = true;
                                            if (this.QueryTotalsUpdate != null && sale.TotalVolume > 0)
                                            {
                                                this.QueryTotalsUpdate(this, new FuelPump.QueryTotalsUpdateArgs()
                                                {
                                                    NozzleId = saleNozzle.NozzleId,
                                                    TotalVolumeCounter = sale.TotalizerEnd
                                                });
                                            }
                                        }
                                        workFlow.SetSale(values.Sale);
                                        workFlow.CloseSale(values.Sale);
                                        
                                        //Logger.Instance.LogToFile()
                                        if (dispenser.CurrentVendingMachineSale != null)
                                        {
                                            foreach (OTPConsoleController otp in this.OTPConsoles)
                                            {
                                                otp.TranactionCompleted(saleNozzle, values.Sale.TotalPrice, values.Sale.TotalVolume);
                                            }
                                        }
                                    }
                                }
                                if (values == null || values.Sale == null || values.Sale.TotalVolume == 0)
                                {
                                    for (int i = 0; i < dispenser.Nozzles.Length; i++)
                                    {
                                        VirtualNozzle nz = dispenser.Nozzles[i];
                                        decimal oldValue = nz.TotalVolumeCounter;
                                        try
                                        {
                                            nz.TotalVolumeCounter = workFlow.Controller.GetNozzleTotalVolume(nz.ParentDispenser.ChannelId, nz.ParentDispenser.AddressId, i + 1);
                                        }
                                        catch(Exception ex)
                                        {
                                            string caption = string.Format("Channel {0}, Address {1}, NozzleNumber {2}", nz.ParentDispenser.ChannelId, nz.ParentDispenser.AddressId, i);
                                            Logger.Instance.LogToFile(caption, ex);
                                        }
                                        if(oldValue != nz.TotalVolumeCounter && nz.TotalVolumeCounter > 0)
                                        {
                                            if (this.QueryTotalsUpdate != null)
                                            {
                                                this.QueryTotalsUpdate(this, new FuelPump.QueryTotalsUpdateArgs()
                                                {
                                                    NozzleId = nz.NozzleId,
                                                    TotalVolumeCounter = nz.TotalVolumeCounter
                                                });
                                            }
                                        }
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
                                    //if(values.Sale == null)
                                    //    Logger.Instance.LogToFile("Values With No Sale:" + controller.CommunicationPort, values);
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
                                Logging.Logger.Instance.LogToGivenFile("ThreadFlow.log", "Dispenser: " + dispenser.SerialNumber + "  End");
                            }
                            #endregion
                        }
                        //if (this.QueryResolveAlarm != null)
                        //{
                        //    this.QueryResolveAlarm(this, new QueryResolveAlarmArgs());
                        //}
                        System.Threading.Thread.Sleep(300);
                        //if (this.RefreshAlerts != null)
                        //    this.RefreshAlerts(this, new EventArgs());
                    }
                    catch (Exception ex1)
                    {
                        Logging.Logger.Instance.LogToFile("Controller Exception", ex1);
                    }
                    finally
                    {
                        try
                        {
                            int modulus = Properties.Settings.Default.AlertIndexModulus > 0 ? Properties.Settings.Default.AlertIndexModulus : 5;
                            if (alertIndex % modulus == 0)
                            {
                                Logging.Logger.Instance.LogToGivenFile("ThreadFlow.log", "Check Alert Start");
                                AlertChecker.Instance.CheckForAlerts();
                                Logging.Logger.Instance.LogToGivenFile("ThreadFlow.log", "Check Alert End");
                                alertIndex = 0;
                            }
                            alertIndex++;
                            //if(AlertChecker.Instance.HasNozzleAlerts)
                            //{

                            //}
                            ////foreach (VirtualTank vt in tanks)
                            ////    AlertHandler.Instance.CheckForTankAlerts(vt);

                            ////foreach (VirtualDispenser vd in dispensers)
                            ////    AlertHandler.Instance.CheckForDispenserAlerts(vd);

                            ////if (this.RefreshAlerts != null)
                            ////    this.RefreshAlerts(this, new EventArgs());

                        }
                        catch (Exception ex2)
                        {
                            Logging.Logger.Instance.LogToFile("CheckForAlerts Exception", ex2);
                        }
                        igc++;
                        if (igc >= 100)
                        {
                            igc = 0;
                            try
                            {
                                System.GC.Collect();
                            }
                            catch (Exception ex3)
                            {
                                Logging.Logger.Instance.LogToFile("CheckForAlerts Exception", ex3);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Logger.Instance.LogToFile("ControllerThread::", ex);
            }
            finally
            {
            }
            #endregion
            try
            {
                System.GC.Collect();
            }
            catch
            {
            }
        }

        void workFlow_QueryFleetData(object sender, EventArgs e)
        {
            if (this.ReadFleetData != null)
                this.ReadFleetData(sender, new EventArgs());
        }

        #endregion Private Methods

        #region events

        void nozzle_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            VirtualNozzle nozzle = sender as VirtualNozzle;
            if (this.UpdateNozzleStatus != null)
                this.UpdateNozzleStatus(this, new FuelPump.NozzleStatusChangedArgs() { NozzleId = nozzle.NozzleId, NozzleStatus = nozzle.Status });
        }

        void workFlow_FillingCompleted(object sender, Tank.TankFillingEventArgs e)
        {
            this.fillingsToProcess.Enqueue(e.Data);
            if (this.TankFillingAvaliable != null)
                this.TankFillingAvaliable(this, new EventArgs());
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

        void workFlow_QueryTotalsUpdate(object sender, FuelPump.QueryTotalsUpdateArgs e)
        {
            if (this.QueryTotalsUpdate != null)
                this.QueryTotalsUpdate(this, e);
        }

        void workFlow_SaleCompleted(object sender, WorkFlow.SaleCompletedArgs e)
        {
            //this.salesToProcess.Enqueue(e.Sale);
            if (SaleAvaliable != null)
            {
                this.SaleAvaliable(this, e);
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

    public class DispenserControllerClass
    {
        #region Public Properties

        public Common.IController Controller { set; get; }
        public VirtualDispenser Dispenser { set; get; }

        #endregion Public Properties
    }

    public class DispenserValuesEventArgs : EventArgs
    {
        #region Public Constructors

        public DispenserValuesEventArgs(Guid dispenserId, Common.FuelPointValues values)
        {
            this.DispenserId = dispenserId;
            this.Values = values;


        }

        #endregion Public Constructors

        #region Public Properties

        public Guid DispenserId { set; get; }
        public Common.FuelPointValues Values { set; get; }

        #endregion Public Properties
    }

    public class PrintInvoiceArgs : EventArgs
    {
        #region Public Constructors

        public PrintInvoiceArgs(Guid id, string signString)
        {
            this.InvoiceId = id;
            this.SignString = signString;
        }

        #endregion Public Constructors

        #region Public Properties

        public bool Competed { set; get; }
        public Guid InvoiceId { set; get; }
        public string SignString { set; get; }

        #endregion Public Properties
    }

    //        xmlSerializer.Serialize(textWriter, toSerialize);
    //        return textWriter.ToString();
    //    }
    //}
    public class TankValuesEventArgs : EventArgs
    {
        #region Public Constructors

        public TankValuesEventArgs(Guid tankId, Common.TankValues values)
        {
            this.TankId = tankId;
            this.Values = values;
        }

        #endregion Public Constructors

        #region Public Properties

        public Guid TankId { set; get; }
        public Common.TankValues Values { set; get; }

        #endregion Public Properties
    }
    public class TimerStartEventArgs : EventArgs
    {
        #region Public Constructors

        public TimerStartEventArgs(TimeSpan ts)
        {
            this.WaitingTime = ts;
        }

        #endregion Public Constructors

        #region Public Properties

        public TimeSpan WaitingTime { set; get; }

        #endregion Public Properties
    }
}

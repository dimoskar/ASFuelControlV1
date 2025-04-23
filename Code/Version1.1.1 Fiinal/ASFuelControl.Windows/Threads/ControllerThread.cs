using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASFuelControl.VirtualDevices;
using System.Collections.Concurrent;
using ASFuelControl.Logging;

namespace ASFuelControl.Windows.Threads
{
    public class ControllerThread
    {
        #region events

        public event EventHandler<ASFuelControl.WorkFlow.SaleCompletedArgs> SaleAvaliable;
        public event EventHandler TankFillingAvaliable;
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

        public int TankCheckInterval { set; get; }

        public Common.IController Controller { set; get; }

        public bool HaltThread 
        {
            get { return this.haltThread; }
            set { this.haltThread = value; }
        }

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

        public Common.Sales.SaleData GetNextSale()
        {
            if (this.salesToProcess.Count == 0)
                return null;
            Common.Sales.SaleData sale = null;
            if (this.salesToProcess.TryPeek(out sale))
                return sale;
            return null;
        }

        public void AddSale(Common.Sales.SaleData sale)
        {
            this.salesToProcess.Enqueue(sale);
        }

        public void RenoveSale()
        {
            Common.Sales.SaleData sale;
            this.salesToProcess.TryDequeue(out sale);
        }

        public Common.Sales.TankFillingData GetNextFilling()
        {
            if (this.fillingsToProcess.Count == 0)
                return null;
            Common.Sales.TankFillingData filling = null;
            if (this.fillingsToProcess.TryDequeue(out filling))
                return filling;
            return null;
        }

        public VirtualDevices.VirtualDispenser AddDispenserWorkFlow(Common.IController controller, Guid dispenserId, int channel, int address, bool suspendAlarms)
        {
            VirtualDevices.VirtualDispenser dispenser = new VirtualDispenser();
            dispenser.ChannelId = channel;
            dispenser.AddressId = address;
            dispenser.DispenserId = dispenserId;
            FuelPump.FuelPumpWorkFlow workFlow = new FuelPump.FuelPumpWorkFlow(dispenser);
            workFlow.Controller = controller;
            workFlow.SuspendAlarms = suspendAlarms;
            workFlow.AlarmRaised+=new EventHandler<WorkFlow.AlarmRaisedEventArgs>(workFlow_AlarmRaised);
            workFlow.SaleCompleted += new EventHandler<WorkFlow.SaleCompletedArgs>(workFlow_SaleCompleted);
            workFlow.QueryTotalsUpdate += new EventHandler<FuelPump.QueryTotalsUpdateArgs>(workFlow_QueryTotalsUpdate);
            workFlow.NozzleStatusChanged += new EventHandler<FuelPump.NozzleStatusChangedArgs>(workFlow_NozzleStatusChanged);
            workFlow.QueryStationLocked += new System.ComponentModel.CancelEventHandler(workFlow_QueryStationLocked);
            this.fpWorkFlows.Add(workFlow);

            return dispenser;
        }

        public VirtualDevices.VirtualNozzle AddNozzle(Guid dispenserId, Guid nozzleId, int orderIndex, int number, int status)
        {
            VirtualDevices.VirtualDispenser dispenser = this.fpWorkFlows.Select(f => f.Dispenser).Where(d => d.DispenserId == dispenserId).FirstOrDefault();
            if (dispenser == null)
                return null;
            List<VirtualNozzle> nozzles = new List<VirtualNozzle>(dispenser.Nozzles);
            VirtualNozzle nozzle = new VirtualNozzle();
            nozzle.NozzleNumber = orderIndex;
            nozzle.NozzleOfficialNumber = number;
            nozzle.NozzleId = nozzleId;
            nozzle.Status = (Common.Enumerators.NozzleStateEnum)status;
            nozzles.Add(nozzle);
            dispenser.Nozzles = nozzles.ToArray();
            nozzle.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(nozzle_PropertyChanged);
            return nozzle;
        }

        public VirtualDevices.VirtualTank AddTankWorkFlow(Common.IController controller, Guid tankId, int channel, int address, bool suspendAlarms)
        {
            VirtualDevices.VirtualTank tank = new VirtualTank();
            tank.ChannelId = channel;
            tank.AddressId = address;
            tank.TankId = tankId;
            Tank.TankWorkFlow workFlow = new Tank.TankWorkFlow(tank);
            workFlow.SuspendAlarms = suspendAlarms;
            workFlow.FillingCompleted += new EventHandler<Tank.TankFillingEventArgs>(workFlow_FillingCompleted);
            workFlow.AlarmRaised += new EventHandler<WorkFlow.AlarmRaisedEventArgs>(workFlow_AlarmRaised);
            workFlow.Controller = controller;
            workFlow.QueryAlarmResolved += new EventHandler<WorkFlow.QueryAlarmResolvedArgs>(workFlow_QueryAlarmResolved);
            this.tankWorkFlows.Add(workFlow);
            return tank;
        }

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

        public VirtualTank[] GetTanks()
        {
            return this.tankWorkFlows.Select(t => t.Tank).OrderBy(t => t.TankNumber).ToArray();
        }

        public VirtualDispenser[] GetDispensers()
        {
            return this.fpWorkFlows.Select(f => f.Dispenser).OrderBy(d => d.DispenserNumber).ToArray();
        }

        public VirtualTank GetTank(Guid tankId)
        {
            return this.tankWorkFlows.Select(t => t.Tank).Where(t => t.TankId == tankId).FirstOrDefault();
        }

        public VirtualNozzle GetNozzle(Guid nozzleId)
        {
            return this.fpWorkFlows.SelectMany(t => t.Dispenser.Nozzles).Where(n => n.NozzleId == nozzleId).FirstOrDefault();
        }

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

        public void StopControllers()
        {
            List<Common.IController> controllers = this.fpWorkFlows.Select(f => f.Controller).Union(this.tankWorkFlows.Select(t => t.Controller)).Distinct().ToList();
            foreach (Common.IController controller in controllers)
            {
                if(controller == null)
                    continue;
                controller.DisConnect();
            }
            
            haltThread = true;
        }

        #endregion

        private void CheckForInvoices()
        {
            try
            {
                if (outFolder != null && outFolder != "")
                {
                    System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(this.outFolder);
                    if (!dir.Exists)
                        return;
                    System.IO.FileInfo[] files = dir.GetFiles("*.out");
                    foreach (System.IO.FileInfo file in files)
                    {
                        string id = file.Name.Replace(".out", "");

                        Guid invoiceId;
                        if (!Guid.TryParse(id, out invoiceId))
                        {
                            string originalFile = file.Name.Replace(".out", ".txt");
                            string str = System.IO.File.ReadAllText(file.FullName);
                            int signIndex = str.IndexOf(",");
                            if (signIndex < 0)
                                return;

                            str = str.Substring(7, signIndex - 6);

                            originalFile = this.signFolder + "\\" + originalFile;
                            System.IO.FileInfo origFileInfo = new System.IO.FileInfo(originalFile);
                            if (!origFileInfo.Exists)
                                continue;

                            System.IO.File.AppendAllText(originalFile, "\r\n" + str);
                            System.IO.DirectoryInfo dirInfo = new System.IO.DirectoryInfo(dir.Parent.Parent.FullName + "\\SingnedDocuments");
                            if (!dirInfo.Exists)
                                dirInfo.Create();
                            string newFileName = dirInfo.FullName + "\\" + file.Name.Replace(".out", ".txt");
                            if(System.IO.File.Exists(newFileName))
                                continue;
                            origFileInfo.MoveTo(newFileName);
                            file.Delete();
                            continue;
                        }
                        if (this.PrintInvoice != null)
                        {
                            string str = System.IO.File.ReadAllText(file.FullName);
                            int signIndex = str.IndexOf(",");
                            if (signIndex < 0)
                                return;

                            string signStr = str.Substring(7, signIndex - 6);
                            if (signStr == null || signStr == "")
                            {
                                
                                signStr = str;
                            }
                            PrintInvoiceArgs args = new PrintInvoiceArgs(invoiceId, signStr);
                            this.PrintInvoice(this, args);
                            if (args.Competed)
                                file.Delete();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message, "Σφάλμα Εκτυπώσεων");
            }
        }

        private void CheckForAlarms()
        {
            if (outFolder != null && outFolder != "")
            {
                System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(this.outFolder);
                if (!dir.Exists)
                    return;

            }
        }

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

        private void InitializeThread(object parameter)
        {
            DispenserControllerClass dispClass = parameter as DispenserControllerClass;
            
            bool initialized = false;
            while (!initialized && !haltThread)
            {
                if (!dispClass.Controller.IsConnected)
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

        private void ThreadStart()
        {
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

                    workFlow.Tank.LastValuesUpdate = DateTime.Now.Subtract(TimeSpan.FromSeconds(29));
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
                        if(this.TankFillingAvaliable != null)
                            this.TankFillingAvaliable(this, new EventArgs());
                    }
                }
                foreach(ASFuelControl.FuelPump.FuelPumpWorkFlow workFlow in this.fpWorkFlows)
                {
                    workFlow.IsInitialized = true;
                    workFlow.Process.IsInitialized = true;
                    if(workFlow.Controller != null && workFlow.Dispenser.IsValid)
                    {
                        workFlow.Controller.AddFuelPoint(workFlow.Dispenser.ChannelId, workFlow.Dispenser.AddressId, workFlow.Dispenser.Nozzles.Length, workFlow.Dispenser.DecimalPlaces, workFlow.Dispenser.UnitPriceDecimalPlaces);
                        foreach(VirtualNozzle nz in workFlow.Dispenser.Nozzles)
                            workFlow.Controller.SetNozzlePrice(workFlow.Dispenser.ChannelId, workFlow.Dispenser.AddressId, nz.NozzleNumber, nz.CurrentSaleUnitPrice);
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

                while(!haltThread)
                {
                    try
                    {
                        bool isTankCheck = false;
                        DateTime dt = DateTime.Now;
                        int interval = this.TankCheckInterval;

                        if(this.PriceChanged)
                        {
                            if(this.QueryPrices != null)
                                this.QueryPrices(this, new EventArgs());
                            this.PriceChanged = false;
                        }

                        if((int)dt.TimeOfDay.TotalMinutes % interval == 0 && dt.Subtract(this.lastTankCheck).TotalMinutes > interval)
                        {
                            isTankCheck = true;
                            this.lastTankCheck = dt;
                        }
                        foreach(Common.IController controller in controllers)
                        {
                            if (controller == null)
                            {
                                System.Threading.Thread.Sleep(200);
                                continue;
                            }
                            if (!controller.IsConnected)
                            {
                                System.Threading.Thread.Sleep(200);
                                continue;
                            }

                            foreach(ASFuelControl.Tank.TankWorkFlow workFlow in this.tankWorkFlows)
                            {
                                if(workFlow.Controller != controller && !workFlow.Tank.IsVirtualTank)
                                {
                                    //workFlow.Tank.TankStatus = Common.Enumerators.TankStatusEnum.Offline;
                                    continue;
                                }
                                VirtualTank tank = workFlow.Tank;
                                if(workFlow.Tank.IsVirtualTank)
                                {
                                    if(DateTime.Now.Subtract(tank.LastValuesUpdate).TotalSeconds > 1)
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
                                    if(!controller.IsConnected)
                                        continue;
                                    Common.TankValues values = controller.GetTankValues(tank.ChannelId, tank.AddressId);
                                    if(values == null)
                                    {

                                        if(DateTime.Now.Subtract(tank.LastValuesUpdate).TotalSeconds > 30)
                                        {
                                            tank.TankStatus = Common.Enumerators.TankStatusEnum.Offline;
                                            tank.HasChanges = false;
                                        }
                                        //tank.LastValuesUpdate = DateTime.Now;
                                    }
                                    else
                                    {
                                        tank.LastValuesUpdate = DateTime.Now;
                                        tank.TankValues = values;
                                        if(tank.TankStatus == Common.Enumerators.TankStatusEnum.Offline)
                                            tank.TankStatus = Common.Enumerators.TankStatusEnum.Idle;
                                        if(this.TankValuesAvaliable != null)
                                            this.TankValuesAvaliable(this, new TankValuesEventArgs(tank.TankId, values));

                                        workFlow.SetValues(values);
                                    }

                                }
                                workFlow.ValidateTransitions();

                                tank.HasChanges = false;
                            }
                            foreach(ASFuelControl.WorkFlow.IFuelPumpWorkFlow workFlow in this.fpWorkFlows)
                            {
                                if(!controller.IsConnected)
                                    continue;
                                if(workFlow.Controller != controller)
                                    continue;
                                VirtualDispenser dispenser = workFlow.Dispenser;
                                if(!dispenser.IsValid)
                                    continue;
                                if(Program.CurrentShiftId == Guid.Empty)
                                    continue;
                                if (this.salesToProcess.Count > 0)
                                {
                                    for (int i = 0; i < this.salesToProcess.Count; i++ )
                                    {
                                        if (this.SaleAvaliable != null)
                                            this.SaleAvaliable(this, new WorkFlow.SaleCompletedArgs(new Common.Sales.SaleData()));
                                    }
                                }
                                workFlow.CheckOpenSales();
                                Common.FuelPointValues values = controller.GetDispenserValues(dispenser.ChannelId, dispenser.AddressId);
                                //if(values != null && values.CurrentPriceTotal > 0)
                                //    Console.WriteLine(values.CurrentPriceTotal);
                                foreach(VirtualNozzle nz in dispenser.Nozzles)
                                {
                                    Common.Sales.SaleData nzSale = controller.GetSale(dispenser.ChannelId, dispenser.AddressId, nz.NozzleNumber);
                                    if(nzSale == null)
                                        continue;
                                    nzSale.NozzleId = nz.NozzleId;
                                    workFlow.SetSale(nzSale);
                                    if(this.QueryTotalsUpdate != null && nzSale.TotalVolume > 0)
                                    {
                                        this.QueryTotalsUpdate(this, new FuelPump.QueryTotalsUpdateArgs()
                                        {
                                            NozzleId = nz.NozzleId,
                                            TotalVolumeCounter = nzSale.TotalizerEnd
                                        });
                                    }
                                }

                                //Common.Sales.SaleData sale = null;
                                if(values == null)
                                {
                                    if(dispenser.LastValues != null)
                                    {
                                        if(dispenser.LastValuesId != dispenser.LastValues.ID || DateTime.Now.Subtract(dispenser.LastValues.AssignDateTime).TotalSeconds > 3)
                                        {
                                            workFlow.SetValues(dispenser.LastValues);
                                            if(values != null)
                                                dispenser.LastValuesId = values.ID;
                                            dispenser.HasChanges = true;
                                            dispenser.LastValues.AssignDateTime = DateTime.Now;
                                        }
                                    }
                                }
                                else
                                {
                                    //Logger.Instance.LogToFile(values, dispenser);
                                    dispenser.LastValues = values;
                                    dispenser.LastValuesUpdate = DateTime.Now;
                                    if(values.ActiveNozzle >= 0 && (values.Status == Common.Enumerators.FuelPointStatusEnum.Idle || values.Status == Common.Enumerators.FuelPointStatusEnum.Offline))
                                    {
                                        values.ActiveNozzle = -1;
                                    }

                                    workFlow.SetValues(values);
                                    dispenser.LastValuesId = values.ID;
                                    dispenser.HasChanges = true;

                                    if(this.DispenserValuesAvaliable != null)
                                        this.DispenserValuesAvaliable(this, new DispenserValuesEventArgs(dispenser.DispenserId, values));

                                }
                                workFlow.ValidateTransitions();
                                foreach(VirtualNozzle nz in workFlow.Dispenser.Nozzles)
                                {
                                    if(nz.StatusChanged)
                                    {
                                        if(this.UpdateNozzleStatus != null)
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
                        }
                        if(this.QueryResolveAlarm != null)
                        {
                            this.QueryResolveAlarm(this, new QueryResolveAlarmArgs());
                        }
                        System.Threading.Thread.Sleep(200);
                        //if (this.CheckInvoices != null)
                        //    this.CheckInvoices(this, new EventArgs());
                    }
                    catch
                    {

                    }
                }
            }
            catch(Exception ex)
            {
                System.IO.File.AppendAllText(System.Environment.CurrentDirectory + "\\" + "ThreadRun.txt", ex.ToString());
            }
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

        void workFlow_AlarmRaised(object sender, WorkFlow.AlarmRaisedEventArgs e)
        {
            if (this.AlarmRaised != null)
                this.AlarmRaised(this, e);
        }

        void workFlow_QueryAlarmResolved(object sender, WorkFlow.QueryAlarmResolvedArgs e)
        {
            if (this.QueryAlarmResolved != null)
                this.QueryAlarmResolved(this, e);
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASFuelControl.WorkFlow;
using System.ComponentModel;
using ASFuelControl.Logging;

namespace ASFuelControl.FuelPump
{
    public class FuelPumpWorkFlow : WorkFlow.IWorkFlow, IFuelPumpWorkFlow
    {
        #region public events

        public event EventHandler<AlarmRaisedEventArgs> AlarmRaised;
        public event EventHandler<QueryAlarmResolvedArgs> QueryAlarmResolved;
        public event EventHandler QueryCheckState;
        public event EventHandler<SaleCompletedArgs> SaleCompleted;
        public event EventHandler<QueryTotalsUpdateArgs> QueryTotalsUpdate;
        public event EventHandler<NozzleStatusChangedArgs> NozzleStatusChanged;
        public event CancelEventHandler QueryStationLocked;

        #endregion

        public delegate bool StatusCheckDelegate(ASFuelControl.Common.Enumerators.FuelPointStatusEnum status);

        #region private variables

        private bool suspendAlarms = false;
        private bool initialized = false;
        private ProcessState errorState = null;
        private ProcessState offlineState = null;
        private ProcessState checkStateState = null;
        private ProcessState idleState = null;
        private ProcessState initializeSaleState = null;
        private ProcessState nozzleState = null;
        private ProcessState readyState = null;
        private ProcessState workState = null;
        private ProcessState transactionCompletedState = null;
        private ProcessState finalizeSaleState = null;
        private ProcessState getTotalEndState = null;
        private ProcessState tankLocked = null;
        private Guid AlarmNozzleId = Guid.Empty;
        private bool priceChecked = false;
        private bool totalsUpdated = false;
        private bool totalsEndUpdated = false;
        private bool tankUpdated = false;
        private bool transCompleted = false;
        private bool counterDiffResolved = false;
        private decimal lastSaleTotal = 0;
        private decimal lastVolume = 0;
        private DateTime totalRequest;
        private List<Common.Sales.SaleData> openSales = new List<Common.Sales.SaleData>();
        private List<Common.Sales.TankSaleData> currentSaleTankData = new List<Common.Sales.TankSaleData>();

        private Common.Sales.SaleData currentSale = null;

        #endregion

        #region public properties

        public bool SuspendAlarms 
        {
            set { this.suspendAlarms = value; }
            get { return this.suspendAlarms; }
        }

        public bool IsInitialized
        {
            set
            {
                if (this.initialized == value)
                    return;
                this.initialized = value;
            }
            get { return this.initialized; }
        }

        public WorkFlow.WorkFlowProcess Process { set; get; }

        public VirtualDevices.VirtualDispenser Dispenser { set; get; }

        public Common.IController Controller { set; get; }

        #endregion

        public FuelPumpWorkFlow(VirtualDevices.VirtualDispenser dispenser)
        {

            
            this.Process = new WorkFlow.WorkFlowProcess();
            this.Dispenser = dispenser;

            this.errorState = this.Process.AddProcessState("Error");
            this.offlineState = this.Process.AddProcessState("Offline");
            this.checkStateState = this.Process.AddProcessState("CheckState", 10000);
            this.idleState = this.Process.AddProcessState("Idle");
            this.initializeSaleState = this.Process.AddProcessState("InitializeSale", 10000);
            this.nozzleState = this.Process.AddProcessState("Nozzle");
            this.readyState = this.Process.AddProcessState("Ready");
            this.workState = this.Process.AddProcessState("Work");
            this.transactionCompletedState = this.Process.AddProcessState("TransactionCompleted");
            this.tankLocked = this.Process.AddProcessState("TankLocked");
            this.getTotalEndState = this.Process.AddProcessState("GetTotalEnd");
            this.finalizeSaleState = this.Process.AddProcessState("FinalizeSale", 10000);

            //Main WorkFlow transitions
            this.Process.AddProcessTransition(this.offlineState, this.checkStateState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.StatusCheck), Common.Enumerators.FuelPointStatusEnum.Idle);
            this.Process.AddProcessTransition(this.offlineState, this.getTotalEndState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.StatusCheck), Common.Enumerators.FuelPointStatusEnum.GetEndTotals);
            this.Process.AddProcessTransition(this.checkStateState, this.idleState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.StateChecked), null);
            this.Process.AddProcessTransition(this.idleState, this.initializeSaleState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.StatusCheck), Common.Enumerators.FuelPointStatusEnum.Nozzle);
            this.Process.AddProcessTransition(this.initializeSaleState, this.nozzleState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.TotalsUpdated), null);
            this.Process.AddProcessTransition(this.nozzleState, this.readyState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.StatusCheck), Common.Enumerators.FuelPointStatusEnum.Ready);
            this.Process.AddProcessTransition(this.nozzleState, this.workState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.StatusCheck), Common.Enumerators.FuelPointStatusEnum.Work);
            this.Process.AddProcessTransition(this.readyState, this.workState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.StatusCheck), Common.Enumerators.FuelPointStatusEnum.Work);

            this.Process.AddProcessTransition(this.workState, this.idleState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.StatusCheck), Common.Enumerators.FuelPointStatusEnum.Idle);
            this.Process.AddProcessTransition(this.workState, this.offlineState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.StatusCheck), Common.Enumerators.FuelPointStatusEnum.Offline);
            
            //this.Process.AddProcessTransition(this.workState, this.transactionCompletedState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.StatusCheck), Common.Enumerators.FuelPointStatusEnum.Idle);
            //this.Process.AddProcessTransition(this.workState, this.transactionCompletedState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.StatusCheck), Common.Enumerators.FuelPointStatusEnum.Offline);
            
            //this.Process.AddProcessTransition(this.workState, this.transactionCompletedState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.StatusCheck), Common.Enumerators.FuelPointStatusEnum.TransactionStopped);
            this.Process.AddProcessTransition(this.transactionCompletedState, this.getTotalEndState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.TotalsUpdatedEnd), null);
            this.Process.AddProcessTransition(this.getTotalEndState, this.finalizeSaleState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.InvoiceCreated), null);
            this.Process.AddProcessTransition(this.finalizeSaleState, this.idleState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.StatusCheck), Common.Enumerators.FuelPointStatusEnum.Idle);
            this.Process.AddProcessTransition(this.finalizeSaleState, this.idleState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.StatusCheck), Common.Enumerators.FuelPointStatusEnum.GetEndTotals);
            this.Process.AddProcessTransition(this.nozzleState, this.idleState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.StatusCheck), Common.Enumerators.FuelPointStatusEnum.Idle);
            this.Process.AddProcessTransition(this.readyState, this.idleState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.StatusCheck), Common.Enumerators.FuelPointStatusEnum.Idle);
            this.Process.AddProcessTransition(this.initializeSaleState, this.idleState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.StatusCheck), Common.Enumerators.FuelPointStatusEnum.Idle);

            //Offline Transitions
            this.Process.AddProcessTransition(this.finalizeSaleState, this.offlineState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.StatusCheck), Common.Enumerators.FuelPointStatusEnum.Offline);
            this.Process.AddProcessTransition(this.idleState, this.offlineState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.StatusCheck), Common.Enumerators.FuelPointStatusEnum.Offline);
            this.Process.AddProcessTransition(this.initializeSaleState, this.offlineState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.StatusCheck), Common.Enumerators.FuelPointStatusEnum.Offline);
            this.Process.AddProcessTransition(this.nozzleState, this.offlineState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.StatusCheck), Common.Enumerators.FuelPointStatusEnum.Offline);

            //TankLocked
            this.Process.AddProcessTransition(this.idleState, this.tankLocked, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.IsTankLocked), null);
            this.Process.AddProcessTransition(this.tankLocked, this.idleState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.IsTankUnLocked), null);

            //Error Transitions
            this.Process.AddProcessTransition(this.checkStateState, this.errorState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.StatusCheck), Common.Enumerators.FuelPointStatusEnum.Error);
            this.Process.AddProcessTransition(this.initializeSaleState, this.errorState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.HasAlerts), null);
            this.Process.AddProcessTransition(this.idleState, this.errorState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.StatusCheck), Common.Enumerators.FuelPointStatusEnum.Error);
            //this.Process.AddProcessTransition(this.finalizeSaleState, this.errorState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.HasAlerts), null);
            this.Process.AddProcessTransition(this.transactionCompletedState, this.errorState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.StatusCheck), Common.Enumerators.FuelPointStatusEnum.Error);

            //Error Resolved
            this.Process.AddProcessTransition(this.errorState, this.idleState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.ErrorResolved), null);

            this.Dispenser.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(Dispenser_PropertyChanged);
            this.Process.CurrentState = this.offlineState;
            this.Process.StateChanged += new EventHandler(Process_StateChanged);

            foreach (VirtualDevices.VirtualNozzle nozzle in this.Dispenser.Nozzles)
            {
                nozzle.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(nozzle_PropertyChanged);
            }
        }

        public void ResetState()
        {
            this.Process.StateChanged -= new EventHandler(Process_StateChanged);
            this.Process.CurrentState = this.offlineState;
            this.Dispenser.TargetStatus = Common.Enumerators.FuelPointStatusEnum.Offline;
            this.Dispenser.Status = Common.Enumerators.FuelPointStatusEnum.Offline;
            this.currentSale = null;
            this.totalsEndUpdated = false;
            foreach (VirtualDevices.VirtualNozzle nz in this.Dispenser.Nozzles)
                nz.TotalsUpdated = false;
            this.Process.StateChanged += new EventHandler(Process_StateChanged);

        }

        #region public Methods

        public void SetValues(object _values)
        {
            Common.FuelPointValues values = _values as Common.FuelPointValues;
            
            //if(values.CurrentVolume > 0 && values.CurrentSalePrice > 0)
            //{
            //    decimal totalPrice1 = values.CurrentVolume * values.CurrentSalePrice;
            //    decimal totalPrice2 = values.CurrentPriceTotal;
            //    if (totalPrice1 > 0 && totalPrice2 / totalPrice1 > (decimal)1.1)
            //        values.CurrentPriceTotal = values.CurrentPriceTotal / 10;
            //}
            if (values.Status != this.Dispenser.TargetStatus)
            {
                this.Dispenser.TargetStatus = values.Status;
                if (this.Dispenser.TargetStatus == Common.Enumerators.FuelPointStatusEnum.TransactionCompleted)
                    this.transCompleted = true;
                if (this.Dispenser.Status == Common.Enumerators.FuelPointStatusEnum.Idle)
                    this.transCompleted = false;
            }
            if (values.ActiveNozzle >= 0 && values.Status != Common.Enumerators.FuelPointStatusEnum.Idle && values.Status != Common.Enumerators.FuelPointStatusEnum.Offline)
            {
                VirtualDevices.VirtualNozzle nozzle = this.Dispenser.Nozzles[values.ActiveNozzle];
                if (this.Dispenser.ActiveNozzle != nozzle)
                    this.Dispenser.ActiveNozzle = nozzle;
                
                if(values.CurrentTemperatur != this.Dispenser.ActiveNozzle.CurrentTemperature)
                    this.Dispenser.ActiveNozzle.CurrentTemperature = values.CurrentTemperatur;
            }

            else
            {
                this.Dispenser.ActiveNozzle = null;
            }
            VirtualDevices.VirtualNozzle nz = this.Dispenser.ActiveNozzle;
            if (this.Dispenser.ActiveNozzle == null)
                nz = this.Dispenser.LastActiveNozzle;
            if (nz != null)
            {
                nz.CurrentSaleUnitPrice = values.CurrentSalePrice;
                nz.CurrentSaleTotalPrice = values.CurrentPriceTotal;
                nz.CurrentSaleTotalVolume = values.CurrentVolume;
            }

            if (this.Dispenser.Status == Common.Enumerators.FuelPointStatusEnum.Work || this.Dispenser.Status == Common.Enumerators.FuelPointStatusEnum.TransactionCompleted || this.Dispenser.Status == Common.Enumerators.FuelPointStatusEnum.GetEndTotals)
            {
                //if (values.CurrentPriceTotal > 0)
                    this.lastSaleTotal = values.CurrentPriceTotal;
                //if (values.CurrentVolume > 0)
                    this.lastVolume = values.CurrentVolume;
            }
            else
            {
                this.lastSaleTotal = 0;
                this.lastVolume = 0;
            }
        }

        public void CheckOpenSales()
        {
            Common.Sales.SaleData[] sales = this.openSales.ToArray();
            foreach (Common.Sales.SaleData sale in sales)
                this.CloseSale(sale);
        }

        public void SetSale(Common.Sales.SaleData sale)
        {
            if (sale.TotalizerStart == sale.TotalizerEnd)
                return;
            
            this.currentSale = sale;
            sale.TankData = this.currentSaleTankData.ToArray();
            
            sale.TraderId = Dispenser.NextSaleTrader;
            sale.VehicleId = Dispenser.NextSaleVehicle;
            sale.InvoiceTypeId = Dispenser.InvoiceTypeId;
            sale.FuelTypeDescription = this.Dispenser.Nozzles.Where(n => n.NozzleId == sale.NozzleId).First().FuelTypeDescription;
            VirtualDevices.VirtualNozzle nozzle = this.Dispenser.Nozzles.Where(n => n.NozzleId == sale.NozzleId).FirstOrDefault();
            if (nozzle == null)
                return;

            if (sale.TotalVolume == 0)
                return;

            nozzle.TotalVolumeCounter = sale.TotalizerEnd;
            nozzle.LastVolumeCounter = sale.TotalizerEnd;
            sale.SaleEndTime = DateTime.Now;

            nozzle.HasOpenSales = true;
            this.openSales.Add(sale);

            //foreach (VirtualDevices.VirtualTank tank in nozzle.ConnectedTanks)
            //{
            //    lock (tank)
            //    {
            //        lock (this.currentSale)
            //        {
            //            if (initialize)
            //            {
            //                Common.Sales.TankSaleData tData = new Common.Sales.TankSaleData();
            //                tData.StartLevel = tank.CurrentFuelLevel;
            //                tData.StartTemperature = tank.CurrentTemperature;
            //                tData.TankId = tank.TankId;
            //                tData.StartWaterLevel = tank.CurrentWaterLevel;
            //                List<Common.Sales.TankSaleData> list = new List<Common.Sales.TankSaleData>();
            //                if (this.currentSale.TankData != null)
            //                    list = new List<Common.Sales.TankSaleData>(this.currentSale.TankData);
            //                list.Add(tData);
            //                this.currentSale.TankData = list.ToArray();
            //            }
            //            else
            //            {
            //                Common.Sales.TankSaleData tData = this.currentSale.TankData.Where(td => td.TankId == tank.TankId).FirstOrDefault();
            //                if (tData != null)
            //                {
            //                    tData.EndLevel = tank.CurrentFuelLevel;
            //                    tData.EndTemperature = tank.CurrentTemperature;
            //                    tData.EndWaterLevel = tank.CurrentWaterLevel;
            //                }
            //            }
            //        }
            //    }
            //}
            
        }

        #endregion

        #region private methods

        private void CloseSale(Common.Sales.SaleData sale)
        {
            if(sale == null)
                return;
            VirtualDevices.VirtualNozzle nozzle = this.Dispenser.Nozzles.Where(n => n.NozzleId == sale.NozzleId).FirstOrDefault();
            if (nozzle == null)
            {
                this.openSales.Remove(sale);
                return;
            }
            foreach (VirtualDevices.VirtualTank tank in nozzle.ConnectedTanks)
            {
                if (tank.LastValuesUpdate < sale.SaleEndTime)
                    return;
                
            }
            this.UpdateTankData(sale);
            if (this.SaleCompleted == null)
            {
                this.openSales.Remove(sale);
                return;
            }

            if (nozzle.Status == Common.Enumerators.NozzleStateEnum.LiterCheck)
                sale.LiterCheck = true;

            

            if (sale.TotalVolume != (sale.TotalizerEnd - sale.TotalizerStart) / 100)
            {
                sale.TotalVolume = (sale.TotalizerEnd - sale.TotalizerStart) / 100;
                sale.TotalPrice = decimal.Round(sale.TotalVolume * sale.UnitPrice, 2);
            }
            Logger.Instance.LogToFile("Sale about to complete", sale);

            SaleCompletedArgs args = new SaleCompletedArgs(sale);
            this.SaleCompleted(this, args);
            
            if (args.InvoiceLineId != Guid.Empty && nozzle != null && nozzle.Status == Common.Enumerators.NozzleStateEnum.LiterCheck)
            {
                foreach (VirtualDevices.VirtualTank tank in nozzle.ConnectedTanks)
                {
                    tank.InvoiceLineId = args.InvoiceLineId;
                    tank.InitializeFilling = true;
                    tank.IsLiterCheck = true;
                    nozzle.Status = Common.Enumerators.NozzleStateEnum.Locked;
                }
            }
            this.Dispenser.AddSale(nozzle, sale.TotalVolume, sale.TotalPrice);
            sale.VehicleId = Guid.Empty;
            sale.TraderId = Guid.Empty;
            this.Dispenser.NextSaleVehicle = Guid.Empty;
            this.Dispenser.NextSaleTrader = Guid.Empty;

            this.openSales.Remove(sale);
            if (this.openSales.Where(o => o.NozzleId == sale.NozzleId).Count() == 0)
                nozzle.HasOpenSales = false;
            Console.WriteLine("Dispenser Sale Competed");
        }

        private void CreateTankData(VirtualDevices.VirtualNozzle nozzle)
        {
            this.currentSaleTankData.Clear();
            foreach (VirtualDevices.VirtualTank tank in nozzle.ConnectedTanks)
            {
                lock (tank)
                {
                    Common.Sales.TankSaleData tData = new Common.Sales.TankSaleData();
                    tData.StartLevel = tank.CurrentFuelLevel;
                    tData.StartTemperature = tank.CurrentTemperature;
                    tData.TankId = tank.TankId;
                    tData.StartWaterLevel = tank.CurrentWaterLevel;
                    this.currentSaleTankData.Add(tData);
                }
            }
        }

        private void UpdateTankData(Common.Sales.SaleData sale)
        {
            VirtualDevices.VirtualNozzle nozzle = this.Dispenser.Nozzles.Where(n => n.NozzleId == sale.NozzleId).FirstOrDefault();
            if (nozzle == null)
                return;
            if (sale.TankData == null || sale.TankData.Length == 0)
            {
                this.currentSaleTankData.Clear();
                foreach (VirtualDevices.VirtualTank tank in nozzle.ConnectedTanks)
                {
                    lock (tank)
                    {
                        Common.Sales.TankSaleData tData = new Common.Sales.TankSaleData();
                        tData.StartLevel = tank.CurrentFuelLevel;
                        tData.StartTemperature = tank.CurrentTemperature;
                        tData.TankId = tank.TankId;
                        tData.StartWaterLevel = tank.CurrentWaterLevel;
                        this.currentSaleTankData.Add(tData);
                    }
                }
                sale.TankData = this.currentSaleTankData.ToArray();
            }
            foreach (VirtualDevices.VirtualTank tank in nozzle.ConnectedTanks)
            {
                lock (tank)
                {
                    Common.Sales.TankSaleData tData = sale.TankData.Where(td => td.TankId == tank.TankId).FirstOrDefault();
                    if (tData != null)
                    {
                        tData.EndLevel = tank.CurrentFuelLevel;
                        tData.EndTemperature = tank.CurrentTemperature;
                        tData.EndWaterLevel = tank.CurrentWaterLevel;
                    }
                }
            }
        }

        private bool StatusCheck(object _status)
        {
            if (_status == null)
                return false;
            if (_status.GetType() != typeof(Common.Enumerators.FuelPointStatusEnum))
                return false;

            Common.Enumerators.FuelPointStatusEnum status = (Common.Enumerators.FuelPointStatusEnum)_status;

            if (status == Common.Enumerators.FuelPointStatusEnum.Nozzle && this.QueryStationLocked != null)
            {
                CancelEventArgs args = new CancelEventArgs();
                this.QueryStationLocked(this, args);
                if (args.Cancel)
                    return false;
            }

            if (status == Common.Enumerators.FuelPointStatusEnum.Nozzle &&  this.IsTankLocked(null))
                return false;
            //if (this.Process.CurrentState == this.workState)
            //{
            //    if (DateTime.Now.Subtract(this.Process.ChangedStateTime).TotalSeconds < 2)
            //        return false;
            //}
            if (this.Dispenser.TargetStatus == status && this.Dispenser.Initialized)
                return true;
            return false;
        }

        private bool IsTankLocked(object foo)
        {
            if (this.Dispenser.ActiveNozzle == null)
                return false;
            
            foreach (VirtualDevices.VirtualTank tank in this.Dispenser.ActiveNozzle.ConnectedTanks)
            {
                if (tank.TankStatus != Common.Enumerators.TankStatusEnum.Idle && tank.TankStatus != Common.Enumerators.TankStatusEnum.Selling)
                    return true;
            }
            return false;
        }

        private bool IsTankUnLocked(object foo)
        {
            if (this.Dispenser.ActiveNozzle == null)
                return true;
            foreach (VirtualDevices.VirtualTank tank in this.Dispenser.ActiveNozzle.ConnectedTanks)
            {
                if (tank.TankStatus != Common.Enumerators.TankStatusEnum.Idle && tank.TankStatus != Common.Enumerators.TankStatusEnum.Selling)
                    return false;
            }
            return true;
        }

        private bool StateChecked(object foo)
        {
            if (this.Dispenser == null)
                return false;
            if (this.Dispenser.AddressId <= 0 && this.Dispenser.ChannelId <= 0)
                return false;
            return priceChecked;
        }

        private bool TotalsUpdated(object foo)
        {
            
            if (this.Dispenser.ActiveNozzle == null)
                return false;
            try
            {
                if (this.Dispenser.ActiveNozzle.TotalsUpdated)
                {
                    if (this.HasAlerts(null))
                        return false;
                    //if (this.Dispenser.ActiveNozzle.LastVolumeCounter != this.Dispenser.ActiveNozzle.TotalVolumeCounter)
                    //    this.Dispenser.ActiveNozzle.Status = Common.Enumerators.NozzleStateEnum.Locked;
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
                this.Dispenser.ActiveNozzle.TotalsUpdated = false;
            }
        }

        private bool TotalsUpdatedEnd(object foo)
        {
            if (this.currentSale == null)
                return true;
            VirtualDevices.VirtualNozzle currentNozzle = this.Dispenser.Nozzles.Where(n => n.NozzleId == this.currentSale.NozzleId).FirstOrDefault();
            if (currentNozzle == null)
                return false;
            if (currentNozzle.TotalsUpdated)
            {
                return true;
            }
            return false;
        }

        private bool InvoiceCreated(object foo)
        {
            if (this.currentSale == null)
                return true;
            VirtualDevices.VirtualNozzle currentNozzle = this.Dispenser.Nozzles.Where(n => n.NozzleId == this.currentSale.NozzleId).FirstOrDefault();
            if (currentNozzle == null)
                return false;

            //bool totalsUpdated = true;
            //bool totalsUpdated = this.TotalsUpdatedEnd(foo);
            //if (totalsUpdated == false && DateTime.Now.Subtract(this.totalRequest).TotalSeconds > 2)
            //{
            //    currentNozzle.TotalVolumeCounter = currentNozzle.TotalVolumeCounter + (currentSale.TotalVolume * 100);
            //    totalsUpdated = true;
            //}
            bool updated = true;
            if(this.currentSale.SaleEndTime == DateTime.MinValue)
                this.currentSale.SaleEndTime = DateTime.Now;

            

            foreach (VirtualDevices.VirtualTank tank in currentNozzle.ConnectedTanks)
            {
                lock (tank)
                {
                    if (tank.LastValuesUpdate < this.currentSale.SaleEndTime)
                        updated = false;
                    else
                    {
                        lock (this.currentSale)
                        {
                            Common.Sales.TankSaleData tData = this.currentSale.TankData.Where(td => td.TankId == tank.TankId).FirstOrDefault();
                            if (tData != null)
                            {
                                tData.EndLevel = tank.CurrentFuelLevel;
                                tData.EndTemperature = tank.CurrentTemperature;
                                tData.EndWaterLevel = tank.CurrentWaterLevel;
                            }
                        }
                    }
                }
            }
            //if (!totalsEndUpdated)
            //{
            //    currentNozzle.LastVolumeCounter = currentNozzle.TotalVolumeCounter;
            //    currentNozzle.TotalVolumeCounter = currentNozzle.LastVolumeCounter + (currentNozzle.CurrentSaleTotalVolume * 100);
            //    currentSale.TotalizerEnd = currentNozzle.TotalVolumeCounter;
            //    this.totalsEndUpdated = true;
            //if (this.QueryTotalsUpdate != null)
            //    this.QueryTotalsUpdate(this, new QueryTotalsUpdateArgs() { NozzleId = currentNozzle.NozzleId, TotalVolumeCounter = currentNozzle.TotalVolumeCounter });
            //}
            //if (totalsUpdated)
            //{
            //    currentSale.TotalizerEnd = currentNozzle.TotalVolumeCounter;
            //    if (this.QueryTotalsUpdate != null)
            //        this.QueryTotalsUpdate(this, new QueryTotalsUpdateArgs() { NozzleId = currentNozzle.NozzleId, TotalVolumeCounter = currentNozzle.TotalVolumeCounter });
            //}
            //if (updated)// && totalsUpdated)
            //{
            //    this.currentSale.TotalPrice = currentNozzle.CurrentSaleTotalPrice;
            //    this.currentSale.TotalVolume = currentNozzle.CurrentSaleTotalVolume;
            //    //currentNozzle.TotalVolumeCounter = currentNozzle.TotalVolumeCounter + decimal.Round(this.currentSale.TotalVolume * 100, 0);
            //}
            if (!(updated))// && totalsUpdated))
                this.Dispenser.HasChanges = true;
            return updated;// && totalsUpdated;
        }

        private bool ErrorResolved(object foo)
        {
            foreach (VirtualDevices.VirtualNozzle nozzle in this.Dispenser.Nozzles)
            {
                if (nozzle.LastVolumeCounter != nozzle.TotalVolumeCounter)
                    return false;
            }
            return true;
        }

        private bool CheckPrices()
        {
            try
            {
                decimal[] prices = this.Controller.GetPrices(this.Dispenser.ChannelId, this.Dispenser.AddressId);
                foreach (VirtualDevices.VirtualNozzle nozzle in this.Dispenser.Nozzles)
                {
                    if (prices.Length >= nozzle.NozzleNumber && nozzle.CurrentSaleUnitPrice != prices[nozzle.NozzleNumber - 1])
                        this.Controller.SetNozzlePrice(nozzle.ParentDispenser.ChannelId, nozzle.ParentDispenser.AddressId, nozzle.NozzleNumber, nozzle.CurrentSaleUnitPrice);
                }
                return true;
            }
            catch(Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("CheckPrices :: " + ex.Message);
                return false;
            }
        }

        private void GetTotals(VirtualDevices.VirtualNozzle nozzle)
        {
            if (nozzle == null)
                return;
            nozzle.TotalsUpdated = false;
            totalRequest = DateTime.Now;
            
            this.Controller.GetNozzleTotalValues(this.Dispenser.ChannelId, this.Dispenser.AddressId, nozzle.NozzleNumber);
        }

        private void AuthorizeSale()
        {
            if (this.Dispenser.DeviceLocked)
                return;
            if(this.Process.CurrentState == this.nozzleState)
                this.Controller.AuthorizeDispenser(this.Dispenser.ChannelId, this.Dispenser.AddressId);
        }

        private void CreateAlarm()
        {
            if(this.AlarmRaised != null)
            {
                if (this.AlarmNozzleId != Guid.Empty)
                {
                    VirtualDevices.VirtualNozzleAlarm alarm = new VirtualDevices.VirtualNozzleAlarm();
                    VirtualDevices.VirtualNozzle nozzle = this.Dispenser.Nozzles.Where(n => n.NozzleId == this.AlarmNozzleId).First();
                    alarm.AlarmTime = DateTime.Now;
                    alarm.TotalCounter = nozzle.TotalVolumeCounter;
                    alarm.AlertType = Common.Enumerators.AlarmEnum.NozzleTotalError;
                    alarm.DeviceId = this.AlarmNozzleId;
                    alarm.MessageText = "Σφάλμα στον μετρητή";

                    alarm.AddData("TotalVolumeCounter", nozzle.TotalVolumeCounter.ToString());
                    alarm.AddData("LastVolumeCounter", nozzle.LastVolumeCounter.ToString());

                    this.AlarmRaised(this, new AlarmRaisedEventArgs(alarm));
                    this.AlarmNozzleId = Guid.Empty;
                }
            }
        }

        private bool HasAlerts(object sale)
        {

            //if (this.suspendAlarms)
            //    return false;
            VirtualDevices.VirtualNozzle nozzle = null;

            Common.Sales.SaleData curSale = sale as Common.Sales.SaleData;
            if (curSale != null)
            {
                VirtualDevices.VirtualNozzle currentNozzle = this.Dispenser.Nozzles.Where(n => n.NozzleId == this.currentSale.NozzleId).FirstOrDefault();
                if (currentNozzle != null)
                {

                    nozzle = currentNozzle;

                    //if (currentNozzle.LastVolumeCounter != currentNozzle.TotalVolumeCounter)
                    //{
                    //    AlarmNozzleId = currentNozzle.NozzleId;
                    //    System.Diagnostics.Trace.WriteLine(string.Format("Nozzle Alert Id:{0}, Previous Value :{1:N4}, Current Value :{2:N4}", AlarmNozzleId, currentNozzle.LastVolumeCounter, currentNozzle.TotalVolumeCounter));
                    //    return true;
                    //}
                    //return false;
                }
            }

            if (this.Dispenser.ActiveNozzle == null)
                return false;

            nozzle = this.Dispenser.ActiveNozzle;

            if (nozzle.LastVolumeCounter != nozzle.TotalVolumeCounter)
            {
                decimal diff = System.Math.Abs(nozzle.TotalVolumeCounter - nozzle.LastVolumeCounter);
                if (diff <= 1)
                {
                    return false;
                }
                else
                {
                    AlarmNozzleId = nozzle.NozzleId;
                    System.Diagnostics.Trace.WriteLine(string.Format("Nozzle Alert Id:{0}, Previous Value :{1:N4}, Current Value :{2:N4}", AlarmNozzleId, nozzle.LastVolumeCounter, nozzle.TotalVolumeCounter));
                    return true;
                    //return true;
                }
            }

            return false;
        }

        #endregion

        public void ValidateTransitions()
        {
            if (!this.Dispenser.HasChanges && this.Process.CurrentState != this.initializeSaleState)
                return;
            foreach (StateTransition transition in this.Process.Transitions)
            {
                if (transition.SourceState != this.Process.CurrentState)
                    continue;
                if (!transition.ValidateTransition())
                    continue;
                this.Process.PreviousState = transition.SourceState;
                this.Process.CurrentState = transition.TargetState;
                break;
            }
            this.Dispenser.HasChanges = false;
            foreach (VirtualDevices.VirtualNozzle nozzle in this.Dispenser.Nozzles)
                nozzle.HasChanges = false;
        }

        #region Events

        void nozzle_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "Status")
                return;
            VirtualDevices.VirtualNozzle nozzle = sender as VirtualDevices.VirtualNozzle;
            if (this.NozzleStatusChanged != null)
                this.NozzleStatusChanged(this, new NozzleStatusChangedArgs() { NozzleId = nozzle.NozzleId, NozzleStatus = nozzle.Status });
        }

        void Process_StateChanged(object sender, EventArgs e)
        {
            if (Process.CurrentState == this.offlineState)
            {
                this.Dispenser.Status = Common.Enumerators.FuelPointStatusEnum.Offline;
            }
            else if (Process.CurrentState == this.checkStateState)
            {
                priceChecked = this.CheckPrices();
                this.Dispenser.Status = Common.Enumerators.FuelPointStatusEnum.CheckState;
                    
            }
            else if (Process.CurrentState == this.initializeSaleState)
            {
                if (this.Dispenser.ActiveNozzle != null)
                {
                    this.Dispenser.ActiveNozzle.TotalsUpdated = true;
                }
                this.Dispenser.Status = Common.Enumerators.FuelPointStatusEnum.InitializeSale;
            }
            else if (Process.CurrentState == this.idleState)
            {
                this.Dispenser.ActiveNozzle = null;
                this.Dispenser.Status = Common.Enumerators.FuelPointStatusEnum.Idle;
                //this.currentSale = null;
                this.totalsEndUpdated = false;
                this.counterDiffResolved = false;
                //this.currentSaleTankData.Clear();
            }
            else if (Process.CurrentState == this.nozzleState)
            {
                this.Dispenser.ActiveNozzle.CurrentSaleTotalPrice = 0;
                this.Dispenser.ActiveNozzle.CurrentSaleTotalVolume = 0;
                this.Dispenser.Status = Common.Enumerators.FuelPointStatusEnum.Nozzle;
                this.currentSale = new Common.Sales.SaleData();
                this.CreateTankData(this.Dispenser.ActiveNozzle);
                if (this.Dispenser.ActiveNozzle.Status != Common.Enumerators.NozzleStateEnum.Locked)
                    this.AuthorizeSale();
            }
            else if (Process.CurrentState == this.readyState)
            {
                this.Dispenser.Status = Common.Enumerators.FuelPointStatusEnum.Ready;
            }
            else if (Process.CurrentState == this.workState)
            {
                this.Dispenser.Status = Common.Enumerators.FuelPointStatusEnum.Work;
            }
            else if (Process.CurrentState == this.transactionCompletedState)
            {
                if (this.currentSale != null)
                {
                    VirtualDevices.VirtualNozzle currentNozzle = this.Dispenser.Nozzles.Where(n => n.NozzleId == this.currentSale.NozzleId).FirstOrDefault();
                    currentNozzle.TotalsUpdated = true;
                }
                this.totalsEndUpdated = true;
                this.Dispenser.Status = Common.Enumerators.FuelPointStatusEnum.TransactionCompleted;
                System.Threading.Thread.Sleep(100);
            }
            else if (Process.CurrentState == this.finalizeSaleState)
            {
                this.Dispenser.Status = Common.Enumerators.FuelPointStatusEnum.SalingCompleted;
               // this.currentSale = null;
                this.lastVolume = 0;
                this.lastSaleTotal = 0;
            }
            else if (Process.CurrentState == this.getTotalEndState)
            {
                this.Dispenser.Status = Common.Enumerators.FuelPointStatusEnum.GetEndTotals;
                
            }
            else if (Process.CurrentState == this.tankLocked)
                this.Dispenser.Status = Common.Enumerators.FuelPointStatusEnum.TankLocked;
            else if (Process.CurrentState == this.errorState)
            {
                this.Dispenser.Status = Common.Enumerators.FuelPointStatusEnum.Error;
                this.CreateAlarm();
            }
            System.Console.WriteLine(string.Format("Dispenser : {0}  State : {1} Dispenser Status : {2}", this.Dispenser.OfficialNumber, this.Process.CurrentState, this.Dispenser.Status));
        }

        void Dispenser_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Reset")
            {
                this.ResetState();
            }
        }

        #endregion
    }

    public class NozzleStatusChangedArgs :EventArgs
    {
        public Guid NozzleId { set; get; }
        public Common.Enumerators.NozzleStateEnum NozzleStatus { set; get; }
    }

    public class QueryTotalsUpdateArgs : EventArgs
    {
        public Guid NozzleId { set; get; }
        public decimal TotalVolumeCounter { set; get; }
    }
}

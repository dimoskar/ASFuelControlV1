﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASFuelControl.WorkFlow;

namespace ASFuelControl.Tank
{
    public class TankWorkFlow : WorkFlow.IWorkFlow, ITankWorkFlow
    {
        public event EventHandler<AlarmRaisedEventArgs> AlarmRaised;
        public event EventHandler<QueryAlarmResolvedArgs> QueryAlarmResolved;
        public delegate bool StatusCheckDelegate(ASFuelControl.Common.Enumerators.TankStatusEnum status);

        #region Event Definitions

        public event EventHandler<TankFillingEventArgs> FillingCompleted;
        public event EventHandler ProcessStateChanged;

        #endregion

        #region private variables

        private int errorCheckIndex = 0;
        private bool initialized = false;
        private DateTime lastSaleEnded;
        private ProcessState errorState = null;
        private ProcessState lowLevelState = null;
        private ProcessState highWaterState = null;
        private ProcessState offlineState = null;
        private ProcessState idleState = null;
        private ProcessState sallingState = null;
        private ProcessState levelDecreaseState = null;
        private ProcessState levelIncreaseState = null;
        private ProcessState extractionInitializedState = null;
        private ProcessState fillingInitializedState = null;
        private ProcessState extractionState = null;
        private ProcessState fillingState = null;
        private ProcessState waitingState = null;
        private System.Threading.Timer waitingTimer;
        private bool waitingElapsed = false;
        private DateTime deliveryStarted;

        #endregion

        #region public properties

        public Guid CurrentShiftId { set; get; }

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

        public int LiterCheckTime { set; get; }

        public WorkFlow.WorkFlowProcess Process { set; get; }

        public VirtualDevices.VirtualTank Tank { set; get; }

        public Common.IController Controller { set; get; }

        public Common.Sales.TankFillingData CurrentFillingData { set; get; }

        public TimeSpan CurrentWaitingTime { set; get; }

        #endregion

        public TankWorkFlow(VirtualDevices.VirtualTank tank)
        {
            this.Process = new WorkFlow.WorkFlowProcess();
            this.Tank = tank;

            this.errorState = this.Process.AddProcessState("Error");
            this.lowLevelState = this.Process.AddProcessState("LowLevel");
            this.highWaterState = this.Process.AddProcessState("HighWater");
            this.offlineState = this.Process.AddProcessState("Offline");
            this.idleState = this.Process.AddProcessState("Idle");
            this.sallingState = this.Process.AddProcessState("Selling");
            this.levelDecreaseState = this.Process.AddProcessState("LevelDecrease");
            this.levelIncreaseState = this.Process.AddProcessState("LevelIncrease");
            this.extractionInitializedState = this.Process.AddProcessState("ExtractionInitialized");
            this.fillingInitializedState = this.Process.AddProcessState("FillingInitialized");
            this.extractionState = this.Process.AddProcessState("Extraction");
            this.fillingState = this.Process.AddProcessState("Filling");
            this.waitingState = this.Process.AddProcessState("Waiting");

            //Source State is Offline
            this.Process.AddProcessTransition(this.offlineState, this.idleState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.IsIdle), Common.Enumerators.TankStatusEnum.Idle);
            //this.Process.AddProcessTransition(this.offlineState, this.fillingState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.IsFilling), null);
            //this.Process.AddProcessTransition(this.offlineState, this.extractionState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.IsDraining), null);
            this.Process.AddProcessTransition(this.offlineState, this.levelIncreaseState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.QueryFilling), null);
            this.Process.AddProcessTransition(this.offlineState, this.levelDecreaseState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.QueryExtraction), null);

            //Source State is Idle
            this.Process.AddProcessTransition(this.idleState, this.sallingState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.IsOnSale), null);
            this.Process.AddProcessTransition(this.idleState, this.fillingInitializedState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.QueryFilling), null);
            this.Process.AddProcessTransition(this.idleState, this.extractionInitializedState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.QueryExtraction), null);
            this.Process.AddProcessTransition(this.sallingState, this.fillingState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.QueryFilling), null);
            this.Process.AddProcessTransition(this.idleState, this.fillingState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.IsIncreasing), null);
            this.Process.AddProcessTransition(this.idleState, this.extractionState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.IsDecreasing), null);

            this.Process.AddProcessTransition(this.lowLevelState, this.fillingInitializedState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.QueryFilling), null);
            this.Process.AddProcessTransition(this.highWaterState, this.extractionInitializedState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.QueryExtraction), null);

            //this.Process.AddProcessTransition(this.idleState, this.levelIncreaseState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.IsIncreasing), null);
            //this.Process.AddProcessTransition(this.idleState, this.levelDecreaseState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.IsDecreasing), null);

            //Source State is Salling
            this.Process.AddProcessTransition(this.sallingState, this.idleState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.IsIdle), null);

            //this.Process.AddProcessTransition(this.sallingState, this.fillingInitializedState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.IsIdle), null);

            //Source State is FillingInitialized
            this.Process.AddProcessTransition(this.fillingInitializedState, this.fillingState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.IsIncreasing), null);
            this.Process.AddProcessTransition(this.fillingInitializedState, this.idleState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.CancelFilling), null);

            //Source State is ExtractionInitialized
            this.Process.AddProcessTransition(this.extractionInitializedState, this.extractionState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.IsDecreasing), null);
            this.Process.AddProcessTransition(this.extractionInitializedState, this.idleState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.CancelExtraction), null);

            //Source State is LevelIncrease
            this.Process.AddProcessTransition(this.levelIncreaseState, this.fillingInitializedState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.QueryFilling), null);
            this.Process.AddProcessTransition(this.levelIncreaseState, this.idleState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.IsCorrectLevel), null);

            //Source State is LevelDecreaser
            this.Process.AddProcessTransition(this.levelDecreaseState, this.extractionInitializedState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.QueryExtraction), null);
            this.Process.AddProcessTransition(this.levelDecreaseState, this.idleState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.IsCorrectLevel), null);

            //Source State is other
            this.Process.AddProcessTransition(this.fillingState, this.waitingState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.IsFillingFinished), null);
            this.Process.AddProcessTransition(this.extractionState, this.waitingState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.IsExtractionFinished), null);
            this.Process.AddProcessTransition(this.waitingState, this.idleState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.IsWaitingFinished), null);
            //this.Process.AddProcessTransition(this.errorState, this.idleState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.HasNoAlerts), Common.Enumerators.TankStatusEnum.Idle);

            this.Process.AddProcessTransition(this.idleState, this.errorState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.HasAlerts), null);
            this.Process.AddProcessTransition(this.sallingState, this.errorState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.HasAlerts), null);
            this.Process.AddProcessTransition(this.errorState, this.idleState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.HasNoAlertsOffline), null);
            this.Process.AddProcessTransition(this.errorState, this.fillingInitializedState, new ASFuelControl.WorkFlow.WorkFlowProcess.ValidationkDelegate(this.QueryFilling), null);

            switch(this.Tank.TankStatus)
            {
                case Common.Enumerators.TankStatusEnum.Offline:
                    this.Process.CurrentState = this.offlineState;
                    break;
                case Common.Enumerators.TankStatusEnum.Filling:
                    this.Process.CurrentState = this.fillingState;
                    break;
                case Common.Enumerators.TankStatusEnum.FuelExtraction:
                    this.Process.CurrentState = this.extractionState;
                    break;
                case Common.Enumerators.TankStatusEnum.FuelExtractionInit:
                    this.Process.CurrentState = this.extractionInitializedState;
                    break;
                case Common.Enumerators.TankStatusEnum.FillingInit:
                    this.Process.CurrentState = this.fillingInitializedState;
                    break;
                case Common.Enumerators.TankStatusEnum.Waiting:
                    this.Process.CurrentState = this.waitingState;
                    break;
                case Common.Enumerators.TankStatusEnum.Idle:
                    this.Process.CurrentState = this.idleState;
                    break;
                default:
                    this.Process.CurrentState = this.offlineState;
                    break;
            }

            switch (this.Tank.PreviousStatus)
            {
                case Common.Enumerators.TankStatusEnum.Offline:
                    this.Process.PreviousState = this.offlineState;
                    break;
                case Common.Enumerators.TankStatusEnum.Filling:
                    this.Process.PreviousState = this.fillingState;
                    break;
                case Common.Enumerators.TankStatusEnum.FuelExtraction:
                    this.Process.PreviousState = this.extractionState;
                    break;
                case Common.Enumerators.TankStatusEnum.FuelExtractionInit:
                    this.Process.PreviousState = this.extractionInitializedState;
                    break;
                case Common.Enumerators.TankStatusEnum.FillingInit:
                    this.Process.PreviousState = this.fillingInitializedState;
                    break;
                case Common.Enumerators.TankStatusEnum.Waiting:
                    this.Process.PreviousState = this.waitingState;
                    break;
                case Common.Enumerators.TankStatusEnum.Idle:
                    this.Process.PreviousState = this.idleState;
                    break;
                default:
                    this.Process.PreviousState = this.offlineState;
                    break;
            }

            this.Tank.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(Tank_PropertyChanged);
            this.Process.StateChanged += new EventHandler(Process_StateChanged);
        }

        public void ValidateTransitions()
        {
            lock (this)
            {
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
                this.Tank.HasChanges = false;
            }
        }

        #region private methods

        private bool StatusCheck(object _status)
        {
            if (_status == null)
                return false;
            if (_status.GetType() != typeof(Common.Enumerators.TankStatusEnum))
                return false;
            Common.Enumerators.TankStatusEnum status = (Common.Enumerators.TankStatusEnum)_status;
            if (this.Tank.TankStatus == status)
                return true;
            return false;
        }

        private bool IsOnSale(object foo)
        {
            if (this.Process.CurrentState != sallingState && this.Process.CurrentState != this.idleState)
            {
                //foreach (VirtualDevices.VirtualNozzle nozzle in this.Tank.ConnectedNozzles)
                //{
                //    if (nozzle.HasOpenSales)
                //        return true;
                //}
                return false;
            }
            //if (this.Tank.TankDeliverySensed)
            //    return false;

            foreach (VirtualDevices.VirtualNozzle nozzle in this.Tank.ConnectedNozzles)
            {

                if (
                    (
                        nozzle.ParentDispenser.Status != Common.Enumerators.FuelPointStatusEnum.Idle ||
                        nozzle.ParentDispenser.Status != Common.Enumerators.FuelPointStatusEnum.Offline
                    ) && 
                    nozzle.ParentDispenser.ActiveNozzle == nozzle)
                {
                    return true;
                }
                //if (nozzle.HasOpenSales)
                //    return true;
            }
            return false;
        }

        private bool IsIdle(object _status)
        {
            if (this.Process.CurrentState == this.sallingState)
            {
                return !this.IsOnSale(false);
            }
            //if (this.Tank.Alerts.Length > 0 && !this.Tank.IsVirtualTank)
            //    return false;
            bool isIdle = true;
            
            return isIdle;
        }

        private bool IsFilling(object foo)
        {
            if (this.Tank.TankStatus == Common.Enumerators.TankStatusEnum.Filling)
                return true;
            return false;
        }

        private bool IsDraining(object foo)
        {
            if (this.Tank.TankStatus == Common.Enumerators.TankStatusEnum.FuelExtraction)
                return true;
            return false;
        }

        private bool IsDecreasing(object foo)
        {
            if (this.Process.CurrentState == this.idleState && this.Tank.ExtractingByError)
                return true;
            if (this.Process.CurrentState == this.idleState)
                return false;

            if (this.Tank.Alerts.Where(ta => ta.AlertType == Common.Enumerators.AlarmEnum.FuelDecrease).Count() > 0 && this.Process.CurrentState == this.extractionInitializedState)
            {
                return true;
            }

            if (this.Tank.FuelLevelFlow < -1 && this.Tank.FuelVolumeFlow < -20)
            {
                return true;
            }
            if (this.Tank.FuelVolumeFlow < (decimal)-0.75)
                return true;
            return false;

            //decimal currentVolume = this.Tank.GetTankVolume(this.Tank.CurrentFuelLevel);
            //decimal lastVolume = this.Tank.GetTankVolume(this.Tank.LastFuelHeight);
            //decimal currentNormalizedVol = this.Tank.NormalizeVolume(currentVolume, this.Tank.CurrentTemperature, this.Tank.CurrentDensity);
            //decimal lastNormalizedVol = this.Tank.NormalizeVolume(lastVolume, this.Tank.LastTemperature, this.Tank.CurrentDensity);

            

            //if (currentNormalizedVol < lastNormalizedVol && lastNormalizedVol - currentNormalizedVol > 10)
            //    return true;
            //return false;

            //decimal errorThreshhold = this.Tank.CalculateStatisticalErrors(this.Tank.CurrentFuelLevel);
            //if (Math.Abs(currentNormalizedVol - lastNormalizedVol) > errorThreshhold &&
            //    currentNormalizedVol < lastNormalizedVol &&
            //    (this.Process.CurrentState == this.idleState || this.Process.CurrentState == this.levelDecreaseState || this.Process.CurrentState == this.levelIncreaseState || this.Process.CurrentState == this.extractionInitializedState || 
            //    this.Process.CurrentState == this.lowLevelState))
            //    return true;

            //return false;
        }

        private bool IsIncreasing(object foo)
        {
            if (this.Process.CurrentState == this.idleState && this.Tank.FillingByError)
                return true;
            if (this.Process.CurrentState == this.idleState)
                return false;
            if (this.Tank.IsLiterCheck && this.Process.CurrentState == this.fillingInitializedState)
            {
                return true;
            }

            if (this.Tank.FuelVolumeFlow > 20 && this.Tank.FuelLevelFlow > 1)
            {
                return true;
            }
            if(this.Tank.FuelVolumeFlow > (decimal)0.75)
                return true;

            //decimal currentVolume = this.Tank.GetTankVolume(this.Tank.CurrentFuelLevel);
            //decimal lastVolume = this.Tank.GetTankVolume(this.Tank.LastFuelHeight);
            //decimal currentNormalizedVol = this.Tank.NormalizeVolume(currentVolume, this.Tank.CurrentTemperature, this.Tank.CurrentDensity);
            //decimal lastNormalizedVol = this.Tank.NormalizeVolume(lastVolume, this.Tank.LastTemperature, this.Tank.CurrentDensity);

            //decimal errorThreshhold = this.Tank.CalculateStatisticalErrors(this.Tank.CurrentFuelLevel);



            ////if (this.Tank.Alerts.Where(ta => ta.AlertType == Common.Enumerators.AlarmEnum.FuelIncrease).Count() > 0 && this.Process.CurrentState == this.fillingInitializedState)
            ////{
            ////    return true;
            ////}

            //if (currentNormalizedVol > lastNormalizedVol && currentNormalizedVol - lastNormalizedVol > 10)
            //    return true;


            return false;

            //if (Math.Abs(currentNormalizedVol - lastNormalizedVol) > errorThreshhold &&
            //    currentNormalizedVol > lastNormalizedVol &&
            //    (this.Process.CurrentState == this.idleState || this.Process.CurrentState == this.levelDecreaseState || this.Process.CurrentState == this.levelIncreaseState || this.Process.CurrentState == this.fillingInitializedState ||
            //    (this.Process.CurrentState == this.errorState && this.Tank.MinHeight < this.Tank.CurrentFuelLevel) || this.Process.CurrentState == this.lowLevelState))
            //    return true;

            ////if (this.Process.CurrentState == this.errorState && this.Tank.MinHeight >= this.Tank.CurrentFuelLevel)
            ////    return true;

            //return false;
        }

        private bool QueryFilling(object foo)
        {
            bool currentState = (this.Process.CurrentState == this.idleState || 
                this.Process.CurrentState == this.levelIncreaseState || 
                this.Process.CurrentState == this.lowLevelState ||
                (
                    this.Process.CurrentState == this.sallingState && this.Tank.IsLiterCheck
                ) || 
                this.Process.CurrentState == this.errorState || 
                this.Process.CurrentState == this.levelIncreaseState || 
                this.Process.CurrentState == this.offlineState);

            bool ret = this.Tank.InitializeFilling && currentState;
            if (ret)
            {
                if (this.CurrentFillingData == null)
                    this.CurrentFillingData = new Common.Sales.TankFillingData();
                this.CurrentFillingData.StartValues = new Common.TankValues() { FuelHeight = this.Tank.CurrentFuelLevel };
                this.Tank.FillingStartTankLevel = this.Tank.CurrentFuelLevel;
            }
            return this.Tank.InitializeFilling && currentState;
        }

        private bool CancelFilling(object foo)
        {
            return !this.Tank.InitializeFilling;
        }

        private bool QueryExtraction(object foo)
        {
            bool correntState = (this.Process.CurrentState == this.idleState || this.Process.CurrentState == this.levelDecreaseState || this.Process.CurrentState == this.highWaterState);
            bool ret = this.Tank.InitializeExtraction && correntState;
            if (ret)
            {
                if (this.CurrentFillingData == null)
                    this.CurrentFillingData = new Common.Sales.TankFillingData();
                this.CurrentFillingData.StartValues = new Common.TankValues() { FuelHeight = this.Tank.CurrentFuelLevel };
                this.Tank.FillingStartTankLevel = this.Tank.CurrentFuelLevel;
            }
            return this.Tank.InitializeExtraction && correntState;
        }

        private bool CancelExtraction(object foo)
        {
            return !this.Tank.InitializeExtraction;
        }

        private bool IsFillingFinished(object foo)
        {
            return this.Tank.FillingFinished;
        }

        private bool IsExtractionFinished(object foo)
        {
            return this.Tank.ExtractionFinished;
        }

        private bool IsCorrectLevel(object foo)
        {
            if (this.Tank.IsVirtualTank)
                return true;
            if (this.IsIncreasing(null) || this.IsDecreasing(null))
                return false;
            return true;
        }

        private bool IsWaitingFinished(object foo)
        {
            return this.waitingElapsed;
        }

        private bool HasNoAlerts(object foo)
        {
            return !this.HasAlerts();
        }

        private bool HasAlerts(object foo)
        {
            return this.HasAlerts();
        }

        private bool HasNoAlertsOffline(object foo)
        {
            return !this.HasAlerts() && this.Tank.TankStatus != Common.Enumerators.TankStatusEnum.Offline;
        }

        private bool HasAlerts()
        {
            if (Tank.Alerts.Length > 0)
                return true;
            return false;
            //if (this.Tank.IsVirtualTank)
            //    return false;
            //if (this.Tank.TankStatus == Common.Enumerators.TankStatusEnum.Offline)
            //{
            //    VirtualDevices.VirtualTankAlarm alarm = new VirtualDevices.VirtualTankAlarm();
            //    alarm.AlarmTime = DateTime.Now;
            //    alarm.AlarmType = VirtualDevices.VirtualTankAlarm.TankAlarmEnum.TankOffline;
            //    alarm.DeviceId = this.Tank.TankId;
            //    alarm.MessageText = "Δεξαμενή Εκτός Λειτουργίας";

            //    if (this.AlarmRaised != null)
            //        this.AlarmRaised(this, new AlarmRaisedEventArgs(alarm));
                
                
            //    return false;
            //}

            //if (this.Tank.TankStatus == Common.Enumerators.TankStatusEnum.FuelExtraction || this.Tank.TankStatus == Common.Enumerators.TankStatusEnum.Filling || this.Tank.TankStatus == Common.Enumerators.TankStatusEnum.Waiting)
            //{
            //    return false;
            //}

            //bool alarmSuspected = false;
            //ProcessState nextState = null;
            //if (this.Tank.TankStatus == Common.Enumerators.TankStatusEnum.Idle || this.Tank.TankStatus == Common.Enumerators.TankStatusEnum.Error)
            //{
            //    decimal errorThreshhold = this.Tank.CalculateStatisticalErrors(this.Tank.CurrentFuelLevel);
            //    errorThreshhold = (errorThreshhold * 3);/// this.Tank.AlarmThreshold) * 2;
            //    //if (this.Tank.LastSalesVolumeDifference > Math.Abs(errorThreshhold) && Math.Abs(errorThreshhold) > 0)
            //    //{
            //    //    //VirtualDevices.VirtualTankAlarm alarm = new VirtualDevices.VirtualTankAlarm();
            //    //    //alarm.AlarmTime = DateTime.Now;
            //    //    //alarm.AlarmType = VirtualDevices.VirtualTankAlarm.TankAlarmEnum.GaugeStuck;
            //    //    //alarm.DeviceId = this.Tank.TankId;
            //    //    //alarm.MessageText = "Κολλημένη βέργα δεξαμενής";
            //    //    //alarm.AddData("Error Allowed", Math.Abs(errorThreshhold).ToString());
            //    //    //alarm.AddData("Volume Diference", this.Tank.LastSalesVolumeDifference.ToString());

            //    //    //if (this.AlarmRaised != null)
            //    //    //    this.AlarmRaised(this, new AlarmRaisedEventArgs(alarm));
            //    //    //nextState = this.errorState;

            //    //    VirtualDevices.VirtualTankAlarm alarm = new VirtualDevices.VirtualTankAlarm();
            //    //    alarm.AlarmTime = DateTime.Now;
            //    //    alarm.AlarmType = VirtualDevices.VirtualTankAlarm.TankAlarmEnum.LeavelIncrease;
            //    //    alarm.DeviceId = this.Tank.TankId;
            //    //    alarm.MessageText = "Αδικαιολόγητη αύξηση καυσίμου (Πιθανώς κολλημένη βέργα)";
            //    //    alarm.AddData("CurrentFuelLevel", this.Tank.CurrentFuelLevel.ToString("N2"));
            //    //    alarm.AddData("LastFuelLevel", this.Tank.LastFuelHeight.ToString("N2"));
            //    //    alarm.AddData("CurrentTemperature", this.Tank.CurrentTemperature.ToString("N1"));
            //    //    alarm.AddData("LastTemperature", this.Tank.LastTemperature.ToString("N1"));

            //    //    if (this.AlarmRaised != null)
            //    //        this.AlarmRaised(this, new AlarmRaisedEventArgs(alarm));
            //    //    nextState = this.levelIncreaseState;
            //    //    this.Tank.TankStatus = Common.Enumerators.TankStatusEnum.LevelIncrease;

            //    //}
            //}
            //if (this.Tank.CurrentFuelLevel > this.Tank.MaxHeight)
            //{   
            //    VirtualDevices.VirtualTankAlarm alarm = new VirtualDevices.VirtualTankAlarm();
            //    alarm.AlarmTime = DateTime.Now;
            //    alarm.AlarmType = VirtualDevices.VirtualTankAlarm.TankAlarmEnum.MaxHeight;
            //    alarm.DeviceId = this.Tank.TankId;
            //    alarm.MessageText = "Υψηλή Στάθμη Καυσίμου";
            //    alarm.AddData("CurrentFuelLevel", this.Tank.CurrentFuelLevel.ToString());
            //    alarm.AddData("MaxHeight", this.Tank.MaxHeight.ToString());

            //    if(this.AlarmRaised != null)
            //        this.AlarmRaised(this, new AlarmRaisedEventArgs(alarm));
            //    nextState = this.errorState;
            //    this.Tank.TankStatus = Common.Enumerators.TankStatusEnum.Error;
            //}
            //else if (this.Tank.CurrentFuelLevel < this.Tank.MinHeight && (this.Process.CurrentState == this.idleState || this.Process.CurrentState == this.sallingState || this.Process.CurrentState == this.lowLevelState))
            //{
            //    VirtualDevices.VirtualTankAlarm alarm = new VirtualDevices.VirtualTankAlarm();
            //    alarm.AlarmTime = DateTime.Now;
            //    alarm.AlarmType = VirtualDevices.VirtualTankAlarm.TankAlarmEnum.MinHeight;
            //    alarm.DeviceId = this.Tank.TankId;
            //    alarm.MessageText = "Χαμηλή Στάθμη Καυσίμου";

            //    alarm.AddData("CurrentFuelLevel", this.Tank.CurrentFuelLevel.ToString());
            //    alarm.AddData("MinHeight", this.Tank.MinHeight.ToString());
            //    if(this.AlarmRaised != null)
            //        this.AlarmRaised(this, new AlarmRaisedEventArgs(alarm));
            //    nextState = this.lowLevelState;
            //    this.Tank.TankStatus = Common.Enumerators.TankStatusEnum.LowLevel;
            //}
            //else if(this.Tank.CurrentWaterLevel > this.Tank.MaxWaterHeight)
            //{
            //    VirtualDevices.VirtualTankAlarm alarm = new VirtualDevices.VirtualTankAlarm();
            //    alarm.AlarmTime = DateTime.Now;
            //    alarm.AlarmType = VirtualDevices.VirtualTankAlarm.TankAlarmEnum.WaterTooHeight;
            //    alarm.DeviceId = this.Tank.TankId;
            //    alarm.MessageText = "Υψηλή Στάθμη Νερού";
            //    alarm.AddData("CurrentWaterLevel", this.Tank.CurrentWaterLevel.ToString());
            //    alarm.AddData("MaxWaterHeight", this.Tank.MaxWaterHeight.ToString());
            //    if(this.AlarmRaised != null)
            //        this.AlarmRaised(this, new AlarmRaisedEventArgs(alarm));
            //    nextState = this.highWaterState;
            //    this.Tank.TankStatus = Common.Enumerators.TankStatusEnum.HighWaterLevel;
            //}
            //if (this.IsIncreasing(null) && this.Process.CurrentState != this.fillingInitializedState && this.Process.CurrentState != this.fillingState)
            //{
            //    if (errorCheckIndex < 3)
            //    {
            //        errorCheckIndex++;
            //        alarmSuspected = true;
            //    }
            //    else
            //    {
            //        errorCheckIndex = 0;
            //        VirtualDevices.VirtualTankAlarm alarm = new VirtualDevices.VirtualTankAlarm();
            //        alarm.AlarmTime = DateTime.Now;
            //        alarm.AlarmType = VirtualDevices.VirtualTankAlarm.TankAlarmEnum.LeavelIncrease;
            //        alarm.DeviceId = this.Tank.TankId;
            //        alarm.MessageText = "Αδικαιολόγητη αύξηση καυσίμου";
            //        alarm.AddData("CurrentFuelLevel", this.Tank.CurrentFuelLevel.ToString("N2"));
            //        alarm.AddData("LastFuelLevel", this.Tank.LastFuelHeight.ToString("N2"));
            //        alarm.AddData("CurrentTemperature", this.Tank.CurrentTemperature.ToString("N1"));
            //        alarm.AddData("LastTemperature", this.Tank.LastTemperature.ToString("N1"));
            //        if (this.AlarmRaised != null)
            //            this.AlarmRaised(this, new AlarmRaisedEventArgs(alarm));
            //        nextState = this.levelIncreaseState;
            //        this.Tank.TankStatus = Common.Enumerators.TankStatusEnum.LevelIncrease;
            //    }
            //}
            //if (this.IsDecreasing(null) && this.Process.CurrentState != this.extractionInitializedState && this.Process.CurrentState != this.extractionState
            //    && this.Process.CurrentState != sallingState)
            //{
            //    if (errorCheckIndex < 3)
            //    {
            //        errorCheckIndex++;
            //        alarmSuspected = true;
            //    }
            //    else
            //    {
            //        errorCheckIndex = 0;
            //        VirtualDevices.VirtualTankAlarm alarm = new VirtualDevices.VirtualTankAlarm();
            //        alarm.AlarmTime = DateTime.Now;
            //        alarm.AlarmType = VirtualDevices.VirtualTankAlarm.TankAlarmEnum.LevelDecrease;
            //        alarm.DeviceId = this.Tank.TankId;
            //        alarm.MessageText = "Αδικαιολόγητη μείωση καυσίμου";
            //        alarm.AddData("CurrentFuelLevel", this.Tank.CurrentFuelLevel.ToString("N2"));
            //        alarm.AddData("LastFuelLevel", this.Tank.LastFuelHeight.ToString("N2"));
            //        alarm.AddData("CurrentTemperature", this.Tank.CurrentTemperature.ToString("N1"));
            //        alarm.AddData("LastTemperature", this.Tank.LastTemperature.ToString("N1"));

            //        if (this.AlarmRaised != null)
            //            this.AlarmRaised(this, new AlarmRaisedEventArgs(alarm));
            //        nextState = this.levelDecreaseState;
            //        this.Tank.TankStatus = Common.Enumerators.TankStatusEnum.LevelDecrease;
            //    }
            //}
            //if (!alarmSuspected)
            //    errorCheckIndex = 0;

            //if (nextState != null || (nextState == errorState || nextState == lowLevelState || nextState == levelDecreaseState || nextState == levelIncreaseState || nextState == highWaterState))
            //{
            //    this.Process.PreviousState = Process.CurrentState;
            //    this.Process.CurrentState = nextState;
            //    switch (nextState.Name)
            //    {
            //        case "LevelDecrease":
            //            this.Tank.TankStatus = Common.Enumerators.TankStatusEnum.LevelDecrease;
            //            break;
            //        case "LevelIncrease":
            //            this.Tank.TankStatus = Common.Enumerators.TankStatusEnum.LevelIncrease;
            //            break;
            //        case "Error":
            //            this.Tank.TankStatus = Common.Enumerators.TankStatusEnum.Error;
            //            break;
            //        case "LowLevel":
            //            this.Tank.TankStatus = Common.Enumerators.TankStatusEnum.LowLevel;
            //            break;
            //        case "HighWater":
            //            this.Tank.TankStatus = Common.Enumerators.TankStatusEnum.HighWaterLevel;
            //            break;

            //    }
            //    return true;
            //}
            //if (this.Tank.TankStatus == Common.Enumerators.TankStatusEnum.Offline && this.Tank.PreviousStatus != Common.Enumerators.TankStatusEnum.Offline)
            //{
            //    VirtualDevices.VirtualTankAlarm alarm = new VirtualDevices.VirtualTankAlarm();
            //    alarm.AlarmTime = DateTime.Now;
            //    alarm.AlarmType = VirtualDevices.VirtualTankAlarm.TankAlarmEnum.TankOffline;
            //    alarm.DeviceId = this.Tank.TankId;
            //    alarm.MessageText = "Αποσύνδεση Δεξαμενής";
                
            //    if (this.AlarmRaised != null)
            //        this.AlarmRaised(this, new AlarmRaisedEventArgs(alarm));
            //}
            //if (this.Tank.TankStatus == Common.Enumerators.TankStatusEnum.Error)
            //{
            //    if (this.IsOnSale(null))
            //        this.Tank.TankStatus = Common.Enumerators.TankStatusEnum.Selling;
            //    else
            //        this.Tank.TankStatus = Common.Enumerators.TankStatusEnum.Idle;
            //}
            //return false;
        }

        private void WaitingElapsed(object foo)
        {
            lock (this)
            {
                this.waitingElapsed = true;
            }
        }

        #endregion

        #region public Methods

        public void SetValues(object _values)
        {
            Common.TankValues values = _values as Common.TankValues;
            
            if (values.CurrentTemperatur != Tank.CurrentTemperature)
                this.Tank.CurrentTemperature = values.CurrentTemperatur;
            //if (Math.Abs(values.FuelHeight + this.Tank.FuelOffset - Tank.CurrentFuelLevel) > (decimal)0.01)
            //    this.Tank.CurrentFuelLevel = values.FuelHeight + this.Tank.FuelOffset;
            //if (Math.Abs(values.FuelHeight - Tank.CurrentFuelLevel) > 0)
            this.Tank.CurrentFuelLevel = values.FuelHeight + this.Tank.FuelOffset;
            if (values.WaterHeight != Tank.CurrentWaterLevel)
                this.Tank.CurrentWaterLevel = values.WaterHeight + this.Tank.WaterOffset;
        }

        #endregion

        #region Events

        void Process_StateChanged(object sender, EventArgs e)
        {
            if (this.Process.CurrentState == this.idleState)
            {
                if (this.Process.PreviousState == this.sallingState)
                {
                    lastSaleEnded = DateTime.Now;
                }
                bool hasChanges = this.Tank.HasChanges;
                this.Tank.ExtractionFinished = false;
                this.Tank.FillingFinished = false;
                this.Tank.InitializeExtraction = false;
                this.Tank.InitializeFilling = false;
                this.Tank.FillingByError = false;
                this.Tank.ExtractingByError = false;
                this.Tank.HasChanges = hasChanges;
                this.Tank.IsLiterCheck = false;
                if(this.Tank.CurrentFuelLevel > 0)
                    this.Tank.LastCalculatedStart = this.Tank.CurrentFuelLevel;
                if (this.Process.PreviousState == this.waitingState)
                {
                    if (this.FillingCompleted != null)
                    {
                        if (this.CurrentFillingData.StartValues == null)
                        {
                            this.CurrentFillingData.StartValues = new Common.TankValues() { FuelHeight = this.Tank.FillingStartTankLevel };
                        }
                        this.CurrentFillingData.EndValues = this.Tank.TankValues;
                        this.FillingCompleted(this, new TankFillingEventArgs(this.CurrentFillingData));
                    }
                    this.CurrentFillingData = null;
                    this.Tank.InvoiceTypeId = Guid.Empty;
                    this.Tank.VehicleId = Guid.Empty;
                    this.Tank.FillingFuelTypeId = Guid.Empty;
                    this.Tank.InvoiceLineId = Guid.Empty;
                }
                this.Tank.WaitingStarted = DateTime.MinValue;
                this.Tank.WaitingShouldEnd = DateTime.MinValue;
                
            }
            else if (this.Process.CurrentState == this.waitingState && this.Process.PreviousState == this.fillingState)
            {
                this.waitingElapsed = false;
                this.CurrentFillingData = new Common.Sales.TankFillingData();
                this.CurrentFillingData.TankId = this.Tank.TankId;
                this.CurrentFillingData.Mode = Common.Sales.TankFillingData.DataModeEnum.Filling;
                this.CurrentFillingData.InvoiceLineId = this.Tank.InvoiceLineId;
                this.CurrentFillingData.InvoiceTypeId = this.Tank.InvoiceTypeId;
                this.CurrentFillingData.FuelTypeId = this.Tank.FillingFuelTypeId;
                this.CurrentFillingData.VehicelId = this.Tank.VehicleId;
                this.CurrentFillingData.DeliveryStarted = this.Tank.DeliveryStarted;//.deliveryStarted;
                this.Tank.WaitingStarted = DateTime.Now;
                if (this.Tank.IsLiterCheck)
                {
                    this.CurrentWaitingTime = TimeSpan.FromMilliseconds(this.Tank.LiterCheckTime);
                    this.waitingTimer = new System.Threading.Timer(new System.Threading.TimerCallback(this.WaitingElapsed), null, this.Tank.LiterCheckTime, System.Threading.Timeout.Infinite);
                    this.Tank.WaitingShouldEnd = this.Tank.WaitingStarted.Add(TimeSpan.FromMilliseconds(this.Tank.LiterCheckTime));
                }
                else
                {
                    this.CurrentWaitingTime = TimeSpan.FromMilliseconds(this.Tank.DeliveryTime);
                    this.waitingTimer = new System.Threading.Timer(new System.Threading.TimerCallback(this.WaitingElapsed), null, this.Tank.DeliveryTime, System.Threading.Timeout.Infinite);
                    this.Tank.WaitingShouldEnd = this.Tank.WaitingStarted.Add(TimeSpan.FromMilliseconds(this.Tank.DeliveryTime));
                }
                
            }
            else if (this.Process.CurrentState == this.waitingState && this.Process.PreviousState == this.extractionState)
            {
                this.waitingElapsed = false;
                this.CurrentFillingData = new Common.Sales.TankFillingData();
                this.CurrentFillingData.TankId = this.Tank.TankId;
                this.CurrentFillingData.Mode = Common.Sales.TankFillingData.DataModeEnum.Extraction;
                this.CurrentFillingData.InvoiceLineId = this.Tank.InvoiceLineId;
                this.CurrentFillingData.InvoiceTypeId = this.Tank.InvoiceTypeId;
                this.CurrentFillingData.FuelTypeId = this.Tank.FillingFuelTypeId;
                this.CurrentFillingData.VehicelId = this.Tank.VehicleId;
                //this.CurrentFillingData.DeliveryStarted = this.deliveryStarted;
                this.CurrentFillingData.DeliveryStarted = this.Tank.DeliveryStarted;
                this.Tank.WaitingStarted = DateTime.Now;
                this.CurrentWaitingTime = TimeSpan.FromMilliseconds(this.Tank.DeliveryTime);
                this.waitingTimer = new System.Threading.Timer(new System.Threading.TimerCallback(this.WaitingElapsed), null, this.Tank.DeliveryTime, System.Threading.Timeout.Infinite);
            }
            else if (this.Process.CurrentState == this.errorState)
            {
                this.Tank.TankStatus = this.Tank.ErrorStatus;
            }
            switch (this.Process.CurrentState.Name)
            {
                case "Offline":
                    this.Tank.TankStatus = Common.Enumerators.TankStatusEnum.Offline;
                    break;
                case "Idle":
                    this.Tank.TankStatus = Common.Enumerators.TankStatusEnum.Idle;
                    break;
                case "Selling":
                    this.Tank.TankStatus = Common.Enumerators.TankStatusEnum.Selling;
                    break;
                case "FillingInitialized":
                    this.Tank.TankStatus = Common.Enumerators.TankStatusEnum.FillingInit;
                    
                    break;
                case "Filling":
                    this.Tank.TankStatus = Common.Enumerators.TankStatusEnum.Filling;
                    this.deliveryStarted = DateTime.Now;
                    this.Tank.DeliveryStarted = this.deliveryStarted;
                    break;
                case "Waiting":
                    this.Tank.TankStatus = Common.Enumerators.TankStatusEnum.Waiting;
                    break;
                case "ExtractionInitialized":
                    this.Tank.TankStatus = Common.Enumerators.TankStatusEnum.FuelExtractionInit;
                    break;
                case "Extraction":
                    this.Tank.TankStatus = Common.Enumerators.TankStatusEnum.FuelExtraction;
                    this.deliveryStarted = DateTime.Now;
                    this.Tank.DeliveryStarted = this.deliveryStarted;
                    break;
                case "LevelIncrease":
                    this.Tank.TankStatus = Common.Enumerators.TankStatusEnum.LevelIncrease;
                    break;
                case "LevelDecrease":
                    this.Tank.TankStatus = Common.Enumerators.TankStatusEnum.LevelDecrease;
                    break;
                case "WaitingEllapsed":
                    this.Tank.TankStatus = Common.Enumerators.TankStatusEnum.WaitingEllapsed;
                    break;
                //case "Error":
                //    if(this.Tank.Alerts.Where(a=>((VirtualDevices.VirtualTankAlarm)a).AlertType == Common.Enumerators.AlarmEnum.FuelIncrease).Count() > 0)
                //        this.Tank.TankStatus = Common.Enumerators.TankStatusEnum.LevelIncrease;
                //    else if (this.Tank.Alerts.Where(a => ((VirtualDevices.VirtualTankAlarm)a).AlertType == Common.Enumerators.AlarmEnum.FuelDecrease).Count() > 0)
                //        this.Tank.TankStatus = Common.Enumerators.TankStatusEnum.LevelDecrease;
                //    else if (this.Tank.Alerts.Where(a => ((VirtualDevices.VirtualTankAlarm)a).AlertType == Common.Enumerators.AlarmEnum.FuelTooHigh).Count() > 0)
                //        this.Tank.TankStatus = Common.Enumerators.TankStatusEnum.HighLevel;
                //    else if (this.Tank.Alerts.Where(a => ((VirtualDevices.VirtualTankAlarm)a).AlertType == Common.Enumerators.AlarmEnum.FuelTooLow).Count() > 0)
                //        this.Tank.TankStatus = Common.Enumerators.TankStatusEnum.LowLevel;
                //    else if (this.Tank.Alerts.Where(a => ((VirtualDevices.VirtualTankAlarm)a).AlertType == Common.Enumerators.AlarmEnum.WaterTooHigh).Count() > 0)
                //        this.Tank.TankStatus = Common.Enumerators.TankStatusEnum.HighWaterLevel;
                //    else
                //        this.Tank.TankStatus = Common.Enumerators.TankStatusEnum.Error;
                //    break;
                //case "LowLevel":
                //    this.Tank.TankStatus = Common.Enumerators.TankStatusEnum.LowLevel;
                //    break;
                //case "HighWater":
                //    this.Tank.TankStatus = Common.Enumerators.TankStatusEnum.HighWaterLevel;
                //    break; 
            }
            System.Console.WriteLine(string.Format("Tank : {0}  State : {1}", this.Tank.TankNumber, this.Process.CurrentState));

            if (this.ProcessStateChanged != null)
                this.ProcessStateChanged(this, new EventArgs());
        }

        void Tank_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            
        }

        #endregion
    }

    public class TankFillingEventArgs : EventArgs
    {
        public Common.Sales.TankFillingData Data { set; get; }

        public TankFillingEventArgs(Common.Sales.TankFillingData data)
        {
            this.Data = data;
        }
    }
    
}

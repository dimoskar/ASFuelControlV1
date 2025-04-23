using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telerik.OpenAccess;
using ASFuelControl.VirtualDevices;

namespace ASFuelControl.Windows.Threads
{
    /// <summary>
    /// Handles all alerts of the virtual devices.
    /// </summary>
    public class AlertHandler
    {
        public event EventHandler AlarmAdded;
        public event EventHandler AlarmResolved;

        private static AlertHandler instance = null;

        public static AlertHandler Instance
        {
            get 
            {
                if (instance == null)
                    instance = new AlertHandler();
                return instance;
            }
        }

        private Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection);
        private bool runThread = false;

        public VirtualDevices.VirtualTank[] Tanks { set; get; }
        public VirtualDevices.VirtualDispenser[] Dispensers { set; get; }

        public void WireUpEvents()
        {
            foreach (VirtualDevices.VirtualTank tank in this.Tanks)
            {
                tank.AlertAdded -= new EventHandler(device_AlertAdded);
                tank.AlertRemoved -= new EventHandler(device_AlertRemoved);
                tank.AlertAdded += new EventHandler(device_AlertAdded);
                tank.AlertRemoved += new EventHandler(device_AlertRemoved);
            }
            foreach (VirtualDevices.VirtualDispenser dispenser in this.Dispensers)
            {
                dispenser.AlertAdded -= new EventHandler(device_AlertAdded);
                dispenser.AlertRemoved -= new EventHandler(device_AlertRemoved);
                dispenser.AlertAdded += new EventHandler(device_AlertAdded);
                dispenser.AlertRemoved += new EventHandler(device_AlertRemoved);
                foreach (VirtualDevices.VirtualNozzle nozzle in dispenser.Nozzles)
                {
                    nozzle.AlertAdded -= new EventHandler(device_AlertAdded);
                    nozzle.AlertRemoved -= new EventHandler(device_AlertRemoved);
                    nozzle.AlertAdded += new EventHandler(device_AlertAdded);
                    nozzle.AlertRemoved += new EventHandler(device_AlertRemoved);
                }
            }
        }


        /// <summary>
        /// Discovers not resolved alerts and adds them in the device alerts list after programm restart. 
        /// </summary>
        public void AddAlertsFromDatabase()
        {
            foreach (VirtualDevices.VirtualTank tank in this.Tanks)
            {
                List<Data.SystemEvent> tankAlerts = this.database.SystemEvents.Where(se => se.TankId == tank.TankId && !se.ResolvedDate.HasValue).ToList();
                this.AddExistingTankAlerts(tank, tankAlerts);
            }
            foreach (VirtualDevices.VirtualDispenser dispenser in this.Dispensers)
            {
                List<Data.SystemEvent> dispAlerts = this.database.SystemEvents.Where(se => se.DispenserId == dispenser.DispenserId && !se.ResolvedDate.HasValue).ToList();
                this.AddExistingDispenserAlerts(dispenser, dispAlerts);
                foreach (VirtualDevices.VirtualNozzle nozzle in dispenser.Nozzles)
                {
                    List<Data.SystemEvent> nozAlerts = this.database.SystemEvents.Where(se => se.NozzleId == nozzle.NozzleId && !se.ResolvedDate.HasValue).ToList();
                    this.AddExistingNozzleAlerts(nozzle, nozAlerts);
                }
            }
        }

        public void CheckForAlerts()
        {
            foreach (VirtualDevices.VirtualTank tank in this.Tanks)
                CheckForTankAlerts(tank);
            foreach (VirtualDevices.VirtualDispenser dispenser in this.Dispensers)
                this.CheckForDispenserAlerts(dispenser);
        }

        /// <summary>
        /// Check if alerts should be raised on a tank 
        /// </summary>
        /// <param name="tank"></param>
        public void CheckForTankAlerts(VirtualDevices.VirtualTank tank)
        {
            if (tank.IsVirtualTank)
            {
                List<VirtualBaseAlarm> oldAlerts = tank.Alerts.ToList();
                foreach (VirtualBaseAlarm alert in oldAlerts)
                    tank.RemoveAlert(alert);
                return;
            }
            Common.Enumerators.TankStatusEnum newStatus = Common.Enumerators.TankStatusEnum.Idle;
            List<VirtualDevices.VirtualTankAlarm> alerts = new List<VirtualDevices.VirtualTankAlarm>();
            try
            {
                tank.AlertStatuses.Clear();
                tank.ClearAlerts();
                
                Common.Enumerators.TankStatusEnum currentStatus = tank.TankStatus;
                bool alarmSuspected = false;

                if (tank.TankStatus == Common.Enumerators.TankStatusEnum.Offline)
                    tank.AlertStatuses.Add(Common.Enumerators.TankStatusEnum.Offline);
                else
                {
                    if (tank.TankStatus == Common.Enumerators.TankStatusEnum.Idle || tank.TankStatus == Common.Enumerators.TankStatusEnum.Error)
                    {
                        decimal errorThreshhold = tank.CalculateStatisticalErrors(tank.CurrentFuelLevel);
                        errorThreshhold = (errorThreshhold * 3);/// tank.AlarmThreshold) * 2;
                    }
                    if (tank.CurrentFuelLevel > tank.MaxHeight)
                        tank.AlertStatuses.Add(Common.Enumerators.TankStatusEnum.HighLevel);
                    if (tank.CurrentFuelLevel < tank.MinHeight)
                        tank.AlertStatuses.Add(Common.Enumerators.TankStatusEnum.LowLevel);
                    if (tank.CurrentWaterLevel > tank.MaxWaterHeight)
                        tank.AlertStatuses.Add(Common.Enumerators.TankStatusEnum.HighWaterLevel);
                    if (IsIncreasing(tank, currentStatus))
                    {
                        tank.AlertStatuses.Add(Common.Enumerators.TankStatusEnum.LevelIncrease);
                    }
                    
                    if (IsDecreasing(tank, currentStatus))
                    {
                        tank.AlertStatuses.Add(Common.Enumerators.TankStatusEnum.LevelDecrease);
                    }
                    if (!alarmSuspected)
                        tank.ErrorIndex = 0;

                    Common.Enumerators.TankStatusEnum[] aStatuses = tank.AlertStatuses.ToArray();
                    foreach (Common.Enumerators.TankStatusEnum alertStatus in aStatuses)
                    {

                        if (alertStatus == Common.Enumerators.TankStatusEnum.LevelDecrease && DateTime.Now.Subtract(tank.LastSaleEnd).TotalSeconds < 10)
                            tank.AlertStatuses.Remove(alertStatus);
                        else if (alertStatus == Common.Enumerators.TankStatusEnum.LevelDecrease && tank.TankStatus == Common.Enumerators.TankStatusEnum.Selling)
                            tank.AlertStatuses.Remove(alertStatus);
                        else if (alertStatus == Common.Enumerators.TankStatusEnum.LevelIncrease && (tank.TankStatus == Common.Enumerators.TankStatusEnum.FillingInit || tank.TankStatus == Common.Enumerators.TankStatusEnum.Filling))
                            tank.AlertStatuses.Remove(alertStatus);
                        else if (alertStatus == Common.Enumerators.TankStatusEnum.LevelDecrease && (tank.TankStatus == Common.Enumerators.TankStatusEnum.FuelExtractionInit || tank.TankStatus == Common.Enumerators.TankStatusEnum.FuelExtraction))
                            tank.AlertStatuses.Remove(alertStatus);
                        else if (alertStatus == Common.Enumerators.TankStatusEnum.LevelDecrease && tank.TankStatus == Common.Enumerators.TankStatusEnum.Waiting)
                            tank.AlertStatuses.Remove(alertStatus);
                        else if (alertStatus == Common.Enumerators.TankStatusEnum.LevelIncrease && tank.TankStatus == Common.Enumerators.TankStatusEnum.Waiting)
                            tank.AlertStatuses.Remove(alertStatus);
                        else if (alertStatus == Common.Enumerators.TankStatusEnum.LowLevel && (tank.TankStatus == Common.Enumerators.TankStatusEnum.FillingInit || tank.TankStatus == Common.Enumerators.TankStatusEnum.Filling || tank.TankStatus == Common.Enumerators.TankStatusEnum.Waiting))
                            tank.AlertStatuses.Remove(alertStatus);
                    }
                }
            }
            catch
            {
            }
            finally
            {
                foreach (Common.Enumerators.TankStatusEnum alertStatus in tank.AlertStatuses)
                {
                    VirtualTankAlarm alarm = CreateTankAlert(tank, alertStatus);
                    alerts.Add(alarm);
                }
                List<VirtualTankAlarm> listToRemove = new List<VirtualTankAlarm>();
                foreach (VirtualDevices.VirtualTankAlarm oldAlarm in tank.Alerts)
                {
                    VirtualTankAlarm newAlarm = alerts.Where(a => a.AlertType == oldAlarm.AlertType).FirstOrDefault();
                    if (newAlarm != null)
                        continue;
                    oldAlarm.ResolvedTime = DateTime.Now;
                    oldAlarm.ResolveText = "";
                    listToRemove.Add(oldAlarm);
                }
                foreach (VirtualTankAlarm alarm in listToRemove)
                {
                    tank.RemoveAlert(alarm);
                    bool suspendLevelCorrection = false;
                    if (tank.PreviousStatus == Common.Enumerators.TankStatusEnum.LevelIncrease && tank.TankStatus == Common.Enumerators.TankStatusEnum.FillingInit)
                        suspendLevelCorrection = true;
                    else if (tank.PreviousStatus == Common.Enumerators.TankStatusEnum.LevelDecrease && tank.TankStatus == Common.Enumerators.TankStatusEnum.FuelExtractionInit)
                        suspendLevelCorrection = true;
                    else if (tank.PreviousStatus == Common.Enumerators.TankStatusEnum.LevelDecrease && tank.TankStatus == Common.Enumerators.TankStatusEnum.Idle)
                        suspendLevelCorrection = true;
                    else if (tank.PreviousStatus == Common.Enumerators.TankStatusEnum.LevelIncrease && tank.TankStatus == Common.Enumerators.TankStatusEnum.Idle)
                        suspendLevelCorrection = true;
                    else if (tank.TankStatus == Common.Enumerators.TankStatusEnum.Offline)
                        suspendLevelCorrection = true;

                    ResolveAlarm(alarm, suspendLevelCorrection);
                }
                foreach (VirtualDevices.VirtualTankAlarm tAlarm in alerts)
                {
                    tank.AddAlert(tAlarm);
                    if (tAlarm.AlertType == Common.Enumerators.AlarmEnum.FuelIncrease && tank.TankStatus == Common.Enumerators.TankStatusEnum.Idle)
                    {
                        tank.TankStatus = Common.Enumerators.TankStatusEnum.LevelIncrease;
                    }
                    else if (tAlarm.AlertType == Common.Enumerators.AlarmEnum.FuelDecrease && tank.TankStatus == Common.Enumerators.TankStatusEnum.Idle)
                    {
                        tank.TankStatus = Common.Enumerators.TankStatusEnum.LevelDecrease;
                    }
                    else if (tAlarm.AlertType == Common.Enumerators.AlarmEnum.FuelTooHigh && tank.TankStatus == Common.Enumerators.TankStatusEnum.Idle)
                    {
                        tank.TankStatus = Common.Enumerators.TankStatusEnum.HighLevel;
                    }
                    else if (tAlarm.AlertType == Common.Enumerators.AlarmEnum.FuelTooLow && tank.TankStatus == Common.Enumerators.TankStatusEnum.Idle)
                    {
                        tank.TankStatus = Common.Enumerators.TankStatusEnum.LowLevel;
                    }
                    else if (tAlarm.AlertType == Common.Enumerators.AlarmEnum.WaterTooHigh && tank.TankStatus == Common.Enumerators.TankStatusEnum.Idle)
                    {
                        tank.TankStatus = Common.Enumerators.TankStatusEnum.HighWaterLevel;
                    }
                }
                if(alerts.Count == 0)
                    tank.TankStatus = newStatus;
            }
        }

        /// <summary>
        /// Check if alerts should be raised on a Dispenser
        /// </summary>
        /// <param name="dispenser"></param>
        public void CheckForDispenserAlerts(VirtualDevices.VirtualDispenser dispenser)
        {
            dispenser.ClearAlerts();
            List<VirtualDevices.VirtualDispenserAlarm> alerts = new List<VirtualDevices.VirtualDispenserAlarm>();
            try
            {
                if (dispenser.Status == Common.Enumerators.FuelPointStatusEnum.Offline)
                {
                    VirtualDevices.VirtualDispenserAlarm alarm = new VirtualDevices.VirtualDispenserAlarm();

                    alarm.AlarmTime = DateTime.Now;
                    alarm.AlertType = Common.Enumerators.AlarmEnum.FuelPumpOffline;
                    alarm.DeviceId = dispenser.DispenserId;
                    alarm.DeviceDescription = "Αντλία : " + dispenser.OfficialNumber;
                    alarm.MessageText = "Αντλία Εκτός Λειτουργίας";
                    alerts.Add(alarm);
                }
                foreach (VirtualDevices.VirtualNozzle nozzle in dispenser.Nozzles)
                    CheckForNozzleAlerts(nozzle);
            }
            catch
            {
            }
            finally
            {
                List<VirtualDispenserAlarm> listToRemove = new List<VirtualDispenserAlarm>();
                foreach (VirtualDevices.VirtualDispenserAlarm oldAlarm in dispenser.Alerts)
                {
                    VirtualDispenserAlarm newAlarm = alerts.Where(a => a.AlertType == oldAlarm.AlertType).FirstOrDefault();
                    if (newAlarm != null)
                        continue;
                    oldAlarm.ResolvedTime = DateTime.Now;
                    oldAlarm.ResolveText = "";
                }
                foreach (VirtualDispenserAlarm alarm in listToRemove)
                {
                    dispenser.RemoveAlert(alarm);
                    ResolveAlarm(alarm);
                }
                foreach (VirtualDevices.VirtualDispenserAlarm tAlarm in alerts)
                {
                    dispenser.AddAlert(tAlarm);
                }
                
            }
        }

        /// <summary>
        /// Check if alerts should be raised on a Nozzle
        /// </summary>
        /// <param name="dispenser"></param>
        private void CheckForNozzleAlerts(VirtualDevices.VirtualNozzle nozzle)
        {
            nozzle.ClearAlerts();
            bool suspendCheck = false;
            List<VirtualDevices.VirtualNozzleAlarm> alerts = new List<VirtualDevices.VirtualNozzleAlarm>();
            try
            {
                if (nozzle.LastVolumeCounter != nozzle.TotalVolumeCounter && nozzle.LastVolumeCounter > 0 && nozzle.TotalVolumeCounter > 0)
                {
                    if (DateTime.Now.Subtract(nozzle.ParentDispenser.LastIdleTime).TotalSeconds < 2)
                    {
                        suspendCheck = true;
                        return;
                    }
                    decimal diff = System.Math.Abs(nozzle.TotalVolumeCounter - nozzle.LastVolumeCounter);
                    if (Math.Abs(diff) <= 1)
                    {
                        return;
                    }
                    else
                    {
                        VirtualDevices.VirtualNozzleAlarm alarm = new VirtualDevices.VirtualNozzleAlarm();
                        alarm.AlarmTime = DateTime.Now;
                        alarm.AlarmType = VirtualDevices.VirtualNozzleAlarm.NozzleAlarmEnum.TotalizerMismatch;
                        alarm.DeviceId = nozzle.NozzleId;
                        alarm.DeviceDescription = "Ακροσωλήνιο : " + nozzle.ParentDispenser.OfficialNumber + " " + nozzle.NozzleOfficialNumber;
                        alarm.AddData("TotalVolumeCounter", nozzle.TotalVolumeCounter.ToString("N2"));
                        alarm.AddData("LastVolumeCounter", nozzle.LastVolumeCounter.ToString("N2"));
                        alarm.MessageText = "Σφάλμα μετρητή ακροσωλήνιου";

                        alerts.Add(alarm);
                    }
                }
            }
            catch
            {
            }
            finally
            {
                if (!suspendCheck)
                {
                    List<VirtualNozzleAlarm> listToRemove = new List<VirtualNozzleAlarm>();
                    foreach (VirtualDevices.VirtualNozzleAlarm oldAlarm in nozzle.Alerts)
                    {
                        VirtualNozzleAlarm newAlarm = alerts.Where(a => a.AlertType == oldAlarm.AlertType).FirstOrDefault();
                        if (newAlarm != null)
                            continue;
                        oldAlarm.ResolvedTime = DateTime.Now;
                        oldAlarm.ResolveText = "";
                    }
                    foreach (VirtualNozzleAlarm alarm in listToRemove)
                    {
                        nozzle.RemoveAlert(alarm);
                        ResolveAlarm(alarm);
                    }
                    foreach (VirtualDevices.VirtualNozzleAlarm tAlarm in alerts)
                    {
                        nozzle.AddAlert(tAlarm);
                    }
                }
            }
        }

        /// <summary>
        /// Discovers not resolved alerts and adds them in the Tank alerts list after programm restart. 
        /// </summary>
        public void AddExistingTankAlerts(VirtualDevices.VirtualTank tank, List<Data.SystemEvent> alerts)
        {
            if (alerts == null || alerts.Count == 0)
                return;
            foreach (Data.SystemEvent se in alerts)
            {
                VirtualDevices.VirtualTankAlarm alarm = new VirtualDevices.VirtualTankAlarm();
                alarm.DatabaseEntityId = se.EventId;
                alarm.ExistingAlarm = true;
                alarm.DeviceDescription = se.DeviceDescription;
                alarm.DeviceId = tank.TankId;
                alarm.MessageText = se.Message;
                alarm.AlarmTime = se.EventDate;
                alarm.AlertType = (Common.Enumerators.AlarmEnum)se.AlarmType;
                //alarm.AlarmType = VirtualTankAlarm.TankAlarmEnum
                foreach (Data.SystemEventDatum data in se.SystemEventData)
                {
                    alarm.AddData(data.PropertyName, data.Value);
                }
                tank.AddAlert(alarm);
            }
        }

        /// <summary>
        /// Discovers not resolved alerts and adds them in the Dispenser alerts list after programm restart. 
        /// </summary>
        public void AddExistingDispenserAlerts(VirtualDevices.VirtualDispenser dispenser, List<Data.SystemEvent> alerts)
        {
            if(alerts == null || alerts.Count == 0)
                return;
            foreach (Data.SystemEvent se in alerts)
            {
                VirtualDevices.VirtualDispenserAlarm alarm = new VirtualDevices.VirtualDispenserAlarm();
                alarm.DatabaseEntityId = se.EventId;
                alarm.ExistingAlarm = true;
                alarm.DeviceDescription = se.DeviceDescription;
                alarm.DeviceId = dispenser.DispenserId;
                alarm.MessageText = se.Message;
                alarm.AlarmTime = se.EventDate;
                alarm.AlertType = (Common.Enumerators.AlarmEnum)se.AlarmType;

                foreach (Data.SystemEventDatum data in se.SystemEventData)
                {
                    alarm.AddData(data.PropertyName, data.Value);
                }
                dispenser.AddAlert(alarm);
            }
        }

        /// <summary>
        /// Discovers not resolved alerts and adds them in the Nozzle alerts list after programm restart. 
        /// </summary>
        public void AddExistingNozzleAlerts(VirtualDevices.VirtualNozzle nozzle, List<Data.SystemEvent> alerts)
        {
            if (alerts == null || alerts.Count == 0)
                return;
            foreach (Data.SystemEvent se in alerts)
            {
                VirtualDevices.VirtualNozzleAlarm alarm = new VirtualDevices.VirtualNozzleAlarm();
                alarm.DatabaseEntityId = se.EventId;
                alarm.ExistingAlarm = true;
                alarm.DeviceDescription = se.DeviceDescription;
                alarm.DeviceId = nozzle.NozzleId;
                alarm.MessageText = se.Message;
                alarm.AlarmTime = se.EventDate;
                alarm.AlertType = (Common.Enumerators.AlarmEnum)se.AlarmType;

                foreach (Data.SystemEventDatum data in se.SystemEventData)
                {
                    alarm.AddData(data.PropertyName, data.Value);
                }
                nozzle.AddAlert(alarm);
            }
        }

        /// <summary>
        /// If tank has alerts this method adds the specific alert in the device alert list
        /// </summary>
        /// <param name="tank"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        private VirtualDevices.VirtualTankAlarm CreateTankAlert(VirtualDevices.VirtualTank tank, Common.Enumerators.TankStatusEnum status)
        {
            VirtualDevices.VirtualTankAlarm alarm = new VirtualDevices.VirtualTankAlarm();
            alarm.AlarmTime = DateTime.Now;
            alarm.DeviceId = tank.TankId;
            alarm.DeviceDescription = "Δεξαμενή " + tank.TankNumber.ToString();

            if (status == Common.Enumerators.TankStatusEnum.HighLevel)
            {
                alarm.AlertType = Common.Enumerators.AlarmEnum.FuelTooHigh;
                alarm.MessageText = "Υψηλή Στάθμη Καυσίμου";
                alarm.AddData("CurrentFuelLevel", tank.CurrentFuelLevel.ToString("N2"));
                alarm.AddData("MaxHeight", tank.MaxHeight.ToString("N2"));
            }
            else if (status == Common.Enumerators.TankStatusEnum.LowLevel)
            {
                alarm.AlertType = Common.Enumerators.AlarmEnum.FuelTooLow;
                alarm.MessageText = "Χαμηλή Στάθμη Καυσίμου";
                alarm.AddData("CurrentFuelLevel", tank.CurrentFuelLevel.ToString("N2"));
                alarm.AddData("MinHeight", tank.MinHeight.ToString("N2"));
            }
            else if (status == Common.Enumerators.TankStatusEnum.HighWaterLevel)
            {
                alarm.AlertType = Common.Enumerators.AlarmEnum.WaterTooHigh;
                alarm.MessageText = "Υψηλή Στάθμη Νερού";
                alarm.AddData("CurrentWaterLevel", tank.CurrentWaterLevel.ToString("N2"));
                alarm.AddData("MaxWaterHeight", tank.MaxWaterHeight.ToString("N2"));
            }
            else if (status == Common.Enumerators.TankStatusEnum.LevelIncrease)
            {
                alarm.AlertType = Common.Enumerators.AlarmEnum.FuelIncrease;
                alarm.MessageText = "Αδικαιολόγητη αύξηση καυσίμου";
                alarm.AddData("CurrentFuelLevel", tank.CurrentFuelLevel.ToString("N2"));
                alarm.AddData("LastFuelLevel", tank.LastFuelHeight.ToString("N2"));
                alarm.AddData("CurrentTemperature", tank.CurrentTemperature.ToString("N1"));
                alarm.AddData("LastTemperature", tank.LastTemperature.ToString("N1"));
                alarm.AddData("CurrentVolume", tank.GetTankVolume(tank.CurrentFuelLevel).ToString("N2"));
                alarm.AddData("LastVolume", tank.GetTankVolume(tank.LastFuelHeight).ToString("N2"));
            }
            else if (status == Common.Enumerators.TankStatusEnum.LevelDecrease)
            {
                alarm.AlertType = Common.Enumerators.AlarmEnum.FuelDecrease;
                alarm.MessageText = "Αδικαιολόγητη μείωση καυσίμου";
                alarm.AddData("CurrentFuelLevel", tank.CurrentFuelLevel.ToString("N2"));
                alarm.AddData("LastFuelLevel", tank.LastFuelHeight.ToString("N2"));
                alarm.AddData("CurrentTemperature", tank.CurrentTemperature.ToString("N1"));
                alarm.AddData("LastTemperature", tank.LastTemperature.ToString("N1"));
                alarm.AddData("CurrentVolume", tank.GetTankVolume(tank.CurrentFuelLevel).ToString("N2"));
                alarm.AddData("LastVolume", tank.GetTankVolume(tank.LastFuelHeight).ToString("N2"));
            }
            else if (status == Common.Enumerators.TankStatusEnum.Offline)
            {
                alarm.AlertType = Common.Enumerators.AlarmEnum.TankOffline;
                alarm.MessageText = "Αποσύνδεση Δεξαμενής";
            }

            return alarm;
        }

        //private bool IsInErrorState(VirtualDevices.VirtualTank tank)
        //{
        //    switch (tank.TankStatus)
        //    {
        //        case Common.Enumerators.TankStatusEnum.Error:
        //        case Common.Enumerators.TankStatusEnum.HighLevel:
        //        case Common.Enumerators.TankStatusEnum.HighWaterLevel:
        //        case Common.Enumerators.TankStatusEnum.LevelDecrease:
        //        case Common.Enumerators.TankStatusEnum.LevelIncrease:
        //        case Common.Enumerators.TankStatusEnum.LowLevel:
        //            return true;
        //    }
        //    return false;
        //}

        /// <summary>
        /// Checks if the tanklevel is decreasing
        /// </summary>
        /// <param name="tank"></param>
        /// <param name="currentStatus"></param>
        /// <returns></returns>
        private bool IsDecreasing(VirtualDevices.VirtualTank tank, Common.Enumerators.TankStatusEnum currentStatus)
        {

            decimal currentVolume = tank.GetTankVolume(tank.CurrentFuelLevel);
            decimal lastVolume = tank.GetTankVolume(tank.LastFuelHeight);
            decimal currentNormalizedVol = tank.NormalizeVolume(currentVolume, tank.CurrentTemperature, tank.CurrentDensity);
            decimal lastNormalizedVol = tank.NormalizeVolume(lastVolume, tank.LastTemperature, tank.CurrentDensity);

            if (currentNormalizedVol < lastNormalizedVol && lastNormalizedVol - currentNormalizedVol > tank.GetTankAlertThreshold())
                return true;
            return false;
        }

        /// <summary>
        /// Checks if the tanklevel is increasing
        /// </summary>
        /// <param name="tank"></param>
        /// <param name="currentStatus"></param>
        /// <returns></returns>
        private bool IsIncreasing(VirtualDevices.VirtualTank tank, Common.Enumerators.TankStatusEnum currentStatus)
        {
            //if (tank.CurrentFuelLevel < tank.MinHeight)
            //    return false;

            decimal currentVolume = tank.GetTankVolume(tank.CurrentFuelLevel);
            decimal lastVolume = tank.GetTankVolume(tank.LastFuelHeight);
            decimal currentNormalizedVol = tank.NormalizeVolume(currentVolume, tank.CurrentTemperature, tank.CurrentDensity);
            decimal lastNormalizedVol = tank.NormalizeVolume(lastVolume, tank.LastTemperature, tank.CurrentDensity);

            //if tank is in LiterCheck Mode return true to ensure that the status will be changed to Increasing.
            if (tank.IsLiterCheck && currentStatus == Common.Enumerators.TankStatusEnum.FillingInit)
            {
                return true;
            }

            if (currentNormalizedVol > lastNormalizedVol && currentNormalizedVol - lastNormalizedVol > tank.GetTankAlertThreshold())//TankAlertMargin)
                return true;
            return false;
        }

        private void ResolveAlarm(VirtualDevices.VirtualBaseAlarm alarm)
        {
            this.ResolveAlarm(alarm, false);
        }

        /// <summary>
        /// Resolves the scpecific alarm
        /// </summary>
        /// <param name="alarm"></param>
        private void ResolveAlarm(VirtualDevices.VirtualBaseAlarm alarm, bool suspendLevelCorrection)
        {
            Data.SystemEvent sysEv = this.database.SystemEvents.Where(s => s.EventId == alarm.DatabaseEntityId).FirstOrDefault();
            if (sysEv == null)
                return;
            if (sysEv.ResolvedDate.HasValue)
                return;
            if (!sysEv.AlarmType.HasValue)
                return;
            Common.Enumerators.AlarmEnum alarmType = (Common.Enumerators.AlarmEnum)sysEv.AlarmType.Value;
            switch (alarmType)
            {
                case Common.Enumerators.AlarmEnum.NozzleTotalError:

                    decimal lastTotals = this.database.Nozzles.Where(n => n.NozzleId == sysEv.NozzleId.Value).First().LastValidTotalizer;
                    decimal newTotals = this.database.Nozzles.Where(n => n.NozzleId == sysEv.NozzleId.Value).First().TotalCounter;
                    VirtualNozzle nozzle = this.Dispensers.SelectMany(d=>d.Nozzles).Where(n=>n.NozzleId == sysEv.NozzleId.Value).FirstOrDefault();
                    if (lastTotals == newTotals)
                    {
                        this.database.RemoveCounterAlarm(sysEv);
                        ////alarms.Remove(alarm);
                        nozzle.LastVolumeCounter = lastTotals;
                        nozzle.TotalVolumeCounter = newTotals;

                    }
                    else
                    {
                        this.database.RemoveCounterAlarm(sysEv);
                        nozzle.LastVolumeCounter = this.database.Nozzles.Where(n => n.NozzleId == sysEv.NozzleId.Value).First().LastValidTotalizer;
                        nozzle.TotalVolumeCounter = this.database.Nozzles.Where(n => n.NozzleId == sysEv.NozzleId.Value).First().TotalCounter;
                    }

                    break;
                case Common.Enumerators.AlarmEnum.FuelDecrease:
                    if(!suspendLevelCorrection)
                        this.database.RemoveLevelAlarm(sysEv);
                    break;
                case Common.Enumerators.AlarmEnum.FuelIncrease:
                    if (!suspendLevelCorrection)
                        this.database.RemoveLevelAlarm(sysEv);
                    break;
            }
            if (alarm.ResolvedTime.Year < 1900)
                alarm.ResolvedTime = DateTime.Now;
            if (alarm.ResolveText == null || alarm.ResolveText == "")
                alarm.ResolveText = "[System Resolved]";
            sysEv.ResolvedDate = alarm.ResolvedTime;//.ResolveDateTime;
            sysEv.ResolveMessage = alarm.ResolveText;

            this.database.SaveChanges();
        }

        /// <summary>
        /// Saves an Alarm into the database 
        /// </summary>
        /// <param name="alarm"></param>
        private void RaiseAlarm(VirtualBaseAlarm alarm)
        {
            Data.SystemEvent existingEvent = new Data.SystemEvent();
            if (alarm.GetType() == typeof(VirtualTankAlarm))
            {
                existingEvent = this.database.SystemEvents.Where(s => !s.ResolvedDate.HasValue && s.TankId == alarm.DeviceId && s.Message == alarm.MessageText).FirstOrDefault();
                if (existingEvent != null)
                    alarm.DeviceDescription = "Δεξαμενή : " + existingEvent.Tank.TankNumber;
            }
            else if (alarm.GetType() == typeof(VirtualNozzleAlarm))
            {
                existingEvent = this.database.SystemEvents.Where(s => !s.ResolvedDate.HasValue && s.NozzleId == alarm.DeviceId && s.Message == alarm.MessageText).FirstOrDefault();
                if (existingEvent != null)
                    alarm.DeviceDescription = "Ακροσωλήνιο : " + existingEvent.Nozzle.Dispenser.OfficialPumpNumber + " " + existingEvent.Nozzle.OfficialNozzleNumber;
            }
            else if (alarm.GetType() == typeof(VirtualDispenserAlarm))
            {
                existingEvent = this.database.SystemEvents.Where(s => !s.ResolvedDate.HasValue && s.DispenserId == alarm.DeviceId && s.Message == alarm.MessageText).FirstOrDefault();
                if (existingEvent != null)
                    alarm.DeviceDescription = "Αντλία : " + existingEvent.Dispenser.OfficialPumpNumber;
            }
            if (existingEvent == null)
            {
                Data.SystemEvent sysEvent = new Data.SystemEvent();
                sysEvent.EventId = Guid.NewGuid();
                database.Add(sysEvent);
                sysEvent.EventDate = DateTime.Now;

                foreach (VirtualAlarmData data in alarm.Data)
                {
                    Data.SystemEventDatum sysData = new Data.SystemEventDatum();
                    sysData.SystemEventDataId = Guid.NewGuid();
                    sysData.SystemEventId = sysEvent.EventId;
                    sysData.PropertyName = data.PropertyName;
                    sysData.Value = data.Value;
                    this.database.Add(sysData);
                }

                if (alarm.GetType() == typeof(VirtualTankAlarm))
                {
                    Data.Tank tank = this.database.Tanks.Where(n => n.TankId == alarm.DeviceId).FirstOrDefault();
                    if (tank == null)
                        return;
                    VirtualTankAlarm tAlarm = alarm as VirtualTankAlarm;
                    sysEvent.EventType = (int)Common.Enumerators.AlertTypeEnum.TankAlert;
                    sysEvent.TankId = tAlarm.DeviceId;
                    sysEvent.AlarmType = (int)tAlarm.AlertType;
                    sysEvent.Message = tAlarm.MessageText == null ? "" : tAlarm.MessageText;

                    alarm.DeviceDescription = "Δεξαμενή : " + tank.TankNumber;
                }
                else if (alarm.GetType() == typeof(VirtualNozzleAlarm))
                {
                    Data.Nozzle nozzle = this.database.Nozzles.Where(n => n.NozzleId == alarm.DeviceId).FirstOrDefault();
                    if (nozzle == null)
                        return;
                    VirtualNozzleAlarm nAlarm = alarm as VirtualNozzleAlarm;
                    sysEvent.EventType = (int)Common.Enumerators.AlertTypeEnum.FuelPointError;
                    sysEvent.NozzleId = nAlarm.DeviceId;
                    sysEvent.AlarmType = (int)nAlarm.AlertType;
                    sysEvent.Message = nAlarm.MessageText == null ? "" : nAlarm.MessageText;

                    alarm.DeviceDescription = "Ακροσωλήνιο : " + nozzle.Dispenser.OfficialPumpNumber + " " + nozzle.OfficialNozzleNumber;

                }
                else if (alarm.GetType() == typeof(VirtualDispenserAlarm))
                {
                    Data.Dispenser dispenser = this.database.Dispensers.Where(n => n.DispenserId == alarm.DeviceId).FirstOrDefault();
                    if (dispenser == null)
                        return;
                    VirtualDispenserAlarm nAlarm = alarm as VirtualDispenserAlarm;
                    sysEvent.EventType = (int)Common.Enumerators.AlertTypeEnum.CommunicationLossFuelPoint;
                    sysEvent.DispenserId = nAlarm.DeviceId;
                    sysEvent.AlarmType = (int)nAlarm.AlertType;
                    sysEvent.Message = nAlarm.MessageText == null ? "" : nAlarm.MessageText;

                    alarm.DeviceDescription = "Αντλία : " + dispenser.OfficialPumpNumber;

                }
                alarm.DatabaseEntityId = sysEvent.EventId;

                sysEvent.CRC = this.CalculateCRC32();
                if (database.HasChanges)
                    database.SaveChanges();

            }
            else
            {
                alarm.AlarmTime = existingEvent.EventDate;
                alarm.DatabaseEntityId = existingEvent.EventId;
            }
        }

        /// <summary>
        /// Event fired when an Alert is resolved
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void device_AlertRemoved(object sender, EventArgs e)
        {
            VirtualBaseAlarm alarm = sender as VirtualBaseAlarm;
            if(alarm == null)
                return;
            this.ResolveAlarm(alarm);
            if (this.AlarmResolved != null)
                this.AlarmResolved(alarm, new EventArgs());
        }

        /// <summary>
        /// Event fired after an Alert is added in the Alert list of a device
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void device_AlertAdded(object sender, EventArgs e)
        {
            VirtualBaseAlarm alarm = sender as VirtualBaseAlarm;
            if(alarm == null)
                return;
            this.RaiseAlarm(alarm);
            if (this.AlarmAdded != null)
                this.AlarmAdded(sender, new EventArgs());
        }
    }
}

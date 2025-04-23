using ASFuelControl.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.Data.Implementation
{
    public class AlertChecker
    {
        public event EventHandler AlarmAdded;
        public event EventHandler AlarmResolved;
        public event EventHandler AlarmUpdated;

        private static AlertChecker instance;
        private Data.DatabaseModel database = new Data.DatabaseModel(ConnectionString);
        private Dictionary<Guid, string> toResolve = new Dictionary<Guid, string>();
        private bool firstRun = true;
        private int index;
        public static string ConnectionString { set; get; }

        public bool HasNozzleAlerts { set; get; }

        public Data.DatabaseModel Database
        {
            set { this.database = value; }
            get { return this.database; }
        }

        public static AlertChecker Instance
        {
            get 
            {
                if (instance == null)
                    instance = new AlertChecker();
                return instance;
            }
        }

        public void CheckForAlerts()
        {
            //this.database.Refresh(Telerik.OpenAccess.RefreshMode.OverwriteChangesFromStore, this.database.SystemEvents.Where(s => !s.ResolvedDate.HasValue));
            index++;
            if (index > 20)
                index = 0;
            lock (this.toResolve)
            {
                try
                {
                    foreach (Guid id in this.toResolve.Keys)
                    {
                        SystemEvent se = this.database.SystemEvents.Where(s => s.EventId == id).FirstOrDefault();
                        if (se == null)
                            continue;
                        se.ResolveMessage = toResolve[id];
                        se.ResolvedDate = DateTime.Now;
                        if (se.AlarmType == (int)Common.Enumerators.AlarmEnum.NozzleTotalError)
                        {

                            Nozzle nz = this.database.Nozzles.Where(n => n.NozzleId == se.NozzleId).FirstOrDefault();
                            this.database.Refresh(Telerik.OpenAccess.RefreshMode.OverwriteChangesFromStore, nz);
                            nz = this.database.Nozzles.Where(n => n.NozzleId == se.NozzleId).FirstOrDefault();
                            if (nz != null)
                            {
                                nz.ReferenceTotalizerStartDateTime = DateTime.Now;
                                nz.ReferenceTotalizer = nz.TotalCounter;
                            }
                        }
                        else if (se.AlarmType == (int)Common.Enumerators.AlarmEnum.LiterCheckNotReturned)
                        {
                            //Tank tank = this.database.Tanks.Where(t => t.TankId == se.TankId).FirstOrDefault();
                            //if (tank != null)
                            //    tank.IsLiterCheck = false;
                        }
                        else if (se.AlarmType == (int)Common.Enumerators.AlarmEnum.TitrimetryDataChange)
                        {
                            Data.Tank tank = this.database.Tanks.Where(t => t.TankId == se.TankId).FirstOrDefault();
                            if (tank != null)
                                tank.TitrimetryCRC = tank.CalculateTitrimetryCRC();
                        }
                        else if (se.AlarmType == (int)Common.Enumerators.AlarmEnum.FuelIncrease || se.AlarmType == (int)Common.Enumerators.AlarmEnum.FuelDecrease)
                        {
                            Data.Tank tank = this.database.Tanks.Where(t => t.TankId == se.TankId).FirstOrDefault();
                            if (tank != null)
                            {
                                tank.StateIdleDateTime = DateTime.Now.AddSeconds(10);
                                tank.ReferenceLevel = tank.FuelLevel;
                                //tank.ReferenceLevel = tank.FuelLevel;
                            }
                        }
                        if (this.AlarmResolved != null)
                            this.AlarmResolved(se, new EventArgs());
                    }
                    toResolve.Clear();
                }
                catch(Exception ex)
                {
                    Logging.Logger.Instance.LogToFile("CheckForAlerts :: Clear Previous Alerts", ex);
                }
            }
            if (firstRun)
            {
                try
                {
                    var qe = this.database.SystemEvents.Where(s => !s.ResolvedDate.HasValue && s.AlarmType.HasValue).ToList();
                    foreach (SystemEvent se in qe)
                    {

                        if (this.AlarmAdded != null)
                            this.AlarmAdded(se, new EventArgs());
                    }
                    firstRun = false;
                }
                catch(Exception ex)
                {
                    Logging.Logger.Instance.LogToFile("CheckForAlerts :: firstRun", ex);
                }
            }
            foreach (Data.Tank tank in this.database.Tanks)
            {
                try
                {
                    CheckTankAlerts(tank);
                }
                catch (Exception ex)
                {
                    Logging.Logger.Instance.LogToFile("CheckTankAlert", ex);
                }
            }
            foreach (Dispenser dispenser in this.database.Dispensers)
            {
                try
                {
                    this.CheckDispenserAlerts(dispenser);
                }
                catch (Exception ex)
                {
                    Logging.Logger.Instance.LogToFile("CheckDispenserAlerts", ex);
                }
                foreach (Nozzle nz in dispenser.Nozzles)
                {
                    try
                    {
                        this.CheckNozzleAlerts(nz);
                    }
                    catch (Exception ex)
                    {
                        Logging.Logger.Instance.LogToFile("CheckNozzleAlerts", ex);
                    }
                }
            }
            this.database.SaveChanges();
        }

        public void CheckTankAlerts(Data.Tank tank)
        {
            DatabaseModel db = DatabaseModel.GetContext(tank) as DatabaseModel;
            if (db == null)
                return;
            
            List<SystemEvent> existingAlerts = db.SystemEvents.Where(s => s.TankId.HasValue && s.TankId.Value == tank.TankId && 
                                                    !s.ResolvedDate.HasValue).ToList();

            if (tank.Removed)
            {
                foreach (SystemEvent se in existingAlerts)
                {
                    AlertSystemResolved(se);
                }
                return;
            }
            foreach (SystemEvent se in existingAlerts)
            {
                if (se.AlarmType.HasValue && se.AlarmType.Value == (int)Common.Enumerators.AlarmEnum.CommunicationLossTank &&
                    tank.PhysicalState != (int)Common.Enumerators.TankStatusEnum.Offline)
                    AlertSystemResolved(se);
                if (se.AlarmType.HasValue && se.AlarmType.Value == (int)Common.Enumerators.AlarmEnum.FuelTooHigh && 
                    tank.FuelLevel <= tank.MaxFuelHeight)
                    AlertSystemResolved(se);
                if (se.AlarmType.HasValue && se.AlarmType.Value == (int)Common.Enumerators.AlarmEnum.FuelTooLow &&
                    tank.FuelLevel >= tank.MinFuelHeight)
                    AlertSystemResolved(se);
                if (se.AlarmType.HasValue && se.AlarmType.Value == (int)Common.Enumerators.AlarmEnum.WaterTooHigh &&
                    tank.WaterLevel <= tank.MaxWaterHeight)
                    AlertSystemResolved(se);
                if (se.AlarmType.HasValue && se.AlarmType.Value == (int)Common.Enumerators.AlarmEnum.FuelDecrease &&
                    !this.IsLevelDecreasing(tank))
                    AlertSystemResolved(se);
                if (se.AlarmType.HasValue && se.AlarmType.Value == (int)Common.Enumerators.AlarmEnum.FuelIncrease &&
                    !this.IsLevelIncreasing(tank))
                    AlertSystemResolved(se);
            }
            this.database.SaveChanges();
            if (tank.PhysicalState == (int)Common.Enumerators.TankStatusEnum.Offline)
            {
                this.CreateUpdateTankAlert(db, tank.TankId, Common.Enumerators.AlarmEnum.CommunicationLossTank);
            }
            else if (tank.AlertEnabled)
            {
                //Check if increasing or decreasing

                if (tank.FuelLevel > tank.MaxFuelHeight)
                {
                    this.CreateUpdateTankAlert(db, tank.TankId, Common.Enumerators.AlarmEnum.FuelTooHigh);
                }
                if (tank.WaterLevel > tank.MaxWaterHeight)
                {
                    this.CreateUpdateTankAlert(db, tank.TankId, Common.Enumerators.AlarmEnum.WaterTooHigh);
                }
                if (tank.FuelLevel < tank.MinFuelHeight)
                {
                    this.CreateUpdateTankAlert(db, tank.TankId, Common.Enumerators.AlarmEnum.FuelTooLow);
                }
                if (this.IsLevelDecreasing(tank))
                {
                    this.CreateUpdateTankAlert(db, tank.TankId, Common.Enumerators.AlarmEnum.FuelDecrease);
                }
                if (this.IsLevelIncreasing(tank))
                {
                    this.CreateUpdateTankAlert(db, tank.TankId, Common.Enumerators.AlarmEnum.FuelIncrease);
                }
            }
            else if (tank.PhysicalState == (int)Common.Enumerators.TankStatusEnum.Filling && tank.IsLiterCheck)
            {
                if(DateTime.Now.Subtract(tank.LiterCheckStarted).TotalMinutes > tank.LiterCheckAlarmInterval)
                    this.CreateUpdateTankAlert(db, tank.TankId, Common.Enumerators.AlarmEnum.LiterCheckNotReturned);
            }
            if (index == 0)
            {
                if (tank.TitrimetryCRC == 0)
                    tank.TitrimetryCRC = tank.CalculateTitrimetryCRC();
                if (tank.TitrimetryCRC != tank.CalculateTitrimetryCRC())
                {
                    this.CreateUpdateTankAlert(db, tank.TankId, Common.Enumerators.AlarmEnum.TitrimetryDataChange);
                }
                else
                {
                    var q1 = existingAlerts.Where(se => se.TankId == tank.TankId && se.AlarmType == (int)Common.Enumerators.AlarmEnum.TitrimetryDataChange);
                    foreach (Data.SystemEvent sse in q1)
                    {
                        ResolveAlert(sse.EventId, "[Date Resolved]");
                    }
                }
            }
        }

        public void CheckDispenserAlerts(Dispenser dispenser)
        {
            DatabaseModel db = DatabaseModel.GetContext(dispenser) as DatabaseModel;
            if (db == null)
                return;
            List<SystemEvent> existingAlerts = db.SystemEvents.Where
                (s => 
                    s.DispenserId.HasValue && 
                    s.DispenserId.Value == dispenser.DispenserId && 
                    !s.ResolvedDate.HasValue
                ).ToList();

            if (!dispenser.IsValid)
            {
                foreach (SystemEvent se in existingAlerts)
                {
                    se.ResolveMessage = "[System Resolved]";
                    se.ResolvedDate = DateTime.Now;
                    if (this.AlarmResolved != null)
                        this.AlarmResolved(se, new EventArgs());
                }
                return;
            }
            foreach (SystemEvent se in existingAlerts)
            {
                if (dispenser.PhysicalState != (int)Common.Enumerators.FuelPointStatusEnum.Offline)
                {
                    se.ResolveMessage = "[System Resolved]";
                    se.ResolvedDate = DateTime.Now;
                    if (this.AlarmResolved != null)
                        this.AlarmResolved(se, new EventArgs());
                }
            }
            if (dispenser.PhysicalState == (int)Common.Enumerators.FuelPointStatusEnum.Offline)
            {
                this.CreateUpdateDispenserIdAlert(db, dispenser.DispenserId, Common.Enumerators.AlarmEnum.FuelPumpOffline);
            }
        }

        public void CheckNozzleAlerts(Nozzle nozzle)
        {
            DatabaseModel db = DatabaseModel.GetContext(nozzle) as DatabaseModel;
            DatabaseModel db1 = new DatabaseModel(db.Connection.ConnectionString);// = DatabaseModel.GetContext(nozzle) as DatabaseModel;
            if (db == null)
                return;
            nozzle = db1.Nozzles.Where(n => n.NozzleId == nozzle.NozzleId).FirstOrDefault();
            if (nozzle == null || !nozzle.Dispenser.IsValid)
                return;
            if (nozzle.NozzleState == (int)Common.Enumerators.NozzleStateEnum.Normal)
            {
                decimal total = nozzle.ReferenceTotalizer;
                if (total == 0)
                {
                    nozzle.ReferenceTotalizer = nozzle.TotalCounter;
                    nozzle.ReferenceTotalizerStartDateTime = DateTime.Now;
                }
                else
                {
                    DateTime refDate = nozzle.ReferenceTotalizerStartDateTime;
                    if (refDate == DateTime.MinValue)
                        refDate = new DateTime(2000, 1, 1);
                    var q1 = db1.SalesTransactions.Where(s => s.NozzleId == nozzle.NozzleId && s.TransactionTimeStamp >= refDate);
                    q1 = q1.Where(s => !(s.TotalizerEnd == s.TotalizerStart && s.TemperatureStart == 0));
                    q1 = q1.Where(s => s.Volume >= 0);
                    var q = q1.ToList();
                    if (q.Count > 0)
                    {
                        decimal sum = q.Sum(s => s.Volume);
                        total = nozzle.ReferenceTotalizer + sum * (decimal)Math.Pow(10, (double)nozzle.Dispenser.TotalVolumeDecimalPlaces);
                    }
                    decimal diffAllowed = 1 * (decimal)Math.Pow(10, (double)nozzle.Dispenser.TotalVolumeDecimalPlaces);
                    if (Math.Abs(nozzle.TotalCounter - total) > 0 && Math.Abs(nozzle.TotalCounter - total) <= diffAllowed)
                    {
                        Console.WriteLine(string.Format("Total Difference : {0:N4}", Math.Abs(nozzle.TotalCounter - total)));
                    }
                    if (Math.Abs(nozzle.TotalCounter - total) > diffAllowed)
                    {
                        //nozzle.AlertValidIndex++;
                        //if (nozzle.AlertValidIndex < 3)
                        //    return;
                        nozzle.AlertValidIndex = 0;
                        decimal diff = nozzle.TotalCounter - nozzle.LastValidTotalizer;
                        Console.WriteLine(string.Format("Totals  : {0:N3}, Diff : ", nozzle.TotalCounter, diff));
                        this.CreateUpdateNozzleIdAlert(db, nozzle.NozzleId, Common.Enumerators.AlarmEnum.NozzleTotalError, nozzle.TotalCounter - total, diff);
                        this.HasNozzleAlerts = true;
                    }
                    else
                        nozzle.AlertValidIndex = 0;

                }
            }
            db1.Dispose();
        }

        public void CreateBalanceAlert(string fuelType, decimal diff, string message, Common.Enumerators.AlarmEnum alertType, DateTime dt, decimal totalSales)
        {
            SystemEvent se = new SystemEvent();
            se.EventId = Guid.NewGuid();
            this.database.Add(se);
            se.AddData("Balance", diff.ToString("N2"));
            se.AddData("FuelType", fuelType);
            se.AddData("Balance Date", dt.ToString("dd/MM/yyyy HH:mm"));
            se.EventDate = DateTime.Now;
            se.AlarmType = (int)alertType;
            se.Message = string.Format(se.GetMessage(alertType), diff, fuelType, totalSales);
            
            if (this.AlarmAdded != null && totalSales >= 1000)
                this.AlarmAdded(se, new EventArgs());
        }

        public void CreateUpdateAlert(DatabaseModel db, Guid deviceId, Common.Enumerators.AlarmEnum alertType)
        {
            
        }

        public void CreateUpdateTankAlert(DatabaseModel db, Guid deviceId, Common.Enumerators.AlarmEnum alertType)
        {
            SystemEvent se = db.SystemEvents.Where(s => s.AlarmType == (int)alertType && s.TankId == deviceId && 
                !s.ResolvedDate.HasValue).FirstOrDefault();
            Data.Tank tank = db.Tanks.Where(t => t.TankId == deviceId).FirstOrDefault();
            if (tank == null)
                return;
            if (se != null)
            {
                se.UpdateData("CurrentFuelLevel", tank.FuelLevel.ToString("N2"));
                se.UpdateData("CurrentTemperature", tank.Temperatire.ToString("N1"));
                se.UpdateData("CurrentVolume", tank.GetTankVolume(tank.FuelLevel).ToString("N2"));
                if (this.AlarmUpdated != null)
                    this.AlarmUpdated(se, new EventArgs());
            }
            else
            {
                se = new SystemEvent();
                se.EventId = Guid.NewGuid();
                db.Add(se);
                se.AddData("CurrentFuelLevel", tank.FuelLevel.ToString("N2"));
                se.AddData("LastFuelLevel", tank.ReferenceLevel.ToString("N2"));
                se.AddData("CurrentTemperature", tank.Temperatire.ToString("N1"));
                se.AddData("LastTemperature", tank.Temperatire.ToString("N1"));
                se.AddData("CurrentVolume", tank.GetTankVolume(tank.FuelLevel).ToString("N2"));
                se.AddData("LastVolume", tank.GetTankVolume(tank.ReferenceLevel).ToString("N2"));
                se.TankId = tank.TankId;
                se.EventDate = DateTime.Now;
                se.AlarmType = (int)alertType;
                se.Message = string.Format(se.GetMessage(alertType), tank.TankSerialNumber);
                if (this.AlarmAdded != null)
                    this.AlarmAdded(se, new EventArgs());
            }
            
        }

        public void CreateUpdateDispenserIdAlert(DatabaseModel db, Guid deviceId, Common.Enumerators.AlarmEnum alertType)
        {
            SystemEvent se = db.SystemEvents.Where(s => s.AlarmType == (int)alertType && s.DispenserId == deviceId &&
                !s.ResolvedDate.HasValue).FirstOrDefault();
            Dispenser dispenser = db.Dispensers.Where(t => t.DispenserId == deviceId).FirstOrDefault();
            if (dispenser == null)
                return;
            if (se == null)
            {
                se = new SystemEvent();
                se.EventId = Guid.NewGuid();
                db.Add(se);
                se.DispenserId = dispenser.DispenserId;
                se.EventDate = DateTime.Now;
                se.AlarmType = (int)alertType;
                se.Message = string.Format(se.GetMessage(alertType), dispenser.DeviceSerialNumber);
                if (this.AlarmAdded != null)
                    this.AlarmAdded(se, new EventArgs());
            }
        }

        public void CreateUpdateNozzleIdAlert(DatabaseModel db, Guid deviceId, Common.Enumerators.AlarmEnum alertType, decimal diff, decimal saleDiff)
        {
            SystemEvent se = db.SystemEvents.Where(s => s.AlarmType == (int)alertType && s.NozzleId == deviceId &&
                !s.ResolvedDate.HasValue).FirstOrDefault();
            Nozzle nozzle = db.Nozzles.Where(t => t.NozzleId == deviceId).FirstOrDefault();
            if (nozzle == null)
                return;
            decimal totalVolumeDiff = se == null ? 0 : decimal.Parse(se.GetData("VolumeDifference", (0).ToString("N2")));
            
            if (se != null)
            {
                decimal lastUpdate = decimal.Parse(se.GetData("TotalVolumeCounter", (0).ToString("N2")));
                if (lastUpdate == nozzle.TotalCounter)
                    return;
                totalVolumeDiff = totalVolumeDiff + (saleDiff / (decimal)Math.Pow(10, (double)nozzle.Dispenser.TotalVolumeDecimalPlaces));
                decimal totalDiff = totalVolumeDiff * (decimal)Math.Pow(10, (double)nozzle.Dispenser.TotalVolumeDecimalPlaces);
                se.UpdateData("TotalVolumeCounter", nozzle.TotalCounter.ToString("N2"));
                se.UpdateOrAddData("TotalDifference", totalDiff.ToString("N2"));
                se.UpdateOrAddData("VolumeDifference", totalVolumeDiff.ToString("N2"));
                if (this.AlarmUpdated != null)
                    this.AlarmUpdated(se, new EventArgs());
            }
            else
            {
                se = new SystemEvent();
                se.EventId = Guid.NewGuid();
                db.Add(se);
                se.AddData("TotalVolumeCounter", nozzle.TotalCounter.ToString("N2"));
                se.AddData("LastVolumeCounter", nozzle.ReferenceTotalizer.ToString("N2"));
                se.AddData("TotalDifference", saleDiff.ToString("N2"));
                se.AddData("VolumeDifference", (saleDiff / (decimal)Math.Pow(10, (double)nozzle.Dispenser.TotalVolumeDecimalPlaces)).ToString("N2"));
                se.NozzleId = nozzle.NozzleId;
                se.EventDate = DateTime.Now;
                se.AlarmType = (int)alertType;
                se.Message = string.Format(se.GetMessage(alertType), nozzle.SerialNumber, nozzle.Dispenser.DeviceSerialNumber);
                if (this.AlarmAdded != null)
                    this.AlarmAdded(se, new EventArgs());
            }
        }

        public void ResolveAlert(Guid evenId, string message)
        {
            lock (this.toResolve)
            {
                if(!this.toResolve.ContainsKey(evenId))
                    this.toResolve.Add(evenId, message);
            }
        }

        private bool IsLevelIncreasing(Data.Tank tank)
        {
            if (DateTime.Now < tank.StateIdleDateTime)
                return false;

            decimal currentVolume = tank.GetTankVolume(tank.FuelLevel);
            decimal lastVolume = tank.GetTankVolume(tank.ReferenceLevel);//tank.GetTankVolume(tdb.GetLastValidLevel());// tank.GetTankVolume(tank.LastFuelHeight);
            decimal currentNormalizedVol = tank.GetTankVolumeNormalized(tank.FuelLevel);
            decimal lastNormalizedVol = tank.GetTankVolumeNormalized(tank.ReferenceLevel);

            if (currentNormalizedVol > lastNormalizedVol && currentNormalizedVol - lastNormalizedVol > tank.GetTankAlertThreshold())
                return true;
            return false;
        }

        private bool IsLevelDecreasing(Data.Tank tank)
        {
            if (DateTime.Now < tank.StateIdleDateTime)
                return false;

            decimal currentVolume = tank.GetTankVolume(tank.FuelLevel);
            decimal lastVolume = tank.GetTankVolume(tank.ReferenceLevel);//tank.GetTankVolume(tdb.GetLastValidLevel());// tank.GetTankVolume(tank.LastFuelHeight);
            decimal currentNormalizedVol = tank.GetTankVolumeNormalized(tank.FuelLevel);
            decimal lastNormalizedVol = tank.GetTankVolumeNormalized(tank.ReferenceLevel);
            
            if (currentNormalizedVol < lastNormalizedVol && lastNormalizedVol - currentNormalizedVol > tank.GetTankAlertThreshold())
                return true;
            return false;
        }

        private void AlertSystemResolved(SystemEvent se)
        {
            se.ResolveMessage = "[System Resolved]";
            se.ResolvedDate = DateTime.Now;
            if (this.AlarmResolved != null)
                this.AlarmResolved(se, new EventArgs());
        }
    }
}

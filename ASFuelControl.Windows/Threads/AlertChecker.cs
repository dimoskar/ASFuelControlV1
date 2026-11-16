using ASFuelControl.Data;
using ASFuelControl.Data.Implementation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.Windows.Threads
{
    public class AlertChecker : Data.Implementation.IAlertChecker
    {
        private static readonly ISet<OutboundWorkType>
            SuppressedAlertEvaluationWorkTypes =
                new HashSet<OutboundWorkType>
                {
                    OutboundWorkType.AlertEvaluation
                };
        private static readonly TimeSpan NozzleMismatchConfirmationPeriod =
            TimeSpan.FromSeconds(20);

        public event EventHandler AlarmAdded;
        public event EventHandler AlarmResolved;
        public event EventHandler AlarmUpdated;

        private static AlertChecker instance = null;

        public static AlertChecker Instance
        {
            get
            {
                if (instance == null)
                    instance = new AlertChecker();
                return instance;
            }
        }

        private bool firstRun = true;
        private int index = 0;
        private readonly object nozzleMismatchSync = new object();
        private readonly Dictionary<Guid, NozzleMismatchCandidate>
            nozzleMismatchCandidates =
                new Dictionary<Guid, NozzleMismatchCandidate>();

        public bool HasNozzleAlerts { set; get; }

        public void ResolveAlert(Guid alertId, string message)
        {
            using (Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                SystemEvent se = database.SystemEvents.Where(s => s.EventId == alertId).FirstOrDefault();
                if (se == null)
                {
                    database.Dispose();
                    return;
                }
                se.ResolveMessage = message + " (ResolveAlert)";
                se.ResolvedDate = DateTime.Now;
                if (se.AlarmType == (int)Common.Enumerators.AlarmEnum.NozzleTotalError)
                {

                    Nozzle nz = database.Nozzles.Where(n => n.NozzleId == se.NozzleId).FirstOrDefault();
                    database.Refresh(Telerik.OpenAccess.RefreshMode.OverwriteChangesFromStore, nz);
                    nz = database.Nozzles.Where(n => n.NozzleId == se.NozzleId).FirstOrDefault();
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
                    Data.Tank tank = database.Tanks.Where(t => t.TankId == se.TankId).FirstOrDefault();
                    if (tank != null)
                        tank.TitrimetryCRC = tank.CalculateTitrimetryCRC();
                }
                else if (se.AlarmType == (int)Common.Enumerators.AlarmEnum.FuelIncrease || se.AlarmType == (int)Common.Enumerators.AlarmEnum.FuelDecrease)
                {
                    Data.Tank tank = database.Tanks.Where(t => t.TankId == se.TankId).FirstOrDefault();
                    if (tank != null)
                    {
                        tank.StateIdleDateTime = DateTime.Now.AddSeconds(10);
                        tank.ReferenceLevel = tank.FuelLevel;
                        //tank.ReferenceLevel = tank.FuelLevel;
                    }
                }
                database.SaveChanges();
                if (this.AlarmResolved != null)
                    this.AlarmResolved(se, new EventArgs());
            }
        }

        public void CheckForAlerts()
        {
            index++;
            if (index > 20)
                index = 0;

            using (Data.DatabaseModel database = new Data.DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                database.SuppressedOutboundWorkTypes =
                    SuppressedAlertEvaluationWorkTypes;
                List<SystemEvent> existingAlerts = database.SystemEvents
                    .Where(s => !s.ResolvedDate.HasValue)
                    .ToList();
                List<SystemEvent> existingAlarmAlerts = existingAlerts
                    .Where(s => s.AlarmType.HasValue)
                    .ToList();
                this.RaiseExistingAlerts(existingAlarmAlerts);

                List<Data.Tank> tanks = database.Tanks.ToList();
                foreach (Data.Tank tank in tanks)
                {
                    try
                    {
                        this.CheckTankAlerts(tank, existingAlerts);
                    }
                    catch (Exception ex)
                    {
                        Logging.Logger.Instance.LogToFile("CheckTankAlert", ex);
                    }
                }
                List<Dispenser> dispensers = database.Dispensers.ToList();
                foreach (Dispenser dispenser in dispensers)
                {
                    try
                    {
                        this.CheckDispenserAlerts(dispenser, existingAlerts);
                    }
                    catch (Exception ex)
                    {
                        Logging.Logger.Instance.LogToFile(
                            "CheckDispenserAlerts",
                            ex);
                    }
                }
                List<Nozzle> nozzles = database.Nozzles.ToList();
                foreach (Nozzle nozzle in nozzles)
                {
                    try
                    {
                        this.CheckNozzleAlerts(database, nozzle);
                    }
                    catch (Exception ex)
                    {
                        Logging.Logger.Instance.LogToFile(
                            "CheckNozzleAlerts",
                            ex);
                    }
                }

                if (database.HasChanges)
                {
                    database.SaveChanges();
                }
            }
        }

        private void CheckTankAlerts(
            Data.Tank tank,
            IList<SystemEvent> allExistingAlerts)
        {
            DatabaseModel db = DatabaseModel.GetContext(tank) as DatabaseModel;
            if (db == null)
            {
                return;
            }

            List<SystemEvent> existingAlerts = allExistingAlerts
                .Where(s =>
                    s.TankId.HasValue &&
                    s.TankId.Value == tank.TankId)
                .ToList();

            if (tank.Removed || (tank.FuelType.ExcludeFromBalance.HasValue && tank.FuelType.ExcludeFromBalance.Value))
            {
                foreach (SystemEvent se in existingAlerts)
                {
                    AlertSystemResolved(se);
                }
                return;
            }
            bool haseIncAlert = false;
            bool haseDecAlert = false;
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
                if (se.AlarmType.HasValue && se.AlarmType.Value == (int)Common.Enumerators.AlarmEnum.FuelDecrease)
                {
                    if (!this.IsLevelDecreasing(tank, true))
                        AlertSystemResolved(se);
                    else
                        haseDecAlert = true;
                }
                if (se.AlarmType.HasValue && se.AlarmType.Value == (int)Common.Enumerators.AlarmEnum.FuelIncrease)
                {
                    if (!this.IsLevelIncreasing(tank, true))
                        AlertSystemResolved(se);
                    else
                        haseIncAlert = true;
                }
            }
            if (tank.PhysicalState == (int)Common.Enumerators.TankStatusEnum.Offline)
            {
                this.CreateUpdateTankAlert(
                    db,
                    tank,
                    existingAlerts,
                    Common.Enumerators.AlarmEnum.CommunicationLossTank,
                    haseIncAlert,
                    haseDecAlert);
            }
            else if (tank.AlertEnabled)
            {
                this.CheckTankAlert(
                    tank,
                    existingAlerts,
                    haseIncAlert,
                    haseDecAlert);
            }
            else if (tank.PhysicalState == (int)Common.Enumerators.TankStatusEnum.Filling && tank.IsLiterCheck)
            {
                if (DateTime.Now.Subtract(tank.LiterCheckStarted).TotalMinutes > tank.LiterCheckAlarmInterval)
                {
                    this.CreateUpdateTankAlert(
                        db,
                        tank,
                        existingAlerts,
                        Common.Enumerators.AlarmEnum.LiterCheckNotReturned,
                        haseIncAlert,
                        haseDecAlert);
                }
            }
            if (index == 0)
            {
                this.CheckTitrimetryCRC(tank, existingAlerts, haseIncAlert, haseDecAlert);
            }
        }

        private void CheckTankAlert(
            Data.Tank tank,
            IList<SystemEvent> existingAlerts,
            bool incAlert,
            bool decAlert)
        {
            DatabaseModel db = DatabaseModel.GetContext(tank) as DatabaseModel;
            if (tank.FuelLevel > tank.MaxFuelHeight)
            {
                this.CreateUpdateTankAlert(
                    db,
                    tank,
                    existingAlerts,
                    Common.Enumerators.AlarmEnum.FuelTooHigh,
                    incAlert,
                    decAlert);
            }
            if (tank.WaterLevel > tank.MaxWaterHeight)
            {
                this.CreateUpdateTankAlert(
                    db,
                    tank,
                    existingAlerts,
                    Common.Enumerators.AlarmEnum.WaterTooHigh,
                    incAlert,
                    decAlert);
            }
            if (tank.FuelLevel < tank.MinFuelHeight)
            {
                this.CreateUpdateTankAlert(
                    db,
                    tank,
                    existingAlerts,
                    Common.Enumerators.AlarmEnum.FuelTooLow,
                    incAlert,
                    decAlert);
            }
            if (this.IsLevelDecreasing(tank, decAlert))
            {
                this.CreateUpdateTankAlert(
                    db,
                    tank,
                    existingAlerts,
                    Common.Enumerators.AlarmEnum.FuelDecrease,
                    incAlert,
                    decAlert);
            }
            if (this.IsLevelIncreasing(tank, incAlert))
            {
                this.CreateUpdateTankAlert(
                    db,
                    tank,
                    existingAlerts,
                    Common.Enumerators.AlarmEnum.FuelIncrease,
                    incAlert,
                    decAlert);
            }
            if (tank.CurrentDensity >= 900 || tank.CurrentDensity <= 500)
            {
                this.CreateUpdateTankAlert(
                    db,
                    tank,
                    existingAlerts,
                    Common.Enumerators.AlarmEnum.TankDensityError,
                    incAlert,
                    decAlert);
            }
        }

        private void CheckTitrimetryCRC(Data.Tank tank, List<SystemEvent> existingAlerts, bool incAlert, bool decAlert)
        {
            if (tank.TitrimetryCRC == 0)
                tank.TitrimetryCRC = tank.CalculateTitrimetryCRC();
            ulong crc = tank.CalculateTitrimetryCRC();
            if (tank.TitrimetryCRC != crc)
            {
                Data.DatabaseModel db = DatabaseModel.GetContext(tank) as Data.DatabaseModel;
                this.CreateUpdateTankAlert(
                    db,
                    tank,
                    existingAlerts,
                    Common.Enumerators.AlarmEnum.TitrimetryDataChange,
                    incAlert,
                    decAlert);
                tank.TitrimetryCRC = crc;
            }
            else
            {
                var q1 = existingAlerts.Where(se => se.TankId == tank.TankId && se.AlarmType == (int)Common.Enumerators.AlarmEnum.TitrimetryDataChange);
                foreach (Data.SystemEvent sse in q1)
                {
                    this.ResolveAlertInCurrentContext(sse, "[Date Resolved]");
                }
            }
        }

        private void ResolveAlertInCurrentContext(
            SystemEvent systemEvent,
            string message)
        {
            systemEvent.ResolveMessage = message + " (ResolveAlert)";
            systemEvent.ResolvedDate = DateTime.Now;
            if (this.AlarmResolved != null)
            {
                EventArgs eventArgs = new EventArgs();
                this.AlarmResolved(systemEvent, eventArgs);
            }
        }

        private void CheckDispenserAlerts(
            Data.Dispenser dispenser,
            IList<SystemEvent> allExistingAlerts)
        {
            DatabaseModel db = DatabaseModel.GetContext(dispenser) as DatabaseModel;
            if (db == null)
            {
                return;
            }
            List<SystemEvent> existingAlerts = allExistingAlerts
                .Where(s =>
                    s.DispenserId.HasValue &&
                    s.DispenserId.Value == dispenser.DispenserId)
                .ToList();

            if (!dispenser.IsValid)
            {
                foreach (SystemEvent se in existingAlerts)
                {
                    se.ResolveMessage = "[System Resolved]  (CheckDispenserAlerts)";
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
                    se.ResolveMessage = "[System Resolved] (CheckDispenserAlerts status)";
                    se.ResolvedDate = DateTime.Now;
                    if (this.AlarmResolved != null)
                        this.AlarmResolved(se, new EventArgs());
                }
            }
            if (dispenser.PhysicalState == (int)Common.Enumerators.FuelPointStatusEnum.Offline)
            {
                this.CreateUpdateDispenserAlert(
                    db,
                    dispenser,
                    existingAlerts,
                    Common.Enumerators.AlarmEnum.FuelPumpOffline);
            }
        }

        public void CheckNozzleAlerts(Guid nozzleid)
        {
            using (DatabaseModel db = new DatabaseModel(Properties.Settings.Default.DBConnection))
            {
                db.SuppressedOutboundWorkTypes =
                    SuppressedAlertEvaluationWorkTypes;
                try
                {
                    Data.Nozzle nozzle = db.Nozzles.Where(n => n.NozzleId == nozzleid).FirstOrDefault();
                    if (nozzle == null)
                    {
                        return;
                    }

                    this.CheckNozzleAlerts(db, nozzle);
                    if (db.HasChanges)
                    {
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    Logging.Logger.Instance.LogToFile("CheckNozzleAlerts", ex);
                }
                finally
                {
                }
            }
        }

        private void CheckNozzleAlerts(DatabaseModel db, Data.Nozzle nozzle)
        {
            Guid nozzleId = nozzle.NozzleId;
            bool shouldDefer =
                NozzleSalePersistenceCoordinator.ShouldDefer(nozzleId);
            if (shouldDefer)
            {
                this.ClearNozzleMismatchCandidate(nozzleId);
                return;
            }

            Data.SalesTransaction latestSale = db.SalesTransactions
                .Where(s => s.NozzleId == nozzleId)
                .OrderByDescending(s => s.TransactionTimeStamp)
                .ThenByDescending(s => s.SalesTransactionId)
                .FirstOrDefault();
            if (latestSale == null)
            {
                this.ClearNozzleMismatchCandidate(nozzleId);
                return;
            }

            string formattedTotalCounter = nozzle.TotalCounter.ToString("N2");
            SystemEvent matchingEvent = db.SystemEvents
                .Where(s =>
                    s.NozzleId == nozzleId &&
                    s.SystemEventData.Where(sd =>
                            sd.PropertyName == "TotalVolumeCounter" &&
                            sd.Value == formattedTotalCounter)
                        .Count() > 0)
                .OrderBy(s => s.EventDate)
                .LastOrDefault();
            if (matchingEvent != null || latestSale.TotalizerEnd == nozzle.TotalCounter)
            {
                this.ClearNozzleMismatchCandidate(nozzleId);
                return;
            }

            decimal decimalPlaces =
                (decimal)Math.Pow(
                    10,
                    (double)nozzle.Dispenser.TotalVolumeDecimalPlaces);
            decimal diffAllowed = decimalPlaces;
            decimal diff = nozzle.TotalCounter - latestSale.TotalizerEnd;
            decimal absoluteDifference = Math.Abs(diff);
            if (absoluteDifference > 0 && absoluteDifference <= diffAllowed)
            {
                string differenceMessage = string.Format(
                    "Total Difference : {0:N4}",
                    absoluteDifference);
                Console.WriteLine(differenceMessage);
                this.ClearNozzleMismatchCandidate(nozzleId);
                return;
            }

            bool mismatchConfirmed = this.IsNozzleMismatchConfirmed(
                nozzleId,
                nozzle.TotalCounter,
                latestSale.TotalizerEnd);
            if (!mismatchConfirmed)
            {
                return;
            }

            decimal verifiedDifference;
            bool mismatchStillPresent = this.IsNozzleMismatchStillPresent(
                nozzleId,
                nozzle.TotalCounter,
                latestSale.TotalizerEnd,
                out verifiedDifference);
            if (!mismatchStillPresent)
            {
                this.ClearNozzleMismatchCandidate(nozzleId);
                return;
            }

            this.ClearNozzleMismatchCandidate(nozzleId);
            this.CreateUpdateNozzleAlert(
                db,
                nozzleId,
                Common.Enumerators.AlarmEnum.NozzleTotalError,
                verifiedDifference);
        }

        private bool IsNozzleMismatchConfirmed(
            Guid nozzleId,
            decimal totalCounter,
            decimal latestSaleTotalizer)
        {
            DateTime utcNow = DateTime.UtcNow;
            lock (this.nozzleMismatchSync)
            {
                NozzleMismatchCandidate candidate;
                bool candidateExists = this.nozzleMismatchCandidates.TryGetValue(
                    nozzleId,
                    out candidate);
                bool valuesMatch = candidateExists &&
                    candidate.TotalCounter == totalCounter &&
                    candidate.LatestSaleTotalizer == latestSaleTotalizer;
                if (!valuesMatch)
                {
                    candidate = new NozzleMismatchCandidate();
                    candidate.TotalCounter = totalCounter;
                    candidate.LatestSaleTotalizer = latestSaleTotalizer;
                    candidate.FirstObservedUtc = utcNow;
                    this.nozzleMismatchCandidates[nozzleId] = candidate;
                    return false;
                }

                TimeSpan observationPeriod =
                    utcNow.Subtract(candidate.FirstObservedUtc);
                return observationPeriod >= NozzleMismatchConfirmationPeriod;
            }
        }

        private bool IsNozzleMismatchStillPresent(
            Guid nozzleId,
            decimal expectedTotalCounter,
            decimal expectedLatestSaleTotalizer,
            out decimal verifiedDifference)
        {
            verifiedDifference = 0;
            bool shouldDefer =
                NozzleSalePersistenceCoordinator.ShouldDefer(nozzleId);
            if (shouldDefer)
            {
                return false;
            }

            string connectionString = Properties.Settings.Default.DBConnection;
            using (DatabaseModel verificationDatabase =
                new DatabaseModel(connectionString))
            {
                Data.Nozzle verifiedNozzle = verificationDatabase.Nozzles
                    .Where(n => n.NozzleId == nozzleId)
                    .FirstOrDefault();
                if (verifiedNozzle == null)
                {
                    return false;
                }

                Data.SalesTransaction verifiedLatestSale =
                    verificationDatabase.SalesTransactions
                        .Where(s => s.NozzleId == nozzleId)
                        .OrderByDescending(s => s.TransactionTimeStamp)
                        .ThenByDescending(s => s.SalesTransactionId)
                        .FirstOrDefault();
                if (verifiedLatestSale == null)
                {
                    return false;
                }

                bool totalCounterUnchanged =
                    verifiedNozzle.TotalCounter == expectedTotalCounter;
                bool latestSaleUnchanged =
                    verifiedLatestSale.TotalizerEnd ==
                        expectedLatestSaleTotalizer;
                if (!totalCounterUnchanged || !latestSaleUnchanged)
                {
                    return false;
                }

                string formattedTotalCounter =
                    verifiedNozzle.TotalCounter.ToString("N2");
                SystemEvent matchingEvent = verificationDatabase.SystemEvents
                    .Where(s =>
                        s.NozzleId == nozzleId &&
                        s.SystemEventData.Where(sd =>
                                sd.PropertyName == "TotalVolumeCounter" &&
                                sd.Value == formattedTotalCounter)
                            .Count() > 0)
                    .OrderByDescending(s => s.EventDate)
                    .FirstOrDefault();
                if (matchingEvent != null)
                {
                    return false;
                }

                double decimalPlaces =
                    (double)verifiedNozzle.Dispenser.TotalVolumeDecimalPlaces;
                double multiplierValue = Math.Pow(10, decimalPlaces);
                decimal differenceAllowed = (decimal)multiplierValue;
                verifiedDifference =
                    verifiedNozzle.TotalCounter -
                    verifiedLatestSale.TotalizerEnd;
                decimal absoluteDifference = Math.Abs(verifiedDifference);
                if (absoluteDifference <= differenceAllowed)
                {
                    return false;
                }
            }

            bool persistenceStartedDuringVerification =
                NozzleSalePersistenceCoordinator.ShouldDefer(nozzleId);
            return !persistenceStartedDuringVerification;
        }

        private void ClearNozzleMismatchCandidate(Guid nozzleId)
        {
            lock (this.nozzleMismatchSync)
            {
                this.nozzleMismatchCandidates.Remove(nozzleId);
            }
        }

        private sealed class NozzleMismatchCandidate
        {
            public decimal TotalCounter { set; get; }

            public decimal LatestSaleTotalizer { set; get; }

            public DateTime FirstObservedUtc { set; get; }
        }

        public void CreateUpdateTankAlert(DatabaseModel db, Guid deviceId, Common.Enumerators.AlarmEnum alertType, bool incAlert, bool decAlert)
        {
            Data.Tank tank = db.Tanks.Where(t => t.TankId == deviceId).FirstOrDefault();
            if (tank == null)
            {
                return;
            }
            List<SystemEvent> existingAlerts = db.SystemEvents
                .Where(s =>
                    s.TankId == deviceId &&
                    !s.ResolvedDate.HasValue)
                .ToList();
            this.CreateUpdateTankAlert(
                db,
                tank,
                existingAlerts,
                alertType,
                incAlert,
                decAlert);
        }

        private void CreateUpdateTankAlert(
            DatabaseModel db,
            Data.Tank tank,
            IList<SystemEvent> existingAlerts,
            Common.Enumerators.AlarmEnum alertType,
            bool incAlert,
            bool decAlert)
        {
            int alarmType = (int)alertType;
            SystemEvent se = existingAlerts.FirstOrDefault(s =>
                s.AlarmType == alarmType &&
                s.TankId == tank.TankId &&
                !s.ResolvedDate.HasValue);
            if (se != null)
            {
                se.UpdateOrAddData("CurrentFuelLevel", tank.FuelLevel.ToString("N2"));
                se.UpdateOrAddData("CurrentTemperature", tank.Temperatire.ToString("N1"));
                se.UpdateOrAddData("CurrentVolume", tank.GetTankVolume(tank.FuelLevel).ToString("N2"));
                se.UpdateOrAddData("CurrentDensity", tank.GetTankVolume(tank.CurrentDensity).ToString("N2"));
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
                se.AddData("CurrentDensity", tank.GetTankVolume(tank.CurrentDensity).ToString("N2"));
                se.TankId = tank.TankId;
                se.EventDate = DateTime.Now;
                se.AlarmType = (int)alertType;
                se.Message = string.Format(se.GetMessage(alertType), tank.TankSerialNumber);
                existingAlerts.Add(se);
                if (this.AlarmAdded != null)
                    this.AlarmAdded(se, new EventArgs());
            }

        }

        public void CreateUpdateDispenserAlert(DatabaseModel db, Guid deviceId, Common.Enumerators.AlarmEnum alertType)
        {
            Dispenser dispenser = db.Dispensers.Where(t => t.DispenserId == deviceId).FirstOrDefault();
            if (dispenser == null)
            {
                return;
            }
            List<SystemEvent> existingAlerts = db.SystemEvents
                .Where(s =>
                    s.DispenserId == deviceId &&
                    !s.ResolvedDate.HasValue)
                .ToList();
            this.CreateUpdateDispenserAlert(
                db,
                dispenser,
                existingAlerts,
                alertType);
        }

        private void CreateUpdateDispenserAlert(
            DatabaseModel db,
            Dispenser dispenser,
            IList<SystemEvent> existingAlerts,
            Common.Enumerators.AlarmEnum alertType)
        {
            int alarmType = (int)alertType;
            SystemEvent se = existingAlerts.FirstOrDefault(s =>
                s.AlarmType == alarmType &&
                s.DispenserId == dispenser.DispenserId &&
                !s.ResolvedDate.HasValue);
            if (se == null)
            {
                se = new SystemEvent();
                se.EventId = Guid.NewGuid();
                db.Add(se);
                se.DispenserId = dispenser.DispenserId;
                se.EventDate = DateTime.Now;
                se.AlarmType = (int)alertType;
                se.Message = string.Format(se.GetMessage(alertType), dispenser.DeviceSerialNumber);
                existingAlerts.Add(se);
                if (this.AlarmAdded != null)
                    this.AlarmAdded(se, new EventArgs());
            }
        }

        public void CreateUpdateNozzleAlert(DatabaseModel db, Guid deviceId, Common.Enumerators.AlarmEnum alertType, decimal diff)
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
            }
            
            se = new SystemEvent();
            se.EventId = Guid.NewGuid();
            db.Add(se);
            se.AddData("TotalVolumeCounter", nozzle.TotalCounter.ToString("N2"));
            se.AddData("LastVolumeCounter", nozzle.ReferenceTotalizer.ToString("N2"));
            se.AddData("TotalDifference", diff.ToString("N2"));
            se.AddData("VolumeDifference", (diff / (decimal)Math.Pow(10, (double)nozzle.Dispenser.TotalVolumeDecimalPlaces)).ToString("N2"));
            se.NozzleId = nozzle.NozzleId;
            se.EventDate = DateTime.Now;
            se.AlarmType = (int)alertType;
            se.Message = string.Format(se.GetMessage(alertType), nozzle.SerialNumber, nozzle.Dispenser.DeviceSerialNumber);
            if (this.AlarmAdded != null)
                this.AlarmAdded(se, new EventArgs());
            
        }

        public void CreateBalanceAlert(DatabaseModel database, string fuelType, decimal diff, string message, Common.Enumerators.AlarmEnum alertType, DateTime dt, decimal totalSales)
        {
            SystemEvent se = new SystemEvent();
            se.EventId = Guid.NewGuid();
            database.Add(se);
            se.AddData("Balance", diff.ToString("N2"));
            se.AddData("FuelType", fuelType);
            se.AddData("Balance Date", dt.ToString("dd/MM/yyyy HH:mm"));
            se.EventDate = DateTime.Now;
            se.AlarmType = (int)alertType;
            se.Message = string.Format(se.GetMessage(alertType), diff, fuelType, totalSales);

            if (this.AlarmAdded != null && totalSales >= 1000)
                this.AlarmAdded(se, new EventArgs());
        }

        private bool IsLevelIncreasing(Data.Tank tank, bool hasAlert)
        {
            if (DateTime.Now < tank.StateIdleDateTime && !hasAlert)
                return false;

            decimal currentVolume = tank.GetTankVolume(tank.FuelLevel);
            decimal lastVolume = tank.GetTankVolume(tank.ReferenceLevel);//tank.GetTankVolume(tdb.GetLastValidLevel());// tank.GetTankVolume(tank.LastFuelHeight);
            decimal currentNormalizedVol = tank.GetTankVolumeNormalized(tank.FuelLevel);
            decimal lastNormalizedVol = tank.GetTankVolumeNormalized(tank.ReferenceLevel);

            if (currentNormalizedVol > lastNormalizedVol && currentNormalizedVol - lastNormalizedVol > tank.GetTankAlertThreshold())
                return true;
            return false;
        }

        private bool IsLevelIncreasing(Data.Tank tank)
        {
            return IsLevelDecreasing(tank, false);
        }

        private bool IsLevelDecreasing(Data.Tank tank, bool hasAlert)
        {
            if (DateTime.Now < tank.StateIdleDateTime && !hasAlert)
            {
                Common.Logger.Instance.Debug(string.Format("ΝΟΤ LevelDecreasing {0:dd/MM/yyyy HH:mm:ss.fff} {1:dd/MM/yyyy HH:mm:ss.fff} {2}", DateTime.Now, tank.StateIdleDateTime, hasAlert));
                return false;
            }
            decimal currentVolume = tank.GetTankVolume(tank.FuelLevel);
            decimal lastVolume = tank.GetTankVolume(tank.ReferenceLevel);//tank.GetTankVolume(tdb.GetLastValidLevel());// tank.GetTankVolume(tank.LastFuelHeight);
            decimal currentNormalizedVol = tank.GetTankVolumeNormalized(tank.FuelLevel);
            decimal lastNormalizedVol = tank.GetTankVolumeNormalized(tank.ReferenceLevel);

            if (currentNormalizedVol < lastNormalizedVol && lastNormalizedVol - currentNormalizedVol > tank.GetTankAlertThreshold())
            {
                Common.Logger.Instance.Debug(string.Format("ΝΟΤ LevelDecreasing {0} {1} {2}", currentNormalizedVol, lastNormalizedVol, tank.GetTankAlertThreshold()));
                return true;
            }
            return false;
        }

        private bool IsLevelDecreasing(Data.Tank tank)
        {
            return IsLevelDecreasing(tank, false);
        }

        private void RaiseExistingAlerts(IList<SystemEvent> existingAlerts)
        {
            if (firstRun)
            {
                try
                {
                    foreach (SystemEvent se in existingAlerts)
                    {

                        if (this.AlarmAdded != null)
                            this.AlarmAdded(se, new EventArgs());
                    }
                    firstRun = false;
                }
                catch (Exception ex)
                {
                    Logging.Logger.Instance.LogToFile("CheckForAlerts :: firstRun", ex);
                }
            }
        }

        private void AlertSystemResolved(SystemEvent se)
        {
            if (se.Tank != null)
            {
                System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
                Common.Logger.Instance.Debug(stackTrace.ToString());
            }
            se.ResolveMessage = "[System Resolved] (AlertSystemResolved)";
            se.ResolvedDate = DateTime.Now;
            if (this.AlarmResolved != null)
                this.AlarmResolved(se, new EventArgs());
        }
    }
}

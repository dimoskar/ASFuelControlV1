using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telerik.OpenAccess;

namespace ASFuelControl.Data.Implementation
{
    public class AlertEvaluator
    {
        public event EventHandler<AlertAddedEventArgs> AlertAdded;
        public event EventHandler<AlertAddedEventArgs> AlertResolved;

        Data.DatabaseModel database;

        public AlertEvaluator(Data.DatabaseModel db)
        {
            this.database = db;
        }

        //public void EvaluateAlert(Data.Tank tank)
        //{
        //    var q = database.AlertDefinitions.Where(a => !a.AlertIsDisabled && !a.IsGeneric);
        //    foreach(Data.AlertDefinition alert in q)
        //    {
        //        if (!alert.IsTankAlert)
        //            continue;
        //        bool alertIsOn = this.EvaluateAlertCondition(alert, tank);
        //        if (alertIsOn)
        //        {
        //            if (this.CheckIfAlertAlreadyExists(alert, tank))
        //            {
        //                continue;
        //            }
        //            SystemEvent alarm = database.CreateEntity<SystemEvent>();
        //            alarm.AlertDefinitionId = alert.AlertDefinitionId;
        //            alarm.AlertDefinition = alert;
        //            alarm.TankId = tank.TankId;
        //            alarm.SentDate = DateTime.Now;
        //            alarm.Tank = tank;
        //            alarm.Message = alert.AlerttMessage;
        //            tank.SystemEvents.Add(alarm);

        //            this.AddSystemEventData(alarm, "CurrentTankLevel", tank.FuelLevel);
        //            this.AddSystemEventData(alarm, "CurrentTankWaterLevel", tank.WaterLevel);
        //            this.AddSystemEventData(alarm, "CurrentTemperature", tank.Temperatire);
        //            this.AddSystemEventData(alarm, "MaxFuelHeight", tank.MaxFuelHeight);
        //            this.AddSystemEventData(alarm, "MinFuelHeight", tank.MinFuelHeight);
        //            this.AddSystemEventData(alarm, "TotalVolume", tank.TotalVolume);
        //            this.AddSystemEventData(alarm, "CurrentVolume", tank.GetTankVolume(tank.FuelLevel));

        //            if (alert.AlertEnumValue.HasValue)
        //                alarm.EventType = alert.AlertEnumValue.Value;
        //            alarm.EventDate = DateTime.Now;
                    
        //            this.database.SaveChanges();
        //            if (this.AlertAdded != null)
        //                this.AlertAdded(this, new AlertAddedEventArgs(alarm));

                    
        //        }
        //        else
        //        {
        //            if (!alert.AutoResolve)
        //                continue;
        //            var q1 = alert.SystemEvents.Where(e => e.TankId == tank.TankId && e.AlertDefinitionId == alert.AlertDefinitionId && !e.ResolvedDate.HasValue).ToList();
        //            foreach (SystemEvent sv in q1)
        //            {
        //                sv.ResolvedDate = DateTime.Now;
                        
        //                this.database.SaveChanges();
        //                if (this.AlertResolved != null)
        //                    this.AlertResolved(this, new AlertAddedEventArgs(sv));
                        
        //            }
        //        }
        //    }
        //}

        public void EvaluateAlert(Data.Dispenser dispenser)
        {
            var q = database.AlertDefinitions.Where(a => !a.IsDispenserAlert);
            foreach(Data.AlertDefinition alert in q)
            {
                if (!alert.IsTankAlert)
                    continue;
                break;
            }

        }

        public void EvaluateAlert(Data.Nozzle nozzle)
        {
            var q = database.AlertDefinitions.Where(a => a.IsNozzleAlert);
            foreach (Data.AlertDefinition alert in q)
            {
                if (!alert.IsNozzleAlert)
                    continue;
                bool alertIsOn = this.EvaluateAlertCondition(alert, nozzle);
                if (alertIsOn)
                {
                    if (this.CheckIfAlertAlreadyExists(alert, nozzle))
                    {
                        continue;
                    }
                    SystemEvent alarm = database.CreateEntity<SystemEvent>();
                    alarm.AlertDefinitionId = alert.AlertDefinitionId;
                    alarm.AlertDefinition = alert;
                    alarm.NozzleId = nozzle.NozzleId;
                    alarm.SentDate = DateTime.Now;
                    alarm.Nozzle = nozzle;
                    alarm.Message = "-";
                    nozzle.SystemEvents.Add(alarm);

                    this.AddSystemEventData(alarm, "TotalCounter", nozzle.TotalCounter);

                    if (alert.AlertEnumValue.HasValue)
                        alarm.EventType = alert.AlertEnumValue.Value;
                    alarm.EventDate = DateTime.Now;
                    alarm.Nozzle.NozzleState = (int)Common.Enumerators.NozzleStateEnum.Locked;
                    this.database.SaveChanges();
                    if (this.AlertAdded != null)
                        this.AlertAdded(this, new AlertAddedEventArgs(alarm));

                    //
                }
                else
                {
                    if (!alert.AutoResolve)
                        continue;
                    var q1 = alert.SystemEvents.Where(e => e.NozzleId == nozzle.NozzleId && e.AlertDefinitionId == alert.AlertDefinitionId && !e.ResolvedDate.HasValue);
                    foreach (SystemEvent sv in q1)
                    {
                        sv.ResolvedDate = DateTime.Now;
                        sv.Nozzle.NozzleState = (int)Common.Enumerators.NozzleStateEnum.Normal;
                        this.database.SaveChanges();
                        if (this.AlertResolved != null)
                            this.AlertResolved(this, new AlertAddedEventArgs(sv));
                        
                    }
                }
            }

        }

        //public void EvaluateAlert(bool hasConnection, bool authorized)
        //{
        //    bool[] values = new bool[] { hasConnection, authorized };
        //    var q = database.AlertDefinitions.Where(a => a.IsStationAlert);
        //    foreach (Data.AlertDefinition alert in q)
        //    {
        //        if (!alert.IsStationAlert)
        //            continue;
        //        bool alertIsOn = this.EvaluateAlertCondition(alert, values);
        //        if (alertIsOn)
        //        {
        //            if (this.CheckIfAlertAlreadyExists(alert, values))
        //            {
        //                continue;
        //            }
        //            SystemEvent alarm = database.CreateEntity<SystemEvent>();
        //            alarm.AlertDefinitionId = alert.AlertDefinitionId;
        //            alarm.AlertDefinition = alert;
        //            alarm.SentDate = DateTime.Now;
        //            alarm.Message = alert.AlerttMessage;

        //            if (alert.AlertEnumValue.HasValue)
        //                alarm.EventType = alert.AlertEnumValue.Value;
        //            alarm.EventDate = DateTime.Now;
        //            this.database.SaveChanges();
        //            if (this.AlertAdded != null)
        //                this.AlertAdded(this, new AlertAddedEventArgs(alarm));


        //        }
        //        else
        //        {
        //            if (!alert.AutoResolve)
        //                continue;
        //            var q1 = alert.SystemEvents.Where(e => e.AlertDefinitionId == alert.AlertDefinitionId && !e.ResolvedDate.HasValue);
        //            foreach (SystemEvent sv in q1)
        //            {
        //                sv.ResolvedDate = DateTime.Now;
        //                this.database.SaveChanges();
        //                if (this.AlertResolved != null)
        //                    this.AlertResolved(this, new AlertAddedEventArgs(sv));

        //            }
        //        }
        //    }

        //}

        public void AddAlert(Data.AlertDefinition alert, Data.Dispenser disp)
        {
            Data.SystemEvent sEvent = this.database.CreateEntity<Data.SystemEvent>();
            sEvent.AlertDefinitionId = alert.AlertDefinitionId;
            sEvent.AlertDefinition = alert;
            sEvent.DispenserId = disp.DispenserId;
            sEvent.Dispenser = disp;
            sEvent.EventDate = DateTime.Now;
            sEvent.EventType = alert.AlertEnumValue.Value;
            if (this.AlertAdded != null)
                this.AlertAdded(this, new AlertAddedEventArgs(sEvent));
        }

        public void AddAlert(Data.AlertDefinition alert, Data.Dispenser disp, string message, int alertType)
        {
            Data.SystemEvent sEvent = this.database.CreateEntity<Data.SystemEvent>();

            sEvent.AlertDefinitionId = alert.AlertDefinitionId;
            sEvent.AlertDefinition = alert;
            sEvent.DispenserId = disp.DispenserId;
            sEvent.Dispenser = disp;
            sEvent.EventDate = DateTime.Now;
            sEvent.Message = message;
            sEvent.EventType = alertType;

            if (this.AlertAdded != null)
                this.AlertAdded(this, new AlertAddedEventArgs(sEvent));
        }

        public void AddAlert(Data.AlertDefinition alert, Data.Tank tank)
        {
            Data.SystemEvent sEvent = this.database.CreateEntity<Data.SystemEvent>();
            
            sEvent.AlertDefinitionId = alert.AlertDefinitionId;
            sEvent.AlertDefinition = alert;
            sEvent.TankId = tank.TankId;
            sEvent.Tank = tank;
            sEvent.EventDate = DateTime.Now;
            sEvent.EventType = alert.AlertEnumValue.Value;
            this.AddSystemEventData(sEvent, "CurrentTankLevel", tank.FuelLevel);
            this.AddSystemEventData(sEvent, "CurrentTankWaterLevel", tank.WaterLevel);
            this.AddSystemEventData(sEvent, "CurrentTemperature", tank.Temperatire);
            this.AddSystemEventData(sEvent, "MaxFuelHeight", tank.MaxFuelHeight);
            this.AddSystemEventData(sEvent, "MinFuelHeight", tank.MinFuelHeight);
            this.AddSystemEventData(sEvent, "TotalVolume", tank.TotalVolume);
            this.AddSystemEventData(sEvent, "CurrentVolume", tank.GetTankVolume(tank.FuelLevel));

            if (this.AlertAdded != null)
                this.AlertAdded(this, new AlertAddedEventArgs(sEvent));
        }

        public void AddAlert(Data.AlertDefinition alert, Data.Tank tank, string message, int alertType)
        {
            if (this.database == null)
                return;
            Data.SystemEvent sEvent = this.database.CreateEntity<Data.SystemEvent>();

            sEvent.AlertDefinitionId = alert.AlertDefinitionId;
            sEvent.AlertDefinition = alert;
            sEvent.TankId = tank.TankId;
            sEvent.Tank = tank;
            sEvent.Message = message;
            sEvent.EventDate = DateTime.Now;
            sEvent.EventType = alertType;
            this.AddSystemEventData(sEvent, "CurrentTankLevel", tank.FuelLevel);
            this.AddSystemEventData(sEvent, "CurrentTankWaterLevel", tank.WaterLevel);
            this.AddSystemEventData(sEvent, "CurrentTemperature", tank.Temperatire);
            this.AddSystemEventData(sEvent, "MaxFuelHeight", tank.MaxFuelHeight);
            this.AddSystemEventData(sEvent, "MinFuelHeight", tank.MinFuelHeight);
            this.AddSystemEventData(sEvent, "TotalVolume", tank.TotalVolume);
            this.AddSystemEventData(sEvent, "CurrentVolume", tank.GetTankVolume(tank.FuelLevel));

            if (this.AlertAdded != null)
                this.AlertAdded(this, new AlertAddedEventArgs(sEvent));
        }

        private bool CheckIfAlertAlreadyExists(AlertDefinition alert, object entity)
        {
            if (entity.GetType() == typeof(Tank))
            {
                Tank tank = entity as Tank;
                SystemEvent ev = alert.SystemEvents.Where(e => e.TankId == tank.TankId && e.AlertDefinitionId == alert.AlertDefinitionId && !e.ResolvedDate.HasValue).LastOrDefault();
                if (ev == null)
                    return false;
                else
                {
                    if (!alert.ResendAlerts)
                        return true;
                    if( (int)DateTime.Now.Subtract(ev.EventDate).TotalMinutes <= alert.ResendAlertsInterval)
                        return true;
                    return false;
                }
            }
            else if(entity.GetType() == typeof(Nozzle))
            {
                Nozzle nozzle = entity as Nozzle;
                SystemEvent ev = alert.SystemEvents.Where(e => e.NozzleId == nozzle.NozzleId && e.AlertDefinitionId == alert.AlertDefinitionId && !e.ResolvedDate.HasValue).LastOrDefault();
                if (ev == null)
                    return false;
                else
                {
                    if (!alert.ResendAlerts)
                        return true;
                    if ((int)DateTime.Now.Subtract(ev.EventDate).TotalMinutes <= alert.ResendAlertsInterval)
                        return true;
                    return false;
                }
            }
            else if (entity.GetType() == typeof(bool))
            {
                Tank tank = entity as Tank;
                SystemEvent ev = alert.SystemEvents.Where(e => e.AlertDefinitionId == alert.AlertDefinitionId && !e.ResolvedDate.HasValue).LastOrDefault();
                if (ev == null)
                    return false;
                else
                {
                    if (!alert.ResendAlerts)
                        return true;
                    if ((int)DateTime.Now.Subtract(ev.EventDate).TotalMinutes <= alert.ResendAlertsInterval)
                        return true;
                    return false;
                }
            }
            return false;
        }

        private void AddSystemEventData(SystemEvent alarm, string propName, object value)
        {
            SystemEventDatum data = database.CreateEntity<SystemEventDatum>();
            data.PropertyName = propName;
            data.SystemEventId = alarm.EventId;
            alarm.SystemEventData.Add(data);
            if (value.GetType() == typeof(decimal))
            {
                decimal val = (decimal)value;
                val = val * 1000;
                data.Value = ((int)val).ToString();
            }
            else
            {
                data.Value = value.ToString();
            }
        }

        private bool EvaluateAlertCondition(Data.AlertDefinition alert, object entity)
        {
            NCalc.Expression expression = new NCalc.Expression(alert.Expression);
            if (entity.GetType() == typeof(Data.Dispenser))
            {
                Dispenser disp = entity as Dispenser;
                //expression.Parameters[parameter.Name] = disp..CurrentValue;
                
            }
            else if (entity.GetType() == typeof(Data.Tank))
            {
                Tank tank = entity as Tank;
                expression.Parameters["CurrentTankLevel"] = tank.FuelLevel;
                expression.Parameters["CurrentTankWaterLevel"] = tank.WaterLevel;
                expression.Parameters["CurrentTemperature"] = tank.Temperatire;
                expression.Parameters["MaxFuelHeight"] = tank.MaxFuelHeight;
                expression.Parameters["MinFuelHeight"] = tank.MinFuelHeight;
                expression.Parameters["TotalVolume"] = tank.TotalVolume;
                expression.Parameters["CurrentVolume"] = tank.GetTankVolume(tank.FuelLevel);
                expression.Parameters["MaxWaterHeight"] = tank.MaxWaterHeight;
                expression.Parameters["LastNettoFuelNormalizedVolume"] = tank.LastNettoFuelNormalizedVolume;
                expression.Parameters["CurrentNettoFuelNomalizedVolume"] = tank.CurrentNettoFuelNomalizedVolume;
                expression.Parameters["CurrentStateName"] = ((Common.Enumerators.TankStatusEnum)tank.PhysicalState).ToString();
                expression.Parameters["StatisticalVolumeError"] = tank.StatisticalVolumeError;
                
            }
            else if (entity.GetType() == typeof(Data.Nozzle))
            {
                Nozzle nozzle = entity as Nozzle;
                expression.Parameters["TotalCounter"] = nozzle.TotalCounter;
                expression.Parameters["LastValidTotalizer"] = nozzle.LastValidTotalizer;
            }
            else if (entity.GetType() == typeof(bool[]))
            {
                bool[] vals = entity as bool[];
                expression.Parameters["Authorized"] = vals[1];
                expression.Parameters["HasConnection"] = vals[0];
            }
            object ret = expression.Evaluate();
            return (bool)ret;
        }
    }

    public class AlertAddedEventArgs : EventArgs
    {
        public Data.SystemEvent Alert
        {
            private set;
            get;
        }

        public AlertAddedEventArgs(Data.SystemEvent sEvent)
        {
            this.Alert = sEvent;
        }
    }
}

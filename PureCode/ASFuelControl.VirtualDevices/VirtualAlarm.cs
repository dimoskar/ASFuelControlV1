using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.VirtualDevices
{
    public class VirtualBaseAlarm
    {
        List<VirtualAlarmData> dataList = new List<VirtualAlarmData>();

        public Guid DeviceId { set; get; }
        public string DeviceDescription { set; get; }
        public string MessageText { set; get; }
        public DateTime AlarmTime { set; get; }
        public Guid DatabaseEntityId { set; get; }
        public Common.Enumerators.AlarmEnum AlertType { set; get; }
        public string ResolveText { set; get; }
        public DateTime ResolvedTime { set; get; }
        public bool ExistingAlarm { set; get; }
        public VirtualAlarmData[] Data
        {
            get { return this.dataList.ToArray(); }
        }
        public int DiscoverIndex { set; get; }

        public void AddData(string propName, string value)
        {
            this.dataList.Add(new VirtualAlarmData() { PropertyName = propName, Value = value });
        }
    }

    public class VirtualAlarmData
    {
        public string PropertyName { set; get; }
        public string Value { set; get; }
        public string Description
        {
            get 
            {
                return Properties.Resources.ResourceManager.GetString("AlertData_" + this.PropertyName);
            }
        }
    }

    public class VirtualTankAlarm : VirtualBaseAlarm
    {
        //TankAlarmEnum alarmType = TankAlarmEnum.MaxHeight;

        //public TankAlarmEnum AlarmType
        //{
        //    set
        //    {
        //        alarmType = value;
        //        switch (value)
        //        {
        //            case TankAlarmEnum.LeavelIncrease:
        //                this.AlertType = Common.Enumerators.AlarmEnum.FuelIncrease;
        //                break;
        //            case TankAlarmEnum.LevelDecrease:
        //                this.AlertType = Common.Enumerators.AlarmEnum.FuelDecrease;
        //                break;
        //            case TankAlarmEnum.MaxHeight:
        //                this.AlertType = Common.Enumerators.AlarmEnum.FuelTooHigh;
        //                break;
        //            case TankAlarmEnum.MinHeight:
        //                this.AlertType = Common.Enumerators.AlarmEnum.FuelTooLow;
        //                break;
        //            case TankAlarmEnum.WaterTooHeight:
        //                this.AlertType = Common.Enumerators.AlarmEnum.WaterTooHigh;
        //                break;
        //        }
        //    }
        //    get
        //    {
        //        return alarmType;
        //    }
        //}

        //public enum TankAlarmEnum
        //{
        //    MaxHeight,
        //    MinHeight,
        //    LeavelIncrease,
        //    LevelDecrease,
        //    WaterTooHeight,
        //    TankOffline,
        //    GaugeStuck
        //}
    }

    public class VirtualDispenserAlarm : VirtualBaseAlarm
    {
    }

    public class VirtualNozzleAlarm : VirtualBaseAlarm
    {
        NozzleAlarmEnum alarmType;
        public NozzleAlarmEnum AlarmType 
        { 
            set
            {
                this.alarmType = value;
                switch(value)
                {
                    case NozzleAlarmEnum.TotalizerMismatch:
                        this.AlertType = Common.Enumerators.AlarmEnum.NozzleTotalError;
                        break;
                }
            } 
            get
            {
                return this.alarmType;
            }
        }

        public enum NozzleAlarmEnum
        {
            TotalizerMismatch
        }

        public decimal TotalCounter { set; get; }
    }



    public class VirtualTankFillingInfo : VirtualBaseAlarm
    {
        public decimal VolumeInvoiced { set; get; }
        public decimal VolumeReal { set; get; }
        public decimal Difference { set; get; }
    }
}

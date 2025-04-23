using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.VirtualDevices
{
    public class VirtualFleetDispenser
    {
        private List<FleetDispenserSchedule> schedules = new List<FleetDispenserSchedule>();

        public FleetDispenserSchedule[] Schedules
        {
            set { this.schedules = new List<FleetDispenserSchedule>(value); }
            get { return this.schedules.ToArray(); }
        }

        public Guid InvoiceTypeId { set; get; }

        public string ComPort { set; get; }

        public string LastData { set; get; }

        public string ActualData { set; get; }

        public bool ShouldSetStateOn
        {
            get
            {
                int minute = (int)DateTime.Now.TimeOfDay.TotalMinutes;
                DayOfWeek day = DateTime.Today.DayOfWeek;
                FleetDispenserSchedule.DayOfWeekExt dayExt = (FleetDispenserSchedule.DayOfWeekExt)((int)day + 1);
                if (this.schedules.Count == 0)
                    return false;

                var q = this.schedules.Where(s => s.HasValue(dayExt)).ToList();
                foreach (FleetDispenserSchedule sch in q)
                {
                    if (sch.TimeFrom == minute)
                        return true;
                }

                return false;
            }
        }

        public bool ShouldSetStateOff
        {
            get
            {
                int minute = (int)DateTime.Now.TimeOfDay.TotalMinutes;
                DayOfWeek day = DateTime.Today.DayOfWeek;
                int dm = (int)Math.Pow(2, (int)day);
                FleetDispenserSchedule.DayOfWeekExt dayExt = (FleetDispenserSchedule.DayOfWeekExt)dm;
                if (this.schedules.Count == 0)
                    return false;

                var q = this.schedules.Where(s => s.HasValue(dayExt)).ToList();
                foreach (FleetDispenserSchedule sch in q)
                {
                    if (sch.TimeTo == minute)
                        return true;
                }

                return false;
            }
        }
    }

    public class FleetDispenserSchedule
    {
        public int TimeFrom { set; get; }
        public int TimeTo { set; get; }
        public int DayMask { set; get; }

        public bool HasValue(DayOfWeekExt day)
        {
            DayOfWeekExt d = (DayOfWeekExt)this.DayMask;
            return d.HasFlag(day);
        }

        [Flags]
        public enum DayOfWeekExt
        {
            Sunday = 1,
            Monday = 2,
            Tuesday = 4,
            Wednesday = 8,
            Thursday = 16,
            Friday = 32,
            Saturday = 64,
        }
    }
}

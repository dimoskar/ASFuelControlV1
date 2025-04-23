using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Common.Enumerators
{
    public enum ScheduleTypeEnum
    {
        WayOfWeek,
        Date,
    }

    public enum ScheduleWeekDayEnum
    {
        Sunday = 0,
        Monday = 1,
        Tuesday,
        Wednesday,
        Thursday,
        Friday,
        Saturday,
        DayOff
    }
}

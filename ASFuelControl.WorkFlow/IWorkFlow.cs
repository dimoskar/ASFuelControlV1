using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.WorkFlow
{
    public interface IWorkFlow
    {
        event EventHandler<AlarmRaisedEventArgs> AlarmRaised;
        event EventHandler<QueryAlarmResolvedArgs> QueryAlarmResolved;

        bool IsInitialized { set; get; }
        ASFuelControl.WorkFlow.WorkFlowProcess Process { set; get; }
        Common.IController Controller { set; get; }
        Guid CurrentShiftId { set; get; }
        void SetValues(object values);
        
    }

    public class AlarmRaisedEventArgs : EventArgs
    {
        public VirtualDevices.VirtualBaseAlarm Alarm { set; get; }
        public AlarmRaisedEventArgs(VirtualDevices.VirtualBaseAlarm alarm)
        {
            this.Alarm = alarm;
        }
    }

    public class QueryAlarmResolvedArgs : EventArgs
    {
        public Guid DeviceId { set; get; }
        public QueryAlarmResolvedArgs(Guid deviceId)
        {
            this.DeviceId = deviceId;
        }
    }
}

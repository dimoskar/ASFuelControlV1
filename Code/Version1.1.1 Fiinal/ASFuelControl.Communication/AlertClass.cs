using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Communication
{
    [Serializable]
    public class AlertClass
    {
        public Enums.AlertIdEnum Alert { set; get; }
        public DateTime AlertTime { set; get; }
        public string DeviceId { set; get; }
        public string Description { set; get; }
    }
}

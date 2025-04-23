using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.WorkFlow
{
    public interface ITankWorkFlow
    {
        VirtualDevices.VirtualTank Tank { set; get; }
        Common.IController Controller { set; get; }

        void ValidateTransitions();
        void SetValues(object _values);
    }
}

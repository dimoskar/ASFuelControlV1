using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASFuelControl.Common;

namespace ASFuelControl.EMR3
{
    public class EmrController : Common.FuelPumpControllerBase
    {
        public EmrController()
        {
            this.Controller = new Protocol();
        }

        public override void Connect()
        {
            this.Controller.Connect();
        }

        public override void DisConnect()
        {
            this.Controller.Disconnect();
        }

        public void StartFuelPoint(int channelId, int addressId)
        {
            FuelPoint fp = this.GetFulePoint(channelId, addressId);
            if (fp == null)
                return;
            fp.QueryStart = true;
        }

        public void StopFuelPoint(int channelId, int addressId)
        {
            this.HaltDispenser(channelId, addressId);
        }
    }
}

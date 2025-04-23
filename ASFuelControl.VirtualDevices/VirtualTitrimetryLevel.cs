using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.VirtualDevices
{
    public class VirtualTitrimetryLevel
    {
        public decimal Height { set; get; }
        public decimal Volume { set; get; }
        public decimal UncertaintyVolume { set; get; }
        public decimal UncertaintyPercent { set; get; }

        public VirtualTitrimetryLevel(decimal vol, decimal h, decimal uv, decimal up)
        {
            this.Volume = vol;
            this.Height = h;
            this.UncertaintyVolume = uv;
            this.UncertaintyPercent = up;
        }
    }
}

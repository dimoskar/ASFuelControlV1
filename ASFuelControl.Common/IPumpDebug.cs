using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Common
{
    public interface IPumpDebug
    {
         DebugValues DebugStatusDialog(FuelPoint fp);
        //void SetConnectorDebugMode();
    }

    public class DebugValues
    {
        private byte[] tx = new byte[] { };
        private byte[] rx = new byte[] { };
        public decimal Totalizer { get; set; }
        public decimal Volume { get; set; }
        public decimal Amount { get; set; }
        public byte[] TX { get { return tx; } set {tx = value;}}
        public byte[] RX { get { return rx; } set { rx = value; } }
        
        public Common.Enumerators.FuelPointStatusEnum Status { get; set; }

    }
}

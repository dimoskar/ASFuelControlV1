using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Common.Sales
{
    public class TankSaleData
    {
        public Guid TankId { set; get; }
        public decimal StartTemperature { set; get; }
        public decimal StartLevel { set; get; }
        public decimal StartWaterLevel { set; get; }
        public decimal EndTemperature { set; get; }
        public decimal EndLevel { set; get; }
        public decimal EndWaterLevel { set; get; }
        public bool ReadyToProcess { set; get; }
    }
}

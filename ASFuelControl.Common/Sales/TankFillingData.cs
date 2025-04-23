using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Common.Sales
{
    public class TankFillingData
    {
        public Guid TankId { set; get; }
        public Guid InvoiceLineId { set; get; }
        public Guid InvoiceTypeId { set; get; }
        public Guid FuelTypeId { set; get; }
        public Guid VehicelId { set; get; }
        public TankValues StartValues { set; get; }
        public TankValues EndValues { set; get; }
        public DataModeEnum Mode { set; get; }
        public DateTime DeliveryStarted { set; get; }

        public enum DataModeEnum
        {
            Filling,
            Extraction
        }
    }

    
}

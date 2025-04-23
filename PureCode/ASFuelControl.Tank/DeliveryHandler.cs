using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Tank
{
    public class DeliveryHandler
    {
        private VirtualDevices.VirtualTank[] tanks;

        public VirtualDevices.VirtualTank[] Tanks
        {
            set { this.tanks = value; }
            get { return this.tanks; }
        }

        public DateTime DeliveryStart { set; get; }
        public TimeSpan UnlockTime { set; get; }
        public TimeSpan RemainingUnlockTime
        {
            get
            {
                TimeSpan t = this.DeliveryStart.Add(this.UnlockTime).Subtract(DateTime.Now);
                if (t.TotalSeconds > 0)
                    return t;
                return new TimeSpan(0, 0, 0);
            }
        }
        public Guid FuelTypeId { set; get; }
        public Guid InvoiceTypeId { set; get; }

        public DeliveryHandler()
        {
        }

        public void StartDelivery(Guid InvoiceTypeId)
        {
            foreach (VirtualDevices.VirtualTank tank in this.tanks)
            {
                
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Common
{
    public class FuelPointValues
    {
        int activeNozzle = -1;
        int lasrNozzle = -1;
        decimal currentVolume = 0;
        public decimal[] TotalPrices { set; get; }
        public decimal[] TotalVolumes { set; get; }
        public int ActiveNozzle
        {
            set 
            {
                if (this.activeNozzle >= 0)
                    this.LastNozzle = this.activeNozzle;
                this.activeNozzle = value; 
            }
            get { return this.activeNozzle; }
        }
        public int LastNozzle
        {
            set { this.lasrNozzle = value; }
            get { return this.lasrNozzle; }
        }
        public decimal CurrentVolume 
        {
            set { this.currentVolume = value; }
            get { return this.currentVolume; }
        }
        public decimal CurrentPriceTotal { set; get; }
        public decimal CurrentSalePrice { set; get; }
        public decimal CurrentTemperatur { set; get; }
        public Common.Enumerators.FuelPointStatusEnum Status { set; get; }
        public DateTime CreationDateTime { private set; get; }
        public DateTime AssignDateTime { set; get; }
        public Guid ID { private set; get; }

        public bool HasTotalMisfunction { set; get; }

        public FuelPointValues()
        {
            this.CreationDateTime = DateTime.Now;
            this.ID = Guid.NewGuid();
        }
       
    }
}

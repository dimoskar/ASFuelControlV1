using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Common
{
    /// <summary>
    /// class used for storing the dispenser values
    /// </summary>
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
        public string EuromatParameters { set; get; }
        public Sales.SaleData Sale { set; get; }

        public bool HasTotalMisfunction { set; get; }

        public FuelPointValues()
        {
            this.CreationDateTime = DateTime.Now;
            this.ID = Guid.NewGuid();
        }
       
        public bool Equals(FuelPointValues vals)
        {
            if (vals == null)
                return false;
            if (vals.ActiveNozzle != this.ActiveNozzle)
                return false;
            if (vals.CurrentPriceTotal != this.CurrentPriceTotal)
                return false;
            if (vals.CurrentSalePrice != this.CurrentSalePrice)
                return false;
            if (vals.CurrentVolume != this.CurrentVolume)
                return false;
            if (vals.EuromatParameters != this.EuromatParameters)
                return false;
            if (vals.Status != this.Status)
                return false;
            
            if (vals.Sale !=null &&  this.Sale == null)
                return false;
            if (vals.Sale == null && this.Sale != null)
                return false;
            if (vals.Sale != null && this.Sale != null)
            {
                if (vals.Sale.DisplayPrice != this.Sale.DisplayPrice)
                    return false;
                if (vals.Sale.DisplayVolume != this.Sale.DisplayVolume)
                    return false;
                if (vals.Sale.EuromatParameters != this.Sale.EuromatParameters)
                    return false;
                if (vals.Sale.EuromatPumpNumber != this.Sale.EuromatPumpNumber)
                    return false;
                if (vals.Sale.FuelTypeDescription != this.Sale.FuelTypeDescription)
                    return false;
                if (vals.Sale.InvalidSale != this.Sale.InvalidSale)
                    return false;
                if (vals.Sale.TotalizerEnd != this.Sale.TotalizerEnd)
                    return false;
                if (vals.Sale.TotalizerStart != this.Sale.TotalizerStart)
                    return false;
                if (vals.Sale.TotalPrice != this.Sale.TotalPrice)
                    return false;
                if (vals.Sale.TotalVolume != this.Sale.TotalVolume)
                    return false;
                if (vals.Sale.UnitPrice != this.Sale.UnitPrice)
                    return false;
            }
            return true;
        }
    }
}

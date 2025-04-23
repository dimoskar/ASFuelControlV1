using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Common.Sales
{
    public class SaleData
    {
        public event EventHandler SaleCompeted;

        decimal totalVolume = 0;
        decimal totalEnd = 0;
        decimal totalStart = 0;
        bool saleCompleted = false;

        public Guid NozzleId { set; get; }
        public int NozzleNumber { set; get; }
        public string FuelTypeDescription { set; get; }
        public DateTime SaleEndTime { set; get; }
        public decimal TotalPrice { set; get; }
        public decimal TotalVolume 
        {
            set { this.totalVolume = value; }
            get { return this.totalVolume; } 
        }
        public decimal UnitPrice { set; get; }
        public decimal TotalizerStart 
        {
            set 
            { 
                this.totalStart = value;
                if(this.totalStart >= 0)
                    this.SaleInitialized = DateTime.Now;
            }
            get { return this.totalStart; }
        }

        public decimal TotalizerEnd
        {
            set { this.totalEnd = value; }
            get { return this.totalEnd; }
        }
        public Guid VehicleId { set; get; }
        public Guid TraderId { set; get; }
        public Guid InvoiceTypeId { set; get; }
        public bool LiterCheck { set; get; }
        public bool ErrorResolving { set; get; }
        public bool SaleCompleted 
        {
            set 
            {
                if (this.saleCompleted == value)
                    return;
                this.saleCompleted = value;
                if (this.SaleCompeted != null)
                    this.SaleCompeted(this, new EventArgs());
            }
            get { return this.saleCompleted; }
        }
        public bool SaleProcessed { set; get; }
        public TankSaleData[] TankData { set; get; }
        public bool IsOnSale { set; get; }
        public Guid SalesId { private set; get; }
        public DateTime SaleInitialized { set; get; }
        public bool SaleStarted { set;get; }

        public SaleData()
        {
            this.SalesId = Guid.NewGuid();
        }
    }
}

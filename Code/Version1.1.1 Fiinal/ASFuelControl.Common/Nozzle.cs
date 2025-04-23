using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Common
{
    public class Nozzle
    {
        private Sales.SaleData sale;
        private decimal totalVolume;
        private decimal totalPrice;
        private decimal lastTotalVolume;
        private decimal lastTotalPrice;
        private int untiPriceInt = -1;
        private bool queryTotals = false;
        
        private List<Sales.SaleData> lastSales = new List<Sales.SaleData>();

        public event EventHandler<LostSaleEventArgs> SaleLostEvent;

        public int Index { set; get; }
        public decimal UnitPrice { set; get; }
        public int UntiPriceInt 
        {
            set { this.untiPriceInt = value; }
            get { return this.untiPriceInt; }
        }
        public decimal TotalVolume
        {
            set 
            {
                if (this.totalVolume == value)
                    return;
                this.LastTotalVolume = this.totalVolume;
                this.totalVolume = value;
            }
            get { return this.totalVolume; }
        }
        public decimal TotalPrice
        {
            set
            {
                if (this.totalPrice == value)
                    return;
                this.LastTotalPrice = this.totalPrice;
                this.totalPrice = value;
            }
            get { return this.totalPrice; }
        }
        public decimal LastTotalVolume 
        {
            set 
            { 
                this.lastTotalVolume = value;
                //if (this.ParentFuelPoint != null && this.ParentFuelPoint.ActiveNozzle == this)
                //{
                //    this.ParentFuelPoint.DispensedVolume = this.lastTotalVolume;
                //}
            }
            get { return this.lastTotalVolume; } 
        }
        public decimal LastTotalPrice
        {
            set 
            { 
                this.lastTotalPrice = value;
                //if (this.ParentFuelPoint != null && this.ParentFuelPoint.ActiveNozzle == this)
                //{
                //    this.ParentFuelPoint.DispensedAmount = this.lastTotalPrice;
                //}
            }
            get { return this.lastTotalPrice; } 
        }
        public DateTime LastTotalGot { set; get; }

        public Nozzle()
        {
            this.saleHandler.ParentNozzle = this;
        }

        public Sales.SaleHandler saleHandler = new Sales.SaleHandler();
        public bool QueryTotals 
        {
            set { this.queryTotals = value; }
            get { return this.queryTotals; } 
        }
        public bool QuerySetPrice { set; get; }

        public Sales.SaleData CurrentSale 
        {
            set 
            {
                //if (this.sale != null && !sale.SaleCompleted && sale.TotalVolume > 0)
                //{
                //    if (this.SaleLostEvent != null)
                //        this.SaleLostEvent(this, new LostSaleEventArgs() { Sale = this.sale});
                //}
                this.sale = value;
                if(sale == null)
                    return;
                this.sale.SaleCompeted -= new EventHandler(sale_SaleCompeted);
                this.sale.SaleCompeted += new EventHandler(sale_SaleCompeted);

            }
            get 
            {
                if (this.sale == null)
                {
                    this.sale = new Sales.SaleData();
                    this.sale.UnitPrice = this.UnitPrice;
                    this.sale.TotalizerStart = this.totalVolume == 0 ? -1 : this.totalVolume;
                    this.sale.SaleCompeted+=new EventHandler(sale_SaleCompeted);
                }
                return this.sale; 
            }
        }

        void sale_SaleCompeted(object sender, EventArgs e)
        {
            Sales.SaleData sale = sender as Sales.SaleData;

            if (this.lastSales.Count >= 20)
            {
                for(int i=0; i < this.lastSales.Count - 20; i++)
                    this.lastSales.RemoveAt(0);
            }
            
            Sales.SaleData oldSale = this.lastSales.Where(s => s.TotalizerStart == sale.TotalizerStart && s.TotalVolume > 0).FirstOrDefault();
            if (oldSale == null)
            {
                this.lastSales.Add(sale);
            }

            List<Sales.SaleData> newSales = new List<Sales.SaleData>();
            List<Sales.SaleData> sales = this.lastSales.OrderBy(ls => ls.TotalizerStart).ToList();
            for (int i = 0; i < sales.Count - 1; i++)
            {
                if (sales[i].TotalizerEnd != sales[i + 1].TotalizerStart)
                {
                    Sales.SaleData newSale = new Sales.SaleData();
                    if (sales[i].TotalizerEnd < sales[i + 1].TotalizerStart)
                    {
                        newSale.TotalizerStart = sales[i].TotalizerEnd;
                        newSale.TotalizerEnd = sales[i + 1].TotalizerStart;
                        newSale.TotalVolume = (newSale.TotalizerEnd - newSale.TotalizerStart) / (decimal)Math.Pow(10, this.ParentFuelPoint.DecimalPlaces);
                        newSale.UnitPrice = this.UnitPrice;
                        newSale.TotalPrice = decimal.Round(newSale.TotalVolume * newSale.UnitPrice, this.ParentFuelPoint.DecimalPlaces);
                        newSale.SaleEndTime = DateTime.Now;
                        newSale.SaleCompleted = true;
                        newSales.Add(newSale);
                    }
                }
            }
            if (newSales.Count > 0)
                this.lastSales.AddRange(newSales.ToArray());

            decimal maxTotal = this.lastSales.Count == 0? sale.TotalizerEnd : this.lastSales.Select(s => s.TotalizerEnd).Max();

            this.CurrentSale = new Sales.SaleData();
            this.CurrentSale.UnitPrice = this.UnitPrice;
            this.CurrentSale.TotalizerStart = maxTotal;// sale.TotalizerEnd;
            if (maxTotal != sale.TotalizerEnd)
            {
            }
        }

        public FuelPoint ParentFuelPoint { set; get; }

        public decimal GetLastTotalizer()
        {
            Sales.SaleData lastSale = this.lastSales.OrderBy(l => l.TotalizerStart).LastOrDefault();
            if (lastSale == null)
                return -1;
            return lastSale.TotalizerEnd;
        }

        public Sales.SaleData GetLastSale()
        {
            //if (this.lastSales.Count == 0)
            //    return null;
            //Sales.SaleData sale = this.lastSales.Where(s => !s.SaleProcessed).FirstOrDefault();
            //if (sale != null)
            //    return sale;
            //return this.lastSales[this.lastSales.Count - 1];
            Sales.SaleData sale = this.saleHandler.GetLastSale();
            if (sale == null || sale.SaleProcessed)
                return null;
            return sale;
        }
    }

    public class LostSaleEventArgs : EventArgs
    {
        public Sales.SaleData Sale { set; get; }
    }
}

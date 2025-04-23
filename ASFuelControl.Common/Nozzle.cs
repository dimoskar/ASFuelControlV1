using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Common
{
    internal class NozzleClass
    {
        public decimal AmountCash
        {
            get;
            set;
        }

        public decimal AmountCredit
        {
            get;
            set;
        }

        public decimal VolumeCash
        {
            get;
            set;
        }

        public decimal VolumeCredit
        {
            get;
            set;
        }

        public NozzleClass()
        {
        }
    }
    public class Nozzle
    {
        private Dictionary<string, object> extendedProperties = new Dictionary<string, object>();
        private Sales.SaleData sale;
        private decimal totalVolume;
        private decimal totalPrice;
        private decimal lastTotalVolume;
        private decimal lastTotalPrice;
        private int nozzleIndex;
        private int untiPriceInt = -1;
        private bool queryTotals = false;
        
        //private List<Sales.SaleData> lastSales = new List<Sales.SaleData>();

        public event EventHandler<LostSaleEventArgs> SaleLostEvent;

        public int NozzleIndex
        {
            set
            {
                this.nozzleIndex = value;
            }
            get
            {
                return this.nozzleIndex;
            }
        }
        public int Index { set; get; }
        public int NozzleSocket { set; get; }
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

        public List<decimal> Totals = new List<decimal>();

        public bool TotalMisfunction { set; get; }

        public bool HasValidTotals()
        {
            if (this.Totals.Count == 3)
            {
                decimal totals1 = this.Totals[0];
                decimal totals2 = this.Totals[1];
                decimal totals3 = this.Totals[2];
                if (totals1 == totals2)
                    return true;
                if (totals1 == totals3)
                    return true;
                if (totals3 == totals2)
                    return true;
                return false;
            }
            else if (this.Totals.Count == 2)
            {
                decimal totals1 = this.Totals[0];
                decimal totals2 = this.Totals[1];
                if (totals1 == totals2)
                    return true;
                return false;
            }
            return false;
        }

        public decimal GetTotalsFromList()
        {
            if (this.Totals.Count == 3)
            {
                decimal totals1 = this.Totals[0];
                decimal totals2 = this.Totals[1];
                decimal totals3 = this.Totals[2];
                if (totals1 == totals2)
                    return totals1;
                if (totals1 == totals3)
                    return totals1;
                if (totals3 == totals2)
                    return totals3;
                return totals3;
            }
            else if(this.Totals.Count == 2)
            {
                decimal totals1 = this.Totals[0];
                decimal totals2 = this.Totals[1];
                if (totals1 == totals2)
                    return totals1;
                return totals2;
            }
            string data = "\r\n" + "===================================\r\n";
            data = string.Format("{1:dd/MM/yyyy HH:mm:ss} - Count:{0}, Index:{2}", this.Totals.Count, DateTime.Now, this.GetTotalsIndex());
            foreach(decimal total in this.Totals)
            {
                data = data + "\r\n" + string.Format("{0:N3}", total);
            }
            data = data + "\r\n" + "===================================\r\n";
            System.IO.File.AppendAllText("TotalsMissed.log", data);
            return -1;
        }

        public decimal TransactionVolume
        {
            get { return this.TotalVolume - this.LastTotalVolume; }
        }

        public Nozzle()
        {
            //this.saleHandler.ParentNozzle = this;
        }

        //public Sales.SaleHandler saleHandler = new Sales.SaleHandler();
        public bool QueryTotals 
        {
            set
            {
                this.queryTotals = value;
                if (value)
                {
                    if (System.IO.File.Exists("QueryTotalSet.log"))
                        System.IO.File.AppendAllText("QueryTotalSet.log", string.Format("{0:dd/MM/yyyy HH:mm:ss.fff} QueryTotals = TRUE, Total: {1}", DateTime.Now, this.TotalVolume));
                }
            }
            get { return this.queryTotals; } 
        }

        public bool SuspendSale { set; get; }
        

        public bool QuerySetPrice { set; get; }

        //public Sales.SaleData CurrentSale 
        //{
        //    set 
        //    {
        //        //if (this.sale != null && !sale.SaleCompleted && sale.TotalVolume > 0)
        //        //{
        //        //    if (this.SaleLostEvent != null)
        //        //        this.SaleLostEvent(this, new LostSaleEventArgs() { Sale = this.sale});
        //        //}
        //        this.sale = value;
        //        if(sale == null)
        //            return;
        //        this.sale.SaleCompeted -= new EventHandler(sale_SaleCompeted);
        //        this.sale.SaleCompeted += new EventHandler(sale_SaleCompeted);

        //    }
        //    get 
        //    {
        //        if (this.sale == null)
        //        {
        //            this.sale = new Sales.SaleData();
        //            this.sale.UnitPrice = this.UnitPrice;
        //            this.sale.TotalizerStart = this.totalVolume == 0 ? -1 : this.totalVolume;
        //            this.sale.SaleCompeted+=new EventHandler(sale_SaleCompeted);
        //        }
        //        return this.sale; 
        //    }
        //}

        void sale_SaleCompeted(object sender, EventArgs e)
        {
            //Sales.SaleData sale = sender as Sales.SaleData;

            //if (this.lastSales.Count >= 20)
            //{
            //    for(int i=0; i < this.lastSales.Count - 20; i++)
            //        this.lastSales.RemoveAt(0);
            //}
            
            //Sales.SaleData oldSale = this.lastSales.Where(s => s.TotalizerStart == sale.TotalizerStart && s.TotalVolume > 0).FirstOrDefault();
            //if (oldSale == null)
            //{
            //    this.lastSales.Add(sale);
            //}

            //List<Sales.SaleData> newSales = new List<Sales.SaleData>();
            //List<Sales.SaleData> sales = this.lastSales.OrderBy(ls => ls.TotalizerStart).ToList();
            //for (int i = 0; i < sales.Count - 1; i++)
            //{
            //    if (sales[i].TotalizerEnd != sales[i + 1].TotalizerStart)
            //    {
            //        Sales.SaleData newSale = new Sales.SaleData();
            //        if (sales[i].TotalizerEnd < sales[i + 1].TotalizerStart)
            //        {
            //            newSale.TotalizerStart = sales[i].TotalizerEnd;
            //            newSale.TotalizerEnd = sales[i + 1].TotalizerStart;
            //            newSale.TotalVolume = (newSale.TotalizerEnd - newSale.TotalizerStart) / (decimal)Math.Pow(10, this.ParentFuelPoint.AmountDecimalPlaces);
            //            newSale.UnitPrice = this.UnitPrice;
            //            newSale.TotalPrice = decimal.Round(newSale.TotalVolume * newSale.UnitPrice, this.ParentFuelPoint.AmountDecimalPlaces);
            //            newSale.SaleEndTime = DateTime.Now;
            //            newSale.SaleCompleted = true;
            //            newSales.Add(newSale);
            //        }
            //    }
            //}
            //if (newSales.Count > 0)
            //    this.lastSales.AddRange(newSales.ToArray());

            //decimal maxTotal = this.lastSales.Count == 0? sale.TotalizerEnd : this.lastSales.Select(s => s.TotalizerEnd).Max();

            //this.CurrentSale = new Sales.SaleData();
            //this.CurrentSale.UnitPrice = this.UnitPrice;
            //this.CurrentSale.TotalizerStart = maxTotal;// sale.TotalizerEnd;
            //if (maxTotal != sale.TotalizerEnd)
            //{
            //}
        }

        public FuelPoint ParentFuelPoint { set; get; }

        //public decimal GetLastTotalizer()
        //{
        //    Sales.SaleData lastSale = this.lastSales.OrderBy(l => l.TotalizerStart).LastOrDefault();
        //    if (lastSale == null)
        //        return -1;
        //    return lastSale.TotalizerEnd;
        //}

        //public Sales.SaleData GetLastSale()
        //{
        //    //Sales.SaleData sale = this.saleHandler.GetLastSale();
        //    //if (sale == null || sale.SaleProcessed)
        //    //    return null;
        //    //return sale;
        //    Sales.SaleData sale = new Sales.SaleData();
        //    return sale;
        //}
        public object GetExtendedProperty(string name)
        {
            if(!this.extendedProperties.ContainsKey(name))
                return null;
            return this.extendedProperties[name];
        }

        public object GetExtendedProperty(string name, object defaultValue)
        {
            if(!this.extendedProperties.ContainsKey(name))
                return defaultValue;
            return this.extendedProperties[name];
        }

        public void SetExtendedProperty(string name, object value)
        {
            if(!this.extendedProperties.ContainsKey(name))
                this.extendedProperties.Add(name, value);
            else
                this.extendedProperties[name] = value;
        }

        public int GetTotalsIndex()
        {
            if (this.GetExtendedProperty("TotalIndex") == null)
            {
                this.SetExtendedProperty("TotalIndex", 0);
                return 0;
            }
            return (int)this.GetExtendedProperty("TotalIndex");
        }
    }

    public class LostSaleEventArgs : EventArgs
    {
        public Sales.SaleData Sale { set; get; }
    }
}

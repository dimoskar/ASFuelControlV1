using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Common.Sales
{
    public class SaleHandler
    {
        public Common.Nozzle ParentNozzle { set; get; }

        public List<TotalizerState> totalizers = new List<TotalizerState>();

        public SaleData AddTotalizer(decimal nozzleTotalizer, decimal unitPrice, decimal dispensedVolume, decimal dispensedPrice)
        {
            decimal totalizerDiff = 0;
            TotalizerState state = new TotalizerState();
            try
            {
                ParentNozzle.QueryTotals = false;
                state.Totalizer = nozzleTotalizer;
                state.Sale = new SaleData();
                state.Sale.TotalizerEnd = nozzleTotalizer;
                if (this.totalizers.Count == 0)
                {
                    state.Sale.SaleCompleted = true;
                    this.totalizers.Add(state);
                    return state.Sale;
                }
                
                TotalizerState lastState = this.totalizers.LastOrDefault();
                if (lastState == null)
                {
                    state.Sale.SaleCompleted = true;
                    this.totalizers.Add(state);
                    return state.Sale;
                }
                state.Sale.TotalizerStart = lastState.Totalizer;
                state.Sale.TotalVolume = dispensedVolume;
                state.Sale.TotalPrice = dispensedPrice;
                state.Sale.UnitPrice = unitPrice;
                totalizerDiff = (state.Sale.TotalizerEnd - state.Sale.TotalizerStart) / 100;

                if (totalizerDiff > 0 && totalizerDiff != dispensedVolume)
                {
                    state.Sale.TotalVolume = totalizerDiff;
                    state.Sale.TotalPrice = decimal.Round(state.Sale.TotalVolume * unitPrice, 2);
                    state.Sale.SaleCompleted = true;
                    this.totalizers.Add(state);
                }
                else if (totalizerDiff > 0 && totalizerDiff == dispensedVolume)
                {
                    state.Sale.TotalVolume = totalizerDiff;
                    state.Sale.TotalPrice = dispensedPrice;
                    state.Sale.SaleCompleted = true;
                    this.totalizers.Add(state);
                }
                else if (totalizerDiff < 0)
                {
                    this.totalizers.Clear();
                    state.Sale.SaleCompleted = true;
                    this.totalizers.Add(state);
                }
                else if (totalizerDiff == 0 && dispensedVolume > 0)
                    ParentNozzle.QueryTotals = true;
                return state.Sale;
            }

            catch
            {
                return state.Sale;
            }
            finally
            {
                if (!(totalizerDiff == 0 && dispensedVolume > 0))
                {
                    ParentNozzle.ParentFuelPoint.DispensedVolume = 0;
                    ParentNozzle.ParentFuelPoint.DispensedAmount = 0;
                }
                for (int i = 20; i < this.totalizers.Count; i++)
                {
                    this.totalizers.RemoveAt(0);
                }
                
            }
        }

        public SaleData GetLastSale()
        {
            if(totalizers.Count == 0)
                return null;
            return this.totalizers.Last().Sale;
            
        }
    }

    public class TotalizerState
    {
        public decimal Totalizer { set; get; }
        public SaleData Sale { set; get; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.EMR3
{
    public class EmrController : Common.FuelPumpControllerBase
    {
        public EmrController()
        {
            this.Controller = new Protocol();
        }

        public override void Connect()
        {
            this.Controller.Connect();
        }

        public override void DisConnect()
        {
            this.Controller.Disconnect();
        }

        protected override void OnTransactionCompleted(Common.TotalsEventArgs e)
        {
            Common.FuelPoint fp = e.CurrentFuelPoint;
            if (fp == null)
                return;

            Common.Nozzle nz = fp.Nozzles.Where(n => n.Index == e.NozzleId).FirstOrDefault();

            if (nz == null || nz.CurrentSale == null)
            {
                if (nz == null)
                    Console.WriteLine("Totals Recieved. NOZZLE NULL");
                else
                {
                    if (nz.CurrentSale == null)
                        Console.WriteLine("Totals Recieved. CurrentSale NULL");
                }
                return;
            }
            Console.WriteLine("Totals Recieved. TotalStart : {0}, TotalVolume : {1}, Status : {2}", nz.CurrentSale.TotalizerStart, e.TotalVolume, fp.Status);

            if ((fp.Status == Common.Enumerators.FuelPointStatusEnum.TransactionCompleted || fp.Status == Common.Enumerators.FuelPointStatusEnum.TransactionStopped) && !nz.CurrentSale.SaleCompleted)
            {
                fp.Nozzles.Where(n => n.Index == e.NozzleId).First().TotalVolume = e.TotalVolume;

                if (e.TotalVolume <= nz.CurrentSale.TotalizerStart)
                {
                    nz.QueryTotals = true;
                    Console.WriteLine("Query New Totals.");
                    //nz.ParentFuelPoint.QueryTotals = true;
                    return;
                }
                nz.QueryTotals = false;
                nz.CurrentSale.TotalizerEnd = e.TotalVolume;
                nz.CurrentSale.TotalVolume = (nz.CurrentSale.TotalizerEnd - nz.CurrentSale.TotalizerStart) / 100;

                //int roundPrice = (int)Math.Pow(10, fp.DecimalPlaces);
                nz.CurrentSale.TotalVolume = (nz.CurrentSale.TotalizerEnd - nz.CurrentSale.TotalizerStart) / 100;
                nz.CurrentSale.TotalPrice = decimal.Round(nz.CurrentSale.TotalVolume * nz.CurrentSale.UnitPrice, 2);
                //nz.CurrentSale.TotalPrice = decimal.Round(nz.CurrentSale.TotalPrice, fp.DecimalPlaces);

                nz.CurrentSale.SaleEndTime = DateTime.Now;
                nz.CurrentSale.SaleCompleted = true;
                fp.Status = fp.DispenserStatus;
                Common.FuelPointValues values = new Common.FuelPointValues();
                values.Status = Common.Enumerators.FuelPointStatusEnum.TransactionCompleted;
                values.CurrentPriceTotal = nz.CurrentSale.TotalVolume;
                values.CurrentSalePrice = nz.CurrentSale.UnitPrice;
                values.CurrentVolume = nz.CurrentSale.TotalVolume;
                this.EnqueValues(fp, values);
            }
        }
    }
}

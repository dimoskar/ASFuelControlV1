using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASFuelControl.Common.Enumerators;

namespace ASFuelControl.NuovoPignone
{
    public class Controller : Common.FuelPumpControllerBase
    {
        public Controller()
        {
            this.ControllerType = Common.Enumerators.ControllerTypeEnum.NuovoPignone;
            this.Controller = new NuovoPignoneProtocol();
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

            if (!nz.CurrentSale.SaleCompleted)
            {
                nz.CurrentSale.TotalizerEnd = e.TotalVolume;
                decimal totalVolume = (nz.CurrentSale.TotalizerEnd - nz.CurrentSale.TotalizerStart) / 100;

                if (nz.CurrentSale.TotalizerStart < 0)
                {
                    nz.CurrentSale.TotalizerStart = e.TotalVolume;
                    nz.QueryTotals = false;
                    return;
                }
                if (totalVolume > 1000)
                {
                    nz.CurrentSale.TotalVolume = 0;
                    nz.CurrentSale.SaleCompleted = true;
                    nz.QueryTotals = false;
                    return;
                }

                nz.QueryTotals = false;
                nz.CurrentSale.TotalVolume = totalVolume;

                if (nz.CurrentSale.TotalizerStart < 0)
                    nz.CurrentSale.TotalVolume = 0;

                nz.CurrentSale.TotalPrice = decimal.Round(nz.CurrentSale.TotalVolume * nz.CurrentSale.UnitPrice, fp.DecimalPlaces);
                nz.CurrentSale.SaleEndTime = DateTime.Now;
                
                Common.FuelPointValues values = new Common.FuelPointValues();
                values.Status = fp.DispenserStatus;
                values.CurrentPriceTotal = nz.CurrentSale.TotalVolume;
                values.CurrentSalePrice = nz.CurrentSale.UnitPrice;
                values.CurrentVolume = nz.CurrentSale.TotalVolume;

                this.EnqueValues(fp, values);
                nz.CurrentSale.SaleCompleted = true;
            }
        }

        protected override void OnStatusChanged(Common.FuelPointValuesArgs e)
        {
            Common.FuelPoint fuelPoint = e.CurrentFuelPoint;
            if (fuelPoint == null)
                return;


            decimal dividor = (decimal)Math.Pow(10, (double)fuelPoint.DecimalPlaces);

            ASFuelControl.Common.FuelPointValues values = new ASFuelControl.Common.FuelPointValues();
            values.ActiveNozzle = fuelPoint.ActiveNozzleIndex;
            if (fuelPoint.Status != FuelPointStatusEnum.Offline)
            {
                values.CurrentPriceTotal = fuelPoint.DispensedAmount;
                values.CurrentVolume = fuelPoint.DispensedVolume;
            }
            if (fuelPoint.ActiveNozzle != null)
            {
                values.ActiveNozzle = fuelPoint.ActiveNozzleIndex;
            }
            values.Status = fuelPoint.Status;
            values.TotalVolumes = fuelPoint.Nozzles.Select(n => n.TotalVolume).ToArray();


            Common.Nozzle salesNozzle = fuelPoint.ActiveNozzle;
            if (salesNozzle == null)
                salesNozzle = fuelPoint.LastActiveNozzle;
           
            if (fuelPoint.Status == FuelPointStatusEnum.Idle)
            {
                salesNozzle.QueryTotals = true;
            }
            else if (fuelPoint.DispenserStatus == Common.Enumerators.FuelPointStatusEnum.Offline)
            {
                foreach (Common.Nozzle n in fuelPoint.Nozzles)
                    n.CurrentSale = null;
            }
            fuelPoint.Status = values.Status;
            this.EnqueValues(fuelPoint, values);
        }

        public override decimal GetNozzleTotalizer(int channel, int address, int nozzleId)
        {
            Common.FuelPoint fp = this.GetFulePoint(channel, address);
            if (fp == null)
                return 0;
            if (fp.NozzleCount > nozzleId)
                return 0;
            if (!(bool)fp.GetExtendedProperty("TotalsInitialized", false))
                return -1;
            return fp.Nozzles[nozzleId - 1].TotalVolume;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASFuelControl.Common.Enumerators;

namespace ASFuelControl.AK6
{
    public class Controller: Common.FuelPumpControllerBase
    {
        public Controller()
        {
            this.ControllerType = Common.Enumerators.ControllerTypeEnum.AK6;
            this.Controller = new AK6Protocol();
        }
        //protected override void OnTransactionCompleted(Common.TotalsEventArgs e)
        //{
        //    Common.FuelPoint fp = e.CurrentFuelPoint;
        //    if(fp == null)
        //        return;

        //    Common.Nozzle nz = fp.Nozzles.Where(n => n.Index == e.NozzleId).FirstOrDefault();

        //    if(nz == null || nz.CurrentSale == null)
        //    {
        //        if(nz == null)
        //            Console.WriteLine("Totals Recieved. NOZZLE NULL");
        //        else
        //        {
        //            if(nz.CurrentSale == null)
        //                Console.WriteLine("Totals Recieved. CurrentSale NULL");
        //        }
        //        return;
        //    }
        //    Console.WriteLine("Totals Recieved. TotalStart : {0}, TotalVolume : {1}, Status : {2}", nz.CurrentSale.TotalizerStart, e.TotalVolume, fp.Status);

        //    if(!nz.CurrentSale.SaleCompleted) //fp.Status == FuelPointStatusEnum.Idle && 
        //    {
        //        //fp.Nozzles.Where(n => n.Index == e.NozzleId).First().TotalVolume = e.TotalVolume;

        //        //if (e.TotalVolume <= nz.CurrentSale.TotalizerStart)
        //        //{
        //        //    nz.QueryTotals = true;
        //        //    Console.WriteLine("Query New Totals.");
        //        //    //nz.ParentFuelPoint.QueryTotals = true;
        //        //    return;
        //        //}

        //        nz.QueryTotals = false;
        //        nz.CurrentSale.TotalizerEnd = e.TotalVolume;
        //        nz.CurrentSale.TotalVolume = (nz.CurrentSale.TotalizerEnd - nz.CurrentSale.TotalizerStart) / 100;

        //        if(nz.CurrentSale.TotalizerStart < 0)
        //            nz.CurrentSale.TotalVolume = 0;

        //        //int roundPrice = (int)Math.Pow(10, fp.DecimalPlaces);
        //        //nz.CurrentSale.TotalVolume = nz.ParentFuelPoint.DispensedVolume;
        //        nz.CurrentSale.TotalPrice = decimal.Round(nz.CurrentSale.TotalVolume * nz.CurrentSale.UnitPrice, fp.DecimalPlaces);
        //        //nz.CurrentSale.TotalPrice = decimal.Round(nz.CurrentSale.TotalPrice, fp.DecimalPlaces);

        //        nz.CurrentSale.SaleEndTime = DateTime.Now;
        //        nz.CurrentSale.SaleCompleted = true;
        //        Common.FuelPointValues values = new Common.FuelPointValues();
        //        values.Status = Common.Enumerators.FuelPointStatusEnum.Idle;
        //        values.CurrentPriceTotal = nz.CurrentSale.TotalVolume;
        //        values.CurrentSalePrice = nz.CurrentSale.UnitPrice;
        //        values.CurrentVolume = nz.CurrentSale.TotalVolume;
        //        this.EnqueValues(fp, values);
        //    }
        //}

        //protected override void OnStatusChanged(Common.FuelPointValuesArgs e)
        //{
        //    Common.FuelPoint fuelPoint = e.CurrentFuelPoint;
        //    if(fuelPoint == null)
        //        return;


        //    decimal dividor = (decimal)Math.Pow(10, (double)fuelPoint.DecimalPlaces);

        //    ASFuelControl.Common.FuelPointValues values = new ASFuelControl.Common.FuelPointValues();
        //    values.ActiveNozzle = fuelPoint.ActiveNozzleIndex;
        //    if(fuelPoint.Status != FuelPointStatusEnum.Offline)
        //    {
        //        values.CurrentPriceTotal = fuelPoint.DispensedAmount;
        //        values.CurrentVolume = fuelPoint.DispensedVolume;
        //    }
        //    if(fuelPoint.ActiveNozzle != null)
        //    {
        //        values.ActiveNozzle = fuelPoint.ActiveNozzleIndex;
        //    }
        //    values.Status = fuelPoint.Status;
        //    values.TotalVolumes = fuelPoint.Nozzles.Select(n => n.TotalVolume).ToArray();


        //    Common.Nozzle salesNozzle = fuelPoint.ActiveNozzle;
        //    if(salesNozzle == null)
        //        salesNozzle = fuelPoint.LastActiveNozzle;
        //    if(fuelPoint.Status == FuelPointStatusEnum.Nozzle && values.ActiveNozzle >= 0 && (fuelPoint.ActiveNozzle.CurrentSale == null || fuelPoint.ActiveNozzle.CurrentSale.SaleCompleted))
        //    {
        //        Console.WriteLine("Sale Created. TotalizerStart : {0}", fuelPoint.ActiveNozzle.CurrentSale.TotalizerStart);
        //    }
        //    else
        //    {
        //        if((fuelPoint.Status == FuelPointStatusEnum.Idle) && salesNozzle != null && salesNozzle.CurrentSale != null && !salesNozzle.CurrentSale.SaleCompleted)
        //        {
        //            salesNozzle.QueryTotals = true;
        //        }
        //    }
        //    fuelPoint.Status = values.Status;
        //    this.EnqueValues(fuelPoint, values);
        //}

        //public override decimal GetNozzleTotalizer(int channel, int address, int nozzleId)
        //{
        //    Common.FuelPoint fp = this.GetFulePoint(channel, address);
        //    if(fp == null)
        //        return 0;
        //    if(fp.NozzleCount > nozzleId)
        //        return 0;
        //    if(!(bool)fp.GetExtendedProperty("TotalsInitialized", false))
        //        return -1;
        //    return fp.Nozzles[nozzleId - 1].TotalVolume;
        //}
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASFuelControl.Common;
using System.Collections.Concurrent;

namespace ASFuelControl.Gilbarco
{
    public class GilbarcoController : Common.FuelPumpControllerBase
    {
        public GilbarcoController()
        {
            this.Controller = new Connector();
        }

        public override void AddFuelPoint(int channel, int address, int nozzleCount, int decimalPlaces, int volumeDecimalPlaces, int untiPriceDecimalPlaces)
        {
            
            FuelPoint fpi = new FuelPoint();
            fpi.NozzleCount = nozzleCount;
            this.fuelPoints.Add(fpi);
            fpi.Address = address;
            fpi.Channel = channel;
            fpi.UnitPriceDecimalPlaces = untiPriceDecimalPlaces;
            fpi.VolumeDecimalPlaces = volumeDecimalPlaces;
            fpi.DecimalPlaces = decimalPlaces;
            this.internalQueue.TryAdd(fpi, new ConcurrentQueue<ASFuelControl.Common.FuelPointValues>());

            this.Controller.AddFuelPoint(fpi);
        }

        public override bool SetDispenserSalesPrice(int channel, int address, decimal[] price)
        {
            FuelPoint fp = this.GetFulePoint(channel, address);
            if (fp == null)
                return false;
            if (fp.NozzleCount != price.Length)
                return false;
            while (this.fuelPoints.Where(f => f.DispenserStatus == Common.Enumerators.FuelPointStatusEnum.Listen).Count() > 0)
            {
                System.Threading.Thread.Sleep(100);
            }
            for (int i = 0; i < price.Length; i++)
            {
                this.SetNozzlePrice(channel, address, fp.Nozzles[i].Index, price[i]);
                System.Threading.Thread.Sleep(100);
            }
            return true;
        }

        public override bool SetNozzlePrice(int channel, int address, int nozzleId, decimal newPrice)
        {
            Console.WriteLine("Set Price for {0}", address);
            FuelPoint fp = this.GetFulePoint(channel, address);
            if (fp == null)
                return false;

            fp.Nozzles[nozzleId - 1].UnitPrice = newPrice;
            fp.Nozzles[nozzleId - 1].UntiPriceInt = (int)(newPrice * (decimal)Math.Pow(10, fp.UnitPriceDecimalPlaces));
            fp.QuerySetPrice = true;
            return true;
        }
    }
}

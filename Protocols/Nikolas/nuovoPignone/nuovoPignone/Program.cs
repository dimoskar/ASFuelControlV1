using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nuovoPignone
{
    class Program
    {
        static void Main(string[] args)
        {
            byte[] Command = new byte[] { };

            pignoneAPI dispenser = new pignoneAPI();

            Nozzle nozzle = new Nozzle();
            nozzle.ParentFuelPoint.AddressId = 1;
            nozzle.NozzleIndex = 1;
            nozzle.ParentFuelPoint.Status = Common.Enumerators.FuelPointStatusEnum.Idle;
            nozzle.UnitPrice = 1555;
            nozzle.Totalizer = 0;
            nozzle.VolumeDecimalPlaces = 2;
            nozzle.PriceDecimalPlaces  = 2;

            Console.WriteLine("Get nozzle {0} status:",nozzle.NozzleIndex);
            dispenser.GetStatus(nozzle);
            Console.WriteLine("status ->{0}", nozzle.ParentFuelPoint.Status);
            Console.WriteLine("*********************************************");
            dispenser.GetDisplay(nozzle);
            Console.WriteLine("Euro:{0}\nLitra:{1}\nUP:{2} ", nozzle.SalePrice, nozzle.SaleVolume, nozzle.SaleUnitPrice);
            Console.WriteLine("*********************************************");
            dispenser.GetTotals(nozzle);
            Console.WriteLine("Totas:{0} ", nozzle.Totalizer);

            while (true)
            {
                dispenser.GetStatus(nozzle);
                Console.WriteLine("Get nozzle {0} status:", nozzle.NozzleIndex);
                if (nozzle.Status == Common.Enumerators.FuelPointStatusEnum.Idle) continue;
                else if (nozzle.Status == Common.Enumerators.FuelPointStatusEnum.Nozzle) 
                { 
                }

            }
           // dispenser.g


        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Communication
{
    public class FuelTypePumpClass
    {
        public Enums.FuelTypeEnum FuelType { set; get; }
        public string FuelPumpId { set; get; }
        public string FuelPumpSerialNumber { set; get; }
        public int NozzleId { set; get; }
        public string TankSerialNumber { set; get; }
        public decimal TotalizerStart { set; get; }
        public decimal TotalizerEnd { set; get; }
        public decimal TotalSales { set; get; }
        public decimal TotalSalesNormalized { set; get; }
        public decimal TotalOut { set; get; }
        public decimal TotalOutNormalized { set; get; }
        public decimal TotalLiterCheck { set; get; }
        public decimal TotalLiterCheckNormalized { set; get; }
        
        public decimal SumTotalOut { set; get; }
        public decimal SumTotalOutNormalized { set; get; }

        public decimal TotalizerDifference { set; get; }
        public decimal SumTotalOutTotalizer { set; get; }
        public decimal SumTotalOutTotalizerNormalized { set; get; }


        public FuelTypePumpClass()
        {
        }

        public FuelFlowService.ArrayOfFuelflows_TypeBalancePumpsPerFuelTypeFuelTypesFuelTypePumpFuelTypePump GetElement()
        {
            FuelFlowService.ArrayOfFuelflows_TypeBalancePumpsPerFuelTypeFuelTypesFuelTypePumpFuelTypePump fp = new FuelFlowService.ArrayOfFuelflows_TypeBalancePumpsPerFuelTypeFuelTypesFuelTypePumpFuelTypePump();
            fp.FuelType = new FuelFlowService.Fuel_Type();
            fp.FuelType.Code = (int)this.FuelType;
            fp.FuelType.Description = Enums.LocalizedEnumExtensions.GetLocalizedName(this.FuelType);
            fp.F_Pump_Reservoir = this.TankSerialNumber;
            fp.F_ANTLIA_AA = this.FuelPumpSerialNumber;
            fp.F_2241A = this.FuelPumpId;
            fp.F_2241B = this.NozzleId;
            fp.F_2242 = this.TotalizerStart;
            fp.F_2243 = this.TotalizerEnd;
            fp.F_2244A = this.TotalSales;
            fp.F_2244B = this.TotalOut;
            fp.F_2244C = this.TotalOutNormalized;
            fp.F_2244D = this.TotalLiterCheck;
            fp.F_2244E = this.SumTotalOut;
            fp.F_2244F = this.SumTotalOutNormalized;
            fp.F_2245A = this.TotalizerDifference;
            fp.F_2245B = this.SumTotalOutTotalizer;
            fp.F_2245C = this.SumTotalOutTotalizerNormalized;
            return fp;
        }
    }

    public class FuelTypeClass
    {
        public Enums.FuelTypeEnum FuelType { set; get; }
        public decimal SumTotalizerDifference { set; get; }
        public decimal SumTotalizerDifferenceNormalized { set; get; }
        public int TotalPumpsNumber { set; get; }
        public List<FuelTypePumpClass> FuelPumps { set; get; }

        public FuelFlowService.ArrayOfFuelflows_TypeBalancePumpsPerFuelTypeFuelTypesFuelTypes GetElement()
        {
            FuelFlowService.ArrayOfFuelflows_TypeBalancePumpsPerFuelTypeFuelTypesFuelTypes ftype = new FuelFlowService.ArrayOfFuelflows_TypeBalancePumpsPerFuelTypeFuelTypesFuelTypes();
            ftype.F_KAYSIMO_AA = new FuelFlowService.Fuel_Type();
            ftype.F_KAYSIMO_AA.Code = (int)this.FuelType;
            ftype.F_KAYSIMO_AA.Description = Enums.LocalizedEnumExtensions.GetLocalizedName(this.FuelType);
            ftype.F_2244B = this.SumTotalizerDifference;
            ftype.F_2245B = this.SumTotalizerDifferenceNormalized;
            ftype.FuelTypePump = new FuelFlowService.ArrayOfFuelflows_TypeBalancePumpsPerFuelTypeFuelTypesFuelTypePumpFuelTypePump[][] { this.FuelPumps.Select(fpc => fpc.GetElement()).ToArray() };
            ftype.TotalPumpsNumber = ftype.FuelTypePump.Length;
            return ftype;
        }
    }

    public class FuelTypeBalances
    {
        public int FuelTypeCount { set; get; }
        public List<FuelTypeClass> FuelTypes { set; get; }

        public FuelFlowService.SendBalanceDSPumpsPerFuelType GetElement()
        {
            FuelFlowService.SendBalanceDSPumpsPerFuelType ff = new FuelFlowService.SendBalanceDSPumpsPerFuelType();
            ff.FuelTypes = new FuelFlowService.ArrayOfFuelflows_TypeBalancePumpsPerFuelTypeFuelTypesFuelTypes[][] { this.FuelTypes.Select(fpc => fpc.GetElement()).ToArray() };
            ff.F_KAYSIMA_NO = ff.FuelTypes.Length;
            return ff;
        }

    }
}


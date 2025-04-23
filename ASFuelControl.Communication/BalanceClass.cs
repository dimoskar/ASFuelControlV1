using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Communication
{
    [Serializable]
    public class ReservoirClass
    {
        public int TankId { set; get; }
        public string TankSerialNumber { set; get; }
        public Enums.FuelTypeEnum FuelType { set; get; }
        public decimal Capacity { set; get; }
        public decimal LevelStart { set; get; }
        public decimal VolumeStart { set; get; }
        public decimal TemperatureStart { set; get; }
        public decimal LevelEnd { set; get; }
        public decimal VolumeStartNormalized { set; get; }
        public decimal VolumeEnd { set; get; }
        public decimal TemperatureEnd { set; get; }
        public decimal VolumeEndNormalized { set; get; }

        public FuelFlowService.Fuelflows_TypeBalanceReservoirsReservoir GetElement()
        {
            FuelFlowService.Fuelflows_TypeBalanceReservoirsReservoir res = new FuelFlowService.Fuelflows_TypeBalanceReservoirsReservoir();
            res.F_2232_MM = this.LevelStart;
            res.F_2232_TEMP = this.TemperatureEnd;
            res.F_2232_VOL = this.VolumeStart;
            res.F_2233 = this.VolumeStartNormalized;
            res.F_2234_MM = this.LevelEnd;
            res.F_2234_TEMP = this.TemperatureEnd;
            res.F_2234_VOL = this.VolumeEnd;
            res.F_2235 = this.VolumeEndNormalized;
            res.F_DEXAMENH_AA = this.TankId;
            res.F_DEXAMENH_XORITIKOTITA = this.Capacity;
            res.F_KAYSIMO_EIDOS = new FuelFlowService.Fuel_Type();
            res.F_KAYSIMO_EIDOS.Code = (int)this.FuelType;
            res.F_KAYSIMO_EIDOS.Description = Enums.LocalizedEnumExtensions.GetLocalizedName(this.FuelType);
            return res;
        }
    }

    [Serializable]
    public class ReservoirsClass
    {
        public List<ReservoirClass> Reservoirs { set; get; }

        public FuelFlowService.Fuelflows_TypeBalanceReservoirs GetElement()
        {
            FuelFlowService.Fuelflows_TypeBalanceReservoirs res = new FuelFlowService.Fuelflows_TypeBalanceReservoirs();
            res.Reservoir = this.Reservoirs.Select(fmc => fmc.GetElement()).ToArray();
            res.ReservoirsNumber = this.Reservoirs.Count;
            return res;
        }
    }

    [Serializable]
    public class FuelMovementClass
    {
        public Enums.FuelTypeEnum FuelType { set; get; }
        public decimal SumIn { set; get; }
        public decimal SumAdditionalIn { set; get; }
        public decimal SumInNormalized { set; get; }
        public decimal SumAdditionalInNormalized { set; get; }
        public decimal SumInInvoiced { set; get; }
        public decimal SumInInvoicedNormalized { set; get; }
        public decimal Diff { set; get; }
        public decimal DiffNormalized { set; get; }
        public decimal DaylyMove { set; get; }
        public decimal DaylyMoveNormalized { set; get; }

        public FuelMovementClass()
        {
        }

        public FuelFlowService.Fuelflows_TypeBalanceFuelMovementsFuelMovement GetElement()
        {
            FuelFlowService.Fuelflows_TypeBalanceFuelMovementsFuelMovement fm = new FuelFlowService.Fuelflows_TypeBalanceFuelMovementsFuelMovement();
            fm.F_KAYSIMO_EIDOS = new FuelFlowService.Fuel_Type();
            fm.F_KAYSIMO_EIDOS.Code = (int)this.FuelType;
            fm.F_KAYSIMO_EIDOS.Description = Enums.LocalizedEnumExtensions.GetLocalizedName(this.FuelType);
            fm.F_22310 = this.DaylyMove;
            fm.F_22310A = this.DaylyMove;
            fm.F_22310B = this.DaylyMoveNormalized;
            fm.F_2236A1 = this.SumIn;
            fm.F_2236A2 = this.SumAdditionalIn;
            fm.F_2236B1 = this.SumInNormalized;
            fm.F_2236B2 = this.SumAdditionalInNormalized;
            fm.F_2237 = this.SumInInvoiced;
            fm.F_2238 = this.SumInInvoicedNormalized;
            fm.F_2239A = fm.F_2236A1 - fm.F_2237;
            fm.F_2239B = fm.F_2236B1 - fm.F_2238;

            return fm;
        }
    }

    [Serializable]
    public class FuelMovementsClass
    {
        public List<FuelMovementClass> FuelMovements { set; get; }

        public FuelFlowService.Fuelflows_TypeBalanceFuelMovements GetElement()
        {
            FuelFlowService.Fuelflows_TypeBalanceFuelMovements fm = new FuelFlowService.Fuelflows_TypeBalanceFuelMovements();
            fm.FuelMovement = this.FuelMovements.Select(fmc => fmc.GetElement()).ToArray();
            fm.F_KAYSIMA_NO = this.FuelMovements.Count;
            return fm;
        }
    }

    [Serializable]
    public class FuelTypeDivClass
    {
        public Enums.FuelTypeEnum FuelType { set; get; }
        public decimal Divergence { set; get; }
        public decimal DivergenceNormalized { set; get; }
        public decimal Percentage { set; get; }
        public decimal PercentageNormalized { set; get; }

        public FuelFlowService.Fuelflows_TypeBalanceDivergencesPerFuelTypeFuelTypeDivergence GetElement()
        {
            FuelFlowService.Fuelflows_TypeBalanceDivergencesPerFuelTypeFuelTypeDivergence div = new FuelFlowService.Fuelflows_TypeBalanceDivergencesPerFuelTypeFuelTypeDivergence();
            div.FuelType = new FuelFlowService.Fuel_Type();
            div.FuelType.Code = (int)this.FuelType;
            div.FuelType.Description = Enums.LocalizedEnumExtensions.GetLocalizedName(this.FuelType);
            div.F_2251 = this.Divergence;
            div.F_2252 = this.DivergenceNormalized;
            div.F_2253 = this.Percentage;
            div.F_2254 = this.PercentageNormalized;

            return div;
        }
    }

    [Serializable]
    public class FuelTypeDivsClass
    {
        public List<FuelTypeDivClass> Divergences { set; get; }

        public FuelFlowService.Fuelflows_TypeBalanceDivergencesPerFuelType GetElement()
        {
            FuelFlowService.Fuelflows_TypeBalanceDivergencesPerFuelType div = new FuelFlowService.Fuelflows_TypeBalanceDivergencesPerFuelType();
            div.FuelTypeDivergence = this.Divergences.Select(fmc => fmc.GetElement()).ToArray();
            div.FuelTypesNumber = this.Divergences.Count;
            return div;
        }
    }

    [Serializable]
    public class BalanceClass
    {
        public DateTime Date { set; get; }
        public DateTime TimeStart { set; get; }
        public DateTime TimeEnd { set; get; }

        public FuelTypeDivsClass Divergences { set; get; }
        public FuelMovementsClass Movements { set; get; }
        public FuelTypeBalances PumpsPerFuel { set; get; }
        public ReservoirsClass Reservoirs { set; get; }

        public FuelFlowService.Fuelflows_TypeBalance GetElement()
        {
            FuelFlowService.Fuelflows_TypeBalance balance = new FuelFlowService.Fuelflows_TypeBalance();
            balance.DivergencesPerFuelType = this.Divergences.GetElement();
            balance.FuelMovements = this.Movements.GetElement();
            balance.PumpsPerFuelType = this.PumpsPerFuel.GetElement();
            balance.Reservoirs = this.Reservoirs.GetElement();
            balance.F_HMEROMINIA_R = new FuelFlowService.Fuelflows_TypeBalanceF_HMEROMINIA_R();
            balance.F_HMEROMINIA_R.F_DATEDURATION = this.Date;
            balance.F_HMEROMINIA_R.F_ORA_ARXHS_R = this.TimeStart;
            balance.F_HMEROMINIA_R.F_ORA_LIXIS = this.TimeEnd;
            if (balance.F_HMEROMINIA_R.F_DATEDURATION < this.TimeEnd)
                balance.F_HMEROMINIA_R.F_DATEDURATION = this.TimeEnd;
            return balance;
        }
    }
}

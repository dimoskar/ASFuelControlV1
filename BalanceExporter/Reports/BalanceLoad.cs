using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ASFuelControl.Communication;

namespace BalanceExporter.Reports
{
    public class BalanceLoad
    {
        private BalanceDS model = new BalanceDS();

        public BalanceDS Model
        {
            get { return this.model; }
        }

        public void LoadBalance(Guid balanceId, string balanceData)
        {
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(BalanceClass));
            object result = null;
            using (TextReader reader = new StringReader(balanceData))
            {
                result = serializer.Deserialize(reader);
            }
            
            BalanceClass xmlBalance = result as BalanceClass;
            if (xmlBalance == null)
                return;

            BalanceDS.BalanceRow balance = model.Balance.NewBalanceRow();
            balance.BalanceId = balanceId;
            this.model.Balance.Rows.Add(balance);
            balance.BeginDateTime = xmlBalance.TimeStart;
            balance.EndDateTime = xmlBalance.TimeEnd;
            balance.BalanceDateTime = xmlBalance.Date;

            foreach (ReservoirClass tank in xmlBalance.Reservoirs.Reservoirs)
            {
                BalanceDS.TankDataRow tdata = model.TankData.NewTankDataRow();
                tdata.TankDataId = Guid.NewGuid();
                tdata.TankSerialNumber = tank.TankSerialNumber;
                tdata.TankNumber = tank.TankId;
                tdata.TankVolume = tank.Capacity;
                tdata.TemperatureEnd = tank.TemperatureEnd;
                tdata.TemperatureStart = tank.TemperatureStart;
                tdata.VolumeBegin = tank.VolumeStart;
                tdata.VolumeEnd = tank.VolumeEnd;
                
                tdata.VolumeNomalizedBegin = tank.VolumeStartNormalized;
                tdata.VolumeNomalizedEnd = tank.VolumeEndNormalized;

                BalanceDS.FuelTypeDataRow ftype = this.GetFuelType(balance, tank.FuelType);
                tdata.FuelTypeDescription = ftype.FuelTypeName + " (" + ftype.FuelTypeCode + ")";
                tdata.FuelTypeId = ftype.FuelTypeId;
                tdata.FuelTypeDataRow = ftype;

                model.TankData.Rows.Add(tdata);
            }

            foreach (FuelTypeClass fclass in xmlBalance.PumpsPerFuel.FuelTypes)
            {
                BalanceDS.FuelTypeDataRow ftype = this.GetFuelType(balance, fclass.FuelType);
                foreach (FuelTypePumpClass pump in fclass.FuelPumps)
                {
                    BalanceDS.DispenserDataRow pumpData = model.DispenserData.NewDispenserDataRow();
                    pumpData.DispenserDataId = Guid.NewGuid();
                    pumpData.DispenserNumber = int.Parse(pump.FuelPumpId);
                    pumpData.FuelTypeId = ftype.FuelTypeId;
                    
                    
                    pumpData.NozzleNumber = pump.NozzleId;
                    pumpData.TotalizerStart = pump.TotalizerStart;
                    pumpData.TotalizerEnd = pump.TotalizerEnd;
                    pumpData.TotalOut = pump.TotalSales + pump.TotalLiterCheck;
                    pumpData.TotalOutNormalized = pump.TotalSalesNormalized + pump.TotalLiterCheckNormalized;
                    pumpData.TotalLiterCheck = pump.TotalLiterCheck;
                    pumpData.TotalSales = pump.TotalSales;
                    pumpData.FuelTypeDescription = ftype.FuelTypeName + " (" + ftype.FuelTypeCode + ")";
                    pumpData.FuelTypeDataRow = ftype;

                    model.DispenserData.Rows.Add(pumpData);
                }
            }

            foreach (FuelTypeDivClass divergence in xmlBalance.Divergences.Divergences)
            {
                BalanceDS.FuelTypeDataRow ftype = this.GetFuelType(balance, divergence.FuelType);
                ftype.DiffVolume = divergence.Divergence;
                ftype.DiffVolumeNormalized = divergence.DivergenceNormalized;
                ftype.DiffPercentage = divergence.Percentage;
                ftype.DiffPercentafeNormelized = divergence.PercentageNormalized;
            }

            foreach (FuelMovementClass fm in xmlBalance.Movements.FuelMovements)
            {
                BalanceDS.TankFillingDataRow tfr = model.TankFillingData.NewTankFillingDataRow();
                BalanceDS.FuelTypeDataRow ftype = this.GetFuelType(balance, fm.FuelType);
                tfr.TankFillingId = Guid.NewGuid();
                tfr.Deliveries = fm.SumIn;
                tfr.Deliveries15 = fm.SumInNormalized;
                tfr.Additional = fm.SumAdditionalIn;
                tfr.Additional15 = fm.SumAdditionalInNormalized;
                tfr.Invoiced = fm.SumInInvoiced;
                tfr.Invoiced15 = fm.SumInInvoicedNormalized;
                
                tfr.BalanceId = balance.BalanceId;
                tfr.FuelTypeId = ftype.FuelTypeId;
                tfr.BalanceRow = balance;
                tfr.FuelTypeDataRow = ftype;
                tfr.FuelTypeName = ftype.FuelTypeName;
                tfr.FuelTypeCode = ftype.FuelTypeCode;
                model.TankFillingData.Rows.Add(tfr);
            }
        }

        private BalanceDS.FuelTypeDataRow GetFuelType(BalanceDS.BalanceRow balance, ASFuelControl.Communication.Enums.FuelTypeEnum ftype)
        {
            BalanceDS.FuelTypeDataRow fuelType = balance.GetFuelTypeDataRows().Where(ft => ft.FuelTypeCode == ((int)ftype).ToString()).FirstOrDefault();
            if(fuelType != null)
                return fuelType;
            fuelType = this.model.FuelTypeData.NewFuelTypeDataRow();
            fuelType.FuelTypeCode = ((int)ftype).ToString();
            fuelType.FuelTypeName = ftype.ToString();
            fuelType.FuelTypeId = Guid.NewGuid();
            this.model.FuelTypeData.Rows.Add(fuelType);
            fuelType.BalanceId = balance.BalanceId;

            return fuelType;
        }
    }
}

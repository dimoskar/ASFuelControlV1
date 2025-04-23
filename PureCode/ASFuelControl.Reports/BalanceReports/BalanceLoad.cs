using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ASFuelControl.Reports.BalanceReports
{
    public class BalanceLoad
    {
        private Reports.BalanceReports.BalanceDS model = new BalanceDS();

        public BalanceDS Model
        {
            get { return this.model; }
        }

        public void LoadBalance(Guid balanceId, string balanceData)
        {
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(Communication.BalanceClass));
            object result = null;
            using (TextReader reader = new StringReader(balanceData))
            {
                result = serializer.Deserialize(reader);
            }
            
            Communication.BalanceClass xmlBalance = result as Communication.BalanceClass;
            if (xmlBalance == null)
                return;

            Reports.BalanceReports.BalanceDS.BalanceRow balance = model.Balance.NewBalanceRow();
            balance.BalanceId = balanceId;
            this.model.Balance.Rows.Add(balance);
            balance.BeginDateTime = xmlBalance.TimeStart;
            balance.EndDateTime = xmlBalance.TimeEnd;
            balance.BalanceDateTime = xmlBalance.Date;
            
            foreach (Communication.ReservoirClass tank in xmlBalance.Reservoirs.Reservoirs)
            {
                Reports.BalanceReports.BalanceDS.TankDataRow tdata = model.TankData.NewTankDataRow();
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

                Reports.BalanceReports.BalanceDS.FuelTypeDataRow ftype = this.GetFuelType(balance, tank.FuelType);
                tdata.FuelTypeDescription = ftype.FuelTypeName + " (" + ftype.FuelTypeCode + ")";
                tdata.FuelTypeId = ftype.FuelTypeId;
                tdata.FuelTypeDataRow = ftype;

                model.TankData.Rows.Add(tdata);
            }

            foreach (Communication.FuelTypeClass fclass in xmlBalance.PumpsPerFuel.FuelTypes)
            {
                Reports.BalanceReports.BalanceDS.FuelTypeDataRow ftype = this.GetFuelType(balance, fclass.FuelType);
                foreach (Communication.FuelTypePumpClass pump in fclass.FuelPumps)
                {
                    Reports.BalanceReports.BalanceDS.DispenserDataRow pumpData = model.DispenserData.NewDispenserDataRow();
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

            foreach (Communication.FuelTypeDivClass divergence in xmlBalance.Divergences.Divergences)
            {
                Reports.BalanceReports.BalanceDS.FuelTypeDataRow ftype = this.GetFuelType(balance, divergence.FuelType);
                ftype.DiffVolume = divergence.Divergence;
                ftype.DiffVolumeNormalized = divergence.DivergenceNormalized;
                ftype.DiffPercentage = divergence.Percentage;
                ftype.DiffPercentafeNormelized = divergence.PercentageNormalized;
            }

            foreach (Communication.FuelMovementClass fm in xmlBalance.Movements.FuelMovements)
            {
                Reports.BalanceReports.BalanceDS.TankFillingDataRow tfr = model.TankFillingData.NewTankFillingDataRow();
                Reports.BalanceReports.BalanceDS.FuelTypeDataRow ftype = this.GetFuelType(balance, fm.FuelType);
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

        private Reports.BalanceReports.BalanceDS.FuelTypeDataRow GetFuelType(Reports.BalanceReports.BalanceDS.BalanceRow balance, Communication.Enums.FuelTypeEnum ftype)
        {
            Reports.BalanceReports.BalanceDS.FuelTypeDataRow fuelType = balance.GetFuelTypeDataRows().Where(ft => ft.FuelTypeCode == ((int)ftype).ToString()).FirstOrDefault();
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

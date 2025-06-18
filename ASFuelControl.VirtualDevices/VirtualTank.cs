using ASFuelControl.Common.Enumerators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Markup;

namespace ASFuelControl.VirtualDevices
{
    public class VirtualTank : VirtualDevice
    {
        #region Private Variables

        private Common.TankValues tankValues;
        private Common.Enumerators.TankStatusEnum tankStatus = Common.Enumerators.TankStatusEnum.Offline;
        private Common.Enumerators.TankStatusEnum physicalStatus = Common.Enumerators.TankStatusEnum.Offline;
        private decimal currentFuelLevel = 0;
        private decimal currentWaterLevel = 0;
        private decimal currentTemperature = 0;
        private bool initializeFilling = false;
        private bool fillingFinished = false;
        private bool initializeExtraction = false;
        private bool extractionFinished = false;

        #endregion

        #region public properties

        public Guid InvoiceLineId { set; get; }

        public Guid InvoiceTypeId { set; get; }
        public Guid VehicleId { set; get; }
        public Guid FillingFuelTypeId { set; get; }

        public decimal LastCalculatedStart { set; get; }

        public List<Common.Enumerators.TankStatusEnum> AlertStatuses { set; get; }
        public VirtualTitrimetryLevel[] TitrimetryLevels { set; get; }
        public VirtualNozzle[] ConnectedNozzles { set; get; }
        public int TankNumber { set; get; }
        public Guid TankId { set; get; }
        public int ChannelId { set; get; }
        public int AddressId { set; get; }
        public decimal ThermalCoefficient { set; get; }
        public decimal BaseDensity { set; get; }
        public decimal OrderLimit { set; get; }
        public int BaseColor { set; get; }
        public int DeliveryTime { set; get; }
        public int LiterCheckTime { set; get; }
        public DateTime WaitingStarted { set; get; }
        public DateTime WaitingShouldEnd { set; get; }
        public bool IsLiterCheck { set; get; }
        public bool IsVirtualTank { set; get; }
        public DateTime LastSaleEnded { set; get; }
        public decimal AlarmThreshold { set; get; }
        public DateTime DeliveryStarted { set; get; }
        public decimal TankAlertMargin { set; get; }
        public decimal FillingStartTankLevel { set; get; }

        public bool FillingByError
        {
            set;get;
        }

        public bool ExtractingByError
        {
            set; get;
        }

        public bool InitializeFilling 
        {
            set 
            {
                if (value == this.initializeFilling)
                    return;
                this.initializeFilling = value;
                this.OnPropertyChanged(this, "InitializeFilling");
            }
            get { return this.initializeFilling; }
        }

        public bool InitializeExtraction
        {
            set
            {
                if (value == this.initializeExtraction)
                    return;
                this.initializeExtraction = value;
                this.OnPropertyChanged(this, "InitializeExtraction");
            }
            get { return this.initializeExtraction; }
        }

        public bool FillingFinished
        {
            set
            {
                if (value == this.fillingFinished)
                    return;
                this.fillingFinished = value;
                this.OnPropertyChanged(this, "FillingFinished");
            }
            get { return this.fillingFinished; }
        }

        public bool ExtractionFinished
        {
            set
            {
                if (value == this.extractionFinished)
                    return;
                this.extractionFinished = value;
                this.OnPropertyChanged(this, "ExtractionFinished");
            }
            get { return this.extractionFinished; }
        }

        public Common.Enumerators.TankStatusEnum PhysicalStatus
        {
            set
            {
                if (this.physicalStatus == value)
                    return;
                this.PreviousPhysicalStatus = this.physicalStatus;
                this.physicalStatus = value;
                this.OnPropertyChanged(this, "WorkFlowStatus");
            }
            get { return this.physicalStatus; }
        }
        public Common.Enumerators.TankStatusEnum PreviousPhysicalStatus
        {
            set;
            get;
        }

        public Common.Enumerators.TankStatusEnum TankStatus
        {
            set
            {
                if (this.tankStatus == value)
                    return;
                this.PreviousStatus = this.tankStatus;
                this.tankStatus = value;
                if(value == Common.Enumerators.TankStatusEnum.Idle)
                    this.LastCalculatedStart = this.CurrentFuelLevel;
                if (System.IO.File.Exists("TankStatus.log"))
                {
                    System.IO.File.AppendAllText("TankStatus.log", string.Format("{0:dd/MM/yyyy HH:mm:ss.fff}, TANKK: {1}, {2}->{3}\r\n", DateTime.Now, this.TankNumber, this.PreviousStatus, this.tankStatus));
                }
                this.OnPropertyChanged(this, "TankStatus");
            }
            get { return this.tankStatus; }
        }

        public Common.Enumerators.TankStatusEnum ErrorStatus
        {
            set;
            get;
        }

        public Common.Enumerators.TankStatusEnum PreviousStatus
        {
            set;
            get;
        }

        public decimal CurrentTemperature
        {
            set
            {
                if (this.currentTemperature == value)
                    return;
                this.currentTemperature = value;
                this.OnPropertyChanged(this, "CurrentTemperature");
                this.OnPropertyChanged(this, "CurrentVolumeNormalized");
            }
            get { return this.currentTemperature; }
        }
        public decimal CurrentFuelLevel
        {
            set
            {
                if (this.currentFuelLevel == value)
                    return;
                this.currentFuelLevel = value;
                if (this.currentFuelLevel > 0)
                    this.LasValidLevel = value;
                if (this.TankStatus == Common.Enumerators.TankStatusEnum.Idle)
                    this.LastCalculatedStart = this.CurrentFuelLevel;
                //this.CurrentVolumeNormalized = this.NormalizeVolume(this.GetTankVolume(this.currentFuelLevel), this.currentTemperature, this.CurrentDensity);
                this.OnPropertyChanged(this, "CurrentFuelLevel");
                this.OnPropertyChanged(this, "CurrentVolume");
                this.OnPropertyChanged(this, "CurrentVolumeNormalized");
                this.OnPropertyChanged(this, "CurrentFuelPercentage");
                this.OnPropertyChanged(this, "OrderPending");
            }
            get { return this.currentFuelLevel; }
        }
        public decimal CurrentWaterLevel
        {
            set
            {
                if (this.currentWaterLevel == value)
                    return;
                this.currentWaterLevel = value;
                this.OnPropertyChanged(this, "CurrentWaterLevel");
                this.OnPropertyChanged(this, "CurrentWaterVolume");
                this.OnPropertyChanged(this, "CurrentWaterPercentage");
            }
            get { return this.currentWaterLevel; }
        }

        public decimal LasValidLevel { set; get; }

        public DateTime LastSaleEnd { set; get; }

        public decimal FuelOffset { set; get; }
        public decimal WaterOffset { set; get; }

        public decimal CurrentDensity { set; get; }
        public decimal CurrentVolume
        {
            get
            {
                return this.GetTankVolume(this.CurrentFuelLevel);
            }
        }
        public decimal CurrentVolumeNormalized
        {
            get
            {
                return this.NormalizeVolume(this.CurrentVolume, this.currentTemperature, this.CurrentDensity);
            }
        }

        public decimal LastTemperature { set; get; }
        public decimal LastFuelHeight { set; get; }
        public decimal LastWaterHeight { set; get; }

        public decimal TotalVolume { set; get; }
        public decimal MaxHeight { set; get; }
        public decimal MinHeight { set; get; }
        public decimal MinAllowedHeight { set; get; }
        public decimal MaxWaterHeight { set; get; }
        public decimal MinWaterHeight { set; get; }
        public decimal PriceAverage { set; get; }

        public decimal GetTankAlertThreshold()
        {
            VirtualTitrimetryLevel level = this.TitrimetryLevels.Where(l => l.Height > this.CurrentFuelLevel).OrderBy(l => l.Height).FirstOrDefault();
            if (level == null)
                return this.TankAlertMargin;
            if (level.UncertaintyVolume < this.TankAlertMargin)
                return this.TankAlertMargin;
            return level.UncertaintyVolume;
        }

        public decimal LastSalesVolumeDifference { set; get; }

        public Common.TankValues TankValues
        {
            set
            {
                this.tankValues = value;
                if (value != null)
                    AddMonitorData(this.tankValues);
                this.OnPropertyChanged(this, "TankValues");
            }
            get { return this.tankValues; }
        }

        public bool OrderPending
        {
            get
            {
                if (this.OrderLimit > this.CurrentVolume && !this.IsVirtualTank)
                    return true;
                return false;
            }
        }

        public string FuelTypeDescription { set; get; }
        public string FuelTypeShort { set; get; }
        public string SerialNumber { set; get; }
        public Guid FuelTypeId { set; get; }
        public bool TankDeliverySensed { set; get; }

        public decimal FuelLevelFlow { set; get; }
        public decimal WaterLevelFlow { set; get; }
        public decimal FuelVolumeFlow { set; get; }
        public decimal WaterVolumeFlow { set; get; }
        #endregion

        #region public Methods

        public VirtualTank()
        {
            this.ConnectedNozzles = new VirtualNozzle[] { };
            this.LastSaleEnded = DateTime.Now.AddSeconds(-10);
            this.LastValuesUpdate = DateTime.Now;
            this.AlertStatuses = new List<Common.Enumerators.TankStatusEnum>();
        }

        public decimal GetTankVolume(decimal height)
        {
            if (this.TitrimetryLevels == null)
                return 0;
            if (height <= 0)
                return 0;
            VirtualTitrimetryLevel previousLevel = this.TitrimetryLevels.OrderBy(tml => tml.Height).Where(tml => tml.Height <= height).LastOrDefault();
            VirtualTitrimetryLevel nextLevel = this.TitrimetryLevels.OrderBy(tml => tml.Height).Where(tml => tml.Height > height).FirstOrDefault();
            if (nextLevel != null)
            {
                decimal volDiff = nextLevel.Volume - (previousLevel == null ? 0 : previousLevel.Volume);
                decimal heightStep = nextLevel.Height - (previousLevel == null ? 0 : previousLevel.Height);
                decimal dHeight = height - (previousLevel == null ? 0 : previousLevel.Height);
                decimal dVolume = 0;
                if(heightStep != 0)
                    dVolume = volDiff * dHeight / heightStep;
                return (previousLevel == null ? 0 : previousLevel.Volume) + dVolume;
            }
            else
            {
                return (previousLevel == null ? 0 : previousLevel.Volume);
            }
        }

        public decimal NormalizeVolume(decimal volStart, decimal temp, decimal density)
        {
            if (this.ThermalCoefficient == 0 || this.BaseDensity == 0)
                return volStart;

            decimal dt = 15 - temp;
            decimal coefficient = density * (this.ThermalCoefficient / this.BaseDensity);
            decimal dv = volStart * coefficient * dt;
            return volStart + dv;
        }

        public decimal CalculateStatisticalErrors(decimal level)
        {
            try
            {
                decimal error = 0;
                VirtualTitrimetryLevel l1 = this.TitrimetryLevels.OrderBy(t => t.Height).Where(t => t.Height <= level).LastOrDefault();
                VirtualTitrimetryLevel l2 = this.TitrimetryLevels.OrderBy(t => t.Height).Where(t => t.Height >= level).FirstOrDefault();
                if (l1 == null || l2 == null)
                    return 0;

                if (l2.Equals(l1))
                    l2 = this.TitrimetryLevels.OrderBy(t => t.Height).Where(t => t.Height >= level).Skip(1).FirstOrDefault();

                //error = 3 * (l2.Height - l1.Height);
                error = this.AlarmThreshold * (l2.Volume - l1.Volume);
                return error;
            }
            catch
            {
                return 0;
            }
        }

        public decimal GetVolumeStep(decimal level)
        {
            if(this.TitrimetryLevels.Length <=1)
                return 0;
            VirtualTitrimetryLevel curLevel = this.TitrimetryLevels.Where(t => t.Height >= level).OrderBy(t => t.Height).FirstOrDefault();
            if(curLevel == null)
                curLevel = this.TitrimetryLevels.Where(t => t.Height <= level).OrderBy(t => t.Height).LastOrDefault();
            if (curLevel == null)
                return 0;
            int index = this.TitrimetryLevels.OrderBy(t=>t.Height).ToList().IndexOf(curLevel);
            
            int newIndex = -1;
            if(index> 0)
                newIndex = index - 1;
            else
                newIndex = index + 1;

            VirtualTitrimetryLevel nextLevel = this.TitrimetryLevels.OrderBy(t => t.Height).ElementAt(newIndex);
            return Math.Abs(curLevel.Volume - nextLevel.Volume);
        }

        public decimal GetHeightDiffForVolume(decimal level, decimal volume)
        {
            //volume = volume * 100;
            if (this.TitrimetryLevels == null)
                return 0;
            if (this.TitrimetryLevels.Length <= 1)
                return 0;
            VirtualTitrimetryLevel curLevel = this.TitrimetryLevels.Where(t => t.Height >= level).OrderBy(t => t.Height).FirstOrDefault();
            if (curLevel == null)
                curLevel = this.TitrimetryLevels.Where(t => t.Height <= level).OrderBy(t => t.Height).LastOrDefault();
            if (curLevel == null)
                return 0;

            List<VirtualTitrimetryLevel> levels = this.TitrimetryLevels.OrderBy(t => t.Height).ToList();

            int index = levels.IndexOf(curLevel);
            if (index == 0)
                return 0;
            VirtualTitrimetryLevel nextLevel = levels[index - 1];
            while (curLevel.Volume - nextLevel.Volume < volume)
            {
                index = index - 1;
                if (index == 0)
                    return 0;
                nextLevel = levels[index - 1];
            }

            return nextLevel.Height;
        }

        #endregion

        List<TankMonitorData> monitorData = new List<TankMonitorData>();

        private void AddMonitorData(Common.TankValues values)
        {
            
            var statuses = new List<TankStatusEnum>();
            statuses.Add(TankStatusEnum.Idle);
            statuses.Add(TankStatusEnum.WaitingEllapsed);
            var md = new TankMonitorData()
            {
                FuelLevel = values.FuelHeight,
                WaterLevel = values.WaterHeight,
                Temperatur = values.CurrentTemperatur,
                TimeStamp = DateTime.Now
            };
            if (monitorData.Count < 2)
            {
                monitorData.Add(md);

            }
            else
            {
                var m1 = monitorData.First();
                var m2 = monitorData.Last();
                if (m2.TimeStamp.Subtract(m1.TimeStamp).TotalSeconds > 60)
                {
                    monitorData.Remove(m1);
                    monitorData.Add(md);
                    CalculateFlowMmPerSecond();
                    CalculateFlowVolumePerSecond();
                }
            }
        }

        public TankMonitorData[] GetMonitorData()
        {
            return monitorData.ToArray();
        }
        private void CalculateFlowMmPerSecond()
        {
            if (monitorData.Count < 2)
                return;

            // Use the oldest and newest entries for calculation
            var first = monitorData.First();
            var last = monitorData.Last();

            double seconds = (last.TimeStamp - first.TimeStamp).TotalSeconds;
            if (seconds <= 0)
                return;

            // Linear regression for FuelLevel and WaterLevel over time
            int n = monitorData.Count;
            double sumT = 0, sumT2 = 0;
            double sumFuel = 0, sumTFuel = 0;
            double sumWater = 0, sumTWater = 0;

            DateTime t0 = monitorData[0].TimeStamp;
            for (int i = 0; i < n; i++)
            {
                double t = (monitorData[i].TimeStamp - t0).TotalSeconds;
                double fuel = (double)monitorData[i].FuelLevel;
                double water = (double)monitorData[i].WaterLevel;

                sumT += t;
                sumT2 += t * t;
                sumFuel += fuel;
                sumTFuel += t * fuel;
                sumWater += water;
                sumTWater += t * water;
            }

            double denom = n * sumT2 - sumT * sumT;
            decimal fuelFlow = 0;
            decimal waterFlow = 0;
            if (denom != 0)
            {
                double fuelSlope = (n * sumTFuel - sumT * sumFuel) / denom;
                double waterSlope = (n * sumTWater - sumT * sumWater) / denom;
                fuelFlow = Math.Round((decimal)fuelSlope, 2);
                waterFlow = Math.Round((decimal)waterSlope, 2);
            }

            this.FuelLevelFlow = fuelFlow;
            this.WaterLevelFlow = waterFlow;
        }
        private void CalculateFlowVolumePerSecond()
        {
            if (monitorData.Count < 2 || this.TitrimetryLevels == null || this.TitrimetryLevels.Length == 0)
                return;

            int n = monitorData.Count;
            double sumT = 0, sumT2 = 0;
            double sumFuel = 0, sumTFuel = 0;
            double sumWater = 0, sumTWater = 0;

            DateTime t0 = monitorData[0].TimeStamp;
            for (int i = 0; i < n; i++)
            {
                double t = (monitorData[i].TimeStamp - t0).TotalSeconds;
                double fuel = (double)monitorData[i].FuelLevel;
                double water = (double)monitorData[i].WaterLevel;

                sumT += t;
                sumT2 += t * t;
                sumFuel += fuel;
                sumTFuel += t * fuel;
                sumWater += water;
                sumTWater += t * water;
            }

            double denom = n * sumT2 - sumT * sumT;
            decimal fuelVolumeFlow = 0;
            decimal waterVolumeFlow = 0;
            if (denom != 0)
            {
                double fuelSlope = (n * sumTFuel - sumT * sumFuel) / denom;
                double waterSlope = (n * sumTWater - sumT * sumWater) / denom;

                // Convert mm/sec to volume/sec using tank calibration
                // Use average height for conversion
                double avgFuelHeight = sumFuel / n;
                double avgWaterHeight = sumWater / n;

                decimal fuelVolumeStep = this.GetVolumeStep((decimal)avgFuelHeight);
                decimal waterVolumeStep = this.GetVolumeStep((decimal)avgWaterHeight);

                // If step is zero, fallback to 1:1 (should not happen in normal operation)
                if (fuelVolumeStep == 0) fuelVolumeStep = 1;
                if (waterVolumeStep == 0) waterVolumeStep = 1;

                // Calculate flow in volume/sec
                fuelVolumeFlow = Math.Round((decimal)fuelSlope * fuelVolumeStep, 4);
                waterVolumeFlow = Math.Round((decimal)waterSlope * waterVolumeStep, 4);
            }

            this.FuelVolumeFlow = fuelVolumeFlow;
            this.WaterVolumeFlow = waterVolumeFlow;
        }
    }
    public class TankMonitorData
    {
        public DateTime TimeStamp { set; get; }
        public decimal FuelLevel { set; get; }
        public decimal WaterLevel { set; get; }
        public decimal Temperatur { set; get; }
    }
}
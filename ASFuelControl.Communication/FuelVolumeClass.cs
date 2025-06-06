using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Communication
{
    [Serializable]
    public class FuelVolumeClass
    {
        public Enums.FuelTypeEnum FuelType { set; get; }
        public decimal VolumeMeasured { set; get; }
        public decimal VolumeNormalized { set; get; }
        public decimal FuelTemperature { set; get; }
        public decimal Difference { set; get; }
        public decimal Density { set; get; }
    }

    [Serializable]
    public class FuelVolumeSumsClass
    {
        public decimal VolumeMeasured { set; get; }
        public decimal VolumeNormalized { set; get; }
        public decimal Difference { set; get; }
    }

    [Serializable]
    public class TankCheckClass
    {
        decimal tankLevel = 0;

        public string TabkNumber { set; get; }
        public decimal TankLevel 
        {
            set { this.tankLevel = value; }
            get { return decimal.Round(this.tankLevel, 2); } 
        }
        public decimal TankVolume { set; get; }
        public decimal TankVolume15 { set; get; }
        public decimal TankTemperature { set; get; }
        public decimal FuelDensity { set; get; }
        public string TankId { set; get; }
        public DateTime TransactionDate { set; get; }
    }

    [Serializable]
    public class RecieptClass
    {
        public string SubmitterData { set; get; }
        public string FuelPumpLabel { set; get; }
        public string TitrimetryLabel { set; get; }
        public Enums.FuelTypeEnum FuelType { set; get; }
        public decimal FuelVolume { set; get; }
        public decimal UnitPrice { set; get; }
        public decimal VAT { set; get; }
        public decimal TotalAmount { set; get; }
        public string PlateNumbers { set; get; }
        public string CustomerTin { set; get; }
        public string Notes { set; get; }
        public string TankId { set; get; }
        public decimal TankTemperature { set; get; }
        public int TankNumber { set; get; } 
    }

    public class ClientHeader
    {
        public string CompanyTIN 
        {
            get { return CommunicationMethods.CompanyTin; }
        }
        public string SubmitterTIN { set; get; }
        public string EToken { set; get; }
        public int TaxisBranch { set; get; }
        public DateTime SubmissionDate { set; get; }


        public ClientHeader()
        {
            this.TaxisBranch = CommunicationMethods.TaxisBranch;
            this.SubmitterTIN = CommunicationMethods.SubmitterTin;
            this.SubmissionDate = DateTime.Now;
        }
    }
}

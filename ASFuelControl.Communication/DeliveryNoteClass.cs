using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Communication
{
    [Serializable]
    public class DeliveryNoteFuelDataClass
    {
        public Enums.FuelTypeEnum FuelType { set; get; }
        public decimal InvoicedVolume { set; get; }
        public decimal TemperatureLoaded { set; get; }
        public decimal InvoicedVolumeNormalized { set; get; }
        public decimal Density { set; get; }
        public decimal TotalVolume { set; get; }
        public decimal TotalVolumeNormalized { set; get; }
        public decimal Diff { set; get; }

        public FuelFlowService.Fuelflows_TypeDeliveryNoteFuelData GetElement()
        {
            FuelFlowService.Fuelflows_TypeDeliveryNoteFuelData dnfd = new FuelFlowService.Fuelflows_TypeDeliveryNoteFuelData();
            dnfd.FUELTYPE = new FuelFlowService.Fuel_Type();
            dnfd.FUELTYPE.Code = (int)this.FuelType;
            dnfd.FUELTYPE.Description = Enums.LocalizedEnumExtensions.GetLocalizedName(this.FuelType);
            dnfd.F_2154 = this.InvoicedVolume == 0 ? (decimal)0.01 : this.InvoicedVolume;
            dnfd.F_2155 = this.TemperatureLoaded;
            dnfd.F_2156 = this.InvoicedVolumeNormalized == 0 ? (decimal)0.01 : this.InvoicedVolumeNormalized;
            dnfd.F_2157 = this.Density;
            dnfd.F_217 = this.TotalVolume;
            dnfd.F_218 = this.TotalVolumeNormalized;
            dnfd.F_219 = this.Diff;

            return dnfd;
        }
    }

    [Serializable]
    public class DeliveryNoteDocumentClass
    {
        public string SupplyerTin { set; get; }
        public Enums.DeliveryTypeEnum DeliveryType { set; get; }
        public string Amdika { set; get; }
        public int DocumentType { set; get; }
        public int InvoiceNumber { set; get; }
        public string InvoiceSeries { set; get; }
        public DateTime DocumentDate { set; get; }
        public string PlateNumber { set; get; }

        public FuelFlowService.Fuelflows_TypeDeliveryNoteDocument GetElement()
        {
            FuelFlowService.Fuelflows_TypeDeliveryNoteDocument dfd = new FuelFlowService.Fuelflows_TypeDeliveryNoteDocument();
            dfd.F_2151 = this.SupplyerTin;
            dfd.F_2151A = this.Amdika;
            dfd.F_2152A = this.DocumentType;
            dfd.F_2152B = this.InvoiceNumber;
            dfd.F_2152C = this.InvoiceSeries == null ? "" : this.InvoiceSeries;
            dfd.F_2152D = this.DocumentDate;
            dfd.F_2153 = this.PlateNumber;
            dfd.F_DELIVERY_TYPE = (int)this.DeliveryType;
            
            return dfd;
        }
    }

    [Serializable]
    public class DeliveryNoteReservoir
    {
        public string TankId { set; get; }
        public Enums.FuelTypeEnum FuelType { set; get; }
        public decimal LevelBefore { set; get; }
        public decimal VolumeBefore { set; get; }
        public decimal TemperatureBefore { set; get; }
        public decimal VolumeBeforeNormalized { set; get; }
        public DateTime DateStart { set; get; }
        public DateTime TimeStart { set; get; }
        public decimal LevelAfter { set; get; }
        public decimal VolumeAfter { set; get; }
        public decimal TemperatureAfter { set; get; }
        public decimal VolumeAfterNormalized { set; get; }
        public DateTime DateEnd { set; get; }
        public DateTime TimeEnd { set; get; }


        public FuelFlowService.Fuelflows_TypeDeliveryNoteReservoirsReservoir GetElement()
        {
            FuelFlowService.Fuelflows_TypeDeliveryNoteReservoirsReservoir res = new FuelFlowService.Fuelflows_TypeDeliveryNoteReservoirsReservoir();

            res.DataBefore = new FuelFlowService.Fuelflows_TypeDeliveryNoteReservoirsReservoirDataBefore();
            res.DataBefore.F_2141 = this.LevelBefore;
            res.DataBefore.F_2142A = this.VolumeBefore;
            res.DataBefore.F_2142B = this.TemperatureBefore;
            res.DataBefore.F_2143 = this.VolumeBeforeNormalized;
            res.DataBefore.F_2144 = this.DateStart;
            res.DataBefore.F_2145 = this.TimeStart;

            res.DataAfter = new FuelFlowService.Fuelflows_TypeDeliveryNoteReservoirsReservoirDataAfter();
            res.DataAfter.F_2161 = this.LevelAfter;
            res.DataAfter.F_2162A = this.VolumeAfter;
            res.DataAfter.F_2162B = this.TemperatureAfter;
            res.DataAfter.F_2163 = this.VolumeAfterNormalized;
            res.DataAfter.F_2164 = this.DateEnd;
            res.DataAfter.F_2165 = this.TimeEnd;

            res.F_211 = this.TankId;
            res.F_212 = new FuelFlowService.Fuel_Type();
            res.F_212.Code = (int)this.FuelType;
            res.F_212.Description = Enums.LocalizedEnumExtensions.GetLocalizedName(this.FuelType);
            res.F_213 = DateTime.Now;

            return res;
        }
    }

    [Serializable]
    public class DeliveryNoteClass
    {
        public DeliveryNoteDocumentClass Document { set; get; }
        public List<DeliveryNoteFuelDataClass> FuelData { set; get; }
        public List<DeliveryNoteReservoir> Reservoirs { set; get; }

        public FuelFlowService.Fuelflows_TypeDeliveryNote GetElement()
        {
            FuelFlowService.Fuelflows_TypeDeliveryNote dn = new FuelFlowService.Fuelflows_TypeDeliveryNote();
            dn.Header = new FuelFlowService.Header_Type();
            dn.Header.CompanyTIN = Communication.CommunicationMethods.CompanyTin;
            dn.Header.SubmitterTIN = Communication.CommunicationMethods.SubmitterTin;
            dn.Header.SubmissionDate = DateTime.Now;

            dn.Document = this.Document.GetElement();
            dn.Reservoirs = new FuelFlowService.Fuelflows_TypeDeliveryNoteReservoirs();
            dn.Reservoirs.Reservoir = this.Reservoirs.Select(fmc => fmc.GetElement()).ToArray();
            dn.Reservoirs.ReservoirsNumber = this.Reservoirs.Count;
            dn.Header = new FuelFlowService.Header_Type();
            
            dn.FuelData = this.FuelData.Select(fmc => fmc.GetElement()).ToArray();
            dn.F_AR_EIDOS_KAYSIMOU = this.FuelData.Count;
            
            return dn;
        }
    }
}

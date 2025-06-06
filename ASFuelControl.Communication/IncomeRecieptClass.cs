using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Communication
{
    [Serializable]
    public class IncomeRecieptClass
    {
        public string SubmitterDetails { set; get; }
        public string Amdika { set; get; }
        public string PumpSerialNumber { set; get; }
        public string NozzleId { set; get; }
        public Enums.FuelTypeEnum FuelType { set; get; }
        public DateTime PublishDateTime { set; get; }
        public decimal Volume { set; get; }
        public decimal Volume15 { set; get; }
        public decimal FuelDensity { set; get; }
        public decimal UnitPrice { set; get; }
        public decimal VatPercentage { set; get; }
        public decimal TotalValue { set; get; }
        public string PlateNumber { set; get; }
        public string CustomerTin { set; get; }
        public string Notes { set; get; }
        public string TankSerialNumber { set; get; }
        public decimal TankTemperature { set; get; }
        public decimal Totalizer { set; get; }
        public int TankId { set; get; }

        public FuelFlowService.ArrayOfFuelflows_TypeIncomeReceiptsIncomeReceiptIncomeReceipt GetElement()
        {
            FuelFlowService.ArrayOfFuelflows_TypeIncomeReceiptsIncomeReceiptIncomeReceipt inc = new FuelFlowService.ArrayOfFuelflows_TypeIncomeReceiptsIncomeReceiptIncomeReceipt();
            inc.F_31A = this.SubmitterDetails == null ? "" : this.SubmitterDetails;
            inc.F_31AA = this.Amdika;
            inc.F_31B = this.PumpSerialNumber == null ? "" : this.PumpSerialNumber;
            inc.F_31C = this.NozzleId;
            inc.F_31D = new FuelFlowService.Fuel_Type();
            inc.F_31D.Code = (int)this.FuelType;
            inc.F_31D.Description = Enums.LocalizedEnumExtensions.GetLocalizedName(this.FuelType);
            inc.F_31E = this.PublishDateTime;
            inc.F_31F = this.Volume;
            inc.F_31G = this.UnitPrice;
            inc.F_31H = this.VatPercentage;
            inc.F_31I = this.TotalValue;
            inc.F_31K = this.Volume15;
            inc.F_31L = this.TankTemperature;
            inc.F_31M = this.FuelDensity;
            inc.F_32 = new FuelFlowService.CarData_Type();
            inc.F_32.plateLetters = "";
            inc.F_32.plateNumber = "";
            if (this.PlateNumber != null && this.PlateNumber.Length > 0)
            {
                string plate1 = "";
                string plate2 = "";
                for (int i = 0; i < this.PlateNumber.Length; i++)
                {
                    if (char.IsDigit(this.PlateNumber[i]))
                        plate1 = plate1 + this.PlateNumber[i];
                    else
                        plate2 = plate2 + this.PlateNumber[i];
                }
                inc.F_32.plateLetters = plate2;
                inc.F_32.plateNumber = plate1;
                if (inc.F_32.plateNumber == "")
                    inc.F_32.plateLetters = "";
            }
            inc.F_33 = this.CustomerTin == null ? "" : this.CustomerTin;
            inc.F_34 = this.Notes == null ? "" : this.Notes;
            inc.F_35 = this.TankSerialNumber;
            inc.F_36 = this.TankTemperature;
            inc.F_37 = this.TankId;
            inc.F_38 = this.Totalizer;

            return inc;
        }
    }

    public class IncomeRecieptsClass
    {
        public List<IncomeRecieptClass> Reciepts { set; get; }

        public FuelFlowService.SendReceiptDS GetElement()
        {
            FuelFlowService.SendReceiptDS inc = new FuelFlowService.SendReceiptDS();
            inc.IncomeReceipt = new FuelFlowService.ArrayOfFuelflows_TypeIncomeReceiptsIncomeReceiptIncomeReceipt[][] { this.Reciepts.Select(fmc => fmc.GetElement()).ToArray() };
            inc.NUM_OF_RECEIPTS = this.Reciepts.Count;
            
            return inc;
        }
    }
}

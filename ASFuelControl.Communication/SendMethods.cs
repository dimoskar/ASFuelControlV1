using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ASFuelControl.Communication
{
    public class SendMethods
    {
        public bool Simulation { set; get; }

        eTokenLib.eTokenLib etoken = new eTokenLib.eTokenLib();

        public string SendTankCheck(ClientHeader header, TankCheckClass tank)
        {
            FuelFlowService.SendTankCheckDS tankCheck = new FuelFlowService.SendTankCheckDS();
            tankCheck.F_3001 = tank.TransactionDate;
            tankCheck.F_3002 = tank.TankLevel;
            tankCheck.F_3003 = tank.TankVolume;
            tankCheck.F_3004 = tank.TankTemperature;
            tankCheck.F_3005 = tank.FuelDensity;
            tankCheck.F_3006 = tank.TankVolume15;
            tankCheck.F_AM_DIKA = etoken.AMDIKA_ID;
            tankCheck.F_LABEL = tank.TankId; //A.M Dejamenhs
            tankCheck.Header = new FuelFlowService.Header_Type();
            tankCheck.Header.CompanyTIN = header.CompanyTIN;
            tankCheck.Header.SubmissionDate = header.SubmissionDate;
            tankCheck.Header.SubmitterTIN = header.SubmitterTIN;
            tankCheck.Header.F_TAXISBRANCH = header.TaxisBranch;

            try
            {
                string returnStr = "";
                if (!Simulation)
                {
                    ASFuelControl.Communication.FuelFlowService.PykServicesClient client = new FuelFlowService.PykServicesClient();
                    client.Open();
                    tankCheck.Header.eToken = etoken.GetOTP();
                    string ret = client.SendTankCheck(tankCheck);
                    if (ret.StartsWith("ERROR|"))
                        ret = "[ERROR]" + ret;
                    client.Close();
                    returnStr = ret;
                }
                return returnStr + "\r\n" + this.SerializeObject(tankCheck);
            }
            catch
            {
                return "[ERROR]";
            }
        }

        public string SendAlert(ClientHeader header, AlertClass alertClass)
        {
            
            FuelFlowService.SendAlertDS alert = new FuelFlowService.SendAlertDS();
            alert.Header = new FuelFlowService.Header_Type();
            alert.Header.CompanyTIN = header.CompanyTIN;
            alert.Header.SubmissionDate = header.SubmissionDate;
            alert.Header.SubmitterTIN = header.SubmitterTIN;
            alert.Header.F_TAXISBRANCH = header.TaxisBranch;
            alert.F_AM_DIKA = etoken.AMDIKA_ID;
            alert.F_ALERT = new FuelFlowService.Alert_Type();
            alert.F_ALERT.F_ALERTID = (int)alertClass.Alert;
            alert.F_ALERT.F_DATE = alertClass.AlertTime;
            alert.F_ALERT.F_DEVICE_LABEL = alertClass.DeviceId;           
            string reason = alertClass.Description;
            if (reason.Length > 99)
                reason = reason.Substring(0, 99);
            alert.F_ALERT.F_REASONING = reason;

            try
            {
                string returnStr = "";
                if (!Simulation)
                {
                    ASFuelControl.Communication.FuelFlowService.PykServicesClient client = new FuelFlowService.PykServicesClient();
                    client.Open();
                    alert.Header.eToken = etoken.GetOTP();
                    string ret = client.SendAlert(alert);
                    if (ret.StartsWith("ERROR|"))
                        ret = "[ERROR]" + ret;
                    client.Close();
                    returnStr = ret;
                }
                return returnStr + "\r\n" + this.SerializeObject(alert);
            }
            catch(Exception ex)
            {
                return "[ERROR]" + ex.Message;
            }
        }

        public string SendChangePrice(ClientHeader header, ChangePriceClass changeClass)
        {
            FuelFlowService.PriceChangeDS change = new FuelFlowService.PriceChangeDS();
            FuelFlowService.Fuel_Type ft = new FuelFlowService.Fuel_Type();
            ft.Code = (int)changeClass.FuelType;
            ft.Description = Enums.LocalizedEnumExtensions.GetLocalizedName(changeClass.FuelType);
            change.F_4001 = changeClass.ChangeTime;
            change.F_4002 = ft;
            change.F_4003 = changeClass.Price;
            change.F_AM_DIKA = etoken.AMDIKA_ID;
            change.Header = new FuelFlowService.Header_Type();
            change.Header.CompanyTIN = header.CompanyTIN;
            change.Header.SubmissionDate = header.SubmissionDate;
            change.Header.SubmitterTIN = header.SubmitterTIN;
            change.Header.F_TAXISBRANCH = header.TaxisBranch;
            try
            {
                string returnStr = "";
                if (!Simulation)
                {
                    ASFuelControl.Communication.FuelFlowService.PykServicesClient client = new FuelFlowService.PykServicesClient();
                    client.Open();
                    change.Header.eToken = etoken.GetOTP();
                    string ret = client.PriceChange(change);
                    if (ret.StartsWith("ERROR|"))
                        ret = "[ERROR]" + ret;
                    client.Close();
                    returnStr = ret;
                }
                return returnStr + "\r\n" + this.SerializeObject(change);
            }
            catch
            {
                return "[ERROR]";
            }
        }

        public string SendIncome(ClientHeader header, IncomeRecieptClass income)
        {
            try
            {
                income.Amdika = etoken.AMDIKA_ID;
                IncomeRecieptsClass list = new IncomeRecieptsClass();
                list.Reciepts = new List<IncomeRecieptClass>();
                list.Reciepts.Add(income);

                FuelFlowService.SendReceiptDS reciept = list.GetElement();
                reciept.Header = new FuelFlowService.Header_Type();
                reciept.Header.CompanyTIN = header.CompanyTIN;
                reciept.Header.SubmissionDate = header.SubmissionDate;
                reciept.Header.SubmitterTIN = header.SubmitterTIN;
                reciept.Header.F_TAXISBRANCH = header.TaxisBranch;
                string returnStr = "";
                if (!Simulation)
                {
                    ASFuelControl.Communication.FuelFlowService.PykServicesClient client = new FuelFlowService.PykServicesClient();
                    client.Open();
                    reciept.Header.eToken = etoken.GetOTP();
                    string ret = client.SendReceipt(reciept);
                    if (ret.StartsWith("ERROR|"))
                        ret = "[ERROR]" + ret;
                    client.Close();
                    returnStr = ret;
                }
                return returnStr + "\r\n" + this.SerializeObject(reciept);
            }
            catch(Exception ex)
            {
                return "[ERROR]";
            }
        }

        public string SendDelivery(ClientHeader header, DeliveryNoteClass delivery)
        {
            FuelFlowService.SendDeliveryDS deliveryNote = delivery.GetElement();

            deliveryNote.Header.CompanyTIN = header.CompanyTIN;
            deliveryNote.Header.SubmissionDate = DateTime.Now;
            deliveryNote.Header.SubmitterTIN = header.SubmitterTIN;
            deliveryNote.Header.F_TAXISBRANCH = header.TaxisBranch;
            
            string returnStr = "";
            if (!Simulation)
            {
                ASFuelControl.Communication.FuelFlowService.PykServicesClient client = new FuelFlowService.PykServicesClient();
                client.Open();
                deliveryNote.Header.eToken = etoken.GetOTP();
                string ret = client.SendDelivery(deliveryNote);
                if (ret.StartsWith("ERROR|"))
                    ret = "[ERROR]" + ret;
                client.Close();
                returnStr = ret;
            }
            return returnStr + "\r\n" + this.SerializeObject(deliveryNote);
        }

        public string SendLiterCheck(ClientHeader header, Communication.LiterCheckClass lc)
        {
            
            FuelFlowService.SendLiterCheckDS literCheck = lc.GetElement();
            try
            {
                literCheck.F_AM_DIKA = etoken.AMDIKA_ID;
                string returnStr = "";
                if (!Simulation)
                {
                    ASFuelControl.Communication.FuelFlowService.PykServicesClient client = new FuelFlowService.PykServicesClient();
                    client.Open();
                    literCheck.Header = new FuelFlowService.Header_Type();
                    literCheck.Header.CompanyTIN = header.CompanyTIN;
                    literCheck.Header.SubmitterTIN = header.SubmitterTIN;
                    literCheck.Header.F_TAXISBRANCH = header.TaxisBranch;
                    literCheck.Header.SubmissionDate = DateTime.Now;
                    literCheck.Header.eToken = etoken.GetOTP();
                    string ret = client.SendLiterCheck(literCheck);
                    if (ret.StartsWith("ERROR|"))
                        ret = "[ERROR]" + ret;
                    client.Close();
                    returnStr = ret;
                }
                return returnStr + "\r\n" + this.SerializeObject(lc);
            }
            catch
            {
                return "[ERROR]";
            }
        }

        public string SendBalance(ClientHeader header, BalanceClass bc)
        {
            FuelFlowService.SendBalanceDS balance = bc.GetElement();
            try
            {
                balance.F_AM_DIKA = etoken.AMDIKA_ID;
                balance.Header = new FuelFlowService.Header_Type();
                balance.Header.CompanyTIN = header.CompanyTIN;
                balance.Header.SubmitterTIN = header.SubmitterTIN;
                balance.Header.F_TAXISBRANCH = header.TaxisBranch;
                balance.Header.SubmissionDate = DateTime.Now;
                string returnStr = "";
                if (!Simulation)
                {
                    ASFuelControl.Communication.FuelFlowService.PykServicesClient client = new FuelFlowService.PykServicesClient();
                    client.Open();
                    balance.Header.eToken = etoken.GetOTP();
                    string ret = client.SendBalance(balance);
                    if (ret.StartsWith("ERROR|"))
                        ret = "[ERROR]" + ret;
                    client.Close();
                    returnStr = ret;
                }
                return returnStr + "\r\n" + this.SerializeObject(bc);
            }
            catch(Exception ex)
            {
                return "[ERROR]";
            }
        }

        public bool SendSWUpdate(string amdika, string version)
        {
            if (amdika == null || amdika == "")
                return false;
            string otp = etoken.GetOTP();
            bool ok = this.etoken.RegSoftwareUpdate(amdika, otp, "ASFuelControl", version);
            return ok;
        }

        public string GetSationRecord()
        {
            ASFuelControl.Communication.FuelFlowService.PykServicesClient client = new FuelFlowService.PykServicesClient();
            string xml = client.GetRegNums(etoken.AMDIKA_ID, etoken.GetOTP());
            return xml;
        }

        private string SerializeObject(object obj)
        {
            System.Xml.Serialization.XmlSerializer ser = new System.Xml.Serialization.XmlSerializer(obj.GetType());
            using (StringWriter textWriter = new StringWriter())
            {
                ser.Serialize(textWriter, obj);
                string data = textWriter.ToString();
                textWriter.Close();
                return data;
            }
        }
    }
}

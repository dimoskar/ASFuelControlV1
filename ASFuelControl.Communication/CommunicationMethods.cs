using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace ASFuelControl.Communication
{
    public class CommunicationMethods
    {
        private static CommunicationMethods instance;

        public static CommunicationMethods Instance 
        {
            get 
            {
                if (instance == null)
                    instance = new CommunicationMethods();
                return instance;
            }
        }

        eTokenLib.eTokenLib etoken = new eTokenLib.eTokenLib();

        public bool Simulation { set; get; }
        public static string CompanyTin { set; get; }
        public static string SubmitterTin { set; get; }
        public static string InstallatorTin { set; get; }
        public static string Amdika { set; get; }

        public decimal NormalizeVolume(Enums.FuelTypeEnum ft, decimal volume, decimal temperature)
        {
            if(ft == Enums.FuelTypeEnum.Diesel || ft == Enums.FuelTypeEnum.DieselHeat)
            {
                decimal coe = (decimal)0.00085;
                decimal diff = coe * volume * (temperature - 15);
                return volume - diff;
            }
            else if (ft == Enums.FuelTypeEnum.Unlieaded100 || ft == Enums.FuelTypeEnum.Unleaded95)
            {
                decimal coe = (decimal)0.00125;
                decimal diff = coe * volume * (temperature - 15);
                return volume - diff;
            }
            return volume;
        }

        public eTokenLib.eTokenLib TokenLib 
        {
            get 
            {
                if (Amdika == null || Amdika == "")
                    Amdika = etoken.AMDIKA_ID;
                return this.etoken; 
            }
        }

        public bool SendChangePrice(ClientHeader header, Enums.FuelTypeEnum ftype, decimal newPrice)
        {
            ASFuelControl.Communication.FuelFlowService.achilleas_fuelflow_receiptSoapClient client = new FuelFlowService.achilleas_fuelflow_receiptSoapClient();
            
            FuelFlowService.Fuelflows_TypePriceChange change = new FuelFlowService.Fuelflows_TypePriceChange();
            FuelFlowService.Fuel_Type ft = new FuelFlowService.Fuel_Type();
            ft.Code = (int)ftype;
            ft.Description = Enums.LocalizedEnumExtensions.GetLocalizedName(ftype);
            change.F_4001 = DateTime.Now;
            change.F_4002 = ft;
            change.F_4003 = newPrice;
            change.F_AM_DIKA = etoken.AMDIKA_ID;
            change.Header = new FuelFlowService.Header_Type();
            change.Header.CompanyTIN = header.CompanyTIN;
            change.Header.SubmissionDate = header.SubmissionDate;
            change.Header.SubmitterTIN = header.SubmitterTIN;

            try
            {
                
                if (!Simulation)
                {
                    client.Open();
                    change.Header.eToken = etoken.GetOTP();
                    string returnStr = client.PriceChange(change);
                    client.Close();
                    this.LogToken(returnStr);
                }
                this.LogObject(change, change.Header.SubmissionDate);
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool SendAlert(ClientHeader header, Enums.AlertIdEnum alertId, string deviceLabel, DateTime dt, string reason)
        {
            ASFuelControl.Communication.FuelFlowService.achilleas_fuelflow_receiptSoapClient client = new FuelFlowService.achilleas_fuelflow_receiptSoapClient();
            FuelFlowService.Fuelflows_TypeAlertRegistration alert = new FuelFlowService.Fuelflows_TypeAlertRegistration();
            alert.Header = new FuelFlowService.Header_Type();
            alert.Header.CompanyTIN = header.CompanyTIN;
            alert.Header.SubmissionDate = header.SubmissionDate;
            alert.Header.SubmitterTIN = header.SubmitterTIN;
            alert.F_AM_DIKA = etoken.AMDIKA_ID;
            alert.F_ALERT = new FuelFlowService.Alert_Type();
            alert.F_ALERT.F_ALERTID = (int)alertId;
            alert.F_ALERT.F_DATE = dt;
            alert.F_ALERT.F_DEVICE_LABEL = deviceLabel;
            alert.F_ALERT.F_REASONING = reason;

            try
            {
                if (!Simulation)
                {
                    client.Open();
                    alert.Header.eToken = etoken.GetOTP();
                    string ret = client.SendAlert(alert);
                    client.Close();
                    this.LogToken(ret);
                }
                this.LogObject(alert, alert.Header.SubmissionDate);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool SendActionRegistration(ClientHeader header)
        {
            ASFuelControl.Communication.FuelFlowService.achilleas_fuelflow_receiptSoapClient client = new FuelFlowService.achilleas_fuelflow_receiptSoapClient();
            return true;
            //client.SendAlert
        }

        public bool SendBalance(ClientHeader header, Communication.BalanceClass balanceClass)
        {
            ASFuelControl.Communication.FuelFlowService.achilleas_fuelflow_receiptSoapClient client = new FuelFlowService.achilleas_fuelflow_receiptSoapClient();

            FuelFlowService.Fuelflows_TypeBalance balance =  balanceClass.GetElement();
            balance.F_AM_DIKA = etoken.AMDIKA_ID;
            balance.Header = new FuelFlowService.Header_Type();
            balance.Header.CompanyTIN = CommunicationMethods.CompanyTin;
            balance.Header.SubmissionDate = DateTime.Now;
            balance.Header.SubmitterTIN = SubmitterTin;


            this.LogObject(balance, balance.Header.SubmissionDate);
            try
            {
                
                if (!Simulation)
                {
                    client.Open();
                    balance.Header.eToken = etoken.GetOTP();
                    string ret = client.SendBalance(balance);
                    client.Close();
                    this.LogToken(ret);
                }
                this.LogObject(balance, balance.Header.SubmissionDate);
                

                return true;
            }
            catch
            {
                return false;
            }

        }

        public bool SendDelievery(ClientHeader header, Communication.DeliveryNoteClass delivery)
        {
            ASFuelControl.Communication.FuelFlowService.achilleas_fuelflow_receiptSoapClient client = new FuelFlowService.achilleas_fuelflow_receiptSoapClient();
            

            delivery.Document.Amdika = etoken.AMDIKA_ID;
            FuelFlowService.Fuelflows_TypeDeliveryNote deliveryNote = delivery.GetElement();

            deliveryNote.Header.CompanyTIN = CommunicationMethods.CompanyTin;
            deliveryNote.Header.SubmissionDate = DateTime.Now;
            deliveryNote.Header.SubmitterTIN = SubmitterTin;

            try
            {
                if (!Simulation)
                {
                    client.Open();
                    deliveryNote.Header.eToken = etoken.GetOTP();
                    string ret = client.SendDelivery(deliveryNote);
                    client.Close();
                    this.LogToken(ret);
                }
                this.LogObject(deliveryNote, deliveryNote.Header.SubmissionDate);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool SendLiterCheck(ClientHeader header, Communication.LiterCheckClass lc)
        {
            ASFuelControl.Communication.FuelFlowService.achilleas_fuelflow_receiptSoapClient client = new FuelFlowService.achilleas_fuelflow_receiptSoapClient();
            FuelFlowService.Fuelflows_TypeLiterCheck literCheck = lc.GetElement();
            literCheck.Header = new FuelFlowService.Header_Type();
            literCheck.Header.CompanyTIN = CommunicationMethods.CompanyTin;
            literCheck.Header.SubmissionDate = DateTime.Now;
            literCheck.Header.SubmitterTIN = SubmitterTin;
            try
            {
                literCheck.F_AM_DIKA = etoken.AMDIKA_ID;
                if (!Simulation)
                {
                    client.Open();
                    literCheck.Header.eToken = etoken.GetOTP();
                    string ret = client.SendLiterCheck(literCheck);
                    client.Close();
                    this.LogToken(ret);
                }
                this.LogObject(literCheck, literCheck.Header.SubmissionDate);
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool SendReciept(ClientHeader header, List<Communication.IncomeRecieptClass> recs)
        {
            ASFuelControl.Communication.FuelFlowService.achilleas_fuelflow_receiptSoapClient client = new FuelFlowService.achilleas_fuelflow_receiptSoapClient();
            

            Communication.IncomeRecieptsClass reciepts = new IncomeRecieptsClass();
            foreach (Communication.IncomeRecieptClass rec in recs)
                rec.Amdika = etoken.AMDIKA_ID;

            reciepts.Reciepts = recs;
            FuelFlowService.Fuelflows_TypeIncomeReceipts reciept = reciepts.GetElement();
            reciept.Header = new FuelFlowService.Header_Type();
            reciept.Header.CompanyTIN = header.CompanyTIN;
            reciept.Header.SubmissionDate = header.SubmissionDate;
            reciept.Header.SubmitterTIN = header.SubmitterTIN;
            try
            {
                if (!Simulation)
                {
                    client.Open();
                    reciept.Header.eToken = etoken.GetOTP();
                    string ret = client.SendReceipt(reciept);
                    client.Close();
                    this.LogToken(ret);
                }

                this.LogObject(reciept, reciept.Header.SubmissionDate);
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool SendTankCheck(ClientHeader header, TankCheckClass tank)
        {
            ASFuelControl.Communication.FuelFlowService.achilleas_fuelflow_receiptSoapClient client = new FuelFlowService.achilleas_fuelflow_receiptSoapClient();

            FuelFlowService.Fuelflows_TypeTankCheck tankCheck = new FuelFlowService.Fuelflows_TypeTankCheck();
            tankCheck.F_3001 = tank.TransactionDate;
            tankCheck.F_3002 = tank.TankLevel == 0 ? 1 : tank.TankLevel;
            tankCheck.F_3003 = (tank.TankLevel == 0 ? 1 : tank.TankLevel) * (tank.TankVolume == 0 ? 1 : tank.TankVolume);
            tankCheck.F_3004 = tank.TankTemperature;
            tankCheck.F_3005 = tank.FuelDensity;
            tankCheck.F_AM_DIKA = etoken.AMDIKA_ID;
            tankCheck.F_LABEL = tank.TankId; //A.M Dejamenhs
            tankCheck.Header = new FuelFlowService.Header_Type();
            tankCheck.Header.CompanyTIN = header.CompanyTIN;
            tankCheck.Header.SubmissionDate = header.SubmissionDate;
            tankCheck.Header.SubmitterTIN = header.SubmitterTIN;

            try
            {
                if (!Simulation)
                {
                    client.Open();
                    tankCheck.Header.eToken = etoken.GetOTP();
                    string ret = client.SendTankCheck(tankCheck);
                    client.Close();
                    this.LogToken(ret);
                }
                this.LogObject(tankCheck, tankCheck.Header.SubmissionDate);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public void LogObject(object obj, DateTime dt)
        {
            string dirPath = System.Environment.CurrentDirectory + "\\Log";
            if (!System.IO.Directory.Exists(dirPath))
                System.IO.Directory.CreateDirectory(dirPath);
            string fName = dirPath + "\\" + obj.GetType().Name + dt.ToString("yyyy_MM_dd_HH_mm_ss_ffff") + ".xml";
            System.IO.FileStream f = new System.IO.FileStream(fName, System.IO.FileMode.OpenOrCreate);

            System.Xml.Serialization.XmlSerializer ser = new System.Xml.Serialization.XmlSerializer(obj.GetType());
            ser.Serialize(f, obj);
            f.Close();
            f.Dispose();
            string fileName = dirPath + "\\";
            string typeName = obj.GetType().Name;
            switch(typeName)
            {
                case "Fuelflows_TypeTankCheck":
                    this.LogToText(obj, fileName + "\\TankChecks.txt");
                    break;
                case "Fuelflows_TypeIncomeReceipts":
                    FuelFlowService.Fuelflows_TypeIncomeReceipts reciepts = obj as FuelFlowService.Fuelflows_TypeIncomeReceipts;
                    foreach(FuelFlowService.Fuelflows_TypeIncomeReceiptsIncomeReceipt reciept in reciepts.IncomeReceipt)
                        this.LogToText(reciept, fileName + "\\Reciepts.txt");
                    break;
                case "Fuelflows_TypeLiterCheck":
                    this.LogToText(obj, fileName + "\\LiterChecks.txt");
                    break;
                case "Fuelflows_TypeDeliveryNote":
                    FuelFlowService.Fuelflows_TypeDeliveryNote delivery = obj as FuelFlowService.Fuelflows_TypeDeliveryNote;
                    foreach (FuelFlowService.Fuelflows_TypeDeliveryNoteFuelData fuelData in delivery.FuelData)
                    {
                        this.LogToText(fuelData, fileName + "\\Deliveries.txt");
                    }
                    break;
                case "Fuelflows_TypeAlertRegistration":
                    break;
                case "Fuelflows_TypePriceChange":
                    break;
            }
        }

        public void LogToken(string tokenReturn)
        {
            string dirPath = System.Environment.CurrentDirectory + "\\Log\\Tokens.txt";
            int tokenIndex = tokenReturn.IndexOf("eToken:");
            if (tokenIndex > 0)
            {
                int endToken = tokenReturn.IndexOf("\n", tokenIndex);
                if (endToken > tokenIndex)
                {
                    string token = tokenReturn.Substring(tokenIndex, endToken - tokenIndex).Replace("eToken:", "").Replace("\n", "");
                    token  = token + "\r\n";
                    System.IO.File.AppendAllText(dirPath, token);
                }
            }
        }

        public void LogToTextBase(object obj, string fileName)
        {
            if (obj.GetType().IsArray)
            {
                Array arr = obj as Array;
                for(int i=0; i < arr.Length;i++)
                {
                    this.LogToText(arr.GetValue(i), fileName);
                }
            }
            else
                this.LogToText(obj, fileName);
        }

        public void LogToText(object obj, string fileName)
        {
            bool getHeader = !System.IO.File.Exists(fileName);
            string header = "";
            string values = "";
            PropertyInfo[] pinfo = obj.GetType().GetProperties();
            foreach (PropertyInfo prop in pinfo)
            {
                if (getHeader)
                {
                    if (header == "")
                        header = prop.Name;
                    else
                        header = header + "\t" + prop.Name;
                }
                object val = prop.GetValue(obj, null);
                string valStr = this.GetValueString(val);
                
                if (values == "")
                    values = valStr;
                else
                    values = values + "\t" + valStr;
               
            }
            if (!getHeader)
                System.IO.File.AppendAllText(fileName, values + "\r\n");
            else
            {
                System.IO.File.AppendAllText(fileName, header + "\r\n");
                System.IO.File.AppendAllText(fileName, values + "\r\n");
            }
        }

        string GetValueString(object obj)
        {
            if (obj == null)
                return " ";
            if (obj.GetType() == typeof(DateTime))
                return ((DateTime)obj).ToString("dd/MM/yyyy HH:mm:ss.fff");
            else if(obj.GetType() == typeof(decimal))
                return ((decimal)obj).ToString("N2");
            else if (obj.GetType() == typeof(FuelFlowService.Fuel_Type))
            {
                FuelFlowService.Fuel_Type ft = obj as FuelFlowService.Fuel_Type;
                return ft.Description;
            }
            else if(obj.GetType() == typeof(FuelFlowService.Header_Type))
            {
                FuelFlowService.Header_Type header = obj as FuelFlowService.Header_Type;
                return header.eToken == null ? " " :  header.eToken;
            }
            if (obj.ToString() == "")
                return " ";
            return obj.ToString();
        }
    }
}

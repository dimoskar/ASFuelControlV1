using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using static System.Net.WebRequestMethods;

namespace ASFuelControl.Communication
{
    public class SendMethods
    {
        public static event EventHandler<string> SentObjectLog;
        public bool Simulation { set; get; }
        static DateTime dtInitOtp = DateTime.MinValue;
        static ETokenDomainManager _etoken;
        static readonly object etokenSync = new object();
        static ETokenDomainManager etoken 
        {
            get 
            { 
                if (_etoken == null || _etoken.IsUnloaded) 
                {
                    lock (etokenSync)
                    {
                        if (_etoken == null || _etoken.IsUnloaded)
                        {
                            //eTokenLib.eTokenLib
                            GC.Collect(); GC.WaitForPendingFinalizers();
                            _etoken = new ETokenDomainManager();
                            _etoken.Load("C:\\ASFuelControl\\eTokenLib.dll", "eTokenLib.eTokenLib");
                            if (SentObjectLog != null && dtInitOtp == DateTime.MinValue)
                            {
                                dtInitOtp = DateTime.Now;
                                var logText = $"First Initializaion: [{dtInitOtp: dd/MM/yyyy HH:mm:ss.fff}]";
                                SentObjectLog(null, logText);
                                SentObjectLog(null, _etoken.LastErrorMsg());
                            }
                        }
                    }
                }
                return _etoken; 
            }
        }

        private static object foo = new object();
        public static string GetOTP()
        {
            lock (foo)
            {
                DateTime dtStart = DateTime.Now;
                string otp = etoken.GetOTP();
                DateTime dtEnd = DateTime.Now;
                if (otp.Contains("ERROR"))
                {
                    if (otp.Contains("Πρόβλημα συγχρονισμού ώρας"))
                    {
                        etoken.Unload();
                    }
                    Common.Logger.Instance.Error($"Start: {dtStart: yyyy/MM/dd HH:mm:ss.fff} - End: {dtEnd: yyyy/MM/dd HH:mm:ss.fff}, OTP Value: {otp}");
                }
                return otp;
            }
        }

        public string SendTankCheck(ClientHeader header, TankCheckClass tank)
        {
            FuelFlowService.Fuelflows_TypeTankCheck tankCheck = new FuelFlowService.Fuelflows_TypeTankCheck();
            tankCheck.F_3001 = tank.TransactionDate;
            tankCheck.F_3002 = tank.TankLevel;
            tankCheck.F_3003 = tank.TankVolume;
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
                string returnStr = "";
                if (!Simulation)
                {
                    DateTime dtNow = DateTime.Now;
                    string otp = GetOTP();
                    if (otp.Contains("ERROR"))
                    {
                        DateTime dtNow2 = DateTime.Now;
                        Common.Logger.Instance.Debug($"Date1: {dtNow} - Date2: {dtNow2}");
                        return "[ERROR:" + otp + "]";
                    }
                    ASFuelControl.Communication.FuelFlowService.achilleas_fuelflow_receiptSoapClient client = new FuelFlowService.achilleas_fuelflow_receiptSoapClient();
                    client.Open();
                    tankCheck.Header.eToken = otp;
                    header.SubmissionDate = DateTime.Now;
                    tankCheck.Header.SubmissionDate = header.SubmissionDate;
                    string ret = client.SendTankCheck(tankCheck);
                    if (ret.StartsWith("ERROR|"))
                        ret = "[ERROR]" + ret;
                    client.Close();
                    returnStr = ret;
                }
                return returnStr + "\r\n" + this.SerializeObject(tankCheck);
            }
            catch(Exception ex)
            {
                Common.Logger.Instance.Error(ex);
                return "[ERROR]";
            }
        }

        public string SendAlert(ClientHeader header, AlertClass alertClass)
        {
            
            FuelFlowService.Fuelflows_TypeAlertRegistration alert = new FuelFlowService.Fuelflows_TypeAlertRegistration();
            alert.Header = new FuelFlowService.Header_Type();
            alert.Header.CompanyTIN = header.CompanyTIN;
            
            alert.Header.SubmitterTIN = header.SubmitterTIN;
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
                    string otp = GetOTP();
                    if (otp.Contains("ERROR"))
                        return "[ERROR:" + otp + "]";
                    ASFuelControl.Communication.FuelFlowService.achilleas_fuelflow_receiptSoapClient client = new FuelFlowService.achilleas_fuelflow_receiptSoapClient();
                    client.Open();
                    alert.Header.eToken = otp;
                    header.SubmissionDate = DateTime.Now;
                    alert.Header.SubmissionDate = header.SubmissionDate;
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
            FuelFlowService.Fuelflows_TypePriceChange change = new FuelFlowService.Fuelflows_TypePriceChange();
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
            
            try
            {
                string returnStr = "";
                if (!Simulation)
                {
                    string otp = GetOTP();
                    if (otp.Contains("ERROR"))
                        return "[ERROR:" + otp + "]";
                    ASFuelControl.Communication.FuelFlowService.achilleas_fuelflow_receiptSoapClient client = new FuelFlowService.achilleas_fuelflow_receiptSoapClient();
                    client.Open();
                    change.Header.eToken = otp;
                    header.SubmissionDate = DateTime.Now;
                    change.Header.SubmissionDate = header.SubmissionDate;
                    string ret = client.PriceChange(change);
                    if (ret.StartsWith("ERROR|"))
                        ret = "[ERROR]" + ret;
                    client.Close();
                    returnStr = ret;
                }
                return returnStr + "\r\n" + this.SerializeObject(change);
            }
            catch (Exception ex)
            {
                Common.Logger.Instance.Error(ex);
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

                FuelFlowService.Fuelflows_TypeIncomeReceipts reciept = list.GetElement();
                reciept.Header = new FuelFlowService.Header_Type();
                reciept.Header.CompanyTIN = header.CompanyTIN;
                reciept.Header.SubmissionDate = header.SubmissionDate;
                reciept.Header.SubmitterTIN = header.SubmitterTIN;
                string returnStr = "";
                if (!Simulation)
                {
                    string otp = GetOTP();
                    if (otp.Contains("ERROR"))
                        return "[ERROR:" + otp + "]";
                    ASFuelControl.Communication.FuelFlowService.achilleas_fuelflow_receiptSoapClient client = new FuelFlowService.achilleas_fuelflow_receiptSoapClient();
                    client.Open();
                    reciept.Header.eToken = otp;
                    header.SubmissionDate = DateTime.Now;
                    reciept.Header.SubmissionDate = header.SubmissionDate;
                    string ret = client.SendReceipt(reciept);
                    if (ret.StartsWith("ERROR|"))
                        ret = "[ERROR]" + ret;
                    client.Close();
                    returnStr = ret;
                }
                return returnStr + "\r\n" + this.SerializeObject(reciept);
            }
            catch (Exception ex)
            {
                Common.Logger.Instance.Error(ex);
                return "[ERROR]";
            }
        }

        public string SendDelivery(ClientHeader header, DeliveryNoteClass delivery)
        {
            try
            {
                FuelFlowService.Fuelflows_TypeDeliveryNote deliveryNote = delivery.GetElement();

                deliveryNote.Header.CompanyTIN = header.CompanyTIN;
                deliveryNote.Header.SubmissionDate = DateTime.Now;
                deliveryNote.Header.SubmitterTIN = header.SubmitterTIN;
                string returnStr = "";
                if (!Simulation)
                {
                    string otp = GetOTP();
                    if (otp.Contains("ERROR"))
                        return "[ERROR:" + otp + "]";
                    ASFuelControl.Communication.FuelFlowService.achilleas_fuelflow_receiptSoapClient client = new FuelFlowService.achilleas_fuelflow_receiptSoapClient();
                    client.Open();
                    deliveryNote.Header.eToken = otp;
                    header.SubmissionDate = DateTime.Now;
                    deliveryNote.Header.SubmissionDate = header.SubmissionDate;
                    string ret = client.SendDelivery(deliveryNote);
                    if (ret.StartsWith("ERROR|"))
                        ret = "[ERROR]" + ret;
                    client.Close();
                    returnStr = ret;
                }
                return returnStr + "\r\n" + this.SerializeObject(deliveryNote);
            }
            catch (Exception ex)
            {
                Common.Logger.Instance.Error(ex);
                return "[ERROR]";
            }
        }

        public string SendLiterCheck(ClientHeader header, Communication.LiterCheckClass lc)
        {
            
            FuelFlowService.Fuelflows_TypeLiterCheck literCheck = lc.GetElement();
            try
            {
                literCheck.F_AM_DIKA = etoken.AMDIKA_ID;
                string returnStr = "";
                if (!Simulation)
                {
                    string otp = GetOTP();
                    if (otp.Contains("ERROR"))
                        return "[ERROR:" + otp + "]";
                    ASFuelControl.Communication.FuelFlowService.achilleas_fuelflow_receiptSoapClient client = new FuelFlowService.achilleas_fuelflow_receiptSoapClient();
                    client.Open();
                    literCheck.Header = new FuelFlowService.Header_Type();
                    literCheck.Header.CompanyTIN = header.CompanyTIN;
                    literCheck.Header.SubmitterTIN = header.SubmitterTIN;
                    literCheck.Header.SubmissionDate = DateTime.Now;
                    literCheck.Header.eToken = otp;
                    header.SubmissionDate = DateTime.Now;
                    literCheck.Header.SubmissionDate = header.SubmissionDate;
                    string ret = client.SendLiterCheck(literCheck);
                    if (ret.StartsWith("ERROR|"))
                        ret = "[ERROR]" + ret;
                    client.Close();
                    returnStr = ret;
                }
                return returnStr + "\r\n" + this.SerializeObject(lc);
            }
            catch (Exception ex)
            {
                Common.Logger.Instance.Error(ex);
                return "[ERROR]";
            }
        }

        public string SendBalance(ClientHeader header, BalanceClass bc)
        {
            FuelFlowService.Fuelflows_TypeBalance balance = bc.GetElement();
            try
            {
                balance.F_AM_DIKA = etoken.AMDIKA_ID;
                balance.Header = new FuelFlowService.Header_Type();
                balance.Header.CompanyTIN = header.CompanyTIN;
                balance.Header.SubmitterTIN = header.SubmitterTIN;
                balance.Header.SubmissionDate = DateTime.Now;
                string returnStr = "";
                if (!Simulation)
                {
                    string otp = GetOTP();
                    if (otp.Contains("ERROR"))
                        return "[ERROR:" + otp + "]";
                    ASFuelControl.Communication.FuelFlowService.achilleas_fuelflow_receiptSoapClient client = new FuelFlowService.achilleas_fuelflow_receiptSoapClient();
                    client.Open();
                    balance.Header.eToken = otp;
                    header.SubmissionDate = DateTime.Now;
                    balance.Header.SubmissionDate = header.SubmissionDate;
                    string ret = client.SendBalance(balance);
                    if (ret.StartsWith("ERROR|"))
                        ret = "[ERROR]" + ret;
                    client.Close();
                    returnStr = ret;
                }
                return returnStr + "\r\n" + this.SerializeObject(bc);
            }
            catch (Exception ex)
            {
                Common.Logger.Instance.Error(ex);
                return "[ERROR]";
            }
        }

        public bool SendSWUpdate(string amdika, string version)
        {
            if (amdika == null || amdika == "")
                return false;
            string otp = GetOTP();
            bool ok = etoken.RegSoftwareUpdate(amdika, otp, "ASFuelControl", version);
            return ok;
        }

        public string GetSationRecord()
        {
            ASFuelControl.Communication.FuelFlowService.achilleas_fuelflow_receiptSoapClient client = new FuelFlowService.achilleas_fuelflow_receiptSoapClient();
            string xml = client.GetRegNums(etoken.AMDIKA_ID, GetOTP());
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
        public static string ApplicationCRC
        {
            get
            {
                
                string crc = etoken.GetDirFileHash(System.Environment.CurrentDirectory);
                int pos = crc.IndexOf("|");
                return crc.Substring(0, pos);
            }
        }
    }

    public class ETokenProxy : MarshalByRefObject
    {
        private object _etoken;
        private Type _etokenType;
        public override object InitializeLifetimeService()
        {
            return null;
        }
        public void Load(string assemblyPath, string typeName)
        {
            var asm = Assembly.LoadFrom(assemblyPath);
            _etokenType = asm.GetType(typeName);
            _etoken = Activator.CreateInstance(_etokenType);
        }

        public string GetOTP()
        {
            var method = _etokenType.GetMethod("GetOTP");
            return (string)method.Invoke(_etoken, null);
        }
        public string GetDirFileHash(string dirName)
        {
            var method = _etokenType.GetMethod("GetDirFileHash");
            object[] parameters = new object[] { dirName };
            return (string)method.Invoke(_etoken, parameters);
        }
        public bool RegSoftwareUpdate(string amdika, string otp, string swname, string swversion)
        {
            var method = _etokenType.GetMethod("RegSoftwareUpdate");
            object[] parameters = new object[] { amdika, otp, swname, swversion };
            return (bool)method.Invoke(_etoken, parameters);
        }
        public string GetRegNums(string amdika, string otp)
        {
            var method = _etokenType.GetMethod("GetRegisteredNums");
            object[] parameters = new object[] { amdika, otp };
            return (string)method.Invoke(_etoken, parameters);
        }
        public bool IsTimeInSync()
        {
            var prop = _etokenType.GetProperty("IsTimeInSync");
            return (bool)prop.GetValue(_etoken);
        }

        public string LastErrorMsg()
        {
            var prop = _etokenType.GetProperty("LastErrorMsg");
            return (string)prop.GetValue(_etoken);
        }
        public string AMDIKA_ID
        {
            get
            {
                var prop = _etokenType.GetProperty("AMDIKA_ID");
                return (string)prop.GetValue(_etoken);
            }
        }
        // Add more methods/properties as needed
    }
    public class ETokenDomainManager
    {
        string _path = "";
        string _typeName = "";
        private readonly object _sync = new object();
        private AppDomain _domain;
        private ETokenProxy _proxy;
        public bool IsUnloaded { get; private set; } = false;

        public void Load(string assemblyPath, string typeName)
        {
            lock (_sync)
            {
                if (_domain != null)
                    return;

                _path = assemblyPath;
                _typeName = typeName;
                var setup = new AppDomainSetup
                {
                    ApplicationBase = Path.GetDirectoryName(assemblyPath)
                };

                _domain = AppDomain.CreateDomain("ETokenDomain_" + Guid.NewGuid(), null, setup);

                _proxy = (ETokenProxy)_domain.CreateInstanceAndUnwrap(
                    typeof(ETokenProxy).Assembly.FullName,
                    typeof(ETokenProxy).FullName);

                _proxy.Load(assemblyPath, typeName);
                IsUnloaded = false;
            }
        }

        private void EnsureLoaded()
        {
            if (_domain != null && _proxy != null && !IsUnloaded)
                return;

            lock (_sync)
            {
                if (_domain == null || _proxy == null || IsUnloaded)
                    Load(_path, _typeName);
            }
        }

        public string GetOTP()
        {
            EnsureLoaded();
            return _proxy.GetOTP();
        }

        public bool RegSoftwareUpdate(string amdika, string otp, string swname, string swversion)
        {
            EnsureLoaded();
            return _proxy.RegSoftwareUpdate(amdika, otp, swname, swversion);
        }

        public string GetRegNums(string amdika, string otp)
        {
            EnsureLoaded();
            return _proxy.GetRegNums(amdika, otp);
        }
        public string GetDirFileHash(string dirName)
        {
            EnsureLoaded();
            return _proxy.GetDirFileHash(dirName); ;
        }
        public bool IsTimeInSync()
        {
            EnsureLoaded();
            return _proxy.IsTimeInSync();
        }
        public string LastErrorMsg()
        {
            EnsureLoaded();
            return _proxy.LastErrorMsg(); ;
        }
        public string AMDIKA_ID
        {
            get
            {
                EnsureLoaded();
                return _proxy.AMDIKA_ID;
            }
        }

        public void Unload()
        {
            lock (_sync)
            {
                if (_domain != null)
                {
                    AppDomain.Unload(_domain);
                    _domain = null;
                    _proxy = null;
                }
                IsUnloaded = true;
            }
        }
    }
}

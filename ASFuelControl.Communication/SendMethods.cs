using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.Communication
{
    public class SendMethods
    {
        private static readonly TimeSpan FuelFlowOperationTimeout = TimeSpan.FromMinutes(1);
        public static event EventHandler<string> SentObjectLog;
        public bool Simulation { set; get; }
        static DateTime dtInitOtp = DateTime.MinValue;
        static ETokenDomainManager _etoken;
        static readonly object etokenSync = new object();
        static string amdikaId;
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
        internal static ETokenDomainManager TokenManager { get { return etoken; } }

        private static object foo = new object();

        internal static string SharedAmdikaId
        {
            get
            {
                lock (foo)
                {
                    if (string.IsNullOrWhiteSpace(amdikaId))
                        amdikaId = etoken.AMDIKA_ID;
                    return amdikaId;
                }
            }
        }

        private static TResult UseFuelFlowClient<TResult>(Func<FuelFlowService.achilleas_fuelflow_receiptSoapClient, TResult> action)
        {
            FuelFlowService.achilleas_fuelflow_receiptSoapClient client =
                new FuelFlowService.achilleas_fuelflow_receiptSoapClient();
            bool closed = false;
            try
            {
                client.InnerChannel.OperationTimeout = FuelFlowOperationTimeout;
                client.Open();
                TResult result = action(client);
                client.Close();
                closed = true;
                return result;
            }
            finally
            {
                if (!closed)
                    client.Abort();
            }
        }

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
                        // eTokenLib has no reset API. Recycle the isolated host so
                        // Windows unloads its mixed/native state completely. The
                        // next sending pass starts a clean host instance.
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
            tankCheck.F_AM_DIKA = SharedAmdikaId;
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
                    tankCheck.Header.eToken = otp;
                    header.SubmissionDate = DateTime.Now;
                    tankCheck.Header.SubmissionDate = header.SubmissionDate;
                    string ret = UseFuelFlowClient(client => client.SendTankCheck(tankCheck));
                    if (ret.StartsWith("ERROR|"))
                        ret = "[ERROR]" + ret;
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
            alert.F_AM_DIKA = SharedAmdikaId;
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
                    alert.Header.eToken = otp;
                    header.SubmissionDate = DateTime.Now;
                    alert.Header.SubmissionDate = header.SubmissionDate;
                    string ret = UseFuelFlowClient(client => client.SendAlert(alert));
                    if (ret.StartsWith("ERROR|"))
                        ret = "[ERROR]" + ret;
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
            change.F_AM_DIKA = SharedAmdikaId;
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
                    change.Header.eToken = otp;
                    header.SubmissionDate = DateTime.Now;
                    change.Header.SubmissionDate = header.SubmissionDate;
                    string ret = UseFuelFlowClient(client => client.PriceChange(change));
                    if (ret.StartsWith("ERROR|"))
                        ret = "[ERROR]" + ret;
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
                income.Amdika = SharedAmdikaId;
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
                    reciept.Header.eToken = otp;
                    header.SubmissionDate = DateTime.Now;
                    reciept.Header.SubmissionDate = header.SubmissionDate;
                    string ret = UseFuelFlowClient(client => client.SendReceipt(reciept));
                    if (ret.StartsWith("ERROR|"))
                        ret = "[ERROR]" + ret;
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
                    deliveryNote.Header.eToken = otp;
                    header.SubmissionDate = DateTime.Now;
                    deliveryNote.Header.SubmissionDate = header.SubmissionDate;
                    string ret = UseFuelFlowClient(client => client.SendDelivery(deliveryNote));
                    if (ret.StartsWith("ERROR|"))
                        ret = "[ERROR]" + ret;
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
                literCheck.F_AM_DIKA = SharedAmdikaId;
                string returnStr = "";
                if (!Simulation)
                {
                    string otp = GetOTP();
                    if (otp.Contains("ERROR"))
                        return "[ERROR:" + otp + "]";
                    literCheck.Header = new FuelFlowService.Header_Type();
                    literCheck.Header.CompanyTIN = header.CompanyTIN;
                    literCheck.Header.SubmitterTIN = header.SubmitterTIN;
                    literCheck.Header.SubmissionDate = DateTime.Now;
                    literCheck.Header.eToken = otp;
                    header.SubmissionDate = DateTime.Now;
                    literCheck.Header.SubmissionDate = header.SubmissionDate;
                    string ret = UseFuelFlowClient(client => client.SendLiterCheck(literCheck));
                    if (ret.StartsWith("ERROR|"))
                        ret = "[ERROR]" + ret;
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
                balance.F_AM_DIKA = SharedAmdikaId;
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
                    balance.Header.eToken = otp;
                    header.SubmissionDate = DateTime.Now;
                    balance.Header.SubmissionDate = header.SubmissionDate;
                    string ret = UseFuelFlowClient(client => client.SendBalance(balance));
                    if (ret.StartsWith("ERROR|"))
                        ret = "[ERROR]" + ret;
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
            return UseFuelFlowClient(client => client.GetRegNums(SharedAmdikaId, GetOTP()));
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
        private readonly object _sync = new object();
        private object _etoken;
        private Type _etokenType;
        public override object InitializeLifetimeService()
        {
            return null;
        }
        public void Load(string assemblyPath, string typeName)
        {
            lock (_sync)
            {
                var asm = Assembly.LoadFrom(assemblyPath);
                _etokenType = asm.GetType(typeName, true);
                _etoken = Activator.CreateInstance(_etokenType);
                if (_etoken == null)
                    throw new InvalidOperationException("The eToken library did not create an instance of " + typeName + ".");
            }
        }

        private object GetInvocationTarget(MethodInfo method)
        {
            if (method == null)
                throw new MissingMethodException("The required eToken member was not found.");
            if (method.IsStatic)
                return null;
            if (_etokenType == null)
                throw new InvalidOperationException("The eToken library has not been loaded.");
            if (_etoken == null || !method.DeclaringType.IsInstanceOfType(_etoken))
                throw new InvalidOperationException(
                    "The eToken target instance is unavailable. Restart the application; the native token library cannot be re-created safely in-process.");
            return _etoken;
        }

        private object Invoke(string methodName, object[] parameters)
        {
            lock (_sync)
            {
                if (_etokenType == null)
                    throw new InvalidOperationException("The eToken library has not been loaded.");
                var method = _etokenType.GetMethod(methodName);
                return method.Invoke(GetInvocationTarget(method), parameters);
            }
        }

        private object ReadProperty(string propertyName)
        {
            lock (_sync)
            {
                if (_etokenType == null)
                    throw new InvalidOperationException("The eToken library has not been loaded.");
                var property = _etokenType.GetProperty(propertyName);
                if (property == null)
                    throw new MissingMemberException(_etokenType.FullName, propertyName);
                var getter = property.GetGetMethod(true);
                return getter.Invoke(GetInvocationTarget(getter), null);
            }
        }

        public string GetOTP()
        {
            return (string)Invoke("GetOTP", null);
        }
        public string GetDirFileHash(string dirName)
        {
            object[] parameters = new object[] { dirName };
            return (string)Invoke("GetDirFileHash", parameters);
        }
        public bool RegSoftwareUpdate(string amdika, string otp, string swname, string swversion)
        {
            object[] parameters = new object[] { amdika, otp, swname, swversion };
            return (bool)Invoke("RegSoftwareUpdate", parameters);
        }
        public string GetRegNums(string amdika, string otp)
        {
            object[] parameters = new object[] { amdika, otp };
            return (string)Invoke("GetRegisteredNums", parameters);
        }
        public bool IsTimeInSync()
        {
            return (bool)ReadProperty("IsTimeInSync");
        }

        public string LastErrorMsg()
        {
            return (string)ReadProperty("LastErrorMsg");
        }
        public string AMDIKA_ID
        {
            get
            {
                return (string)ReadProperty("AMDIKA_ID");
            }
        }
        // Add more methods/properties as needed
    }
    internal sealed class ETokenProcessClient : IDisposable
    {
        private const string ProtocolPrefix = "ASFC_ETOKEN";
        private static readonly TimeSpan CommandTimeout = TimeSpan.FromMinutes(1);
        private readonly string _assemblyPath;
        private readonly string _typeName;
        private Process _process;
        private StreamWriter _input;
        private StreamReader _output;
        private int _requestId;

        public ETokenProcessClient(string assemblyPath, string typeName)
        {
            _assemblyPath = assemblyPath;
            _typeName = typeName;
        }

        public string Invoke(string command, params string[] arguments)
        {
            EnsureStarted();

            int requestId = ++_requestId;
            var request = new StringBuilder();
            request.Append(ProtocolPrefix).Append('|').Append(requestId).Append('|').Append(command);
            foreach (string argument in arguments ?? new string[0])
                request.Append('|').Append(Encode(argument));

            try
            {
                _input.WriteLine(request.ToString());
                _input.Flush();

                DateTime deadline = DateTime.UtcNow.Add(CommandTimeout);
                while (true)
                {
                    TimeSpan remaining = deadline - DateTime.UtcNow;
                    if (remaining <= TimeSpan.Zero)
                        throw new TimeoutException("The eToken helper did not answer within one minute.");

                    Task<string> readTask = _output.ReadLineAsync();
                    if (!readTask.Wait(remaining))
                        throw new TimeoutException("The eToken helper did not answer within one minute.");

                    string response = readTask.Result;
                    if (response == null)
                        throw new InvalidOperationException("The eToken helper stopped before returning a response.");

                    string[] fields = response.Split('|');
                    if (fields.Length < 4 || fields[0] != ProtocolPrefix || fields[1] != requestId.ToString())
                        continue;

                    string payload = Decode(fields[3]);
                    if (fields[2] == "OK")
                        return payload;
                    throw new InvalidOperationException("The eToken helper returned an error: " + payload);
                }
            }
            catch
            {
                DisposeProcess();
                throw;
            }
        }

        private void EnsureStarted()
        {
            if (_process != null && !_process.HasExited)
                return;

            DisposeProcess();
            string executablePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "ASFuelControl.ETokenHost.exe");
            if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
                throw new InvalidOperationException(
                    "The isolated eToken host was not found: " + executablePath);

            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = ETokenProcessHost.HostArgument + " " + Encode(_assemblyPath) + " " + Encode(_typeName),
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true
            };

            _process = Process.Start(startInfo);
            if (_process == null)
                throw new InvalidOperationException("The isolated eToken helper could not be started.");
            _input = _process.StandardInput;
            _output = _process.StandardOutput;
        }

        private void DisposeProcess()
        {
            Process process = _process;
            _process = null;

            try { if (_input != null) _input.Dispose(); } catch { }
            try { if (_output != null) _output.Dispose(); } catch { }
            _input = null;
            _output = null;

            if (process == null)
                return;

            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                    process.WaitForExit(5000);
                }
            }
            catch { }
            finally
            {
                process.Dispose();
            }
        }

        public void Dispose()
        {
            DisposeProcess();
        }

        internal static string Encode(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));
        }

        internal static string Decode(string value)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(value ?? string.Empty));
        }
    }

    public static class ETokenProcessHost
    {
        internal const string HostArgument = "--asfuel-etoken-host";
        private const string ProtocolPrefix = "ASFC_ETOKEN";

        public static bool IsHostInvocation(string[] arguments)
        {
            return arguments != null && arguments.Length >= 3 &&
                string.Equals(arguments[0], HostArgument, StringComparison.Ordinal);
        }

        public static int Run(string[] arguments)
        {
            if (!IsHostInvocation(arguments))
                return 2;

            string assemblyPath = ETokenProcessClient.Decode(arguments[1]);
            string typeName = ETokenProcessClient.Decode(arguments[2]);
            ETokenProxy proxy = null;

            using (var input = new StreamReader(Console.OpenStandardInput()))
            using (var output = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true })
            {
                string request;
                while ((request = input.ReadLine()) != null)
                {
                    string[] fields = request.Split('|');
                    if (fields.Length < 3 || fields[0] != ProtocolPrefix)
                        continue;

                    string requestId = fields[1];
                    try
                    {
                        if (proxy == null)
                        {
                            proxy = new ETokenProxy();
                            proxy.Load(assemblyPath, typeName);
                        }

                        string result = Execute(proxy, fields);
                        output.WriteLine(ProtocolPrefix + "|" + requestId + "|OK|" + ETokenProcessClient.Encode(result));
                    }
                    catch (Exception ex)
                    {
                        output.WriteLine(ProtocolPrefix + "|" + requestId + "|ERR|" + ETokenProcessClient.Encode(Unwrap(ex).ToString()));
                    }
                }
            }
            return 0;
        }

        private static string Execute(ETokenProxy proxy, string[] fields)
        {
            string command = fields[2];
            string[] arguments = fields.Skip(3).Select(ETokenProcessClient.Decode).ToArray();
            switch (command)
            {
                case "GET_OTP":
                    return proxy.GetOTP();
                case "GET_DIR_HASH":
                    return proxy.GetDirFileHash(arguments[0]);
                case "REGISTER_UPDATE":
                    return proxy.RegSoftwareUpdate(arguments[0], arguments[1], arguments[2], arguments[3]).ToString();
                case "GET_REG_NUMS":
                    return proxy.GetRegNums(arguments[0], arguments[1]);
                case "IS_TIME_IN_SYNC":
                    return proxy.IsTimeInSync().ToString();
                case "LAST_ERROR":
                    return proxy.LastErrorMsg();
                case "AMDIKA_ID":
                    return proxy.AMDIKA_ID;
                default:
                    throw new InvalidOperationException("Unknown eToken helper command: " + command);
            }
        }

        private static Exception Unwrap(Exception exception)
        {
            var invocationException = exception as TargetInvocationException;
            return invocationException != null && invocationException.InnerException != null
                ? invocationException.InnerException
                : exception;
        }
    }

    public class ETokenDomainManager
    {
        string _path = "";
        string _typeName = "";
        private readonly object _sync = new object();
        private ETokenProcessClient _client;
        public bool IsUnloaded { get; private set; } = false;

        public void Load(string assemblyPath, string typeName)
        {
            lock (_sync)
            {
                if (_client != null && !IsUnloaded)
                    return;

                _path = assemblyPath;
                _typeName = typeName;
                _client = new ETokenProcessClient(_path, _typeName);
                IsUnloaded = false;
            }
        }

        private void EnsureLoaded()
        {
            lock (_sync)
            {
                if (_client == null || IsUnloaded)
                    Load(_path, _typeName);
            }
        }

        public string GetOTP()
        {
            lock (_sync)
            {
                EnsureLoaded();
                return _client.Invoke("GET_OTP");
            }
        }

        public bool RegSoftwareUpdate(string amdika, string otp, string swname, string swversion)
        {
            lock (_sync)
            {
                EnsureLoaded();
                return bool.Parse(_client.Invoke("REGISTER_UPDATE", amdika, otp, swname, swversion));
            }
        }

        public string GetRegNums(string amdika, string otp)
        {
            lock (_sync)
            {
                EnsureLoaded();
                return _client.Invoke("GET_REG_NUMS", amdika, otp);
            }
        }
        public string GetDirFileHash(string dirName)
        {
            lock (_sync)
            {
                EnsureLoaded();
                return _client.Invoke("GET_DIR_HASH", dirName);
            }
        }
        public bool IsTimeInSync()
        {
            lock (_sync)
            {
                EnsureLoaded();
                return bool.Parse(_client.Invoke("IS_TIME_IN_SYNC"));
            }
        }
        public string LastErrorMsg()
        {
            lock (_sync)
            {
                EnsureLoaded();
                return _client.Invoke("LAST_ERROR");
            }
        }
        public string AMDIKA_ID
        {
            get
            {
                lock (_sync)
                {
                    EnsureLoaded();
                    return _client.Invoke("AMDIKA_ID");
                }
            }
        }

        public void Unload()
        {
            lock (_sync)
            {
                if (_client != null)
                    _client.Dispose();
                _client = null;
                IsUnloaded = true;
            }
        }
    }
}

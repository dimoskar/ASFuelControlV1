using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ASFuelControl.VeederRootTLSIB
{
    //public class Connector
    //{

    //    private SerialPort serialPort = new SerialPort();
    //    private Thread th;
    //    private List<ATGProbe> probes = new List<ATGProbe>();
    //    private bool isConnected = false;
    //    private bool errorOccured = false;
    //    private string buffer = "";
    //    public string CommunicationPort
    //    {
    //        get;
    //        set;
    //    }

    //    public bool IsConnected
    //    {
    //        get
    //        {
    //            return this.isConnected;
    //        }
    //    }

    //    public ATGProbe[] Probes
    //    {
    //        get
    //        {
    //            return this.probes.ToArray();
    //        }
    //    }

    //    public Connector()
    //    {
    //    }

    //    public ATGProbe AddProbe(int addressId)
    //    {
    //        ATGProbe aTGProbe;
    //        if ((
    //            from p in this.probes
    //            where p.Address == addressId
    //            select p).Count<ATGProbe>() <= 0)
    //        {
    //            ATGProbe aTGProbe1 = new ATGProbe()
    //            {
    //                Address = addressId
    //            };
    //            this.probes.Add(aTGProbe1);
    //            aTGProbe = aTGProbe1;
    //        }
    //        else
    //        {
    //            aTGProbe = null;
    //        }
    //        return aTGProbe;
    //    }

    //    public void Connect()
    //    {
    //        this.serialPort.PortName = this.CommunicationPort;
    //        this.serialPort.DtrEnable = true;
    //        this.serialPort.BaudRate = 2400;
    //        this.serialPort.Parity = Parity.Even;
    //        this.serialPort.StopBits = StopBits.One;
    //        this.serialPort.DataBits = 7;
    //        this.serialPort.Open();
    //        this.isConnected = true;
    //        this.th = new Thread(new ThreadStart(this.ThreadRun));
    //        this.th.Start();
    //    }

    //    public void DisConnect()
    //    {
    //        this.isConnected = false;
    //        this.serialPort.Close();
    //    }
    //    public string Mantissa(string value)
    //    {

    //        string hexString = value;
    //        uint num = uint.Parse(hexString, System.Globalization.NumberStyles.AllowHexSpecifier);

    //        byte[] floatVals = BitConverter.GetBytes(num);
    //        float f = BitConverter.ToSingle(floatVals, 0);
    //        return (f * 100).ToString();
    //    }
    //    private void EvaluateResponse(string response)
    //    {
    //        try
    //        {
    //            byte[] ba = Encoding.Default.GetBytes(response);
    //            string buffer = BitConverter.ToString(ba);
    //            string[] VR = buffer.Split('-');
    //            int length = response.Length;

    //            int ATG_id= Convert.ToInt32(Commands.ConvertHex(VR[6]));
    //            string Volume = Commands.ConvertHex(VR[50] + VR[51] + VR[52] + VR[53] + VR[54] + VR[55] + VR[56] + VR[57]);
    //            string Water = Commands.ConvertHex(VR[58] + VR[59] + VR[60] + VR[61] + VR[62] + VR[63] + VR[64] + VR[65]);
    //            string Temperature = Commands.ConvertHex(VR[66] + VR[67] + VR[68] + VR[69] + VR[70] + VR[71] + VR[72] + VR[73]);



    //            string str = response.Replace("\n\r", "");
    //            string[] strArrays = str.Split(new char[] { '=' });

    //            if (length == 89)
    //            {
    //                int num = ATG_id;

    //                decimal num2 = Convert.ToDecimal(Mantissa(Temperature));
    //                decimal num3 = Convert.ToDecimal(Mantissa(Volume));
    //                decimal num4 = Convert.ToDecimal(Mantissa(Water));
    //                ATGProbe aTGProbe = (
    //                    from p in this.probes
    //                    where p.Address == num
    //                    select p).FirstOrDefault<ATGProbe>();
    //                if (aTGProbe != null)
    //                {

    //                    aTGProbe.FuelLevel = num3 / new decimal(100);
    //                    aTGProbe.WaterLevel = num4 / new decimal(100);
    //                    aTGProbe.Temperature = num2 / new decimal(100);
    //                    if (this.DataUpdated != null)
    //                    {
    //                        this.DataUpdated(aTGProbe, new EventArgs());
    //                    }
    //                }

    //            }
    //            //if (length < 88)
    //            //{
    //            //    int num = ATG_id;


    //            //    decimal num2 = 0;
    //            //    decimal num3 = 0;
    //            //    decimal num4 = 0;
    //            //    ATGProbe aTGProbe = (
    //            //        from p in this.probes
    //            //        where p.Address == num
    //            //        select p).FirstOrDefault<ATGProbe>();
    //            //    if (aTGProbe != null)
    //            //    {
    //            //        aTGProbe.FuelLevel = num3;
    //            //        aTGProbe.WaterLevel = num4;
    //            //        aTGProbe.Temperature = num2;
    //            //        if (this.DataUpdated != null)
    //            //        {
    //            //            this.DataUpdated(aTGProbe, new EventArgs());
    //            //        }
    //            //    }
    //            //}
    //            response = "";
    //        }
    //        catch (Exception ex)
    //        {
    //            response = "";
    //        }

    //    }



    //    private void ThreadRun()
    //    {
    //        while (this.IsConnected)
    //        {
    //            try
    //            {
    //                if (this.errorOccured)
    //                {
    //                    this.serialPort.Close();
    //                    this.serialPort.Open();
    //                    this.errorOccured = false;
    //                }
    //                int num = 0;
    //                foreach (ATGProbe probe in this.probes)
    //                {

    //                    SerialPort serialPort = this.serialPort;
    //                    string idString = probe.Address.ToString();
    //                    this.serialPort.Write(Commands.GetStatus(idString), 0, Commands.GetStatus(idString).Length);

    //                    Thread.Sleep(500);
    //                    num = num + 100;
    //                    string str = "";
    //                    DateTime now = DateTime.Now;
    //                    while (true)
    //                    {
    //                        if (this.serialPort.BytesToRead > 0)
    //                        {
    //                            str = string.Concat(str, this.serialPort.ReadExisting());
    //                            if (str.Contains("\u0001"))
    //                            {
    //                                break;
    //                            }
    //                        }
    //                        if (DateTime.Now.Subtract(now).TotalMilliseconds > 100)
    //                        {
    //                            break;
    //                        }
    //                    }
    //                    if (str.Length > 0)
    //                    {
    //                        this.EvaluateResponse(str);
    //                    }
    //                }
    //                Thread.Sleep(1500 - num);
    //            }
    //            catch
    //            {
    //                this.errorOccured = true;
    //                Thread.Sleep(500);
    //            }
    //        }
    //    }

    //    public event EventHandler DataUpdated;
    //}

    
    public class Connector : IDisposable
    {
        private readonly SerialPort _serialPort;
        private readonly List<ATGProbe> _probes = new List<ATGProbe>();
        private Thread _workerThread;
        private CancellationTokenSource _cts;

        public string CommunicationPort { get; set; }
        public bool IsConnected { get; private set; }

        public event EventHandler<ATGProbe> DataUpdated;

        public Connector()
        {
            _serialPort = new SerialPort
            {
                BaudRate = 2400,
                Parity = Parity.Even,
                StopBits = StopBits.One,
                DataBits = 7,
                DtrEnable = true,
                Encoding = Encoding.ASCII
            };
        }

        public ATGProbe[] Probes
        {
            get { return _probes.ToArray(); }
        }

        public ATGProbe AddProbe(int addressId)
        {
            if (_probes.Any(p => p.Address == addressId))
                return null;

            var probe = new ATGProbe { Address = addressId };
            _probes.Add(probe);
            return probe;
        }

        public void Connect()
        {
            if (IsConnected)
                return;

            if (string.IsNullOrWhiteSpace(CommunicationPort))
                throw new InvalidOperationException("CommunicationPort is not set.");

            _serialPort.PortName = CommunicationPort;
            _serialPort.Open();

            _cts = new CancellationTokenSource();
            _workerThread = new Thread(() => WorkerLoop(_cts.Token));
            _workerThread.IsBackground = true;
            _workerThread.Start();

            IsConnected = true;
        }

        public void Disconnect()
        {
            if (!IsConnected)
                return;

            _cts.Cancel();
            _workerThread.Join();

            if (_serialPort.IsOpen)
                _serialPort.Close();

            IsConnected = false;
        }

        private void WorkerLoop(CancellationToken token)
        {
            Dictionary<int, DateTime> errorOccured = new Dictionary<int, DateTime>();
            while (!token.IsCancellationRequested)
            {
                int num = 0;
                foreach (var probe in _probes)
                {
                    Thread.Sleep(100);
                    num = num + 50;
                    try
                    {
                        PollProbe(probe);
                        errorOccured.Remove(probe.Address);
                    }
                    catch(Exception ex)
                    {
                        if (!errorOccured.ContainsKey(probe.Address))
                            errorOccured.Add(probe.Address, DateTime.Now);
                        if (DateTime.Now.Subtract(errorOccured[probe.Address]).TotalSeconds < 15)
                        {
                            if (this.DataUpdated != null)
                            {
                                this.DataUpdated(this, probe);
                            }
                        }
                        
                        Common.Logger.Instance.Error("Exception: " + ex.Message);
                        if(ex.InnerException != null)
                            Common.Logger.Instance.Error(ex.InnerException.Message);
                        Common.Logger.Instance.Error("StackTrace: " + ex.StackTrace);
                        Common.Logger.Instance.Error("===========================================================");
                    }
                }
                if(num < 500)
                    Thread.Sleep(500 - num);
            }
        }

        private void PollProbe(ATGProbe probe)
        {
            var cmd = Commands.GetStatus(probe.Address.ToString());
            _serialPort.Write(cmd, 0, cmd.Length);

            Thread.Sleep(500);
            
            var response = ReadResponse();
            if (!string.IsNullOrEmpty(response))
                ParseResponse(response, probe);
            else
            {
                response = "";
                throw (new Exception("Probe poll not valid"));
            }
            response = "";
        }

        private string ReadResponse()
        {
            var sb = new StringBuilder();
            var start = DateTime.Now;

            while (DateTime.Now - start < TimeSpan.FromMilliseconds(200))
            {
                if (_serialPort.BytesToRead > 0)
                {
                    sb.Append(_serialPort.ReadExisting());
                    if (sb.ToString().Contains("\u0001"))
                        break;
                }
            }

            return sb.ToString();
        }

        private void ParseResponse(string response, ATGProbe probe)
        {
            try
            {
                byte[] ba = Encoding.Default.GetBytes(response);
                string buffer = BitConverter.ToString(ba);

                //Common.Logger.Instance.Debug("Response: " + response);
                //Common.Logger.Instance.Debug("Buffer  : " + buffer);

                string[] VR = buffer.Split('-');
                int length = response.Length;

                int ATG_id = Convert.ToInt32(Commands.ConvertHex(VR[6]));
                if (ATG_id != probe.Address)
                    return;
                string Volume = Commands.ConvertHex(VR[50] + VR[51] + VR[52] + VR[53] + VR[54] + VR[55] + VR[56] + VR[57]);
                string Water = Commands.ConvertHex(VR[58] + VR[59] + VR[60] + VR[61] + VR[62] + VR[63] + VR[64] + VR[65]);
                string Temperature = Commands.ConvertHex(VR[66] + VR[67] + VR[68] + VR[69] + VR[70] + VR[71] + VR[72] + VR[73]);
                Common.Logger.Instance.Debug(string.Format("Volume: {0}, Water: {1}, Temperature: {2}", Volume, Water, Temperature));


                string str = response.Replace("\n\r", "");
                string[] strArrays = str.Split(new char[] { '=' });

                //if (length == 89)
                //{
                //    int num = ATG_id;

                decimal num2 = Convert.ToDecimal(ConvertMantissa(Temperature));
                decimal num3 = Convert.ToDecimal(ConvertMantissa(Volume));
                decimal num4 = Convert.ToDecimal(ConvertMantissa(Water));
                    
                if (probe != null)
                {

                    probe.FuelLevel = num3 / new decimal(100);
                    probe.WaterLevel = num4 / new decimal(100);
                    probe.Temperature = num2 / new decimal(100);
                    if (this.DataUpdated != null)
                    {
                        this.DataUpdated(this, probe);
                    }
                }
                //}
            }
            catch(Exception ex)
            {
                if (this.DataUpdated != null)
                {
                    this.DataUpdated(this, probe);
                }
                //Common.Logger.Instance.Error("Exception: " + ex.Message);
                //if (ex.InnerException != null)
                //    Common.Logger.Instance.Error(ex.InnerException.Message);
                //Common.Logger.Instance.Error("StackTrace: " + ex.StackTrace);
                //Common.Logger.Instance.Error("===========================================================");
            }
        }

        private string ConvertMantissa(string hex)
        {
            uint num = uint.Parse(hex, System.Globalization.NumberStyles.HexNumber);
            float f = BitConverter.ToSingle(BitConverter.GetBytes(num), 0);
            return (f * 100).ToString();
        }

        public void Dispose()
        {
            Disconnect();
            _serialPort.Dispose();
        }
    }
    
}

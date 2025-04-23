using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;
using ASFuelControl.Common;
using System.IO;

namespace ASFuelControl.PetrotecM_
{
    public class PetrotecProtocol : IFuelProtocol, IPumpDebug
    {
        private List<FuelPoint> fuelPoints = new List<FuelPoint>();

        public event EventHandler<FuelPointValuesArgs> DataChanged;

        public event EventHandler DispenserOffline;

        public event EventHandler<FuelPointValuesArgs> DispenserStatusChanged;

        public event EventHandler<TotalsEventArgs> TotalsRecieved;

        public event EventHandler<SaleEventArgs> SaleRecieved;


        private SerialPort serialPort = new SerialPort();

        private Thread th;

        #region Interface Comp 

        public FuelPoint[] FuelPoints
        {
            get
            {
                return this.fuelPoints.ToArray();
            }
            set
            {
                this.fuelPoints = new List<FuelPoint>(value);
            }
        }
        public bool IsConnected
        {
            get
            {
                return this.serialPort.IsOpen;
            }
        }
        public string CommunicationPort
        {
            get;
            set;
        }
        public void AddFuelPoint(FuelPoint fp)
        {
            this.fuelPoints.Add(fp);
        }

        public void ClearFuelPoints()
        {
            this.fuelPoints.Clear();
        }

        public DebugValues foo = new DebugValues();

        public DebugValues DebugStatusDialog(FuelPoint fp)
        {
            return foo;
          


        }
        #endregion


        public void Connect()
        {
            try
            {
                this.serialPort.PortName = this.CommunicationPort;
                this.serialPort.Parity = Parity.Even;
                this.serialPort.DataBits = 7;
                this.serialPort.StopBits = StopBits.Two;
                this.serialPort.BaudRate = 1200;
                this.serialPort.RtsEnable = true;
                this.serialPort.DataReceived += SerialPort_DataReceived;
                this.serialPort.Open();
                this.th = new Thread(new ThreadStart(this.WorkFlow));
                this.th.Start();
            }
            catch(Exception ex)
            {

            }
        }

        public void Disconnect()
        {
            if (this.serialPort.IsOpen)
            {
                this.serialPort.Close();
            }
        }

        int num = 0;

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                int num = 0;
                SerialPort serialPort = (SerialPort)sender;
                while (serialPort.BytesToRead < 21 && num < 300)
                {
                    Thread.Sleep(70);
                    num += 20;
                }
                byte[] DataMAsterInCome = new byte[serialPort.BytesToRead];
                serialPort.Read(DataMAsterInCome, 0, serialPort.BytesToRead);

                LastResponse = DateTime.Now;

                if (this.fuelPoints[0].Status == Common.Enumerators.FuelPointStatusEnum.Offline)
                {
                    this.fuelPoints[0].LastStatus = this.fuelPoints[0].Status;
                    this.fuelPoints[0].Status = Common.Enumerators.FuelPointStatusEnum.Idle;
                    this.fuelPoints[0].DispenserStatus = this.fuelPoints[0].Status;
                    
                        this.DispenserStatusChanged(this, new FuelPointValuesArgs()
                        {
                            CurrentFuelPoint = this.fuelPoints[0],
                            CurrentNozzleId = -1,
                            Values = new FuelPointValues() { Status = this.fuelPoints[0].Status }
                        });
                    
                }

                if (DataMAsterInCome.Length < 2)
                    return;

                if (DataMAsterInCome[0] == 0x21)
                {
                    SendtoSlave(Commands.Acknowledge());
                }

                if (DataMAsterInCome.Length > 3)
                {
                    while ((DataMAsterInCome[0] == 0x21 || DataMAsterInCome[0] == 0x3F) && DataMAsterInCome[0] != 0x3A )
                    {
                        DataMAsterInCome = DataMAsterInCome.Skip(1).Take(DataMAsterInCome.Length - 1).ToArray();
                    }
                }

                Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + "  RX-> \t " + Encoding.ASCII.GetString(DataMAsterInCome));
             


                if (DataMAsterInCome.Length < 2)
                    return;
                else if (DataMAsterInCome.Length > 2)
                {
                    EvalData(DataMAsterInCome);
                }

            }
            catch (Exception ex)
            {

                if (File.Exists(this.LogPath()))
                {
                    string str = this.LogPath();
                    DateTime now = DateTime.Now;
                    File.AppendAllText(str, string.Concat(now.ToString("dd-MM HH:mm:ss.fff"), " EvalTotals -> ", ex.ToString(), "\r\n"));
                }
            }
        }

        DateTime LastResponse;
        private void EvalData(byte[] buf)
        {
            try
            {
                if (File.Exists(this.LogPath()))
                {
                    string str = this.LogPath();
                    DateTime now = DateTime.Now;
                    File.AppendAllText(str, string.Concat(now.ToString("dd-MM HH:mm:ss.fff"), " EvalData -> ", Encoding.ASCII.GetString(buf), "\r\n"));
                }
                switch (buf[1])
                {

                    case 0x4F:
                        SendtoSlave(Commands.Acknowledge());
                        if (this.fuelPoints[0].Status != Common.Enumerators.FuelPointStatusEnum.Idle)
                        {
                            this.fuelPoints[0].Status = Common.Enumerators.FuelPointStatusEnum.Idle;
                            this.fuelPoints[0].DispenserStatus = Common.Enumerators.FuelPointStatusEnum.Idle;
                            if (this.DispenserStatusChanged != null)
                            {
                                this.DispenserStatusChanged(this, new FuelPointValuesArgs()
                                {
                                    CurrentFuelPoint = this.fuelPoints[0],
                                    CurrentNozzleId = -1,
                                    Values = new FuelPointValues() { Status = this.fuelPoints[0].Status }
                                });
                            }
                        }

                        break;
                    //GetTotals
                    case 0x57:
                        if (File.Exists(this.LogPath()))
                        {
                            string str = this.LogPath();
                            DateTime now = DateTime.Now;
                            File.AppendAllText(str, string.Concat(now.ToString("dd-MM HH:mm:ss.fff"), " RecieveBuffer -> ", Encoding.ASCII.GetString(buf), "\r\n"));
                        }
                        SendtoSlave(Commands.Acknowledge());
                        EvalTotals(buf);
                        break;
                    //GetDisplay
                    case 0x45:
                    case 0x65:
                        //SendtoSlave(Commands.Acknowledge());
                        EvalDisplay(buf);
                        PendingDisplay = false;
                        break;
                    case 0X41:
                    case 0x61:
                        SendtoSlave(Commands.Acknowledge());
                        this.fuelPoints[0].Status = Common.Enumerators.FuelPointStatusEnum.Nozzle;
                        this.fuelPoints[0].DispenserStatus = this.fuelPoints[0].Status;
                        this.fuelPoints[0].QueryAuthorize = true;
                        int nozDat = buf[2] - 48;
                        this.fuelPoints[0].ActiveNozzle = this.fuelPoints[0].Nozzles[nozDat - 1];

                        if (this.DispenserStatusChanged != null)
                        {
                            this.DispenserStatusChanged(this, new FuelPointValuesArgs()
                            {
                                CurrentFuelPoint = this.fuelPoints[0],
                                CurrentNozzleId = this.fuelPoints[0].ActiveNozzleIndex,
                                Values = new FuelPointValues() { Status = this.fuelPoints[0].Status }
                            });
                        }
                       

                        break;
                    case 0x63:
                    case 0x43:
                        SendtoSlave(Commands.Acknowledge());
                        this.fuelPoints[0].Status = Common.Enumerators.FuelPointStatusEnum.Work;
                        this.fuelPoints[0].DispenserStatus = Common.Enumerators.FuelPointStatusEnum.Work;
                        if (this.DispenserStatusChanged != null)
                        {
                            this.DispenserStatusChanged(this, new FuelPointValuesArgs()
                            {
                                CurrentFuelPoint = this.fuelPoints[0],
                                CurrentNozzleId = this.fuelPoints[0].ActiveNozzleIndex,
                                Values = new FuelPointValues() { Status = this.fuelPoints[0].Status }
                            });
                        }

                        break;
                    case 0x44:
                        SendtoSlave(Commands.Acknowledge());
                        if (this.fuelPoints[0].Status == Common.Enumerators.FuelPointStatusEnum.Work || this.fuelPoints[0].Status == Common.Enumerators.FuelPointStatusEnum.Nozzle)
                        {
                            this.fuelPoints[0].Status = Common.Enumerators.FuelPointStatusEnum.Idle;
                            this.fuelPoints[0].DispenserStatus = this.fuelPoints[0].Status;
                        }
                        this.fuelPoints[0].Nozzles[this.fuelPoints[0].ActiveNozzle.Index - 1].QueryTotals = true;
                        this.fuelPoints[0].ActiveNozzleIndex = -1;
                        if (this.DispenserStatusChanged != null)
                        {
                            this.DispenserStatusChanged(this, new FuelPointValuesArgs()
                            {
                                CurrentFuelPoint = this.fuelPoints[0],
                                CurrentNozzleId = -1,
                                Values = new FuelPointValues() { Status = this.fuelPoints[0].Status }
                            });
                        }

                        break;

                }
            }
            catch
            {

            }
        }



        #region Methods
        public string LogPath()
        {
            return string.Concat("Petrotec_", this.CommunicationPort, ".log");
        }
        private void SendtoSlave(byte[] data)
        {
            try
            {

                this.serialPort.Write(data, 0, data.Length);

                if (File.Exists(this.LogPath()))
                {
                    string str = this.LogPath();
                    DateTime now = DateTime.Now;
                    File.AppendAllText(str, string.Concat(now.ToString("dd-MM HH:mm:ss.fff"), " RecieveBuffer -> ", Encoding.ASCII.GetString(data), "\r\n"));
                }

                Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + "  TX-> \t " + Encoding.ASCII.GetString(data));

            }
            catch (Exception ex)
            {

            }
        }

        private void AuthorizeFuelPoint(FuelPoint fp)
        {
            try
            {
                SendtoSlave(Commands.Authorise(fp.ActiveNozzleIndex, fp.Nozzles[fp.ActiveNozzleIndex].UntiPriceInt));
            }
            catch (Exception ex)
            {

            }

        }

        private void GetTotals(Nozzle nz)
        {
            SendtoSlave(Commands.VolumeTotal(nz.Index));
        }

        private void Halt(FuelPoint fp)
        {
            SendtoSlave(Commands.Halt());
        }

        bool PendingDisplay = false;

        private void EvalDisplay(byte[] buffer)
        {
            try
            {

                if (File.Exists(this.LogPath()))
                {
                    string str = this.LogPath();
                    DateTime now = DateTime.Now;
                    File.AppendAllText(str, string.Concat(now.ToString("dd-MM HH:mm:ss.fff"), " RecieveBuffer -> ", Encoding.ASCII.GetString(buffer), "\r\n"));
                }
                string volString = "", upString = "";
                string[] parms = null;
                int NozzleNum = 0;
                //:E000120001937D
                if (buffer[1] == 69)
                {
                    parms = BitConverter.ToString(buffer).Split('-');
                    volString =

                             parms[2].Substring(1)
                            + parms[3].Substring(1)
                            + parms[4].Substring(1)
                            + parms[5].Substring(1)
                            + parms[6].Substring(1);

                    upString =
                        parms[7].Substring(1) +
                         parms[8].Substring(1)
                        + parms[9].Substring(1)
                        + parms[10].Substring(1)
                        + parms[11].Substring(1);

                    NozzleNum = int.Parse(parms[12]) - 31;
                }
                else if (buffer[1] == 101)
                {
                    //:e00000700000915A
                    parms = BitConverter.ToString(buffer).Split('-');
                    volString =
                             parms[2].Substring(1)
                            + parms[3].Substring(1)
                            + parms[4].Substring(1)
                            + parms[5].Substring(1)
                            + parms[6].Substring(1)
                            + parms[7].Substring(1);

                    upString =
                         parms[8].Substring(1)
                        + parms[9].Substring(1)
                        + parms[10].Substring(1)
                        + parms[11].Substring(1)
                        + parms[12].Substring(1)
                        + parms[13].Substring(1);
                    NozzleNum = int.Parse(parms[14]) - 31;
                }

                try
                {
                    this.fuelPoints[0].DispensedAmount = decimal.Parse(upString) / 100;
                    this.fuelPoints[0].DispensedVolume = decimal.Parse(volString) / 100;

                    if (File.Exists(this.LogPath()))
                    {
                        string str = this.LogPath();
                        DateTime now = DateTime.Now;
                        File.AppendAllText(str, string.Concat(now.ToString("dd-MM HH:mm:ss.fff"), " " +
                            "EvalDisplay Data -> ",
                            "values.CurrentSalePrice = " + this.fuelPoints[0].ActiveNozzle.UnitPrice.ToString("N3") + "\r\n" +
                            "values.CurrentPriceTotal = " + this.fuelPoints[0].DispensedAmount.ToString("N2") + "\r\n" +
                            "values.CurrentVolume = " + this.fuelPoints[0].DispensedVolume.ToString("N2") + "\r\n" +
                            "CurrentNozzleId= " + this.fuelPoints[0].ActiveNozzle.Index.ToString() + "\r\n" +
                            "this.fuelPoints[0].Nozzles[NozzleNum].ParentFuelPoint.DispensedAmount= " + (decimal.Parse(upString) / 100).ToString("N3") + "\r\n" +
                            "this.fuelPoints[0].Nozzles[NozzleNum].ParentFuelPoint.DispensedVolume= " + (decimal.Parse(volString) / 100).ToString("N3")

                            , "\r\n"));
                    }

                    if (this.DataChanged != null)
                    {

                        FuelPointValues values = new FuelPointValues();
                    values.CurrentSalePrice = this.fuelPoints[0].ActiveNozzle.UnitPrice;
                    values.CurrentPriceTotal = this.fuelPoints[0].DispensedAmount;
                    values.CurrentVolume = this.fuelPoints[0].DispensedVolume;

                    this.DataChanged(this, new FuelPointValuesArgs()
                    {
                        CurrentFuelPoint = this.fuelPoints[0],
                        CurrentNozzleId = this.fuelPoints[0].ActiveNozzle.Index,
                        Values = values
                    });
                    }

                }
                catch(Exception ex)
                {
                    if (File.Exists(this.LogPath()))
                    {
                        string str = this.LogPath();
                        DateTime now = DateTime.Now;
                        File.AppendAllText(str, string.Concat(now.ToString("dd-MM HH:mm:ss.fff"), " Error EvalDisplay:: DataChanged Event -> ", ex.ToString(), "\r\n"));
                    }
                }

                Thread.Sleep(50);
            }
            catch(Exception ex)
            {
                if (File.Exists(this.LogPath()))
                {
                    string str = this.LogPath();
                    DateTime now = DateTime.Now;
                    File.AppendAllText(str, string.Concat(now.ToString("dd-MM HH:mm:ss.fff"), " Error EvalDisplay -> ", ex.ToString(), "\r\n"));
                }
            }
        }
              
            

        private void EvalTotals(byte[] buffer)
        {

            try
            {

                int NozzleNum = buffer[2] - 48;
                string[] strArrays1 = BitConverter.ToString(buffer).Split(new char[] { '-' });
                string str1 = string.Concat(new string[] { strArrays1[3].Substring(1), strArrays1[4].Substring(1), strArrays1[5].Substring(1), strArrays1[6].Substring(1), strArrays1[7].Substring(1), strArrays1[8].Substring(1), strArrays1[9].Substring(1), strArrays1[10].Substring(1) });
                
                Nozzle nz = (from a in this.fuelPoints[0].Nozzles where a.Index.Equals(NozzleNum) select a).SingleOrDefault();
                nz.TotalVolume = decimal.Parse(str1);

                Console.WriteLine("Nozzle=" + NozzleNum + "    |     VolumeTotalizer= " + decimal.Parse(str1).ToString("N2"));
                
                if (this.TotalsRecieved != null)
                {
                    this.TotalsRecieved(this, new TotalsEventArgs(this.fuelPoints[0], NozzleNum, decimal.Parse(str1), 0));
                }
                nz.QueryTotals = false;

            }
            catch(Exception ex)
            {
                if (File.Exists(this.LogPath()))
                {
                    string str = this.LogPath();
                    DateTime now = DateTime.Now;
                    File.AppendAllText(str, string.Concat(now.ToString("dd-MM HH:mm:ss.fff"), " Eval Totals Exception -> ", ex.ToString(), "\r\n"));
                }
            }

        }
        #endregion


        private DateTime dtLastHalt;

        #region Thread

        private void WorkFlow()
        {
            try
            {
                while (true)
                {
                    while (IsConnected)
                    {
                        try
                        {
                            foreach (FuelPoint fp in this.fuelPoints)
                            {
                              
                                if (fp.LastStatus == Common.Enumerators.FuelPointStatusEnum.Offline && fp.Status == Common.Enumerators.FuelPointStatusEnum.Idle)
                                {

                                    foreach (Nozzle nz in fp.Nozzles)
                                    {
                                        nz.QueryTotals = true;
                                    }
                                    fp.Initialized = true;
                                  
                                    fp.LastStatus = fp.Status;                                   
                                    this.fuelPoints[0].DispenserStatus = fp.Status;

                                    if (this.DispenserStatusChanged != null)
                                    {
                                        this.DispenserStatusChanged(this, new FuelPointValuesArgs()
                                        {
                                            CurrentFuelPoint = fp,
                                            CurrentNozzleId = -1,
                                            Values = new FuelPointValues() { Status = Common.Enumerators.FuelPointStatusEnum.Idle }
                                        });
                                    }
                                }

                                if ((DateTime.Now - LastResponse).TotalSeconds > 10)
                                {

                                    fp.Status = Common.Enumerators.FuelPointStatusEnum.Offline;
                                    fp.DispenserStatus = fp.Status;
                                    if (this.DispenserStatusChanged != null)
                                    {
                                        this.DispenserStatusChanged(this, new FuelPointValuesArgs()
                                        {
                                            CurrentFuelPoint = fp,
                                            Values = new FuelPointValues() { Status = fp.Status }
                                        });
                                    }
                                }

                                if (fp.Status == Common.Enumerators.FuelPointStatusEnum.Offline)
                                {

                                    SendtoSlave(Commands.Halt());
                                    Thread.Sleep(250);
                                    SendtoSlave(Commands.PowerONOFF());
                                    Thread.Sleep(250);
                                    SendtoSlave(Commands.Halt());
                                    Thread.Sleep(250);
                                    SendtoSlave(Commands.PowerONOFF());
                                    Thread.Sleep(250);
                                }

                                if (fp.Status == Common.Enumerators.FuelPointStatusEnum.Work)
                                {
                                    if (!PendingDisplay)
                                    {
                                        SendtoSlave(Commands.GetDisplay(1));
                                        Thread.Sleep(1000);
                                        PendingDisplay = true;
                                    }
                                }

                                //Authorise
                                if (fp.QueryAuthorize)
                                {
                                    this.AuthorizeFuelPoint(fp);
                                    PendingDisplay = false;
                                    fp.QueryAuthorize = false;
                                }

                                Thread.Sleep(50);


                                //GetTotals
                                foreach (Nozzle nz in fp.Nozzles)
                                {
                                    if (nz.QueryTotals)
                                    {
                                        this.GetTotals(nz);                                        
                                        Thread.Sleep(1250);
                                        this.GetTotals(nz);
                                        Thread.Sleep(1250);
                                        nz.QueryTotals = false;
                                    }
                                }


                                if (fp.QueryHalt)
                                    SendtoSlave(Commands.Halt());


                                Thread.Sleep(50);

                                if ((DateTime.Now - dtLastHalt).TotalSeconds > 2 && fp.Status == Common.Enumerators.FuelPointStatusEnum.Idle)
                                {
                                    SendtoSlave(Commands.Halt());
                                    Thread.Sleep(100);
                                    SendtoSlave(Commands.PowerONOFF());
                                    Thread.Sleep(100);
                                    dtLastHalt = DateTime.Now;
                                }

                                Thread.Sleep(100);

                                if (fp.Status == Common.Enumerators.FuelPointStatusEnum.Nozzle)//|| fp.Status == Common.Enumerators.FuelPointStatusEnum.Work)
                                    SendtoSlave(Commands.PowerONOFF());

                                Thread.Sleep(50);
                            }
                        }
                        finally
                        {
                            Thread.Sleep(250);
                        }
                        Thread.Sleep(200);
                    }
                    Thread.Sleep(200);
                }
            }

            catch(Exception ex)
            {

            }
        }
        #endregion
    }
}

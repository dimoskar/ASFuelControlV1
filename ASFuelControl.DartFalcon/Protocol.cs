using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Ports;
using ASFuelControl.Common;
using ASFuelControl.Common.Enumerators;
using System.IO;

namespace ASFuelControl.DartFalcon
{
    public class Controller : Common.FuelPumpControllerBase
    {
        
        public Controller()
        {
            this.ControllerType = ControllerTypeEnum.DartFalcon;
            this.Controller = new DartFalconProtocol();
        }

    }
    public class DartFalconProtocol : Common.IFuelProtocol
    {
        #region Basics
        public event EventHandler<Common.FuelPointValuesArgs> DataChanged;
        public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;
        public event EventHandler<Common.FuelPointValuesArgs> DispenserStatusChanged;
        public event EventHandler<SaleEventArgs> SaleRecieved;
        public event EventHandler DispenserOffline;

        private List<Common.FuelPoint> fuelPoints = new List<Common.FuelPoint>();

        private SerialPort serialPort = new SerialPort();
        private Thread th;

        public DebugValues foo = new Common.DebugValues();
        public FuelPoint[] FuelPoints
        {
            get
            {
                return this.fuelPoints.ToArray();
            }
            set
            {
                this.fuelPoints = new List<Common.FuelPoint>(value);

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
            set;
            get;
        }
        public void Connect()
        {
            try
            {
                this.serialPort.PortName = this.CommunicationPort;
                this.serialPort.Parity = Parity.Odd;
                this.serialPort.BaudRate = 9600;
                this.serialPort.Open();
                this.th = new Thread(this.WorkFlow);
                th.Start();
            }
            catch
            {
            }
        }
        public void Disconnect()
        {
            if (this.serialPort.IsOpen)
                this.serialPort.Close();
        }
        public void AddFuelPoint(Common.FuelPoint fp)
        {
            this.fuelPoints.Add(fp);
        }
        public void ClearFuelPoints()
        {
            this.fuelPoints.Clear();
        }
        public Common.DebugValues DebugStatusDialog(Common.FuelPoint fp)
        {
            foo = null;
            //GetStatus(fp);
            foo.Status = fp.Status;
            return foo;
        }
        #endregion

        #region Workflow Dispensers

        private void WorkFlow()
        {
            try
            {

                foreach (FuelPoint fp in this.fuelPoints)
                {
                    foreach (Nozzle nz in fp.Nozzles)
                    {
                        nz.QueryTotals = true;
                    }
                    fp.QuerySetPrice = true;
                    fp.SetExtendedProperty("LastValidResponse", DateTime.Now.AddSeconds(-10));
                }
                while (this.IsConnected)
                {
                    try
                    {
                        foreach (FuelPoint fp in this.fuelPoints.OrderBy(x => x.Address))
                        {

                            DateTime dtLastPoll = (DateTime)fp.GetExtendedProperty("LastValidResponse");

                            if ((DateTime.Now - dtLastPoll).TotalSeconds > 8)
                            {
                                fp.Status = FuelPointStatusEnum.Offline;
                                fp.DispenserStatus = FuelPointStatusEnum.Offline;
                                this.DataChanged(this, new FuelPointValuesArgs()
                                {
                                    CurrentFuelPoint = fp,
                                    CurrentNozzleId = 1,
                                    Values = new FuelPointValues()
                                    {
                                        Status = fp.Status
                                    }
                                });
                            }


                            if (fp.Status == FuelPointStatusEnum.Offline)
                            {
                                fp.SetExtendedProperty("dtGetStatus_" + fp.Address, DateTime.Now);
                                fp.Status = FuelPointStatusEnum.Idle;
                                fp.DispenserStatus = FuelPointStatusEnum.Idle;
                                fp.QuerySetPrice = true;

                            }

                            if (fp.Status != fp.LastStatus)
                            {
                                fp.LastStatus = fp.Status;
                            }

                            foreach (Nozzle nz in fp.Nozzles.OrderBy(x => x.Index))
                            {

                                if (nz.QueryTotals)
                                {
                                    GetTotals(nz);
                                }
                            }

                            if (fp.QuerySetPrice)
                            {
                                this.SetPrice(fp);
                                Thread.Sleep(80);
                                this.SetPrice(fp);
                                Thread.Sleep(80);

                            }
                            if (fp.QueryHalt || fp.QueryStop)
                            {
                                this.StopPump(fp);
                            }


                            //Authorise
                            if (fp.QueryAuthorize)
                            {
                                SetPrice(fp);

                                Thread.Sleep(25);
                                for (int i = 0; i <= 1; i++)
                                {
                                    this.Authorize(fp);
                                }

                                if (fp.Status != FuelPointStatusEnum.Nozzle)
                                {
                                    fp.Status = FuelPointStatusEnum.Ready;
                                    fp.DispenserStatus = FuelPointStatusEnum.Ready;
                                }
                                fp.QueryAuthorize = false;

                            }

                            Poll(fp);
                            Thread.Sleep(75);

                        }
                    }
                    catch
                    {

                    }
                    finally
                    {
                        Thread.Sleep(60);
                    }
                }

            }
            catch
            {

            }
            finally
            {
                Thread.Sleep(50);
            }
        }
        private void Authorize(FuelPoint fp)
        {
            try
            {

                ExecuteSlave(Commands.Reset(fp.Address), fp);
                Thread.Sleep(50);
                byte[] numArray = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(numArray, 0, this.serialPort.BytesToRead);
                Logger(numArray);
                Thread.Sleep(25);

                ExecuteSlave(Commands.Poll(fp.Address), fp);
                Thread.Sleep(50);
                numArray = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(numArray, 0, this.serialPort.BytesToRead);
                Logger(numArray);

                Thread.Sleep(25);

                ExecuteSlave(Commands.Authorize(fp.Address), fp);
                Thread.Sleep(25);
                numArray = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(numArray, 0, this.serialPort.BytesToRead);
                Logger(numArray);
                Thread.Sleep(25);

                ExecuteSlave(Commands.Poll(fp.Address), fp);

                Thread.Sleep(25);
                numArray = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(numArray, 0, this.serialPort.BytesToRead);
                Logger(numArray);
                Thread.Sleep(50);
                fp.SetExtendedProperty("PumpAuthorized", true);
            }
            catch
            {

            }
        }
        private void StopPump(FuelPoint fp)
        {
            try
            {
                for (int i = 0; i <= 1; i++)
                {
                    byte[] data = Commands.Stop(fp.Address);
                    ExecuteSlave(data, fp);
                    Logger(data);
                    Thread.Sleep(50);
                    byte[] numArray = new byte[this.serialPort.BytesToRead];
                    this.serialPort.Read(numArray, 0, this.serialPort.BytesToRead);
                    Logger(numArray);
                    fp.QuerySetPrice = false;

                }
            }
            catch
            {

            }
        }

        private void SetPrice(FuelPoint fp)
        {
            try
            {
                byte[] data = Commands.SetPrice(fp);//, decimal.Parse((nz.UntiPriceInt).ToString().PadLeft(4,'0')));
                if (data == null || data.Length == 0)
                    return;
                ExecuteSlave(data, fp);
                Thread.Sleep(50);
                byte[] numArray = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(numArray, 0, this.serialPort.BytesToRead);

                if (numArray.Length < 3 || fp.Status == FuelPointStatusEnum.Offline)
                    return;

                fp.QuerySetPrice = false;

            }
            catch (Exception ex)
            {
                LoggerException(ex.ToString());
            }
        }

        private void ExecuteSlave(byte[] buffer, FuelPoint fp)
        {
            try
            {
                if (buffer == null || buffer.Length == 0)
                    return;
                if (this.serialPort.IsOpen)
                {
                    this.serialPort.Write(buffer, 0, buffer.Length);
                }
                Logger(buffer);

            }
            catch (Exception ex)
            {
                LoggerException(ex.ToString());
            }
        }

        public enum PendingCommand_Dart
        {
            None,
            GetStatus,
            GetTotals,
            Authorise,
            Reset,
            Stop,
            Resume,
            AllowedNozzle
        }

        public void Logger(byte[] buf)
        {
            string fileName = "dart_" + this.serialPort.PortName + ".txt";

            if (File.Exists(fileName))
            {
                using (StreamWriter writer = new StreamWriter(fileName, true, Encoding.UTF8))
                {
                    writer.Write(DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.fff") + " \t" + BitConverter.ToString(buf) + "  " + "\r\n" /*+ Error_Recieve.ToString() + "\r\n\r\n"*/);
                }
            }
        }

        public void LoggerException(string txt)
        {
            string fileName = "dart_" + this.serialPort.PortName + ".txt";
            if (File.Exists(fileName))
            {

                using (StreamWriter writer = new StreamWriter(fileName, true, Encoding.UTF8))
                {
                    writer.Write(DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.fff") + " \t" + txt + "  " + "\r\n" /*+ Error_Recieve.ToString() + "\r\n\r\n"*/);
                }
            }
        }
        public void LoggerExtra(string txt)
        {
            string fileName = "dartextra_" + this.serialPort.PortName + ".txt";
            if (File.Exists(fileName))
            {
                using (StreamWriter writer = new StreamWriter(fileName, true, Encoding.UTF8))
                {
                    writer.Write(DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.fff") + " \t" + txt + "  " + "\r\n" /*+ Error_Recieve.ToString() + "\r\n\r\n"*/);
                }
            }
        }
        #endregion

        #region Methods

        private void Poll(FuelPoint fp)
        {
            try
            {
                DateTime dtGetStatus = (DateTime)fp.GetExtendedProperty("dtGetStatus_" + fp.Address, DateTime.Now);


                if ((DateTime.Now - dtGetStatus).TotalSeconds > 2)
                {
                    fp.SetExtendedProperty("dtGetStatus_" + fp.Address, DateTime.Now);
                    ExecuteSlave(Commands.GetStatus(fp.Address), fp);

                    Thread.Sleep(75);
                    byte[] buf = new byte[this.serialPort.BytesToRead];
                    this.serialPort.Read(buf, 0, this.serialPort.BytesToRead);
                    Logger(buf);
                }

                ExecuteSlave(Commands.Poll(fp.Address), fp);
                Thread.Sleep(80);
                byte[] numArray = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(numArray, 0, this.serialPort.BytesToRead);
                Logger(numArray);
                if (numArray.Length < 3)
                    return;



                //fp.dtLastResponse = DateTime.Now;



                if (numArray.Length >= 3)
                {
                    fp.SetExtendedProperty("LastValidResponse", DateTime.Now);
                    AnalyzeDart(numArray, fp);
                }
            }
            catch (Exception ex)
            {
                LoggerException("Poll Error " + ex.Message);
                //Base.Logger.ProtocolErrror(ex.ToString(), "Poll");
            }
        }
        private void GetTotals(Nozzle nz)
        {
            try
            {
                ExecuteSlave(Commands.RequestVolumeTotalizer(nz.ParentFuelPoint.Address, nz.Index), nz.ParentFuelPoint);
                Thread.Sleep(75);
                byte[] numArray = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(numArray, 0, this.serialPort.BytesToRead);
                Logger(numArray);
                Thread.Sleep(50);

                ExecuteSlave(Commands.Poll(nz.ParentFuelPoint.Address), nz.ParentFuelPoint);

                Thread.Sleep(200);

                numArray = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(numArray, 0, this.serialPort.BytesToRead);
                Logger(numArray);
                string volBuf = "";

                if (numArray[2] == 0x05 && numArray[5] == 0x65)
                {
                    volBuf = BitConverter.ToString(numArray.Skip(8).Take(5).ToArray());
                }
                else
                    volBuf = BitConverter.ToString(numArray.Skip(5).Take(5).ToArray());


                decimal VolumeTotalizer = decimal.Parse(volBuf.Replace("-", null));

                //fp.Nozzles[0].NeedTotalizer = false;
                var prevTotals = nz.TotalVolume;
                if (VolumeTotalizer == 0)
                    nz.TotalVolume = 0.01M;
                else
                    nz.TotalVolume = VolumeTotalizer;

                ExecuteSlave(Commands.ACK(nz.ParentFuelPoint.Address), nz.ParentFuelPoint);
                numArray = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(numArray, 0, this.serialPort.BytesToRead);
                Logger(numArray);

                nz.ParentFuelPoint.Initialized = true;
                nz.QueryTotals = false;

                if (prevTotals == nz.TotalVolume)
                {
                    LoggerExtra("TOTALS ARE THE SAME");
                    LoggerExtra(string.Format("DISPENCED AMOUNT:{1}, DISPENSED VOLUME: {0}", nz.ParentFuelPoint.DispensedVolume, nz.ParentFuelPoint.DispensedAmount));
                    //return;
                }

                this.TotalsRecieved(this, new Common.TotalsEventArgs(nz.ParentFuelPoint, nz.Index, nz.TotalVolume, nz.TotalPrice));


            }
            catch (Exception ex)
            {
                LoggerException("Get Totals Error " + ex.Message);
            }
        }

        private static decimal ParsePackedBcd(byte[] data)
        {
            if (data == null || data.Length == 0)
                return 0M;

            return decimal.Parse(BitConverter.ToString(data).Replace("-", string.Empty));
        }

        private static bool IsNozzleOut(byte nozzleStatus)
        {
            return (nozzleStatus & 0x10) == 0x10;
        }

        private static int? GetSelectedNozzleIndex(FuelPoint fp, byte nozzleStatus)
        {
            int nozzleNumber = nozzleStatus & 0x0F;
            if (nozzleNumber <= 0 || nozzleNumber > fp.NozzleCount)
                return null;

            return nozzleNumber - 1;
        }

        private Nozzle GetCurrentTransactionNozzle(FuelPoint fp, int? nozzleIndex = null)
        {
            if (nozzleIndex.HasValue && nozzleIndex.Value >= 0 && nozzleIndex.Value < fp.Nozzles.Length)
                return fp.Nozzles[nozzleIndex.Value];

            if (fp.ActiveNozzle != null)
                return fp.ActiveNozzle;

            if (fp.LastActiveNozzle != null)
                return fp.LastActiveNozzle;

            return fp.Nozzles.FirstOrDefault();
        }

        private void PublishStatusData(FuelPoint fp, int currentNozzleId, int activeNozzle)
        {
            if (this.DataChanged == null)
                return;

            this.DataChanged(this, new FuelPointValuesArgs()
            {
                CurrentFuelPoint = fp,
                CurrentNozzleId = currentNozzleId,
                Values = new FuelPointValues()
                {
                    Status = fp.Status,
                    ActiveNozzle = activeNozzle
                }
            });
        }

        private void UpdateLiveSaleData(FuelPoint fp, byte[] clearData, Nozzle nozzle)
        {
            if (clearData == null || clearData.Length < 8 || nozzle == null)
                return;

            fp.DispensedAmount = ParsePackedBcd(clearData.Skip(4).Take(4).ToArray()) / (decimal)Math.Pow(10, fp.AmountDecimalPlaces);
            fp.DispensedVolume = ParsePackedBcd(clearData.Take(4).ToArray()) / (decimal)Math.Pow(10, fp.VolumeDecimalPlaces);
            fp.ActiveNozzleIndex = nozzle.Index - 1;

            if (fp.DispensedAmount > 0 && this.DataChanged != null)
            {
                Common.FuelPointValues values = new Common.FuelPointValues();
                values.CurrentSalePrice = nozzle.UnitPrice;
                values.CurrentPriceTotal = fp.DispensedAmount;
                values.CurrentVolume = fp.DispensedVolume;
                values.ActiveNozzle = nozzle.Index - 1;

                this.DataChanged(this, new Common.FuelPointValuesArgs()
                {
                    CurrentFuelPoint = fp,
                    CurrentNozzleId = nozzle.Index,
                    Values = values
                });
            }
        }

        private void UpdatePriceFromDc3(FuelPoint fp, byte[] clearData)
        {
            if (clearData == null || clearData.Length < 4)
                return;

            int? nozzleIndex = GetSelectedNozzleIndex(fp, clearData[3]);
            Nozzle nozzle = GetCurrentTransactionNozzle(fp, nozzleIndex);
            if (nozzle == null)
                return;

            int unitPriceInt = (int)ParsePackedBcd(clearData.Take(3).ToArray());
            nozzle.UntiPriceInt = unitPriceInt;
            nozzle.UnitPrice = unitPriceInt / (decimal)Math.Pow(10, fp.UnitPriceDecimalPlaces);
        }

        private void HandleStatusTransaction(FuelPoint fp, byte[] clearData)
        {
            if (clearData == null || clearData.Length != 1)
                return;

            byte pumpStatus = clearData[0];
            Nozzle currentNozzle = GetCurrentTransactionNozzle(fp);

            switch (pumpStatus)
            {
                case 0x00:
                case 0x01:
                    fp.SetExtendedProperty("PumpAuthorized", false);
                    fp.Status = FuelPointStatusEnum.Idle;
                    fp.DispenserStatus = FuelPointStatusEnum.Idle;
                    break;
                case 0x02:
                    fp.SetExtendedProperty("PumpAuthorized", true);
                    if (fp.Status != FuelPointStatusEnum.Nozzle && fp.Status != FuelPointStatusEnum.Work)
                    {
                        fp.Status = FuelPointStatusEnum.Ready;
                        fp.DispenserStatus = FuelPointStatusEnum.Ready;
                    }
                    break;
                case 0x04:
                    fp.SetExtendedProperty("PumpAuthorized", true);
                    fp.Status = FuelPointStatusEnum.Work;
                    fp.DispenserStatus = FuelPointStatusEnum.Work;
                    if (currentNozzle != null)
                        PublishStatusData(fp, currentNozzle.Index, currentNozzle.Index - 1);
                    break;
                case 0x05:
                case 0x06:
                    fp.SetExtendedProperty("PumpAuthorized", false);
                    fp.Status = FuelPointStatusEnum.TransactionCompleted;
                    fp.DispenserStatus = FuelPointStatusEnum.TransactionCompleted;
                    break;
                case 0x07:
                    fp.SetExtendedProperty("PumpAuthorized", false);
                    fp.Status = FuelPointStatusEnum.Close;
                    fp.DispenserStatus = FuelPointStatusEnum.Close;
                    fp.ActiveNozzleIndex = -1;
                    break;
            }
        }

        private void HandleFilledVolumeAmount(FuelPoint fp, byte[] clearData)
        {
            if (clearData == null || clearData.Length < 8 || fp.Status == FuelPointStatusEnum.Offline)
                return;

            bool allowUpdate =
                (bool)fp.GetExtendedProperty("PumpAuthorized", false) ||
                fp.Status == FuelPointStatusEnum.Nozzle ||
                fp.Status == FuelPointStatusEnum.Ready ||
                fp.Status == FuelPointStatusEnum.Work ||
                fp.Status == FuelPointStatusEnum.TransactionCompleted ||
                fp.Status == FuelPointStatusEnum.TransactionStopped;

            if (!allowUpdate)
                return;

            Nozzle nozzle = GetCurrentTransactionNozzle(fp);
            UpdateLiveSaleData(fp, clearData, nozzle);
        }

        private void RefreshCompletedSaleData(FuelPoint fp)
        {
            for (int i = 1; i <= 4; i++)
            {
                ExecuteSlave(Commands.GetDisplay(fp.Address), fp);
                Thread.Sleep(50);
                byte[] response = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
                Logger(response);

                ExecuteSlave(Commands.Poll(fp.Address), fp);
                Thread.Sleep(50);
                response = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
                Logger(response);

                ExecuteSlave(Commands.Poll(fp.Address), fp);
                Thread.Sleep(125);
                response = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(response, 0, this.serialPort.BytesToRead);
                Logger(response);

                if (response.Length >= 12 && response[2] == 0x02 && response[3] == 0x08)
                {
                    Nozzle nozzle = GetCurrentTransactionNozzle(fp);
                    UpdateLiveSaleData(fp, response.Skip(4).Take(8).ToArray(), nozzle);
                }

                ExecuteSlave(Commands.ACK(fp.Address), fp);

                Thread.Sleep(25);
                byte[] clearBuf = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(clearBuf, 0, this.serialPort.BytesToRead);
            }

            if (fp.DispensedAmount <= 0.01M)
            {
                ExecuteSlave(Commands.Stop(fp.Address), fp);
                Thread.Sleep(50);
                byte[] stopResponse = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(stopResponse, 0, this.serialPort.BytesToRead);

                Thread.Sleep(50);

                ExecuteSlave(Commands.Reset(fp.Address), fp);
                Thread.Sleep(80);
                byte[] resetResponse = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(resetResponse, 0, this.serialPort.BytesToRead);
            }
        }

        private void HandleNozzleStatusAndPrice(FuelPoint fp, byte[] clearData)
        {
            if (clearData == null || clearData.Length < 4)
                return;

            UpdatePriceFromDc3(fp, clearData);

            byte nozzleStatus = clearData[3];
            bool nozzleOut = IsNozzleOut(nozzleStatus);
            int? selectedNozzleIndex = GetSelectedNozzleIndex(fp, nozzleStatus);

            if (!nozzleOut)
            {
                if (fp.Status == FuelPointStatusEnum.Offline)
                {
                    ExecuteSlave(Commands.Reset(fp.Address), fp);
                    Thread.Sleep(80);
                    byte[] clearBuf = new byte[this.serialPort.BytesToRead];
                    this.serialPort.Read(clearBuf, 0, this.serialPort.BytesToRead);

                    Thread.Sleep(25);
                    foreach (Nozzle nz in fp.Nozzles)
                    {
                        ExecuteSlave(Commands.AllowedNozzle(nz.ParentFuelPoint.Address, nz.Index), fp);
                        Thread.Sleep(25);
                        byte[] allowedNozzleResponse = new byte[this.serialPort.BytesToRead];
                        this.serialPort.Read(allowedNozzleResponse, 0, this.serialPort.BytesToRead);
                        Thread.Sleep(50);
                    }

                    fp.SetExtendedProperty("dtGetStatus_" + fp.Address, DateTime.Now);
                    fp.Status = FuelPointStatusEnum.Idle;
                    fp.DispenserStatus = FuelPointStatusEnum.Idle;
                }
                else if (fp.Status == FuelPointStatusEnum.Nozzle || fp.Status == FuelPointStatusEnum.Ready)
                {
                    fp.Status = FuelPointStatusEnum.Idle;
                    fp.DispenserStatus = FuelPointStatusEnum.Idle;
                    PublishStatusData(fp, 1, -1);
                }
                else if (fp.Status == FuelPointStatusEnum.Work ||
                         fp.Status == FuelPointStatusEnum.TransactionCompleted ||
                         fp.Status == FuelPointStatusEnum.TransactionStopped)
                {
                    try
                    {
                        if (selectedNozzleIndex.HasValue)
                            fp.ActiveNozzleIndex = selectedNozzleIndex.Value;

                        RefreshCompletedSaleData(fp);

                        fp.Status = FuelPointStatusEnum.Idle;
                        fp.DispenserStatus = FuelPointStatusEnum.Idle;
                        fp.ActiveNozzleIndex = -1;
                        PublishStatusData(fp, 1, -1);
                    }
                    catch (Exception ex)
                    {
                        LoggerException("Handle DC3 completion error " + ex.Message);
                    }
                }

                fp.SetExtendedProperty("PumpAuthorized", false);
                return;
            }

            if (selectedNozzleIndex.HasValue)
            {
                fp.ActiveNozzleIndex = selectedNozzleIndex.Value;

                if (fp.Status == FuelPointStatusEnum.Idle ||
                    fp.Status == FuelPointStatusEnum.Ready ||
                    fp.Status == FuelPointStatusEnum.TransactionCompleted ||
                    fp.Status == FuelPointStatusEnum.TransactionStopped)
                {
                    fp.Status = FuelPointStatusEnum.Nozzle;
                    fp.DispenserStatus = FuelPointStatusEnum.Nozzle;
                    PublishStatusData(fp, selectedNozzleIndex.Value + 1, selectedNozzleIndex.Value);
                }

                int nozzleNumber = selectedNozzleIndex.Value + 1;
                ExecuteSlave(Commands.AllowedNozzle(fp.Address, nozzleNumber), fp);
                byte[] allowedNozzleResponse = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(allowedNozzleResponse, 0, this.serialPort.BytesToRead);
            }
        }

        private void AnalyzeDart(byte[] buf, FuelPoint fp)
        {
            try
            {
                fp.LastStatus = fp.Status;

                int addressValid = fp.Address + 79;

                if (addressValid != buf[0] || (buf.Length == 3 && buf[1] == 0x70))
                    return;

                if (buf[0] >= 0x50 && buf[0] <= 0x5F)
                {

                    //Finalize Buff
                    if (buf.Length > 3)
                    {
                        ExecuteSlave(Commands.ACK(fp.Address), fp);
                        Thread.Sleep(75);
                        byte[] clearBuf = new byte[this.serialPort.BytesToRead];
                        this.serialPort.Read(clearBuf, 0, this.serialPort.BytesToRead);
                        Logger(clearBuf);

                    }


                    //int Skip = buf[1];

                    byte[] Cmd = buf.Skip(2).Take(buf.Length).ToArray();
                    //int DartLength = buf.Length;


                    while (Cmd.Length > 0)
                    {
                        if (Cmd.Length == 4 && Cmd[2] == 0x03 && Cmd[3] == 0xFA)
                        {
                            break;
                        }
                        if (Cmd.Length < 2)
                            break;

                        byte Data = Cmd[0];
                        int DataTake = Cmd[1];
                        int DataSkip = 2 + (int)Cmd[1];
                        if (Cmd.Length < DataSkip)
                            break;

                        byte[] ClearData = Cmd.Skip(2).Take(DataTake).ToArray();

                        switch (Data)
                        {
                            case 0x01:
                                HandleStatusTransaction(fp, ClearData);
                                break;
                            case 0x02:
                                HandleFilledVolumeAmount(fp, ClearData);
                                Thread.Sleep(5);
                                break;
                            case 0x03:
                                HandleNozzleStatusAndPrice(fp, ClearData);
                                break;

                        }
                        Cmd = Cmd.Skip(DataSkip).Take(Cmd.Length).ToArray();
                    }


                }

                fp.DispenserStatus = fp.Status;
                if (this.DispenserStatusChanged != null)
                    this.DispenserStatusChanged(this, new FuelPointValuesArgs()
                    {
                        CurrentFuelPoint = fp,
                        //CurrentNozzleId = 1,
                        Values = new FuelPointValues()
                        {
                            Status = fp.Status,

                        }
                    });

            }
            catch (Exception ex)
            {
                LoggerException("Analyze Error " + ex.Message);
                //Base.Logger.ProtocolErrror(ex.ToString(), "AnalyzeDart");
            }
        }

        /*
           if (this.TotalsRecieved != null)
           {
            this.TotalsRecieved(this, new Common.TotalsEventArgs(fp, nz.Index, nz.TotalVolume, nz.TotalPrice));
            }
         */

        #endregion
    }
}

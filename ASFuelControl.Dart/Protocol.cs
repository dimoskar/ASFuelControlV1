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

namespace ASFuelControl.Dart
{
    public class Controller : Common.FuelPumpControllerBase
    {
        
        public Controller()
        {
            this.ControllerType = ControllerTypeEnum.Dart;
            this.Controller = new DartProtocol();
        }

    }
    public class DartProtocol : Common.IFuelProtocol
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
                        foreach (FuelPoint fp in this.fuelPoints.OrderBy(x=>x.Address))
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

                            foreach (Nozzle nz in fp.Nozzles.OrderBy(x=>x.Index))
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
                            if(fp.QueryHalt || fp.QueryStop)
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

                                fp.Status = FuelPointStatusEnum.Work;
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
                if (buffer.Length < 0)
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

        public  void Logger(byte[] buf)
        {
            string fileName = "dart_"+ this.serialPort.PortName +".txt";

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
                int num = 0;
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
                int num = 0;
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

                if (numArray[2] == 0x05 && numArray[5]==0x65)
                {
                    volBuf = BitConverter.ToString(numArray.Skip(8).Take(5).ToArray());
                }
                else
                    volBuf = BitConverter.ToString(numArray.Skip(5).Take(5).ToArray());
                

                decimal VolumeTotalizer = decimal.Parse(volBuf.Replace("-", null)) ;

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


                    while ((Cmd.Length) > 0)
                    {
                        if (Cmd.Length == 4 && Cmd[2] == 0x03 && Cmd[3] == 0xFA)
                        {
                            Cmd = null;
                            break;
                        }
                        byte Data = Cmd[0];
                        int DataTake = Cmd[1];
                        int DataSkip = 2 + (int)Cmd[1];
                        byte[] ClearData = Cmd.Skip(2).Take(DataTake).ToArray();

                        switch (Data)
                        {
                            case 0x01:
                                {
                                    if (DataTake == 1)
                                    {
                                        if (Cmd[2] == 0x04 && fp.Status == FuelPointStatusEnum.Nozzle)
                                        {
                                            
                                            fp.Status = FuelPointStatusEnum.Work;
                                            fp.DispenserStatus = FuelPointStatusEnum.Work;
                                            this.DataChanged(this, new FuelPointValuesArgs()
                                            {
                                                CurrentFuelPoint = fp,
                                                CurrentNozzleId = 1,

                                                Values = new FuelPointValues()
                                                {
                                                    Status = fp.Status = FuelPointStatusEnum.Idle,
                                                    ActiveNozzle = 0
                                                }

                                            });

                                        }
                                        else if (Cmd[2] == 0x05)
                                        {
                                            if (fp.Status == FuelPointStatusEnum.Offline)
                                            {
                                                
                                                fp.Status = FuelPointStatusEnum.Idle;
                                                fp.DispenserStatus = FuelPointStatusEnum.Idle;
                                                fp.ActiveNozzleIndex = -1;
                                                this.DataChanged(this, new FuelPointValuesArgs()
                                                {
                                                    CurrentFuelPoint = fp,
                                                    CurrentNozzleId = 1,
                                                    Values = new FuelPointValues()
                                                    {
                                                        Status = fp.Status = FuelPointStatusEnum.Idle,
                                                        ActiveNozzle = -1
                                                    }

                                                });
                                            }
                                        }

                                    }
                                }
                                break;
                            case 0x02:
                                {
                                    
                                        string VolSub = BitConverter.ToString(ClearData.Take(4).ToArray()).Replace("-", null);
                                        string AmtSub = BitConverter.ToString(ClearData.Skip(4).Take(4).ToArray()).Replace("-", null);

                                        fp.DispensedAmount = decimal.Parse(AmtSub) / (decimal)Math.Pow(10, fp.AmountDecimalPlaces);
                                        fp.DispensedVolume = decimal.Parse(VolSub) / (decimal)Math.Pow(10, fp.VolumeDecimalPlaces);

                                    if (fp.DispensedAmount > 0)
                                    {
                                        if (this.DataChanged != null)
                                        {

                                            Common.FuelPointValues values = new Common.FuelPointValues();
                                            values.CurrentSalePrice = fp.Nozzles[0].UnitPrice;
                                            values.CurrentPriceTotal = fp.DispensedAmount;
                                            values.CurrentVolume = fp.DispensedVolume;
                                            values.ActiveNozzle = 0;


                                            this.DataChanged(this, new Common.FuelPointValuesArgs()
                                            {
                                                CurrentFuelPoint = fp,
                                                CurrentNozzleId = 1,
                                                Values = values
                                            });
                                        }
                                    }
                                    Thread.Sleep(5);
                                    
                                }
                                break;
                            case 0x03:
                                {
                                    //fp.dtGetStatus = DateTime.Now;

                                    if (ClearData[3] >= 0x00 && ClearData[3] <= 0x0F)
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
                                                ExecuteSlave(Commands.AllowedNozzle(nz.ParentFuelPoint.Address, nz.Index),fp);
                                                Thread.Sleep(25);
                                                byte[] bb = new byte[this.serialPort.BytesToRead];
                                                this.serialPort.Read(bb, 0, this.serialPort.BytesToRead);
                                                Thread.Sleep(50);
                                            }
                                            

                                            //clearBuf = new byte[this.serialPort.BytesToRead];
                                            //this.serialPort.Read(clearBuf, 0, this.serialPort.BytesToRead);

                                            fp.SetExtendedProperty("dtGetStatus_" + fp.Address, DateTime.Now);
                                            fp.Status = FuelPointStatusEnum.Idle;
                                            fp.DispenserStatus = FuelPointStatusEnum.Idle;
                                         
                                        }
                                        else if (fp.Status == FuelPointStatusEnum.Nozzle)
                                        {

                                            fp.Status = FuelPointStatusEnum.Idle;
                                            fp.DispenserStatus = FuelPointStatusEnum.Idle;
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
                                        else if (fp.Status == FuelPointStatusEnum.Work)
                                        {
                                            try
                                            {
                                                for (int i = 1; i <= 4; i++)
                                                {
                                                    ExecuteSlave(Commands.GetDisplay(fp.Address), fp);
                                                    Thread.Sleep(50);
                                                    byte[] ssss = new byte[this.serialPort.BytesToRead];
                                                    this.serialPort.Read(ssss, 0, this.serialPort.BytesToRead);
                                                    Logger(ssss);

                                                   

                                                    ExecuteSlave(Commands.Poll(fp.Address), fp);
                                                    Thread.Sleep(50);
                                                    ssss = new byte[this.serialPort.BytesToRead];
                                                    this.serialPort.Read(ssss, 0, this.serialPort.BytesToRead);
                                                    Logger(ssss);
                                                    

                                                    ExecuteSlave(Commands.Poll(fp.Address), fp);
                                                    Thread.Sleep(125);
                                                    ssss = new byte[this.serialPort.BytesToRead];
                                                    this.serialPort.Read(ssss, 0, this.serialPort.BytesToRead);
                                                    Logger(ssss);
                                                    
                                                    if(ssss[2] == 0x02 && ssss[3] == 0x08)
                                                    {
                                                        string VolSub = BitConverter.ToString(ssss.Skip(4).Take(4).ToArray()).Replace("-", null);
                                                        string AmtSub = BitConverter.ToString(ssss.Skip(8).Take(4).ToArray()).Replace("-", null);

                                                        fp.DispensedAmount = decimal.Parse(AmtSub) / (decimal)Math.Pow(10, fp.AmountDecimalPlaces);
                                                        fp.DispensedVolume = decimal.Parse(VolSub) / (decimal)Math.Pow(10, fp.VolumeDecimalPlaces);

                                                        if (fp.DispensedAmount > 0)
                                                        {
                                                            if (this.DataChanged != null)
                                                            {

                                                                Common.FuelPointValues values = new Common.FuelPointValues();
                                                                values.CurrentSalePrice = fp.Nozzles[0].UnitPrice;
                                                                values.CurrentPriceTotal = fp.DispensedAmount;
                                                                values.CurrentVolume = fp.DispensedVolume;
                                                                values.ActiveNozzle = 0;
                                                                this.DataChanged(this, new Common.FuelPointValuesArgs()
                                                                {
                                                                    CurrentFuelPoint = fp,
                                                                    CurrentNozzleId = 1,
                                                                    Values = values
                                                                });
                                                            }
                                                        }
                                                    }

                                                    ExecuteSlave(Commands.ACK(fp.Address), fp);

                                                    Thread.Sleep(25);
                                                    byte[] clearBuf = new byte[this.serialPort.BytesToRead];
                                                    this.serialPort.Read(clearBuf, 0, this.serialPort.BytesToRead);
                                                    
                                                }


                                                

                                                if (fp.DispensedAmount > 0.01M)
                                                {

                                                }
                                                else
                                                {

                                                    ExecuteSlave(Commands.Stop(fp.Address), fp);
                                                    Thread.Sleep(50);
                                                    byte[] arar = new byte[this.serialPort.BytesToRead];
                                                    this.serialPort.Read(arar, 0, this.serialPort.BytesToRead);

                                                    Thread.Sleep(50);

                                                    ExecuteSlave(Commands.Reset(fp.Address), fp);
                                                    Thread.Sleep(80);
                                                     arar = new byte[this.serialPort.BytesToRead];
                                                    this.serialPort.Read(arar, 0, this.serialPort.BytesToRead);
                                                  
                                                }

                                                

                                                fp.Status = FuelPointStatusEnum.Idle;
                                                fp.DispenserStatus = FuelPointStatusEnum.Idle;
                                                fp.ActiveNozzleIndex = -1;
                                                this.DataChanged(this, new FuelPointValuesArgs()
                                                {
                                                    CurrentFuelPoint = fp,
                                                    CurrentNozzleId = 1,
                                                    Values = new FuelPointValues()
                                                    {
                                                        Status = fp.Status,
                                                        //ActiveNozzle = -1
                                                    }
                                                });

                                            }
                                            catch (Exception ex)
                                            {
                                                //Base.Logger.ProtocolErrror(ex.ToString(), "AnalyzeDart _ From Work To Idle");
                                            }
                                        }
                                    }
                                    
                                    if (ClearData[3] >= (byte)0x11 && ClearData[3] <= (byte)0x18)
                                    {
                                        if (fp.Status == FuelPointStatusEnum.Idle)
                                        {
                                            int SelectedNozzle = (int)ClearData[3] - 17;
                                            fp.Status = FuelPointStatusEnum.Nozzle;
                                            fp.DispenserStatus = FuelPointStatusEnum.Nozzle;
                                            fp.ActiveNozzleIndex = SelectedNozzle;
                                            this.DataChanged(this, new FuelPointValuesArgs()
                                            {
                                                CurrentFuelPoint = fp,
                                                CurrentNozzleId = SelectedNozzle+1,
                                                Values = new FuelPointValues()
                                                {
                                                    Status = fp.Status,
                                                    ActiveNozzle = SelectedNozzle
                                                }
                                            });

                                        }
                                        if(ClearData[3]>=0x11)
                                        {
                                            int noz = ClearData[3] - 16;
                                            ExecuteSlave(Commands.AllowedNozzle(fp.Address, noz),fp);
                                            byte[] numArray = new byte[this.serialPort.BytesToRead];
                                            this.serialPort.Read(numArray, 0, this.serialPort.BytesToRead);
                                        }
                                    }
                                }
                                break;

                        }
                        Cmd = Cmd.Skip(DataSkip).Take(Cmd.Length).ToArray();
                    }


                }

                fp.DispenserStatus = fp.Status;
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

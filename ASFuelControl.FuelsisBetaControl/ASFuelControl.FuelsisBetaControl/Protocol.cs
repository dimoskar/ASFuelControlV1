using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO.Ports;
using ASFuelControl.Common;
using ASFuelControl.Common.Enumerators;
using System.Diagnostics;

namespace ASFuelControl.FuelsisBetaControl
{
    public class Protocol : IFuelProtocol, IPumpDebug
    {
        public event EventHandler<FuelPointValuesArgs> DataChanged;

        public event EventHandler<TotalsEventArgs> TotalsRecieved;

        public event EventHandler<FuelPointValuesArgs> DispenserStatusChanged;

        public event EventHandler<SaleEventArgs> SaleRecieved;

        public event EventHandler DispenserOffline;

        private List<FuelPoint> fuelPoints = new List<FuelPoint>();

        private SerialPort SerPort = new SerialPort();

        private Thread th;

        private IFSF_COMPO IfsfDriver;

        public DebugValues DebugStatusDialog(FuelPoint fp)
        {
            return null;
        }

        public DebugValues foo = new DebugValues();

        public void AddFuelPoint(FuelPoint fp)
        {
            this.fuelPoints.Add(fp);
        }

        public void ClearFuelPoints()
        {
            this.fuelPoints.Clear();
        }

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
                return IfsfDriver.IsConnected();
            }
        }

        public string CommunicationPort
        {
            set;
            get;
        }


        public Protocol()
        {
            this.th = new Thread(BetControlThread);
            this.th.Start();
        }

        public void Connect()
        {
            try
            {
                IfsfDriver = new IFSF_COMPO("127.0.0.50", 9494);
                IfsfDriver.OnConnectEvent += new IFSF_COMPO.OnConnectEventHandler(OnConnect);
                IfsfDriver.OnDataRecievedEvent += new IFSF_COMPO.DataReceivedEventHandler(OnRecieved);
                IfsfDriver.Connect();
            }
            catch (Exception ex)
            {

            }
        }

        private void OnConnect(bool status)
        {
            try
            {

            }
            catch (Exception ex)
            {

            }
        }

        private void OnRecieved(string data)
        {
            try
            {
                Trace.WriteLine("Server Response: " + data);
                string dat = data;

                if (dat.StartsWith("FP_ST_"))
                {
                    int fpaddress = int.Parse(dat.Substring(6, 2));
                    string status = dat.Substring(9, dat.Length - 9);
                    FuelPoint fp = this.fuelPoints.Where(x => x.Address.Equals(fpaddress)).SingleOrDefault();
                    fp.LastStatus = fp.Status;
                    fp.DispenserLastStatus = fp.DispenserStatus;
                    switch (status)
                    {
                        case "Offline":
                            {
                                fp.Status = FuelPointStatusEnum.Offline;
                                fp.DispenserStatus = FuelPointStatusEnum.Offline;
                            }
                            break;
                        case "Idle":
                            {
                                fp.Status = FuelPointStatusEnum.Idle;
                                fp.DispenserStatus = FuelPointStatusEnum.Idle;
                                fp.ActiveNozzle = null;
                                fp.ActiveNozzleIndex = -1;
                                if (fp.LastStatus == FuelPointStatusEnum.Work && fp.Status == FuelPointStatusEnum.Idle)
                                    IfsfDriver.Send("GET_DISPLAY=FP" + fp.Address.ToString("00") + "\n\r");
                                Thread.Sleep(100);
                            }
                            break;
                        case "Nozzle":
                            {
                                fp.Status = FuelPointStatusEnum.Nozzle;
                                fp.DispenserStatus = FuelPointStatusEnum.Nozzle;
                                fp.ActiveNozzle = fp.Nozzles[0];
                                fp.ActiveNozzleIndex = 0;
                            }
                            break;
                        case "Work":
                            {
                                fp.Status = FuelPointStatusEnum.Work;
                                fp.DispenserStatus = FuelPointStatusEnum.Work;
                            }
                            break;
                        case "TransactionCompleted":
                            {
                                fp.Status = FuelPointStatusEnum.TransactionCompleted;
                                fp.DispenserStatus = FuelPointStatusEnum.TransactionCompleted;
                            }
                            break;
                        case "Error":
                            {
                                fp.Status = FuelPointStatusEnum.Error;
                                fp.DispenserStatus = FuelPointStatusEnum.Error;
                            }
                            break;

                    }
                    this.DispenserStatusChanged(this, new FuelPointValuesArgs()
                    {
                        CurrentFuelPoint = fp,
                        CurrentNozzleId = -1,
                        Values = new FuelPointValues() { Status = fp.Status }
                    });
                }

                if (dat.StartsWith("VOLT_FP_"))
                {
                    int fpaddress = int.Parse(dat.Substring(8, 2));
                    int Nozzle = int.Parse(dat.Substring(14, 2));
                    decimal Volume = decimal.Parse(dat.Substring(17, dat.Length - 17));
                    Common.FuelPoint fp = this.fuelPoints.Where(x => x.Address.Equals(fpaddress)).SingleOrDefault();
                    Common.Nozzle nz = fp.Nozzles[0];
                    nz.TotalVolume = Volume * 100;
                    nz.QueryTotals = false;
                    if (this.TotalsRecieved != null)
                    {
                        this.TotalsRecieved(this, new TotalsEventArgs(nz.ParentFuelPoint, nz.Index, nz.TotalVolume, nz.TotalPrice));
                    }
                    if (!nz.ParentFuelPoint.Initialized)
                        nz.ParentFuelPoint.Initialized = true;
                }

                if (dat.StartsWith("DIS_FP_"))
                {
                    int fpaddress = int.Parse(dat.Substring(7, 2));
                    Common.FuelPoint fp = this.fuelPoints.Where(x => x.Address.Equals(fpaddress)).SingleOrDefault();
                    Common.Nozzle nz = fp.Nozzles[0];
                    decimal Amount = decimal.Parse(dat.Substring(12, 9).Replace(".", "").Replace(",", ""));
                    decimal Volume = decimal.Parse(dat.Substring(24, 9).Replace(".", "").Replace(",", ""));
                    fp.DispensedVolume = Volume / 100;
                    fp.DispensedAmount = Amount / 100;
                    if (this.DataChanged != null)
                    {
                        FuelPointValues fuelPointValue = new FuelPointValues()
                        {
                            CurrentSalePrice = nz.UnitPrice,
                            CurrentPriceTotal = nz.ParentFuelPoint.DispensedAmount,
                            CurrentVolume = nz.ParentFuelPoint.DispensedVolume
                        };
                        this.DataChanged(this, new FuelPointValuesArgs()
                        {
                            CurrentFuelPoint = nz.ParentFuelPoint,
                            CurrentNozzleId = nz.Index,
                            Values = fuelPointValue
                        });
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        public void Disconnect()
        {
            IfsfDriver.Disconnect();
        }

        private void BetControlThread()
        {
            try
            {
                while (true)
                {
                    try
                    {
                        foreach (FuelPoint fp in this.fuelPoints)
                        {
                            foreach (Nozzle nz in fp.Nozzles)
                            {
                                nz.QueryTotals = true;
                                nz.QuerySetPrice = true;
                            }
                        }

                        while (IsConnected)
                        {
                            foreach (FuelPoint fp in this.fuelPoints.OrderBy(x => x.Address))
                            {
                                foreach (Nozzle nz in fp.Nozzles.Where(x=>x.QueryTotals.Equals(true) || x.QuerySetPrice.Equals(true)))
                                {
                                    if (nz.QueryTotals)
                                    {

                                        IfsfDriver.Send("GET_TOTALS=FP_" + fp.Address.ToString("00") + "_NZ_" + nz.NozzleIndex.ToString("00") + "\n\r");
                                        nz.QueryTotals = false;
                                        Thread.Sleep(500);

                                    }
                                    if (nz.QuerySetPrice)
                                    {

                                        IfsfDriver.Send("SET_PRICE=FP_" + fp.Address.ToString("00") + "_NZ_" + nz.NozzleIndex.ToString("00") + "_PRICE_" + nz.UntiPriceInt.ToString("0000") + "\n\r");
                                        Thread.Sleep(80);
                                        nz.QuerySetPrice = false;

                                    }
                                }

                                if (fp.Status == FuelPointStatusEnum.Work)
                                {
                                    Thread.Sleep(20);
                                    IfsfDriver.Send("GET_DISPLAY=FP" + fp.Address.ToString("00") + "\n\r");
                                    Thread.Sleep(70);
                                }

                                if (fp.QuerySetPrice != true || fp.QueryAuthorize != true || fp.QueryHalt || fp.QueryStop)
                                {
                                    if (fp.QuerySetPrice)
                                    {
                                        IfsfDriver.Send("SET_PRICE=FP_" + fp.Address.ToString("00") + "_NZ_" + fp.Nozzles[0].NozzleIndex.ToString("00") + "_PRICE_" + fp.Nozzles[0].UntiPriceInt.ToString("0000") + "\n\r");
                                        Thread.Sleep(80);
                                        fp.QuerySetPrice = false;
                                    }

                                    if (fp.QueryAuthorize)
                                    {

                                        IfsfDriver.Send("SET_PRICE=FP_" + fp.Address.ToString("00") + "_NZ_" + fp.Nozzles[0].NozzleIndex.ToString("00") + "_PRICE_" + fp.Nozzles[0].UntiPriceInt.ToString("0000") + "\n\r");
                                        Thread.Sleep(80);

                                        IfsfDriver.Send("AUTHORIZE=FP_" + fp.Address.ToString("00") + "\n\r");
                                        Thread.Sleep(80);

                                        IfsfDriver.Send("AUTHORIZE=FP_" + fp.Address.ToString("00") + "\n\r");
                                        Thread.Sleep(80);

                                        fp.QueryAuthorize = false;

                                    }

                                    if (fp.QueryHalt || fp.QueryStop)
                                    {
                                        IfsfDriver.Send("HALT=FP_" + fp.Address.ToString("00") + "\n\r");
                                        fp.QueryHalt = false;
                                        fp.QueryStop = false;
                                        Thread.Sleep(80);
                                    }
                                }
                             
                                IfsfDriver.Send("GET_FP=" + fp.Address.ToString("00") + "\n\r");

                                
                                Thread.Sleep(80);
                            }
                        }
                        Thread.Sleep(100);
                    }
                    catch (Exception ex)
                    {

                    }
                    finally
                    {
                        Thread.Sleep(60);
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}
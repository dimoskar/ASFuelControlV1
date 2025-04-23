using ASFuelControl.Common;
using ASFuelControl.Common.Enumerators;
using ASFuelControl.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;

namespace ASFuelControl.BoxIFSF
{
    public class Controller : Common.FuelPumpControllerBase
    {
        public Controller()
        {
            this.ControllerType = Common.Enumerators.ControllerTypeEnum.Box69;
            this.Controller = new Box69IfsfConnector();
        }

    }

    public class Box69IfsfConnector : IFuelProtocol, IPumpDebug
    {
        private List<byte> buffer = new List<byte>();
        private List<FuelPoint> fuelPoints = new List<FuelPoint>();
        private SerialPort serialPort = new SerialPort();
        private byte[] cmd;
        private Thread th;

        private double speed { get; set; }

        public FuelPoint[] FuelPoints
        {
            get
            {
                return this.fuelPoints.ToArray();
            }
            set
            {
                this.fuelPoints = new List<FuelPoint>((IEnumerable<FuelPoint>)value);
            }
        }

        public bool IsConnected
        {
            get
            {
                return this.serialPort.IsOpen;
            }
        }

        public string CommunicationPort { get; set; }

        public event EventHandler<TotalsEventArgs> TotalsRecieved;

        public event EventHandler<FuelPointValuesArgs> DataChanged;

        public event EventHandler<FuelPointValuesArgs> DispenserStatusChanged;

        public DebugValues DebugStatusDialog(FuelPoint fp)
        {
            throw new NotImplementedException();
        }

        public void Connect()
        {
            try
            {
                this.serialPort.PortName = this.CommunicationPort;
                this.serialPort.Open();
                this.speed = (double)(1 / (this.serialPort.BaudRate / 8000));
                this.th = new Thread(new ThreadStart(this.ThreadRun));
                this.th.Start();
            }
            catch
            {
            }
        }

        public void Disconnect()
        {
            if (!this.serialPort.IsOpen)
                return;
            this.serialPort.Close();
        }

        public void AddFuelPoint(FuelPoint fp)
        {
            this.fuelPoints.Add(fp);
        }

        public void ClearFuelPoints()
        {
            this.fuelPoints.Clear();
        }

        private void ThreadRun()
        {
            foreach (FuelPoint fuelPoint in this.fuelPoints)
            {
                fuelPoint.QuerySetPrice = true;
                foreach (Nozzle nozzle in fuelPoint.Nozzles)
                    nozzle.QueryTotals = true;
            }
            while (this.IsConnected)
            {
                try
                {
                    foreach (FuelPoint fp in this.fuelPoints)
                    {
                        try
                        {
                            
                            if (!(bool)fp.GetExtendedProperty("amInitialized", (object)false))
                            {
                                //fp.Status;
                                this.InitializeFuelPoint(fp);
                                if (fp.Status == FuelPointStatusEnum.Idle)
                                {
                                    this.SetPrice(fp);
                                    if (!fp.QuerySetPrice)
                                        fp.SetExtendedProperty("amInitialized", (object)true);
                                }
                            }
                            else
                            {
                                if (fp.QueryHalt && !fp.Halted)
                                {
                                    this.Halt(fp);
                                    continue;
                                }
                                this.GetStatus(fp);
                                if (fp.QuerySetPrice)
                                    this.SetPrice(fp);
                                else if (Enumerable.Count<Nozzle>(Enumerable.Where<Nozzle>((IEnumerable<Nozzle>)fp.Nozzles, (Func<Nozzle, bool>)(n => n.QueryTotals))) > 0)
                                {
                                    foreach (Nozzle nz in fp.Nozzles)
                                    {
                                        if (nz.QueryTotals)
                                        {
                                            this.GetDisplay(fp);
                                            this.GetTotals(nz);
                                        }
                                    }
                                }
                                else if (fp.QueryAuthorize)
                                    this.Authorize(fp.ActiveNozzle);
                                else if (fp.Status == FuelPointStatusEnum.Work)
                                {
                                    this.GetDisplay(fp);
                                }
                                else
                                {
                                    if ((bool)fp.GetExtendedProperty("AckDeactivatedNozzle", (object)false))
                                        this.AckDeactivatedNozzle(fp);
                                    if (fp.Status == FuelPointStatusEnum.Offline)
                                        fp.SetExtendedProperty("amInitialized", (object)false);
                                }
                            }
                        }
                        finally
                        {
                            Thread.Sleep(80);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogToFile("TrheadRun", ex);
                    Thread.Sleep(100);
                }
            }
        }

        private void InitializeFuelPoint(FuelPoint fp)
        {
            byte num1 = BitConverter.GetBytes(240 + fp.Address - 1)[0];
            List<byte> list = new List<byte>();
            byte num2 = (byte)166;
            list.Add(num1);
            list.Add(num2);
            for (int index = 0; index < fp.Nozzles.Length; ++index)
            {
                byte[] decimalBytes1 = this.GetDecimalBytes(fp.Nozzles[index].UnitPrice, fp.UnitPriceDecimalPlaces, 2);
                byte[] decimalBytes2 = this.GetDecimalBytes(fp.Nozzles[index].UnitPrice, fp.UnitPriceDecimalPlaces, 2);
                list.AddRange((IEnumerable<byte>)decimalBytes1);
                list.AddRange((IEnumerable<byte>)decimalBytes2);
            }
            for (int length = fp.Nozzles.Length; length < 4; ++length)
            {
                byte[] decimalBytes1 = this.GetDecimalBytes(new Decimal(0), fp.UnitPriceDecimalPlaces, 2);
                byte[] decimalBytes2 = this.GetDecimalBytes(new Decimal(0), fp.UnitPriceDecimalPlaces, 2);
                list.AddRange((IEnumerable<byte>)decimalBytes1);
                list.AddRange((IEnumerable<byte>)decimalBytes2);
            }
            this.cmd = this.NormaliseBuffer(list.ToArray());
            if (!this.ExecuteCommnad(this.cmd, Box69IfsfConnector.ResponseLenght.Basic, Box69IfsfConnector.CommandType.Initiliaze))
                return;
            this.EvaluateStatus(this.buffer.ToArray(), fp);
        }

        private void GetDisplay(FuelPoint fp)
        {
            if (fp.ActiveNozzle == null)
                fp.ActiveNozzle = fp.LastActiveNozzle;
            byte num1 = BitConverter.GetBytes(240 + fp.Address - 1)[0];
            byte num2 = (byte)((uint)byte.MaxValue - (uint)num1);
            this.cmd = new byte[4]
      {
        num1,
        num2,
        (byte) 161,
        (byte) 94
      };
            if (!this.ExecuteCommnad(this.cmd, Box69IfsfConnector.ResponseLenght.Display, Box69IfsfConnector.CommandType.Display))
                return;
            this.EvaluateDisplayData(fp.ActiveNozzle, this.buffer.ToArray());
        }

        private void Authorize(Nozzle nz)
        {
            byte num1 = BitConverter.GetBytes(240 + nz.ParentFuelPoint.Address - 1)[0];
            List<byte> list1 = new List<byte>();
            byte num2 = (byte)32;
            list1.Add(num1);
            list1.Add((byte)165);
            list1.Add(num2);
            string str = nz.UntiPriceInt.ToString();
            for (int length = str.Length; length < 4; ++length)
                str = "0" + str;
            int num3 = Convert.ToInt32(str.Substring(0, 2));
            int num4 = Convert.ToInt32(str.Substring(2, 2));
            byte num5 = (byte)this.ith(num3);
            byte num6 = (byte)this.ith(num4);
            byte[] decimalBytes1 = this.GetDecimalBytes(new Decimal(999999), nz.ParentFuelPoint.UnitPriceDecimalPlaces, 3);
            byte[] decimalBytes2 = this.GetDecimalBytes(new Decimal(999999), nz.ParentFuelPoint.UnitPriceDecimalPlaces, 3);
            list1.Add(num6);
            list1.Add(num5);
            list1.AddRange(Enumerable.Reverse<byte>((IEnumerable<byte>)decimalBytes1));
            list1.AddRange(Enumerable.Reverse<byte>((IEnumerable<byte>)decimalBytes2));
            this.cmd = this.NormaliseBuffer(list1.ToArray());
            List<byte> list2 = new List<byte>();
            list2.AddRange((IEnumerable<byte>)Enumerable.ToArray<byte>(Enumerable.Take<byte>((IEnumerable<byte>)this.cmd, 10)));
            byte[] numArray = new byte[12]
      {
        (byte) 153,
        (byte) 102,
        (byte) 153,
        (byte) 102,
        (byte) 153,
        (byte) 102,
        (byte) 0,
        byte.MaxValue,
        (byte) 9,
        (byte) 246,
        (byte) 153,
        (byte) 102
      };
            list2.AddRange((IEnumerable<byte>)numArray);
            if (!this.ExecuteCommnad(list2.ToArray(), Box69IfsfConnector.ResponseLenght.Basic, Box69IfsfConnector.CommandType.Authorize) || this.buffer.ToArray().Length == 0)
                return;
            nz.ParentFuelPoint.QueryAuthorize = false;
        }

        private void SetPrice(FuelPoint fp)
        {
            byte num1 = BitConverter.GetBytes(192 + fp.Address - 1)[0];
            byte[] cmd = new byte[84]
      {
        (byte) 192,
        (byte) 63,
        (byte) 163,
        (byte) 92,
        (byte) 17,
        (byte) 238,
        (byte) 0,
        byte.MaxValue,
        (byte) 0,
        byte.MaxValue,
        (byte) 0,
        byte.MaxValue,
        (byte) 2,
        (byte) 253,
        (byte) 34,
        (byte) 221,
        (byte) 0,
        byte.MaxValue,
        (byte) 0,
        byte.MaxValue,
        (byte) 3,
        (byte) 252,
        (byte) 51,
        (byte) 204,
        (byte) 0,
        byte.MaxValue,
        (byte) 0,
        byte.MaxValue,
        (byte) 4,
        (byte) 251,
        (byte) 68,
        (byte) 187,
        (byte) 0,
        byte.MaxValue,
        (byte) 0,
        byte.MaxValue,
        (byte) 0,
        byte.MaxValue,
        (byte) 0,
        byte.MaxValue,
        (byte) 0,
        byte.MaxValue,
        (byte) 0,
        byte.MaxValue,
        (byte) 0,
        byte.MaxValue,
        (byte) 0,
        byte.MaxValue,
        (byte) 0,
        byte.MaxValue,
        (byte) 0,
        byte.MaxValue,
        (byte) 0,
        byte.MaxValue,
        (byte) 0,
        byte.MaxValue,
        (byte) 0,
        byte.MaxValue,
        (byte) 0,
        byte.MaxValue,
        (byte) 0,
        byte.MaxValue,
        (byte) 0,
        byte.MaxValue,
        (byte) 0,
        byte.MaxValue,
        (byte) 0,
        byte.MaxValue,
        (byte) 0,
        byte.MaxValue,
        (byte) 0,
        byte.MaxValue,
        (byte) 0,
        byte.MaxValue,
        (byte) 0,
        byte.MaxValue,
        (byte) 0,
        byte.MaxValue,
        (byte) 0,
        byte.MaxValue,
        (byte) 0,
        byte.MaxValue,
        (byte) 0,
        byte.MaxValue
      };
            List<FuelPoint> list = new List<FuelPoint>();
            if (fp.Address == 1 || fp.Address == 2)
                list = Enumerable.ToList<FuelPoint>(Enumerable.Where<FuelPoint>((IEnumerable<FuelPoint>)this.fuelPoints, (Func<FuelPoint, bool>)(x => x.Address <= 2)));
            else if (fp.Address == 3 || fp.Address == 4)
                list = Enumerable.ToList<FuelPoint>(Enumerable.Where<FuelPoint>((IEnumerable<FuelPoint>)this.fuelPoints, (Func<FuelPoint, bool>)(x => x.Address >= 3)));
            foreach (FuelPoint fuelPoint in list)
            {
                string str = Convert.ToString((int)(fuelPoint.Nozzles[0].UnitPrice * new Decimal(1000)));
                for (int length = str.Length; length < 4; ++length)
                    str = "0" + str;
                int num2 = Convert.ToInt32(str.Substring(0, 2));
                int num3 = Convert.ToInt32(str.Substring(2, 2));
                byte num4 = (byte)this.ith(num2);
                byte num5 = (byte)this.ith(num3);
                cmd[0] = num1;
                cmd[1] = (byte)((uint)byte.MaxValue - (uint)num1);
                int index = 0;
                if (fuelPoint.Nozzles[0].NozzleIndex == 1)
                    index = 4;
                if (fuelPoint.Nozzles[0].NozzleIndex == 2)
                    index = 12;
                cmd[index] = num4;
                cmd[index + 1] = (byte)((uint)byte.MaxValue - (uint)num4);
                cmd[index + 2] = num5;
                cmd[index + 3] = (byte)((uint)byte.MaxValue - (uint)num5);
            }
            if (!this.ExecuteCommnad(cmd, Box69IfsfConnector.ResponseLenght.Basic, Box69IfsfConnector.CommandType.SetPrice))
                return;
            if (this.buffer.ToArray().Length == 0)
                fp.QuerySetPrice = true;
            if ((int)this.buffer.ToArray()[0] == 176)
                fp.QuerySetPrice = false;
        }

        private void SetActiveNozzle(FuelPoint fp)
        {
            byte num1 = BitConverter.GetBytes(192 + fp.Address - 1)[0];
            List<byte> list = new List<byte>();
            byte num2 = (byte)161;
            list.Add(num1);
            list.Add(num2);
            if (!this.ExecuteCommnad(this.NormaliseBuffer(list.ToArray()), Box69IfsfConnector.ResponseLenght.Basic, Box69IfsfConnector.CommandType.GetActiveNozzle))
                return;
            this.EvaluateActiveNozzle(fp, this.buffer.ToArray());
        }

        private void AckDeactivatedNozzle(FuelPoint fp)
        {
            byte num1 = BitConverter.GetBytes(192 + fp.ActiveNozzleIndex)[0];
            List<byte> list = new List<byte>();
            byte num2 = (byte)162;
            list.Add(num1);
            list.Add(num2);
            if (!this.ExecuteCommnad(this.NormaliseBuffer(list.ToArray()), Box69IfsfConnector.ResponseLenght.Basic, Box69IfsfConnector.CommandType.GetActiveNozzle) || (int)this.buffer.ToArray()[0] != 176)
                return;
            fp.SetExtendedProperty("AckDeactivatedNozzle", (object)false);
        }

        private void GetStatus(FuelPoint fp)
        {
            byte num1 = BitConverter.GetBytes(240 + fp.Address - 1)[0];
            byte num2 = (byte)((uint)byte.MaxValue - (uint)num1);
            this.cmd = new byte[4]
              {
                num1,
                num2,
                (byte) 162,
                (byte) 93
              };
            if (!this.ExecuteCommnad(this.cmd, Box69IfsfConnector.ResponseLenght.Basic, Box69IfsfConnector.CommandType.Status))
            {
                if (DateTime.Now.Subtract(fp.LastValidResponse).TotalSeconds > 5)
                {
                    fp.Status = Common.Enumerators.FuelPointStatusEnum.Offline;
                    if (this.DispenserStatusChanged != null)
                    {
                        Common.FuelPointValues values = new Common.FuelPointValues();
                        values.Status = Common.Enumerators.FuelPointStatusEnum.Offline;
                        this.DispenserStatusChanged(this, new Common.FuelPointValuesArgs()
                        {
                            CurrentFuelPoint = fp,
                            CurrentNozzleId = 0,
                            Values = values
                        });
                    }
                }
            }
            this.EvaluateStatus(this.buffer.ToArray(), fp);
        }

        private void Halt(Common.FuelPoint fp)
        {
            byte startByte = BitConverter.GetBytes(240 + fp.Address - 1)[0];
            byte startByteNeg = (byte)(255 - startByte);
            cmd = new byte[] { startByte, startByteNeg, 0xA3, 0x5C };
            if (this.ExecuteCommnad(cmd, ResponseLenght.Basic, CommandType.Status))
            {
                this.EvaluateStatus(this.buffer.ToArray(), fp);
                //fp.Halted = true;
                //fp.QueryHalt = false;
            }
            else
            {

            }
        }

        private void GetTotals(Nozzle nz)
        {
            byte num1 = BitConverter.GetBytes(240 + nz.ParentFuelPoint.Address - 1)[0];
            List<byte> list = new List<byte>();
            byte num2 = (byte)169;
            list.Add(num1);
            list.Add(num2);
            if (nz.NozzleIndex == 1)
                list.Add(this.TotalNozzleDefinition(nz.Index - 1));
            if (nz.NozzleIndex == 2)
                list.Add(this.TotalNozzleDefinition(nz.Index));
            this.cmd = this.NormaliseBuffer(list.ToArray());
            if (!this.ExecuteCommnad(this.cmd, Box69IfsfConnector.ResponseLenght.Totals, Box69IfsfConnector.CommandType.Totlas))
                return;
            this.EvaluateTotals(nz, this.buffer.ToArray());
        }

        private bool EvaluateDisplayData(Nozzle nozzle, byte[] buffer)
        {
            try
            {
                byte[] numArray1 = Enumerable.ToArray<byte>(Enumerable.Reverse<byte>((IEnumerable<byte>)Enumerable.ToArray<byte>(Enumerable.Take<byte>((IEnumerable<byte>)buffer, 2))));
                byte[] numArray2 = Enumerable.ToArray<byte>(Enumerable.Reverse<byte>((IEnumerable<byte>)Enumerable.ToArray<byte>(Enumerable.Take<byte>(Enumerable.Skip<byte>((IEnumerable<byte>)buffer, 2), 3))));
                byte[] numArray3 = Enumerable.ToArray<byte>(Enumerable.Reverse<byte>((IEnumerable<byte>)Enumerable.ToArray<byte>(Enumerable.Take<byte>(Enumerable.Skip<byte>((IEnumerable<byte>)buffer, 5), 3))));
                Enumerable.ToArray<byte>(Enumerable.Take<byte>(Enumerable.Skip<byte>((IEnumerable<byte>)buffer, 8), 1));
                BitConverter.ToString(numArray1).Replace("-", "");
                string s1 = BitConverter.ToString(numArray3).Replace("-", "");
                string s2 = BitConverter.ToString(numArray2).Replace("-", "");
                nozzle.ParentFuelPoint.DispensedVolume = (Decimal.Parse(s1) / (Decimal)Math.Pow(10.0, (double)nozzle.ParentFuelPoint.AmountDecimalPlaces));
                nozzle.ParentFuelPoint.DispensedAmount = (Decimal.Parse(s2) / (Decimal)Math.Pow(10.0, (double)nozzle.ParentFuelPoint.AmountDecimalPlaces));
                if (this.DataChanged != null)
                    this.DataChanged((object)this, new FuelPointValuesArgs()
                    {
                        CurrentFuelPoint = nozzle.ParentFuelPoint,
                        CurrentNozzleId = nozzle.Index,
                        Values = new FuelPointValues()
                        {
                            CurrentSalePrice = nozzle.UnitPrice,
                            CurrentPriceTotal = nozzle.ParentFuelPoint.DispensedAmount,
                            CurrentVolume = nozzle.ParentFuelPoint.DispensedVolume
                        }
                    });
                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.LogToFile("evalDisplay " + BitConverter.ToString(buffer), ex);
                return false;
            }
        }

        private bool EvaluateTotals(Nozzle nz, byte[] buffer)
        {
            try
            {
                byte[] numArray1 = Enumerable.ToArray<byte>(Enumerable.Reverse<byte>((IEnumerable<byte>)Enumerable.ToArray<byte>(Enumerable.Take<byte>((IEnumerable<byte>)buffer, 5))));
                byte[] numArray2 = Enumerable.ToArray<byte>(Enumerable.Reverse<byte>((IEnumerable<byte>)Enumerable.ToArray<byte>(Enumerable.Take<byte>(Enumerable.Skip<byte>((IEnumerable<byte>)buffer, 5), 5))));
                byte[] buffer1 = Enumerable.ToArray<byte>(Enumerable.Take<byte>(Enumerable.Skip<byte>((IEnumerable<byte>)buffer, 10), 1));
                string s1 = BitConverter.ToString(numArray1).Replace("-", "");
                string s2 = BitConverter.ToString(numArray2).Replace("-", "");
                this.EvaluateStatus(buffer1, nz.ParentFuelPoint);
                nz.TotalPrice = Decimal.Parse(s2);
                nz.TotalVolume = Decimal.Parse(s1);
                nz.ParentFuelPoint.Initialized = true;
                if (this.TotalsRecieved != null)
                    this.TotalsRecieved((object)this, new TotalsEventArgs(nz.ParentFuelPoint, nz.Index, nz.TotalVolume, nz.TotalPrice));
                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.LogToFile("Totals: " + BitConverter.ToString(buffer), ex);
                return false;
            }
        }

        private void EvaluateStatus(byte[] buffer, FuelPoint fp)
        {
            fp.LastValidResponse = DateTime.Now;

            FuelPointStatusEnum status = fp.Status;
            try
            {
                switch (buffer[0])
                {
                    case (byte)47:
                        fp.Status = (FuelPointStatusEnum.Offline);
                        break;
                    case (byte)32:
                        fp.Status = (FuelPointStatusEnum.Idle);
                        fp.QueryHalt = false;
                        fp.Halted = false;
                        break;
                    case (byte)160:
                        fp.Status = (FuelPointStatusEnum.Nozzle);
                        break;
                    case (byte)144:
                        fp.Status = (FuelPointStatusEnum.Ready);
                        break;
                    case (byte)208:
                        fp.Status = (FuelPointStatusEnum.Work);
                        break;
                    case (byte)240:
                        fp.Status = (FuelPointStatusEnum.Work);
                        break;
                    case (byte)145:
                        fp.Status = (FuelPointStatusEnum.TransactionCompleted);
                        break;
                    case (byte)146:
                        fp.Status = (FuelPointStatusEnum.TransactionStopped);
                        break;
                    case (byte)153:
                        fp.Status = (FuelPointStatusEnum.TransactionStopped);
                        fp.QueryHalt = false;
                        fp.Halted = true;
                        break;
                }
                fp.DispenserStatus = (fp.Status);
                if (status == fp.Status || this.DispenserStatusChanged == null)
                    return;
                FuelPointValues fuelPointValues = new FuelPointValues();
                fuelPointValues.Status = fp.Status;
                if (fp.Status != (FuelPointStatusEnum)14 && fp.Status != FuelPointStatusEnum.Idle && fp.Status != FuelPointStatusEnum.Offline)
                {
                    this.SetActiveNozzle(fp);
                    fuelPointValues.ActiveNozzle = fp.ActiveNozzleIndex;
                }
                else
                {
                    fp.ActiveNozzleIndex = (-1);
                    fuelPointValues.ActiveNozzle = -1;
                }
                this.DispenserStatusChanged((object)this, new FuelPointValuesArgs()
                {
                    CurrentFuelPoint = fp,
                    CurrentNozzleId = fp.ActiveNozzleIndex,
                    Values = fuelPointValues
                });
                if ((status == FuelPointStatusEnum.TransactionCompleted || status == FuelPointStatusEnum.Work || status == FuelPointStatusEnum.TransactionStopped) && fp.Status == FuelPointStatusEnum.Idle)
                    this.GetDisplay(fp);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogToFile("Status " + BitConverter.ToString(buffer), ex);
            }
        }

        private void EvaluateActiveNozzle(FuelPoint fp, byte[] buffer)
        {
            try
            {
                BitArray bitArray = new BitArray(new byte[1]
        {
          buffer[0]
        });
                BitArray binary = new BitArray(7);
                binary.Set(0, bitArray.Get(6));
                binary.Set(1, bitArray.Get(5));
                binary.Set(2, bitArray.Get(4));
                binary.Set(3, bitArray.Get(3));
                binary.Set(4, bitArray.Get(2));
                binary.Set(5, bitArray.Get(1));
                binary.Set(6, bitArray.Get(0));
                int index = this.ToNumeral(binary, 7) - 1;
                if (index < 0)
                {
                    fp.ActiveNozzle = null;
                    fp.ActiveNozzleIndex = (-1);
                }
                else
                {
                    if (fp.Nozzles[0].NozzleIndex == 1)
                    {
                        fp.ActiveNozzleIndex = (index);
                        fp.ActiveNozzle = (fp.Nozzles[index]);
                    }
                    if (fp.Nozzles[0].NozzleIndex == 2)
                    {
                        fp.ActiveNozzleIndex = (index - 1);
                        fp.ActiveNozzle = (fp.Nozzles[index - 1]);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogToFile("evalActiveNz " + BitConverter.ToString(buffer), ex);
            }
        }

        private byte[] GetDecimalBytes(Decimal value, int decimalPlaces, int byteCount)
        {
            long num1 = (long)((double)value * Math.Pow(10.0, (double)decimalPlaces));
            List<byte> list = new List<byte>();
            int num2 = 2 * (byteCount - 1);
            double num3 = (double)num1;
            string hex = "";
            for (int index = 0; index < byteCount; ++index)
            {
                int num4 = num2 - 2 * index;
                double num5 = (double)(int)(num3 / Math.Pow(10.0, (double)num4));
                int num6 = (int)num5;
                hex = hex + num6.ToString();
                num3 -= num5 * Math.Pow(10.0, (double)num4);
            }
            return this.StringToByteArray(hex);
        }

        public byte[] StringToByteArray(string hex)
        {
            if (hex.Length % 2 != 0)
                hex = "0" + hex;
            return Enumerable.ToArray<byte>(Enumerable.Select<int, byte>(Enumerable.Where<int>(Enumerable.Range(0, hex.Length), (Func<int, bool>)(x => x % 2 == 0)), (Func<int, byte>)(x => Convert.ToByte(hex.Substring(x, 2), 16))));
        }

        private int ToNumeral(BitArray binary, int length)
        {
            int num = 0;
            for (int index = 0; index < length; ++index)
            {
                if (binary[index])
                    num |= 1 << length - 1 - index;
            }
            return num;
        }

        private bool ExecuteCommnad(byte[] cmd, Box69IfsfConnector.ResponseLenght l, Box69IfsfConnector.CommandType c)
        {
            int num1 = 120;
            try
            {
                this.buffer.Clear();
                this.serialPort.Write(cmd, 0, cmd.Length);
                if (c == Box69IfsfConnector.CommandType.SetPrice)
                {
                    this.serialPort.Write(cmd, 0, cmd.Length);
                    Thread.Sleep((int)((double)cmd.Length * this.speed) + 50);
                }
                Thread.Sleep((int)((double)(cmd.Length + l) * this.speed) + 10);
                int num2 = 0;
                while ((Box69IfsfConnector.ResponseLenght)this.serialPort.BytesToRead < l && num2 < num1 / 20 * 10)
                {
                    num2 += 10;
                    Thread.Sleep(20);
                }
                byte[] buffer = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(buffer, 0, buffer.Length);
                this.buffer.AddRange((IEnumerable<byte>)buffer);
                return this.CheckBuffer(this.buffer.ToArray());
            }
            catch (Exception ex)
            {
                Logger.Instance.LogToFile("executeCommand cmd= " + BitConverter.ToString(cmd), ex);
                return false;
            }
        }

        private byte[] NormaliseBuffer(byte[] buffer)
        {
            List<byte> list = new List<byte>();
            foreach (byte num in buffer)
            {
                list.Add(num);
                list.Add((byte)((uint)byte.MaxValue - (uint)num));
            }
            return list.ToArray();
        }

        private byte TotalNozzleDefinition(int nozzleIndex)
        {
            BitArray bitArray1 = new BitArray(8);
            BitArray bitArray2 = new BitArray(new byte[1]
      {
        (byte) nozzleIndex
      });
            bitArray1.Set(0, false);
            bitArray1.Set(1, bitArray2.Get(0));
            bitArray1.Set(2, bitArray2.Get(1));
            bitArray1.Set(3, bitArray2.Get(2));
            bitArray1.Set(4, false);
            bitArray1.Set(5, false);
            bitArray1.Set(6, true);
            bitArray1.Set(7, false);
            byte[] numArray = new byte[1];
            bitArray1.CopyTo((Array)numArray, 0);
            return numArray[0];
        }

        private int ith(int num)
        {
            return 16 * (num / 10) + num % 10;
        }

        private bool CheckBuffer(byte[] t)
        {
            if (t.Length == 0 || t.Length % 2 != 0)
                return false;
            for (int index1 = 0; index1 < t.Length / 2; ++index1)
            {
                int index2 = 2 * index1;
                if ((int)t[index2] + (int)t[index2 + 1] != (int)byte.MaxValue)
                    return false;
            }
            this.buffer = Enumerable.ToList<byte>((IEnumerable<byte>)this.GetBuffer(t));
            return true;
        }

        private byte[] GetBuffer(byte[] buffer)
        {
            if (buffer.Length % 2 != 0)
                return new byte[0];
            List<byte> list = new List<byte>();
            for (int index1 = 0; index1 < buffer.Length / 2; ++index1)
            {
                int index2 = 2 * index1;
                list.Add(buffer[index2]);
            }
            return list.ToArray();
        }

        private enum ResponseLenght
        {
            Basic = 2,
            Display = 18,
            Totals = 22,
        }

        private enum CommandType
        {
            Status = 1,
            SetPrice = 2,
            Totlas = 3,
            Initiliaze = 4,
            Display = 5,
            Authorize = 6,
            GetActiveNozzle = 7,
        }
    }
}

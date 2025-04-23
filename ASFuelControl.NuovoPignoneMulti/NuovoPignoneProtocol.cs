using ASFuelControl.Common;
using ASFuelControl.Common.Enumerators;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ASFuelControl.NuovoPignoneMulti
{
    public class NuovoPignoneProtocol : IFuelProtocol, IPumpDebug
    {
        private List<FuelPoint> fuelPoints = new List<FuelPoint>();

        private SerialPort serialPort = new SerialPort();

        private Thread th;

        public DebugValues foo = new DebugValues();

        public string CommunicationPort
        {
            get;
            set;
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
                return this.serialPort.IsOpen;
            }
        }

        public NuovoPignoneProtocol()
        {
        }

        public void AddFuelPoint(FuelPoint fp)
        {
            this.fuelPoints.Add(fp);
        }

        private byte[] authoriseFuelPoint(int id)
        {
            return new byte[] { 0, (byte)this.idSetPrice(id), 225 };
        }

        public bool AuthorizeFuelPoint(FuelPoint fp)
        {
            bool flag;
            try
            {
                this.serialPort.DiscardInBuffer();
                byte[] numArray = this.authoriseFuelPoint(fp.Address);
                this.serialPort.Write(numArray, 0, (int)numArray.Length);
                int num = 0;
                while (true)
                {
                    if ((this.serialPort.BytesToRead >= 2 ? true : num >= 300))
                    {
                        break;
                    }
                    Thread.Sleep(20);
                    num += 20;
                }
                byte[] numArray1 = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(numArray1, 0, this.serialPort.BytesToRead);
                numArray = this.confirmAuthoriseFuelPoint(numArray1, fp.Address);
                this.serialPort.Write(numArray, 0, (int)numArray.Length);
            }
            catch
            {
                flag = false;
                return flag;
            }
            flag = true;
            return flag;
        }

        public void ClearFuelPoints()
        {
            this.fuelPoints.Clear();
        }

        private byte[] confirmAuthoriseFuelPoint(byte[] response, int id)
        {
            if (!response.SequenceEqual<byte>(new byte[] { (byte)this.idSetPrice(id), 225 }))
            {
                throw new ArgumentException("AuthoriseFuelPoint() Failed ", "byte[] Response");
            }
            return new byte[] { 0, (byte)(this.idSetPrice(id) + 1), (byte)(254 - this.idSetPrice(id)) };
        }

        private byte[] confirmSetPrices(byte[] response, int unitPrice, int id)
        {
            byte[] numArray = new byte[0];
            numArray = this.setPrices(unitPrice, id);
            if (!response.SequenceEqual<byte>(numArray.Skip<byte>(1).Take<byte>(4).ToArray<byte>()))
            {
                throw new ArgumentException("SetPrice() Failed ", "byte[] Response");
            }
            numArray = new byte[] { 0, (byte)(this.idSetPrice(id) + 1), (byte)(254 - this.idSetPrice(id)), numArray[3], numArray[4] };
            return numArray;
        }

        public void Connect()
        {
            try
            {
                this.serialPort.PortName = this.CommunicationPort;
                this.serialPort.Parity = Parity.Odd;
                this.serialPort.BaudRate = 2400;
                this.serialPort.DtrEnable = true;
                this.serialPort.Open();
                this.th = new Thread(new ThreadStart(this.ThreadRun));
                this.th.Start();
            }
            catch
            {
            }
        }

        public DebugValues DebugStatusDialog(FuelPoint fp)
        {
            this.foo = null;
            fp = this.GetStatus(fp);
            this.foo.Status = fp.Status;
            return this.foo;
        }

        public void Disconnect()
        {
            if (this.serialPort.IsOpen)
            {
                this.serialPort.Close();
            }
        }

        private Nozzle evalDisplay(Nozzle nozzle, byte[] response)
        {
            string[] strArrays = BitConverter.ToString(response).Split(new char[] { '-' });
            for (int i = 1; i < 8; i++)
            {
                if (this.StringToByteArray(strArrays[2 * i])[0] + this.StringToByteArray(strArrays[2 * i + 1])[0] != 255)
                {
                    throw new Exception("evalDisplay() Failed");
                }
            }
            string str = string.Concat(strArrays[2], strArrays[4], strArrays[6]);
            string str1 = string.Concat(strArrays[8], strArrays[10], strArrays[12]);
            nozzle.ParentFuelPoint.DispensedAmount = decimal.Parse(str) / (decimal)Math.Pow(10, (double)nozzle.ParentFuelPoint.AmountDecimalPlaces);
            nozzle.ParentFuelPoint.DispensedVolume = decimal.Parse(str1) / (decimal)Math.Pow(10, (double)nozzle.ParentFuelPoint.VolumeDecimalPlaces);
            if (this.DataChanged != null)
            {
                FuelPointValues fuelPointValue = new FuelPointValues()
                {
                    CurrentSalePrice = nozzle.UnitPrice,
                    CurrentPriceTotal = nozzle.ParentFuelPoint.DispensedAmount,
                    CurrentVolume = nozzle.ParentFuelPoint.DispensedVolume
                };
                this.DataChanged(this, new FuelPointValuesArgs()
                {
                    CurrentFuelPoint = nozzle.ParentFuelPoint,
                    CurrentNozzleId = nozzle.ParentFuelPoint.ActiveNozzleIndex + 1,
                    Values = fuelPointValue
                });
            }
            return nozzle;
        }

        private Nozzle evalMultiTotals(Nozzle nozzle, byte[] response)
        {
            string[] strArrays = BitConverter.ToString(response).Split(new char[] { '-' });
            nozzle.TotalVolume = decimal.Parse(string.Concat(new string[] { strArrays[12 * (nozzle.Index - 1) + 2], strArrays[12 * (nozzle.Index - 1) + 4], strArrays[12 * (nozzle.Index - 1) + 6], strArrays[12 * (nozzle.Index - 1) + 8], strArrays[12 * (nozzle.Index - 1) + 10], strArrays[12 * (nozzle.Index - 1) + 12] }));
            return nozzle;
        }

        private Nozzle evalTotals(Nozzle nozzle, byte[] response)
        {
            string[] strArrays = BitConverter.ToString(response).Split(new char[] { '-' });
            for (int i = 1; i < 7; i++)
            {
                if (this.StringToByteArray(strArrays[2 * i])[0] + this.StringToByteArray(strArrays[2 * i + 1])[0] != 255)
                {
                    throw new Exception("evalTotals() Failed");
                }
            }
            nozzle.TotalVolume = decimal.Parse(string.Concat(new string[] { strArrays[2], strArrays[4], strArrays[6], strArrays[8], strArrays[10], strArrays[12] }));
            return nozzle;
        }

        private FuelPoint evaluateStatus(FuelPoint fp, byte[] response)
        {
            FuelPoint fuelPoint;
            if (((int)response.Length != 3 ? false : response[1] + response[2] == 255))
            {
                byte num = response[2];
                if (num == 255)
                {
                    fp.Status = FuelPointStatusEnum.Idle;
                }
                else if (num == 247)
                {
                    fp.Status = FuelPointStatusEnum.Nozzle;
                    this.serialPort.Write(NuovoPignoneProtocol.GetNozzle(fp.Address), 0, (int)NuovoPignoneProtocol.GetNozzle(fp.Address).Length);
                    Thread.Sleep(200);
                    byte[] numArray = new byte[this.serialPort.BytesToRead];
                    this.serialPort.Read(numArray, 0, this.serialPort.BytesToRead);
                    string str = Convert.ToString(numArray[1], 2).PadLeft(8, '0');
                    string str1 = Convert.ToString(numArray[3], 2).PadLeft(8, '0');
                    int num1 = int.Parse(str.Substring(2, 1));
                    int num2 = int.Parse(str.Substring(6, 1));
                    int num3 = int.Parse(str1.Substring(2, 1));
                    int num4 = int.Parse(str1.Substring(6, 1));
                    if (num1 == 1)
                    {
                        fp.ActiveNozzleIndex = 0;
                        fp.ActiveNozzle = fp.Nozzles[fp.ActiveNozzleIndex];
                    }
                    else if (num2 == 1)
                    {
                        fp.ActiveNozzleIndex = 1;
                        fp.ActiveNozzle = fp.Nozzles[fp.ActiveNozzleIndex];
                    }
                    else if (num3 == 1)
                    {
                        fp.ActiveNozzleIndex = 2;
                        fp.ActiveNozzle = fp.Nozzles[fp.ActiveNozzleIndex];
                    }
                    else if (num4 == 1)
                    {
                        fp.ActiveNozzleIndex = 3;
                        fp.ActiveNozzle = fp.Nozzles[fp.ActiveNozzleIndex];
                    }
                }
                else if (num == 245)
                {
                    fp.Status = FuelPointStatusEnum.Work;
                }
                else if (num != 191)
                {
                    fp.Status = FuelPointStatusEnum.Idle;
                    this.serialPort.Write(NuovoPignoneProtocol.Initialize(fp.Address), 0, 3);
                    Thread.Sleep(50);
                    byte[] numArray1 = new byte[this.serialPort.BytesToRead];
                    this.serialPort.Read(numArray1, 0, this.serialPort.BytesToRead);
                }
                else
                {
                    fp.Status = FuelPointStatusEnum.Idle;
                }
                fp.DispenserStatus = fp.Status;
                fuelPoint = fp;
            }
            else
            {
                fuelPoint = fp;
            }
            return fuelPoint;
        }

        private Nozzle evalUnitPrice(Nozzle nozzle, byte[] response)
        {
            string str = BitConverter.ToString(response);
            string str1 = str.Substring(6, 2);
            string str2 = str.Substring(12, 2);
            nozzle.UntiPriceInt = int.Parse(string.Concat(str1, str2));
            nozzle.UnitPrice = nozzle.UntiPriceInt / (decimal)Math.Pow(10, (double)nozzle.ParentFuelPoint.UnitPriceDecimalPlaces);
            return nozzle;
        }

        public Nozzle GetDisplay(Nozzle nozzle)
        {
            byte[] numArray = this.requestDisplayData(nozzle.ParentFuelPoint.Address);
            this.serialPort.Write(numArray, 0, (int)numArray.Length);
            Thread.Sleep(350);
            byte[] numArray1 = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(numArray1, 0, this.serialPort.BytesToRead);
            return this.evalDisplay(nozzle, numArray1);
        }

        private byte[] getFuelPointStatus(int id)
        {
            return new byte[] { 0, (byte)this.idGetStatus(id) };
        }

        public static byte[] GetNozzle(int Address)
        {
            byte bytes = BitConverter.GetBytes(6 + 8 * Address)[0];
            return new byte[] { 0, bytes };
        }

        public FuelPoint GetStatus(FuelPoint fp)
        {
            byte[] fuelPointStatus = this.getFuelPointStatus(fp.Address);
            this.serialPort.Write(fuelPointStatus, 0, (int)fuelPointStatus.Length);
            int num = 0;
            while (true)
            {
                if ((this.serialPort.BytesToRead >= 3 ? true : num >= 300))
                {
                    break;
                }
                Thread.Sleep(15);
                num += 20;
            }
            byte[] numArray = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(numArray, 0, this.serialPort.BytesToRead);
            return this.evaluateStatus(fp, numArray);
        }

        public Nozzle GetTotals(Nozzle nozzle)
        {
            Nozzle nozzle1;
            if (nozzle.ParentFuelPoint.Nozzles.Count<Nozzle>() <= 1)
            {
                byte[] numArray = this.requestTotals(nozzle.ParentFuelPoint.Address);
                this.serialPort.Write(numArray, 0, (int)numArray.Length);
                Thread.Sleep(650);
                byte[] numArray1 = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(numArray1, 0, this.serialPort.BytesToRead);
                nozzle = this.evalTotals(nozzle, numArray1);
                nozzle1 = nozzle;
            }
            else
            {
                byte[] volumeTotalsMulti = NuovoPignoneProtocol.GetVolumeTotalsMulti(nozzle.ParentFuelPoint.Address);
                this.serialPort.Write(volumeTotalsMulti, 0, (int)volumeTotalsMulti.Length);
                Thread.Sleep(500);
                byte[] numArray2 = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(numArray2, 0, this.serialPort.BytesToRead);
                nozzle = this.evalMultiTotals(nozzle, numArray2);
                nozzle1 = nozzle;
            }
            return nozzle1;
        }

        public Nozzle GetUnitPrice(Nozzle nozzle)
        {
            byte[] numArray = this.requestUnitPrice(nozzle.Index);
            this.serialPort.Write(numArray, 0, (int)numArray.Length);
            int num = 0;
            while (true)
            {
                if ((this.serialPort.BytesToRead >= 6 ? true : num >= 300))
                {
                    break;
                }
                Thread.Sleep(15);
                num += 20;
            }
            byte[] numArray1 = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(numArray1, 0, this.serialPort.BytesToRead);
            return this.evalUnitPrice(nozzle, numArray1);
        }

        private static byte[] GetVolumeTotalsMulti(int Address)
        {
            byte bytes = BitConverter.GetBytes(2 + 8 * Address)[0];
            byte num = BitConverter.GetBytes(45)[0];
            return new byte[] { 0, bytes, num };
        }

        private int idGetDisplay(int num)
        {
            return (num - 1) * 8 + 10;
        }

        private int idGetStatus(int num)
        {
            return (num - 1) * 8 + 9;
        }

        private int idSetPrice(int num)
        {
            return (num - 1) * 8 + 12;
        }

        public static byte[] Initialize(int Address)
        {
            byte bytes = BitConverter.GetBytes(7 + 8 * Address)[0];
            byte num = BitConverter.GetBytes(255 - (7 + 8 * Address))[0];
            return new byte[] { 0, bytes, num };
        }

        private int ith(int num)
        {
            int num1 = 16 * (num / 10) + num % 10;
            return num1;
        }

        public int MinimumTimeNeeded(int cmdl, int respl)
        {
            double baudRate = (double)(1 / (this.serialPort.BaudRate / 8000));
            return (int)((double)(cmdl + respl) * baudRate);
        }

        private byte[] requestDisplayData(int id)
        {
            return new byte[] { 0, (byte)this.idGetDisplay(id), 180 };
        }

        private byte[] requestTotals(int id)
        {
            return new byte[] { 0, (byte)this.idGetDisplay(id), 135 };
        }

        private byte[] requestUnitPrice(int id)
        {
            return new byte[] { 0, (byte)this.idGetDisplay(id), 196 };
        }

        public bool SetPrice(Nozzle nozzle, int unitPrice)
        {
            bool flag;
            try
            {
                if (nozzle.ParentFuelPoint.Nozzles.Count<Nozzle>() <= 1)
                {
                    byte[] numArray = this.setPrices(unitPrice, nozzle.ParentFuelPoint.Address);
                    this.serialPort.Write(numArray, 0, (int)numArray.Length);
                    int num = 0;
                    while (true)
                    {
                        if ((this.serialPort.BytesToRead >= 4 ? true : num >= 300))
                        {
                            break;
                        }
                        Thread.Sleep(20);
                        num += 20;
                    }
                    byte[] numArray1 = new byte[this.serialPort.BytesToRead];
                    this.serialPort.Read(numArray1, 0, this.serialPort.BytesToRead);
                    numArray = this.confirmSetPrices(numArray1, unitPrice, nozzle.ParentFuelPoint.Address);
                    this.serialPort.Write(numArray, 0, (int)numArray.Length);
                }
                else
                {
                    byte[] numArray2 = this.setPriceMulti(nozzle.ParentFuelPoint);
                    this.serialPort.Write(numArray2, 0, (int)numArray2.Length);
                    Thread.Sleep(350);
                    byte[] numArray3 = new byte[this.serialPort.BytesToRead];
                    this.serialPort.Read(numArray3, 0, this.serialPort.BytesToRead);
                    Thread.Sleep(150);
                    byte[] address = new byte[] { 0, (byte)(nozzle.ParentFuelPoint.Address * 8 + 5), (byte)(255 - (nozzle.ParentFuelPoint.Address * 8 + 5)) };
                    this.serialPort.Write(address, 0, (int)address.Length);
                    Thread.Sleep(100);
                    byte[] numArray4 = new byte[this.serialPort.BytesToRead];
                    this.serialPort.Read(numArray4, 0, this.serialPort.BytesToRead);
                }
            }
            catch
            {
                flag = false;
                return flag;
            }
            flag = true;
            return flag;
        }

        private byte[] setPriceMulti(FuelPoint fp)
        {
            byte[] numArray = new byte[15];
            numArray[1] = 12;
            numArray[2] = 165;
            byte[] address = numArray;
            address[1] = (byte)(fp.Address * 8 + 4);
            for (int i = 0; i <= fp.Nozzles.Count<Nozzle>() - 1; i++)
            {
                string str = Convert.ToString(fp.Nozzles[i].UntiPriceInt);
                str = str.PadLeft(4, '0');
                int num = Convert.ToInt16(str.Substring(0, 2));
                int num1 = Convert.ToInt16(str.Substring(2, 2));
                address[3 + i * 2] = (byte)this.ith(num);
                address[4 + i * 2] = (byte)this.ith(num1);
            }
            return address;
        }

        private byte[] setPrices(int unitPrice, int id)
        {
            if (unitPrice >= 9999)
            {
                throw new ArgumentException("max value 9999", "unitPrice");
            }
            string str = Convert.ToString(unitPrice);
            str = str.PadLeft(4, '0');
            int num = Convert.ToInt16(str.Substring(0, 2));
            int num1 = Convert.ToInt16(str.Substring(2, 2));
            return new byte[] { 0, (byte)this.idSetPrice(id), 105, (byte)this.ith(num), (byte)this.ith(num1) };
        }

        public byte[] StringToByteArray(string hex)
        {
            byte[] array = (
                from x in Enumerable.Range(0, hex.Length)
                where x % 2 == 0
                select Convert.ToByte(hex.Substring(x, 2), 16)).ToArray<byte>();
            return array;
        }

        private void ThreadRun()
        {
            foreach (FuelPoint fuelPoint in this.fuelPoints)
            {
                fuelPoint.Nozzles[0].QueryTotals = true;
                fuelPoint.QuerySetPrice = true;
            }
            while (this.IsConnected)
            {
                try
                {
                    foreach (FuelPoint activeNozzleIndex in this.fuelPoints)
                    {
                        try
                        {
                            Nozzle[] nozzles = activeNozzleIndex.Nozzles;
                            for (int i = 0; i < (int)nozzles.Length; i++)
                            {
                                Nozzle nozzle = nozzles[i];
                                if (nozzle.QuerySetPrice)
                                {
                                    nozzle.ParentFuelPoint.QuerySetPrice = true;
                                }
                            }
                            if (activeNozzleIndex.QuerySetPrice)
                            {
                                if (activeNozzleIndex.NozzleCount <= 1)
                                {
                                    for (int j = 0; j <= 1; j++)
                                    {
                                        byte[] numArray = this.setPrices(activeNozzleIndex.Nozzles[0].UntiPriceInt, activeNozzleIndex.Address);
                                        this.serialPort.Write(numArray, 0, (int)numArray.Length);
                                        Thread.Sleep(350);
                                        byte[] numArray1 = new byte[this.serialPort.BytesToRead];
                                        this.serialPort.Read(numArray1, 0, this.serialPort.BytesToRead);
                                        Thread.Sleep(150);
                                        byte[] address = new byte[] { 0, (byte)(activeNozzleIndex.Address * 8 + 5), (byte)(255 - (activeNozzleIndex.Address * 8 + 5)) };
                                        this.serialPort.Write(address, 0, (int)address.Length);
                                        Thread.Sleep(100);
                                        byte[] numArray2 = new byte[this.serialPort.BytesToRead];
                                        this.serialPort.Read(numArray2, 0, this.serialPort.BytesToRead);
                                    }
                                    activeNozzleIndex.QuerySetPrice = false;
                                }
                                else
                                {
                                    for (int k = 0; k <= 1; k++)
                                    {
                                        byte[] numArray3 = this.setPriceMulti(activeNozzleIndex);
                                        this.serialPort.Write(numArray3, 0, (int)numArray3.Length);
                                        Thread.Sleep(350);
                                        byte[] numArray4 = new byte[this.serialPort.BytesToRead];
                                        this.serialPort.Read(numArray4, 0, this.serialPort.BytesToRead);
                                        Thread.Sleep(150);
                                        byte[] address1 = new byte[] { 0, (byte)(activeNozzleIndex.Address * 8 + 5), (byte)(255 - (activeNozzleIndex.Address * 8 + 5)) };
                                        this.serialPort.Write(address1, 0, (int)address1.Length);
                                        Thread.Sleep(100);
                                        byte[] numArray5 = new byte[this.serialPort.BytesToRead];
                                        this.serialPort.Read(numArray5, 0, this.serialPort.BytesToRead);
                                    }
                                    activeNozzleIndex.QuerySetPrice = false;
                                }
                            }
                            if ((
                                from n in (IEnumerable<Nozzle>)activeNozzleIndex.Nozzles
                                where n.QueryTotals
                                select n).Count<Nozzle>() > 0)
                            {
                                Nozzle[] nozzleArray = activeNozzleIndex.Nozzles;
                                for (int l = 0; l < (int)nozzleArray.Length; l++)
                                {
                                    Nozzle nozzle1 = nozzleArray[l];
                                    if (nozzle1.QueryTotals)
                                    {
                                        this.GetTotals(nozzle1);
                                        Thread.Sleep(200);
                                        if ((bool)activeNozzleIndex.GetExtendedProperty("iNeedDisplay", true))
                                        {
                                            this.GetDisplay(nozzle1);
                                            activeNozzleIndex.SetExtendedProperty("iNeedDisplay", false);
                                        }
                                        activeNozzleIndex.Initialized = true;
                                        if (this.TotalsRecieved != null)
                                        {
                                            this.TotalsRecieved(this, new TotalsEventArgs(activeNozzleIndex, nozzle1.Index, nozzle1.TotalVolume, nozzle1.TotalPrice));
                                        }
                                    }
                                }
                                continue;
                            }
                            else if (!activeNozzleIndex.QueryAuthorize)
                            {
                                if (activeNozzleIndex.Status == FuelPointStatusEnum.Work)
                                {
                                    activeNozzleIndex.SetExtendedProperty("iNeedDisplay", true);
                                    this.GetDisplay(activeNozzleIndex.ActiveNozzle);
                                }
                                FuelPointStatusEnum status = activeNozzleIndex.Status;
                                this.GetStatus(activeNozzleIndex);
                                if ((status == activeNozzleIndex.Status ? false : this.DispenserStatusChanged != null))
                                {
                                    FuelPointValues fuelPointValue = new FuelPointValues();
                                    if ((activeNozzleIndex.Status == FuelPointStatusEnum.Idle ? true : activeNozzleIndex.Status == FuelPointStatusEnum.Offline))
                                    {
                                        activeNozzleIndex.ActiveNozzleIndex = -1;
                                        fuelPointValue.ActiveNozzle = -1;
                                    }
                                    else
                                    {
                                        activeNozzleIndex.ActiveNozzleIndex = activeNozzleIndex.ActiveNozzleIndex;
                                        fuelPointValue.ActiveNozzle = activeNozzleIndex.ActiveNozzleIndex;
                                    }
                                    fuelPointValue.Status = activeNozzleIndex.Status;
                                    this.DispenserStatusChanged(this, new FuelPointValuesArgs()
                                    {
                                        CurrentFuelPoint = activeNozzleIndex,
                                        CurrentNozzleId = fuelPointValue.ActiveNozzle + 1,
                                        Values = fuelPointValue
                                    });
                                }
                            }
                            else
                            {
                                if (this.AuthorizeFuelPoint(activeNozzleIndex))
                                {
                                    activeNozzleIndex.QueryAuthorize = false;
                                }
                                continue;
                            }
                        }
                        finally
                        {
                            Thread.Sleep(80);
                        }
                    }
                }
                catch (Exception exception1)
                {
                    Exception exception = exception1;
                    Directory.CreateDirectory(string.Concat(Environment.CurrentDirectory, "\\logs"));
                    File.AppendAllText(string.Concat(Environment.CurrentDirectory, "\\logs\\NuovoPignoneError.txt"), string.Concat("\n", exception.ToString()));
                    Thread.Sleep(250);
                }
            }
        }

        public event EventHandler<FuelPointValuesArgs> DataChanged;

        public event EventHandler DispenserOffline;

        public event EventHandler<FuelPointValuesArgs> DispenserStatusChanged;

        public event EventHandler<SaleEventArgs> SaleRecieved;

        public event EventHandler<TotalsEventArgs> TotalsRecieved;

        private struct IPumpDebugArgs
        {
            public FuelPointStatusEnum status;

            public decimal totalizer;

            public decimal volume;

            public decimal amount;

            public List<byte[]> comBuffer;
        }

        private enum responseLength
        {
            auth = 2,
            status = 3,
            setprice = 4,
            unitprice = 6,
            totals = 14,
            display = 16
        }
    }
}
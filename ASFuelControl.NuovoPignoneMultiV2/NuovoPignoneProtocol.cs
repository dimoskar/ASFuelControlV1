// ASFuelControl.NuovoPignoneMultiV2, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// ASFuelControl.NuovoPignoneMulti.NuovoPignoneProtocol
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using ASFuelControl.Common;
using ASFuelControl.Common.Enumerators;
namespace ASFuelControl.NuovoPignoneMultiV2
{
    public class NuovoPignoneMultiV2Protocol : IFuelProtocol, IPumpDebug
    {
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

        private List<FuelPoint> fuelPoints = new List<FuelPoint>();

        private SerialPort serialPort = new SerialPort();

        private Thread th;

        public DebugValues foo = new DebugValues();

        public string CommunicationPort { get; set; }

        public FuelPoint[] FuelPoints
        {
            get
            {
                return fuelPoints.ToArray();
            }
            set
            {
                fuelPoints = new List<FuelPoint>(value);
            }
        }

        public bool IsConnected => serialPort.IsOpen;

        public event EventHandler<FuelPointValuesArgs> DataChanged;

        public event EventHandler DispenserOffline;

        public event EventHandler<FuelPointValuesArgs> DispenserStatusChanged;

        public event EventHandler<SaleEventArgs> SaleRecieved;

        public event EventHandler<TotalsEventArgs> TotalsRecieved;

        public void AddFuelPoint(FuelPoint fp)
        {
            fuelPoints.Add(fp);
        }

        private byte[] authoriseFuelPoint(int id)
        {
            return new byte[3]
            {
            0,
            (byte)idSetPrice(id),
            225
            };
        }

        public bool AuthorizeFuelPoint(FuelPoint fp)
        {
            try
            {
                serialPort.DiscardInBuffer();
                byte[] array = authoriseFuelPoint(fp.Address);
                serialPort.Write(array, 0, array.Length);
                int num = 0;
                while (serialPort.BytesToRead < 2 && num < 300)
                {
                    Thread.Sleep(20);
                    num += 20;
                }
                byte[] array2 = new byte[serialPort.BytesToRead];
                serialPort.Read(array2, 0, serialPort.BytesToRead);
                array = confirmAuthoriseFuelPoint(array2, fp.Address);
                serialPort.Write(array, 0, array.Length);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public void ClearFuelPoints()
        {
            fuelPoints.Clear();
        }

        private byte[] confirmAuthoriseFuelPoint(byte[] response, int id)
        {
            if (!response.SequenceEqual(new byte[2]
            {
            (byte)idSetPrice(id),
            225
            }))
            {
                throw new ArgumentException("AuthoriseFuelPoint() Failed ", "byte[] Response");
            }
            return new byte[3]
            {
            0,
            (byte)(idSetPrice(id) + 1),
            (byte)(254 - idSetPrice(id))
            };
        }

        private byte[] confirmSetPrices(byte[] response, int unitPrice, int id)
        {
            byte[] array = new byte[0];
            array = setPrices(unitPrice, id);
            if (!response.SequenceEqual(array.Skip(1).Take(4).ToArray()))
            {
                throw new ArgumentException("SetPrice() Failed ", "byte[] Response");
            }
            return new byte[5]
            {
            0,
            (byte)(idSetPrice(id) + 1),
            (byte)(254 - idSetPrice(id)),
            array[3],
            array[4]
            };
        }

        public void Connect()
        {
            try
            {
                serialPort.PortName = CommunicationPort;
                serialPort.Parity = Parity.Odd;
                serialPort.BaudRate = 2400;
                serialPort.DtrEnable = true;
                serialPort.Open();
                th = new Thread(ThreadRun);
                th.Start();
            }
            catch
            {
            }
        }

        public DebugValues DebugStatusDialog(FuelPoint fp)
        {
            foo = null;
            fp = GetStatus(fp);
            foo.Status = fp.Status;
            return foo;
        }

        public void Disconnect()
        {
            if (serialPort.IsOpen)
            {
                serialPort.Close();
            }
        }

        private Nozzle evalDisplay(Nozzle nozzle, byte[] response)
        {
            string[] array = BitConverter.ToString(response).Split('-');
            for (int i = 1; i < 8; i++)
            {
                if (StringToByteArray(array[2 * i])[0] + StringToByteArray(array[2 * i + 1])[0] != 255)
                {
                    throw new Exception("evalDisplay() Failed");
                }
            }
            string s = array[2] + array[4] + array[6];
            string s2 = array[8] + array[10] + array[12];
            nozzle.ParentFuelPoint.DispensedAmount = decimal.Parse(s) / (decimal)Math.Pow(10.0, nozzle.ParentFuelPoint.AmountDecimalPlaces);
            nozzle.ParentFuelPoint.DispensedVolume = decimal.Parse(s2) / (decimal)Math.Pow(10.0, nozzle.ParentFuelPoint.VolumeDecimalPlaces);
            if (this.DataChanged != null)
            {
                FuelPointValues fuelPointValues = new FuelPointValues();
                fuelPointValues.CurrentSalePrice = nozzle.UnitPrice;
                fuelPointValues.CurrentPriceTotal = nozzle.ParentFuelPoint.DispensedAmount;
                fuelPointValues.CurrentVolume = nozzle.ParentFuelPoint.DispensedVolume;
                FuelPointValues values = fuelPointValues;
                EventHandler<FuelPointValuesArgs> eventHandler = this.DataChanged;
                FuelPointValuesArgs fuelPointValuesArgs = new FuelPointValuesArgs();
                fuelPointValuesArgs.CurrentFuelPoint = nozzle.ParentFuelPoint;
                fuelPointValuesArgs.CurrentNozzleId = nozzle.ParentFuelPoint.ActiveNozzleIndex + 1;
                fuelPointValuesArgs.Values = values;
                eventHandler(this, fuelPointValuesArgs);
            }
            return nozzle;
        }

        private Nozzle evalMultiTotals(Nozzle nozzle, byte[] response)
        {
            if (response.Length != 0)
            {
                string[] array = BitConverter.ToString(response).Split('-');
                nozzle.TotalVolume = decimal.Parse(array[12 * (nozzle.Index - 1) + 2] + array[12 * (nozzle.Index - 1) + 4] + array[12 * (nozzle.Index - 1) + 6] + array[12 * (nozzle.Index - 1) + 8] + array[12 * (nozzle.Index - 1) + 10] + array[12 * (nozzle.Index - 1) + 12]);
                return nozzle;
            }
            return nozzle;
        }

        private Nozzle evalTotals(Nozzle nozzle, byte[] response)
        {
            string[] array = BitConverter.ToString(response).Split('-');
            for (int i = 1; i < 7; i++)
            {
                if (StringToByteArray(array[2 * i])[0] + StringToByteArray(array[2 * i + 1])[0] != 255)
                {
                    throw new Exception("evalTotals() Failed");
                }
            }
            nozzle.TotalVolume = decimal.Parse(array[2] + array[4] + array[6] + array[8] + array[10] + array[12]);
            return nozzle;
        }

        private FuelPoint evaluateStatus(FuelPoint fp, byte[] response)
        {
            FuelPoint result;
            if (response.Length != 3 || response[1] + response[2] != 255)
            {
                result = fp;
                if ((DateTime.Now - fp.LastValidResponse).TotalSeconds >= 10.0)
                {
                    fp.Status = FuelPointStatusEnum.Offline;
                }
                fp.DispenserStatus = FuelPointStatusEnum.Offline;
            }
            else
            {
                fp.LastValidResponse = DateTime.Now;
                byte b = response[2];
                if (b == byte.MaxValue || b == 251)
                {
                    fp.Status = FuelPointStatusEnum.Idle;
                }
                else if (b == 247 || b == 243)
                {
                    fp.Status = FuelPointStatusEnum.Nozzle;
                    serialPort.Write(GetNozzle(fp.Address), 0, GetNozzle(fp.Address).Length);
                    Thread.Sleep(200);
                    byte[] array = new byte[serialPort.BytesToRead];
                    serialPort.Read(array, 0, serialPort.BytesToRead);
                    string text = Convert.ToString(array[1], 2).PadLeft(8, '0');
                    string text2 = Convert.ToString(array[3], 2).PadLeft(8, '0');
                    int num = int.Parse(text.Substring(2, 1));
                    int num2 = int.Parse(text.Substring(6, 1));
                    int num3 = int.Parse(text2.Substring(2, 1));
                    int num4 = int.Parse(text2.Substring(6, 1));
                    if (num == 1)
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
                else if (b == 245 || b == 241)
                {
                    fp.Status = FuelPointStatusEnum.Work;
                }
                else if (b == 191 || b == 187)
                {
                    fp.Status = FuelPointStatusEnum.Idle;
                }
                else
                {
                    fp.Status = FuelPointStatusEnum.Idle;
                    serialPort.Write(Initialize(fp.Address), 0, 3);
                    Thread.Sleep(50);
                    byte[] buffer = new byte[serialPort.BytesToRead];
                    serialPort.Read(buffer, 0, serialPort.BytesToRead);
                }
                fp.DispenserStatus = fp.Status;
                result = fp;
            }
            return result;
        }

        private Nozzle evalUnitPrice(Nozzle nozzle, byte[] response)
        {
            string text = BitConverter.ToString(response);
            string text2 = text.Substring(6, 2);
            string text3 = text.Substring(12, 2);
            nozzle.UntiPriceInt = int.Parse(text2 + text3);
            nozzle.UnitPrice = (decimal)nozzle.UntiPriceInt / (decimal)Math.Pow(10.0, nozzle.ParentFuelPoint.UnitPriceDecimalPlaces);
            return nozzle;
        }

        public Nozzle GetDisplay(Nozzle nozzle)
        {
            byte[] array = requestDisplayData(nozzle.ParentFuelPoint.Address);
            serialPort.Write(array, 0, array.Length);
            Thread.Sleep(350);
            byte[] array2 = new byte[serialPort.BytesToRead];
            serialPort.Read(array2, 0, serialPort.BytesToRead);
            return evalDisplay(nozzle, array2);
        }

        private byte[] getFuelPointStatus(int id)
        {
            return new byte[2]
            {
            0,
            (byte)idGetStatus(id)
            };
        }

        public static byte[] GetNozzle(int Address)
        {
            byte b = BitConverter.GetBytes(6 + 8 * Address)[0];
            return new byte[2] { 0, b };
        }

        public FuelPoint GetStatus(FuelPoint fp)
        {
            byte[] fuelPointStatus = getFuelPointStatus(fp.Address);
            serialPort.Write(fuelPointStatus, 0, fuelPointStatus.Length);
            int num = 0;
            while (serialPort.BytesToRead < 3 && num < 300)
            {
                Thread.Sleep(15);
                num += 20;
            }
            byte[] array = new byte[serialPort.BytesToRead];
            serialPort.Read(array, 0, serialPort.BytesToRead);
            return evaluateStatus(fp, array);
        }

        public Nozzle GetTotals(Nozzle nozzle)
        {
            if (nozzle.ParentFuelPoint.Nozzles.Count() > 1)
            {
                byte[] volumeTotalsMulti = GetVolumeTotalsMulti(nozzle.ParentFuelPoint.Address);
                serialPort.Write(volumeTotalsMulti, 0, volumeTotalsMulti.Length);
                Thread.Sleep(400);
                byte[] array = new byte[serialPort.BytesToRead];
                serialPort.Read(array, 0, serialPort.BytesToRead);
                if (array.Length != 0)
                {
                    nozzle = evalMultiTotals(nozzle, array);
                }
                return nozzle;
            }
            byte[] array2 = requestTotals(nozzle.ParentFuelPoint.Address);
            serialPort.Write(array2, 0, array2.Length);
            Thread.Sleep(400);
            byte[] array3 = new byte[serialPort.BytesToRead];
            serialPort.Read(array3, 0, serialPort.BytesToRead);
            if (array3.Length != 0)
            {
                nozzle = evalTotals(nozzle, array3);
            }
            return nozzle;
        }

        public Nozzle GetUnitPrice(Nozzle nozzle)
        {
            byte[] array = requestUnitPrice(nozzle.Index);
            serialPort.Write(array, 0, array.Length);
            int num = 0;
            while (serialPort.BytesToRead < 6 && num < 300)
            {
                Thread.Sleep(15);
                num += 20;
            }
            byte[] array2 = new byte[serialPort.BytesToRead];
            serialPort.Read(array2, 0, serialPort.BytesToRead);
            return evalUnitPrice(nozzle, array2);
        }

        private static byte[] GetVolumeTotalsMulti(int Address)
        {
            byte b = BitConverter.GetBytes(2 + 8 * Address)[0];
            byte b2 = BitConverter.GetBytes(45)[0];
            return new byte[3] { 0, b, b2 };
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
            byte b = BitConverter.GetBytes(7 + 8 * Address)[0];
            byte b2 = BitConverter.GetBytes(255 - (7 + 8 * Address))[0];
            return new byte[3] { 0, b, b2 };
        }

        private int ith(int num)
        {
            return 16 * (num / 10) + num % 10;
        }

        public int MinimumTimeNeeded(int cmdl, int respl)
        {
            double num = 1 / (serialPort.BaudRate / 8000);
            return (int)((double)(cmdl + respl) * num);
        }

        private byte[] requestDisplayData(int id)
        {
            return new byte[3]
            {
            0,
            (byte)idGetDisplay(id),
            180
            };
        }

        private byte[] requestTotals(int id)
        {
            return new byte[3]
            {
            0,
            (byte)idGetDisplay(id),
            135
            };
        }

        private byte[] requestUnitPrice(int id)
        {
            return new byte[3]
            {
            0,
            (byte)idGetDisplay(id),
            196
            };
        }

        public bool SetPrice(Nozzle nozzle, int unitPrice)
        {
            try
            {
                if (nozzle.ParentFuelPoint.Nozzles.Count() > 1)
                {
                    byte[] array = setPriceMulti(nozzle.ParentFuelPoint);
                    serialPort.Write(array, 0, array.Length);
                    Thread.Sleep(350);
                    byte[] buffer = new byte[serialPort.BytesToRead];
                    serialPort.Read(buffer, 0, serialPort.BytesToRead);
                    Thread.Sleep(150);
                    byte[] array2 = new byte[3]
                    {
                    0,
                    (byte)(nozzle.ParentFuelPoint.Address * 8 + 5),
                    (byte)(255 - (nozzle.ParentFuelPoint.Address * 8 + 5))
                    };
                    serialPort.Write(array2, 0, array2.Length);
                    Thread.Sleep(100);
                    byte[] buffer2 = new byte[serialPort.BytesToRead];
                    serialPort.Read(buffer2, 0, serialPort.BytesToRead);
                }
                else
                {
                    byte[] array3 = setPrices(unitPrice, nozzle.ParentFuelPoint.Address);
                    serialPort.Write(array3, 0, array3.Length);
                    int num = 0;
                    while (serialPort.BytesToRead < 4 && num < 300)
                    {
                        Thread.Sleep(20);
                        num += 20;
                    }
                    byte[] array4 = new byte[serialPort.BytesToRead];
                    serialPort.Read(array4, 0, serialPort.BytesToRead);
                    array3 = confirmSetPrices(array4, unitPrice, nozzle.ParentFuelPoint.Address);
                    serialPort.Write(array3, 0, array3.Length);
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        private byte[] setPriceMulti(FuelPoint fp)
        {
            byte[] array = new byte[15]
            {
            0, 12, 165, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0
            };
            array[1] = (byte)(fp.Address * 8 + 4);
            for (int i = 0; i <= fp.Nozzles.Count() - 1; i++)
            {
                string text = Convert.ToString(fp.Nozzles[i].UntiPriceInt);
                text = text.PadLeft(4, '0');
                int num = Convert.ToInt16(text.Substring(0, 2));
                int num2 = Convert.ToInt16(text.Substring(2, 2));
                array[3 + i * 2] = (byte)ith(num);
                array[4 + i * 2] = (byte)ith(num2);
            }
            return array;
        }

        private byte[] setPrices(int unitPrice, int id)
        {
            if (unitPrice >= 9999)
            {
                throw new ArgumentException("max value 9999", "unitPrice");
            }
            string text = Convert.ToString(unitPrice);
            text = text.PadLeft(4, '0');
            int num = Convert.ToInt16(text.Substring(0, 2));
            int num2 = Convert.ToInt16(text.Substring(2, 2));
            return new byte[5]
            {
            0,
            (byte)idSetPrice(id),
            105,
            (byte)ith(num),
            (byte)ith(num2)
            };
        }

        public byte[] StringToByteArray(string hex)
        {
            return (from x in Enumerable.Range(0, hex.Length)
                    where x % 2 == 0
                    select Convert.ToByte(hex.Substring(x, 2), 16)).ToArray();
        }

        private void ThreadRun()
        {
            foreach (FuelPoint fuelPoint in fuelPoints)
            {
                fuelPoint.Nozzles[0].QueryTotals = true;
                fuelPoint.QuerySetPrice = true;
            }
            while (IsConnected)
            {
                try
                {
                    foreach (FuelPoint fuelPoint2 in fuelPoints)
                    {
                        try
                        {
                            Nozzle[] nozzles = fuelPoint2.Nozzles;
                            foreach (Nozzle nozzle in nozzles)
                            {
                                if (nozzle.QuerySetPrice)
                                {
                                    nozzle.ParentFuelPoint.QuerySetPrice = true;
                                }
                            }
                            if (fuelPoint2.QuerySetPrice)
                            {
                                if (fuelPoint2.NozzleCount > 1)
                                {
                                    for (int j = 0; j <= 1; j++)
                                    {
                                        byte[] array = setPriceMulti(fuelPoint2);
                                        serialPort.Write(array, 0, array.Length);
                                        Thread.Sleep(350);
                                        byte[] buffer = new byte[serialPort.BytesToRead];
                                        serialPort.Read(buffer, 0, serialPort.BytesToRead);
                                        Thread.Sleep(150);
                                        byte[] array2 = new byte[3]
                                        {
                                        0,
                                        (byte)(fuelPoint2.Address * 8 + 5),
                                        (byte)(255 - (fuelPoint2.Address * 8 + 5))
                                        };
                                        serialPort.Write(array2, 0, array2.Length);
                                        Thread.Sleep(100);
                                        byte[] buffer2 = new byte[serialPort.BytesToRead];
                                        serialPort.Read(buffer2, 0, serialPort.BytesToRead);
                                    }
                                    fuelPoint2.QuerySetPrice = false;
                                }
                                else
                                {
                                    for (int k = 0; k <= 1; k++)
                                    {
                                        byte[] array3 = setPrices(fuelPoint2.Nozzles[0].UntiPriceInt, fuelPoint2.Address);
                                        serialPort.Write(array3, 0, array3.Length);
                                        Thread.Sleep(350);
                                        byte[] buffer3 = new byte[serialPort.BytesToRead];
                                        serialPort.Read(buffer3, 0, serialPort.BytesToRead);
                                        Thread.Sleep(150);
                                        byte[] array4 = new byte[3]
                                        {
                                        0,
                                        (byte)(fuelPoint2.Address * 8 + 5),
                                        (byte)(255 - (fuelPoint2.Address * 8 + 5))
                                        };
                                        serialPort.Write(array4, 0, array4.Length);
                                        Thread.Sleep(100);
                                        byte[] buffer4 = new byte[serialPort.BytesToRead];
                                        serialPort.Read(buffer4, 0, serialPort.BytesToRead);
                                    }
                                    fuelPoint2.QuerySetPrice = false;
                                }
                            }
                            if (fuelPoint2.Nozzles.Where((Nozzle n) => n.QueryTotals).Count() > 0)
                            {
                                Nozzle[] nozzles2 = fuelPoint2.Nozzles;
                                foreach (Nozzle nozzle2 in nozzles2)
                                {
                                    if (nozzle2.QueryTotals)
                                    {
                                        GetTotals(nozzle2);
                                        Thread.Sleep(200);
                                        if ((bool)fuelPoint2.GetExtendedProperty("iNeedDisplay", true))
                                        {
                                            GetDisplay(nozzle2);
                                            fuelPoint2.SetExtendedProperty("iNeedDisplay", false);
                                        }
                                        fuelPoint2.Initialized = true;
                                        if (this.TotalsRecieved != null)
                                        {
                                            this.TotalsRecieved(this, new TotalsEventArgs(fuelPoint2, nozzle2.Index, nozzle2.TotalVolume, nozzle2.TotalPrice));
                                        }
                                    }
                                }
                                continue;
                            }
                            if (fuelPoint2.QueryAuthorize)
                            {
                                if (AuthorizeFuelPoint(fuelPoint2))
                                {
                                    fuelPoint2.QueryAuthorize = false;
                                }
                                continue;
                            }
                            if (fuelPoint2.Status == FuelPointStatusEnum.Work)
                            {
                                fuelPoint2.SetExtendedProperty("iNeedDisplay", true);
                                GetDisplay(fuelPoint2.ActiveNozzle);
                            }
                            FuelPointStatusEnum status = fuelPoint2.Status;
                            GetStatus(fuelPoint2);
                            if (status != fuelPoint2.Status && this.DispenserStatusChanged != null)
                            {
                                FuelPointValues fuelPointValues = new FuelPointValues();
                                if (fuelPoint2.Status != FuelPointStatusEnum.Idle && fuelPoint2.Status != 0)
                                {
                                    fuelPoint2.ActiveNozzleIndex = fuelPoint2.ActiveNozzleIndex;
                                    fuelPointValues.ActiveNozzle = fuelPoint2.ActiveNozzleIndex;
                                }
                                else
                                {
                                    fuelPoint2.ActiveNozzleIndex = -1;
                                    fuelPointValues.ActiveNozzle = -1;
                                }
                                fuelPointValues.Status = fuelPoint2.Status;
                                EventHandler<FuelPointValuesArgs> eventHandler = this.DispenserStatusChanged;
                                FuelPointValuesArgs fuelPointValuesArgs = new FuelPointValuesArgs();
                                fuelPointValuesArgs.CurrentFuelPoint = fuelPoint2;
                                fuelPointValuesArgs.CurrentNozzleId = fuelPointValues.ActiveNozzle + 1;
                                fuelPointValuesArgs.Values = fuelPointValues;
                                eventHandler(this, fuelPointValuesArgs);
                            }
                        }
                        finally
                        {
                            Thread.Sleep(100);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Directory.CreateDirectory(Environment.CurrentDirectory + "\\logs");
                    File.AppendAllText(Environment.CurrentDirectory + "\\logs\\NuovoPignoneError.txt", "\n" + ex.ToString());
                    Thread.Sleep(250);
                }
            }
        }
    }
}
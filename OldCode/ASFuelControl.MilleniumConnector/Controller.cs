using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading;

namespace ASFuelControl.MilleniumConnector
{
    public class Controller
    {
        public static byte[] StatusNZ = new byte[] { 0xFD, 0x02, 0x00, 0x09, 0x01, 0x00, 0x00, 0x00, 0x04, 0x01, 0x20, 0x14, 0x15, 0xFC, 0x30, 0x30, 0x35, 0x33, 0xFB };

        public static byte[] AuthoriseNZ1 = new byte[]   { 0xFD, 0x01, 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x07, 0x01, 0x21, 0x19, 0x01, 0x0F, 0x3E, 0x00, 0xFC, 0x30, 0x30, 0x44, 0x34, 0xFB };
        public static byte[] AuthoriseNZ2 = new byte[]   { 0xFD, 0x01, 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x07, 0x01, 0x22, 0x19, 0x01, 0x01, 0x3E, 0x00, 0xFC, 0x30, 0x30, 0x43, 0x37, 0xFB };

        //Akkac^ Til^r MF1
        public static byte[] SetPrice_1_NZ1 = new byte[] { 0xFD, 0x01, 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x0D, 0x06, 0x61, 0x00, 0x00, 0x00, 0x01, 0x11, 0x02, 0x04, 0x03, 0x00, 0x25, 0x61, 0xFC, 0x30, 0x31, 0x35, 0x39, 0xFB };
        public static byte[] SetPrice_2_NZ1 = new byte[] { 0xFD, 0x01, 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x0D, 0x06, 0x61, 0x00, 0x00, 0x00, 0x02, 0x11, 0x02, 0x04, 0x03, 0x00, 0x25, 0x61, 0xFC, 0x30, 0x31, 0x35, 0x41, 0xFB };
        
        //Akkac^ Til^r MF2
        public static byte[] SetPrice_1_NZ2 = new byte[] { 0xFD, 0x01, 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x0D, 0x06, 0x61, 0x00, 0x00, 0x00, 0x01, 0x12, 0x02, 0x04, 0x03, 0x00, 0x25, 0x61, 0xFC, 0x30, 0x31, 0x35, 0x41, 0xFB };
        public static byte[] SetPrice_2_NZ2 = new byte[] { 0xFD, 0x01, 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x0D, 0x06, 0x61, 0x00, 0x00, 0x00, 0x02, 0x12, 0x02, 0x04, 0x03, 0x00, 0x25, 0x61, 0xFC, 0x30, 0x31, 0x35, 0x42, 0xFB };



        public static byte[] E0Response = new byte[] { 0xFD, 0x01, 0x00, 0x02, 0x01, 0x00, 0x00, 0x00, 0x04, 0x01, 0x21, 0x14, 0x15, 0xFC, 0x30, 0x30, 0x35, 0x33, 0xFB};
        public static byte[] StartNozzle = new byte[] { 0xFD, 0x01, 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x04, 0x01, 0x21, 0x3C, 0x00, 0xFC, 0x30, 0x30, 0x41, 0x36, 0xFB };
        public static byte[] GetTotalsNZ = new byte[] { 0xFD, 0x01, 0x00, 0x02, 0x01, 0x00, 0x00, 0x00, 0x06, 0x02, 0x21, 0x11, 0x14, 0x15, 0x16, 0xFC, 0x30, 0x30, 0x37, 0x44, 0xFB, 0xFF, 0x01 };
        public static byte[] GetDisplayNZ = new byte[] { 0xFD, 0x01, 0x00, 0x02, 0x01, 0x00, 0x00, 0x00, 0x05, 0x01, 0x21, 0x22, 0x23, 0x1D, 0xFC, 0x30, 0x30, 0x38, 0x44, 0xFB };
        public static byte[] GetData = new byte[] { 0xFF, 0x01 };
        



        
        //***Start Commands***//        
        
        public static byte[] StartPump = new byte[] { 0xFD, 0x01, 0x00, 0x02, 0x01, 0x00, 0x01, 0x00, 0x08, 0x01, 0x01, 0x02, 0x05, 0x04, 0x28, 0x29, 0x2A, 0xFC, 0x30, 0x30, 0x39, 0x35, 0xFB };

        public static byte[] SetPumpToConsole = new byte[] { 0xFD, 0x01, 0x00, 0x02, 0x01, 0x00, 0x00, 0x00, 0x03, 0x01, 0x81, 0x04, 0xFC, 0x30, 0x30, 0x38, 0x44, 0xFB};

        public static byte[] StartComm3 = new byte[] { 0xFD, 0x01, 0x00, 0x02, 0x01, 0x00, 0x04, 0x00, 0x03, 0x01, 0x82, 0x04, 0xFC, 0x30, 0x30, 0x39, 0x32, 0xFB};
        //Response FD 02 01 01 00 00 20 00 05 01 81 04 01 00 FC 30 30 42 30 FB FE

        public static byte[] StartComm6 = new byte[] { 0xFD, 0x01, 0x00, 0x02, 0x01, 0x00, 0x00, 0x00, 0x06, 0x02, 0x21, 0x11, 0x14, 0x15, 0x16, 0xFC, 0x30, 0x30, 0x37, 0x44, 0xFB };
        //Response FD 02 01 01 00 00 20 00 1E [02 21] 11 14 07 0A 00 00 00 00 69 27 15 07 0A 00 00 00 00 91 38 16 07 0C 00 00 00 00 00 17 FC 30 32 35 41 FB FE 

        public static byte[] StartComm7 = new byte[] { 0xFD, 0x01, 0x00, 0x02, 0x01, 0x00, 0x00, 0x00, 0x06, 0x02, 0x22, 0x11, 0x14, 0x15, 0x16, 0xFC, 0x30, 0x30, 0x37, 0x45, 0xFB };
        //Response FD 02 01 01 00 00 20 00 1E [02 22] 11 14 07 0A 00 00 12 20 90 58 15 07 0A 00 00 16 73 31 80 16 07 0C 00 00 00 00 11 68 FC 30 33 42 38 FB FE

        public static byte[] StartComm8 = new byte[] { 0xFD, 0x01, 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x05, 0x01, 0x21, 0xD8, 0x01, 0x00, 0xFC, 0x30, 0x31, 0x34, 0x34, 0xFB, 0xFD, 0x01, 0x00, 0x02, 0x01, 0x00, 0x40, 0x00, 0x05, 0x01, 0x22, 0xD8, 0x01, 0x00, 0xFC, 0x30, 0x31, 0x34, 0x35, 0xFB };
        //Response FD 02 01 01 00 00 E0 00 05 [01 21] 00 D8 00 FC 30 31 45 33 FB FD 02 01 01 00 00 E0 00 05 [01 22] 00 D8 00 FC 30 31 45 34 FB FE 

        public static byte[] StartComm9 = new byte[] { 0xFD, 0x01, 0x00, 0x02, 0x01, 0x00, 0x00, 0x00, 0x07, 0x04, 0x21, 0x22, 0x00, 0x00, 0x01, 0x15, 0xFC, 0x30, 0x30, 0x36, 0x38, 0xFB, 0xFD, 0x01, 0x00, 0x02, 0x01, 0x00, 0x00, 0x00, 0x07, 0x04, 0x21, 0x23, 0x00, 0x00, 0x01, 0x15, 0xFC, 0x30, 0x30, 0x36, 0x39, 0xFB };
        //Response FD 02 01 01 00 00 20 00 0C 04 [21 22] 00 00 01 02 00 17 15 01 01 FC 30 30 41 38 FB FD 02 01 01 00 00 20 00 0C 04 21 23 00 00 01 02 AA AA 15 01 01 FC 30 31 45 36 FB FE 

        public static byte[] StartComm10 = new byte[] { 0xFD, 0x01, 0x00, 0x02, 0x01, 0x00, 0x00, 0x00, 0x07, 0x04, 0x22, 0x22, 0x00, 0x00, 0x01, 0x15, 0xFC, 0x30, 0x30, 0x36, 0x39, 0xFB, 0xFD, 0x01, 0x00, 0x02, 0x01, 0x00, 0x00, 0x00, 0x07, 0x04, 0x22, 0x23, 0x00, 0x00, 0x01, 0x15, 0xFC, 0x30, 0x30, 0x36, 0x41, 0xFB };
        //Response FD 02 01 01 00 00 20 00 0C 04 [22 22] 00 00 01 02 11 68 15 01 01 FC 30 31 30 42 FB FD 02 01 01 00 00 20 00 0C 04 22 23 00 00 01 02 01 83 15 01 01 FC 30 31 31 37 FB FE 


        /*********************************************************************Dimitris***********************************/


        bool startpumpFlag = false;



        private void StartPumpCommand()
        {
            if (!startpumpFlag) // Ean True -> Ekkinisi apostoleis olwn autwn
            {
                this.serialPort.Write(StartPump, 0, StartPump.Length);
                Thread.Sleep(50);
                this.serialPort.Write(StartPump, 0, StartPump.Length);
                Thread.Sleep(50);

                this.serialPort.Write(SetPrice_1_NZ1, 0, SetPrice_1_NZ1.Length);
                Thread.Sleep(50);
                this.serialPort.Write(SetPrice_2_NZ1, 0, SetPrice_2_NZ1.Length);
                Thread.Sleep(50);


                this.serialPort.Write(SetPrice_1_NZ2, 0, SetPrice_1_NZ2.Length);
                Thread.Sleep(50);
                this.serialPort.Write(SetPrice_2_NZ2, 0, SetPrice_2_NZ2.Length);
                Thread.Sleep(50);

                startpumpFlag = true;
            }


        }


        private void Authorise(Nozzle nz)
        {
            if(nz.ParentFuelPoint.Status == Common.Enumerators.FuelPointStatusEnum.Nozzle)
            {
                if (nz.NozzleIndex == 0)
                {
                    this.serialPort.Write(SetPrice_1_NZ1, 0, SetPrice_1_NZ1.Length);
                    this.serialPort.Write(GetData, 0, GetData.Length);
                    Thread.Sleep(25);
                    this.serialPort.Write(SetPrice_2_NZ1, 0, SetPrice_2_NZ1.Length);
                    this.serialPort.Write(GetData, 0, GetData.Length);
                    Thread.Sleep(25);
                    this.serialPort.Write(AuthoriseNZ1, 0, AuthoriseNZ1.Length);
                    this.serialPort.Write(GetData, 0, GetData.Length);
                    Thread.Sleep(25);
                }
                else if (nz.NozzleIndex == 0)
                {
                    this.serialPort.Write(SetPrice_1_NZ2, 0, SetPrice_1_NZ2.Length);
                    this.serialPort.Write(GetData, 0, GetData.Length);
                    Thread.Sleep(25);
                    this.serialPort.Write(SetPrice_2_NZ2, 0, SetPrice_2_NZ2.Length);
                    this.serialPort.Write(GetData, 0, GetData.Length);
                    Thread.Sleep(25);
                    this.serialPort.Write(AuthoriseNZ2, 0, AuthoriseNZ2.Length);
                    this.serialPort.Write(GetData, 0, GetData.Length);
                    Thread.Sleep(25);
                }

            }

        }

        private void States()
        {

        }

        /****************************************************************************************************************/



        public event EventHandler<ValuesChangedEventArgs> ValuesChanged;
        public event EventHandler<StatusChangedEventArgs> StatusChanged;
        public event EventHandler<TotalsUpdatedEventArgs> TotalsUpdated;

        SerialPort serialPort = new SerialPort();
        private List<FuelPoint> fuelPoints = new List<FuelPoint>();
        private System.Threading.Thread th = null;

        public string CoomunicationPort { set; get; }
        public int CoomunicationSpeed { set; get; }

        public bool IsConnected
        {
            get { return this.serialPort.IsOpen; }
        }

        public FuelPoint[] FuelPoints
        {
            get { return this.fuelPoints.ToArray(); }
        }

        public Controller()
        {
            
        }

        public FuelPoint AddFuelPoint(int addressId, int nozzleCount)
        {
            FuelPoint fp = new FuelPoint(nozzleCount);
            this.fuelPoints.Add(fp);
            fp.AddressId = addressId;
            return fp;
        }

        public void Connect(string port)
        {
            if (serialPort.IsOpen)
                return;

            string[] param = port.Split(';');
            if (param.Length == 1)
            {
                this.serialPort.PortName = param[0];
                this.serialPort.Open();
            }
            else if (param.Length == 2)
            {
                this.serialPort.PortName = param[0];
                this.serialPort.BaudRate = int.Parse(param[1]);
                this.serialPort.Open();
            }
            this.serialPort.DataReceived -= new SerialDataReceivedEventHandler(serialPort_DataReceived);
            this.serialPort.DataReceived += new SerialDataReceivedEventHandler(serialPort_DataReceived);
            th = new System.Threading.Thread(new System.Threading.ThreadStart(this.ThreadRun));
            th.Start();
        }

        public void Disconnect()
        {
            if (this.serialPort.IsOpen)
                this.serialPort.Close();
        }

        public void AuthorizeNozzle(Nozzle nz)
        {
            nz.QueryAuthorize = true;
            this.Authorise(nz);
        }

        public void GetTotalsNozzle(Nozzle nz)
        {
            nz.QueryTotals = true;
            this.GetTotals(nz);
        }

        private void GetTotals(Nozzle nz)
        {
            byte[] buffer = GetTotalsNZ;
            buffer[1] = (byte)nz.ParentFuelPoint.AddressId;
            buffer[10] = (byte)(32 + nz.NozzleIndex);
            this.serialPort.Write(buffer, 0, buffer.Length);
            Thread.Sleep(50);
        }

        //private void Authorize(Nozzle nz)
        //{
        //    int part1 = nz.UnitPrice / 100;
        //    int part2 = nz.UnitPrice - (part1 * 100);
        //    byte b1 = byte.Parse(part1.ToString());
        //    byte b2 = byte.Parse(part2.ToString());

        //    byte[] priceBuffer = SetPrice;
        //    priceBuffer[3] = (byte)nz.ParentFuelPoint.AddressId;
        //    priceBuffer[15] = (byte)(16 + nz.NozzleIndex);
        //    priceBuffer[20] = b1;
        //    priceBuffer[21] = b2;
        //    this.serialPort.Write(priceBuffer, 0, priceBuffer.Length);
        //    Thread.Sleep(50);

        //    byte[] buffer = AuthoriseNZ;
        //    buffer[3] = (byte)nz.ParentFuelPoint.AddressId;
        //    buffer[10] = (byte)(32 + nz.NozzleIndex);
        //    this.serialPort.Write(buffer, 0, buffer.Length);
        //    Thread.Sleep(50);
        //}

      /*  private void StartUpNozzle(Nozzle nz)
        {
            byte[] buffer = StartNozzle;
            buffer[1] = (byte)nz.ParentFuelPoint.AddressId;
            buffer[10] = (byte)(32 + nz.NozzleIndex);
            buffer[17] = (byte)(53 + nz.NozzleIndex);
            
            
            this.serialPort.Write(buffer, 0, buffer.Length);
            Thread.Sleep(50);
            byte[] getDataBuffer = GetData;
            getDataBuffer[1] = (byte)nz.ParentFuelPoint.AddressId;
            this.serialPort.Write(GetData, 0, GetData.Length);
            Thread.Sleep(50);
        }*/

        int totalSleep = 200;

        private void ThreadRun()
        {
            this.StartPumpCommand();
            while (this.serialPort.IsOpen)
            {
                try
                {
                    int sleep = totalSleep;
                    foreach (FuelPoint fp in this.fuelPoints)
                    {
                        List<byte> statusBuffer = new List<byte>();
                        foreach (Nozzle nz in fp.Nozzles)
                        {
                            byte[] buffer = StatusNZ;
                            buffer[3] = (byte)fp.AddressId;
                            buffer[10] = (byte)(32 + nz.NozzleIndex);
                            statusBuffer.AddRange(buffer);
                        }
                        this.serialPort.Write(statusBuffer.ToArray(), 0, statusBuffer.Count);
                        Thread.Sleep(50);
                        sleep = sleep - 50;
                        byte[] getDataBuffer = GetData;
                        getDataBuffer[1] = (byte)fp.AddressId;
                        this.serialPort.Write(GetData, 0, GetData.Length);
                    }
                    
                    if (sleep > 0)
                        Thread.Sleep(sleep);
                    else
                        Thread.Sleep(50);
                }
                catch
                {
                }
            }
        }

        private IEnumerable<int> PatternAt(byte[] source, byte[] pattern)
        {
            for (int i = 0; i < source.Length; i++)
            {
                if (source.Skip(i).Take(pattern.Length).SequenceEqual(pattern))
                {
                    yield return i;
                }
            }
        }

        private List<Response> GetResponces()
        {
            byte[] startWord = new byte[]{0xFD, 0x02};
            byte[] endWord = new byte[] { 0xFB };
            byte[] localBuffer = this.buffer.ToArray();
            var qs = PatternAt(localBuffer, startWord);
            var qe = PatternAt(localBuffer, endWord);
            int lastEnd = 0;
            List<Response> responses = new List<Response>();
            foreach (int start in qs)
            {
                int end = qe.Where(q => q > start).FirstOrDefault();
                if (end <= 0)
                    continue;
                if (start < lastEnd)
                    continue;
                byte[] responseBuffer = localBuffer.Skip(start).Take(end - start + 1).ToArray();
                Response response = new Response();
                response.Buffer = responseBuffer;
                lastEnd = end;
                if (responseBuffer.Length == 36)
                    response.ResponseType = ResponseTypeEnum.DisplayData;
                else if (responseBuffer.Length == 46)
                    response.ResponseType = ResponseTypeEnum.Totals;
                else if (responseBuffer.Length == 20 && responseBuffer[6] == 0xE0)
                    response.ResponseType = ResponseTypeEnum.E0;
                else if ((responseBuffer.Length - 1) % 23 == 0)
                    response.ResponseType = ResponseTypeEnum.Status;
                else
                {
                    Console.WriteLine(BitConverter.ToString(responseBuffer));
                    continue;
                }
                responses.Add(response);
            }
            return responses;
        }
     
        

        private void GetDisplayData(Nozzle nz)
        {
            byte[] buffer = GetDisplayNZ;
            buffer[1] = (byte)nz.ParentFuelPoint.AddressId;
            buffer[10] = (byte)(32 + nz.NozzleIndex);
            this.serialPort.Write(buffer, 0, buffer.Length);
        }

        private void EvaluateDisplayDataResponse(byte[] buffer)
        {
            int address = buffer[3];
            int nozzle = buffer[10] - 33;

            FuelPoint fp = this.fuelPoints.Where(f => f.AddressId == address).FirstOrDefault();
            if (fp == null)
                return;
            Nozzle nz = fp.Nozzles[nozzle];

            byte[] priceBuffer = buffer.Skip(14).Take(3).ToArray();
            byte[] volBuffer = buffer.Skip(21).Take(3).ToArray();

            int part1 = int.Parse(priceBuffer.ElementAt(0).ToString());
            int part2 = int.Parse(priceBuffer.ElementAt(1).ToString());
            int part3 = int.Parse(priceBuffer.ElementAt(2).ToString());

            nz.SalePrice = (decimal)((double)(part1 * 10000 + part2 * 100 + part3) / Math.Pow(10, 2));

            part1 = int.Parse(volBuffer.ElementAt(0).ToString());
            part2 = int.Parse(volBuffer.ElementAt(1).ToString());
            part3 = int.Parse(volBuffer.ElementAt(2).ToString());

            nz.SaleVolume = (decimal)((double)(part1 * 10000 + part2 * 100 + part3) / Math.Pow(10, 2));
            fp.LastSaleUnitPrice = nz.SaleUnitPrice;
            fp.LastSaleVolume = nz.SaleVolume;
            fp.LastSalePrice = nz.SalePrice;
            if (this.ValuesChanged != null)
                this.ValuesChanged(this, new ValuesChangedEventArgs() { FuelPoint = fp });
        }

        private void EvaluateStatusResponse(byte[] buffer)
        {
            if ((buffer.Length - 1) % 23 != 0)
                return;
            int statuses = buffer.Length / 23;
            for (int i = 0; i < statuses; i++)
            {
                byte[] statusBuffer = buffer.Skip(23 * i).Take(19).ToArray();

                /*
                 * 
                 * StatusEnumeration from Nozzle
                 * 
                 * FD 02 01 01 00 00 20 00 08 01 21 14 01 [02] 15 01 00 FC 30 30 37 43 FB FE		//////////////////// LOCKED N1
                 * FD 02 01 01 00 00 20 00 08 01 21 14 01 [03] 15 01 00 FC 30 30 37 43 FB FE		//////////////////// IDLE N1
                 * FD 02 01 01 00 00 20 00 08 01 21 14 01 [04] 15 01 00 FC 30 30 37 43 FB FE		//////////////////// NOZZLE N1
                 * FD 02 01 01 00 00 20 00 08 01 21 14 01 [06] 15 01 00 FC 30 30 37 43 FB FE		//////////////////// READY N1
                 * FD 02 01 01 00 00 20 00 08 01 21 14 01 [08] 15 01 00 FC 30 30 37 43 FB FE		//////////////////// WORK N1
                 * 
                 * */


                int st = statusBuffer[13];
                int address = statusBuffer[3];
                int nozzle = statusBuffer[10] - 33;
                //st = st - (2 * nozzle);
                FuelPoint fp = this.fuelPoints.Where(f => f.AddressId == address).FirstOrDefault();
                if (fp == null)
                    continue;
                Nozzle nz = fp.Nozzles[nozzle];
                fp.ActiveNozzle = nz;
                Common.Enumerators.FuelPointStatusEnum newStatus = fp.Status;
                switch(st)
                {
                    case 2:
                        newStatus = Common.Enumerators.FuelPointStatusEnum.Offline;
                        break;
                    case 3:
                        newStatus = Common.Enumerators.FuelPointStatusEnum.Idle;
                        break;
                    case 4:
                        newStatus = Common.Enumerators.FuelPointStatusEnum.Nozzle;
                        nz.QueryAuthorize = true;
                        break;
                    case 6:
                        newStatus = Common.Enumerators.FuelPointStatusEnum.Ready;
                        nz.QueryAuthorize = false;
                        break;
					case 8:
                        newStatus = Common.Enumerators.FuelPointStatusEnum.Work;
						nz.QueryAuthorize = false;
						this.GetDisplayData(nz);
                        break;
                }
                if (fp.Status != newStatus)
                {
                    if(this.StatusChanged != null)
                        this.StatusChanged(this, new StatusChangedEventArgs(){ FuelPoint = fp, PreviousStatus = fp.PreviousStatus, Status = fp.Status});
                }
            }
        }

        private void EvaluateTotalsResponse(byte[] buffer)
        {
            int address = buffer[3];
            int nozzle = buffer[10] - 33;

            FuelPoint fp = this.fuelPoints.Where(f => f.AddressId == address).FirstOrDefault();
            if (fp == null)
                return;
            Nozzle nz = fp.Nozzles[nozzle];
            byte[] volBuffer = buffer.Skip(16).Take(5).ToArray();
            //byte[] priceBuffer = buffer.Skip(24).Take(6).ToArray();

            long part1 = int.Parse(volBuffer.ElementAt(0).ToString());
            long part2 = int.Parse(volBuffer.ElementAt(1).ToString());
            long part3 = int.Parse(volBuffer.ElementAt(2).ToString());
            long part4 = int.Parse(volBuffer.ElementAt(3).ToString());
            long part5 = int.Parse(volBuffer.ElementAt(4).ToString());
            //long part6 = int.Parse(volBuffer.ElementAt(5).ToString());

            nz.Totalizer = (decimal)((double)(part1 * 100000000 + part2 * 1000000 + part3 * 10000 + part4 * 100 + part5) / Math.Pow(10, 2));
            nz.QueryTotals = false;
            if (this.TotalsUpdated != null)
                this.TotalsUpdated(this, new TotalsUpdatedEventArgs() { Nozzle = nz });
        }

        private void EvaluateE0Response(byte[] buffer)
        {
            int address = buffer[3];

            FuelPoint fp = this.fuelPoints.Where(f => f.AddressId == address).FirstOrDefault();
            if (fp == null)
                return;
            byte[] buf = E0Response;
            buf[1] = (byte)fp.AddressId;
            buf[10] = buffer[10];
            this.serialPort.Write(buf, 0, buf.Length);

            Thread.Sleep(50);
            byte[] getDataBuffer = GetData;
            getDataBuffer[1] = (byte)fp.AddressId;
            this.serialPort.Write(GetData, 0, GetData.Length);
        }

        List<byte> buffer = new List<byte>();

        void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] buf = new byte[this.serialPort.BytesToRead];
            this.serialPort.Read(buf, 0, this.serialPort.BytesToRead);
            buffer.AddRange(buf);
            if (buf.Length == 1 && buf[0] == 254)
            {
                this.startpumpFlag = false;
                this.StartPumpCommand();
            }
            List<Response> responses = this.GetResponces();
            if(responses.Count == 0)
                return;
            buffer.Clear();
            foreach (Response response in responses)
            {
                if (response.ResponseType == ResponseTypeEnum.DisplayData)
                {
                    this.EvaluateDisplayDataResponse(response.Buffer);       // Response Display
                }
                else if (response.ResponseType == ResponseTypeEnum.Status)
                {
                    this.EvaluateStatusResponse(response.Buffer);            // Response Status
                }
                else if (response.ResponseType == ResponseTypeEnum.Totals)
                {
                    this.EvaluateTotalsResponse(response.Buffer);            // Response Totals
                }
                else if (response.ResponseType == ResponseTypeEnum.E0)
                {
                    this.EvaluateE0Response(response.Buffer);                // Response ??
                }
            }
        }
    }

    public enum ResponseTypeEnum
    {
        Status,
        Totals,
        DisplayData,
        E0
    }

    public class Response
    {
        public byte[] Buffer { set; get; }
        public ResponseTypeEnum ResponseType { set; get; }
    }

    public class ValuesChangedEventArgs : EventArgs
    {
        public FuelPoint FuelPoint { set; get; }
    }

    public class StatusChangedEventArgs : EventArgs
    {
        public FuelPoint FuelPoint { set; get; }
        public Common.Enumerators.FuelPointStatusEnum PreviousStatus { set; get; }
        public Common.Enumerators.FuelPointStatusEnum Status { set; get; }
    }

    public class TotalsUpdatedEventArgs : EventArgs
    {
        public Nozzle Nozzle { set; get; }
    }
}


using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.Box69b
{
    public class Controller: Common.FuelPumpControllerBase
    {
        public Controller()
        {
            this.ControllerType = Common.Enumerators.ControllerTypeEnum.Box69;
            this.Controller = new Box69bConnector();
        }

    }

    public class Box69bConnector: Common.IFuelProtocol
    {
        public Common.DebugValues DebugStatusDialog(Common.FuelPoint fp) { throw new NotImplementedException(); }
        
        #region events
        public event EventHandler<Common.TotalsEventArgs> TotalsRecieved;
        public event EventHandler<Common.SaleEventArgs> SaleRecieved;
        public event EventHandler<Common.FuelPointValuesArgs> DataChanged;
        public event EventHandler<Common.FuelPointValuesArgs> DispenserStatusChanged;
        public event EventHandler DispenserOffline;
        #endregion
        #region privates
        private double speed
        {
            set;
            get;
        }
        private byte[] cmd;                           //Pump Commands
        private List<byte> buffer = new List<byte>(); //Pump Responses
        private System.Threading.Thread th;
        private List<Common.FuelPoint> fuelPoints = new List<Common.FuelPoint>();
        private System.IO.Ports.SerialPort serialPort = new System.IO.Ports.SerialPort();
        
        #endregion
        #region publics
        public Common.FuelPoint[] FuelPoints
        {
            set
            {
                this.fuelPoints = new List<Common.FuelPoint>(value);
            }
            get
            {
                return this.fuelPoints.ToArray();
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

                this.serialPort.Open();
                this.speed = 1 / (serialPort.BaudRate / 8000);
                this.th = new System.Threading.Thread(new System.Threading.ThreadStart(this.ThreadRun));
                th.Start();
            }
            catch
            {
            }
        }
        public void Disconnect()
        {
            if(this.serialPort.IsOpen)
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
        #endregion

        private void ThreadRun()
        {
            
            foreach(Common.FuelPoint fp in this.fuelPoints)
            {
                this.InitializeFuelPoint(fp);
                this.SetPrice(fp);
                fp.QuerySetPrice = true;

                foreach(Common.Nozzle nz in fp.Nozzles)
                {
                    nz.QueryTotals = true;
                }
            }
            while(this.IsConnected)
            {
                try
                {
                    foreach(Common.FuelPoint fp in this.fuelPoints)
                    {
                        try
                        {
                            if (fp.QueryHalt && !fp.Halted)
                            {
                                this.Halt(fp);
                                continue;
                            }
                            int nozzleForTotals = fp.Nozzles.Where(n => n.QueryTotals).Count();
                            if(nozzleForTotals > 0)
                            {
                                
                                foreach(Common.Nozzle nz in fp.Nozzles)
                                {
                                    if(nz.QueryTotals)
                                    {
                                        this.GetTotals(nz);
                                    }
                                }
                                //this.GetDisplay(fp);
                                this.AcknowledgeDeactivatedNozzle(fp);
                                continue;
                            }
                            //DateTime dt = DateTime.Now;
                            this.GetStatus(fp);
                            //DateTime dt2 = DateTime.Now;
                            //int milsec = (int)dt2.Subtract(dt).TotalMilliseconds;
                            if(fp.QueryAuthorize)
                            {
                                //this.SetPrice(fp);
                                //System.Threading.Thread.Sleep(200);
                                this.Authorize(fp.ActiveNozzle);
                                continue;
                            }

                            if(fp.Status == Common.Enumerators.FuelPointStatusEnum.Work)
                            {
                                this.GetDisplay(fp);
                                continue;
                            }


                            if(fp.Status == Common.Enumerators.FuelPointStatusEnum.Offline)
                            {
                                this.InitializeFuelPoint(fp);
                                continue;
                            }


                            if(fp.QuerySetPrice)
                            {
                                this.SetPrice(fp);
                                continue;
                            }

                        }
                        finally
                        {
                            System.Threading.Thread.Sleep(100);
                        }


                    }
                }
                catch
                {
                    System.Threading.Thread.Sleep(200);
                }
            }
        }

        #region Set/Get Methods
        
        private bool InitializeFuelPoint(Common.FuelPoint fp)
        {
            byte startByte = BitConverter.GetBytes(240 + fp.Address - 1)[0];
            List<Byte> commandBuffer = new List<byte>();
 
            byte suffix = 0xA6;
            commandBuffer.Add(startByte);
            commandBuffer.Add(suffix);

            //for(int i = 0; i < fp.Nozzles.Length; i++)
            //{
            //    byte[] price1 = this.GetDecimalBytes(fp.Nozzles[i].UnitPrice, fp.UnitPriceDecimalPlaces, 2);
            //    byte[] price2 = this.GetDecimalBytes(fp.Nozzles[i].UnitPrice, fp.UnitPriceDecimalPlaces, 2);
            //    commandBuffer.AddRange(price1);
            //    commandBuffer.AddRange(price2);
            //}
            for(int i = 0; i < 4; i++)
            {
                byte[] price1 = this.GetDecimalBytes(0, fp.UnitPriceDecimalPlaces, 2);
                byte[] price2 = this.GetDecimalBytes(0, fp.UnitPriceDecimalPlaces, 2);
                commandBuffer.AddRange(price1);
                commandBuffer.AddRange(price2);
            }

            this.cmd = this.NormaliseBuffer(commandBuffer.ToArray());
            if(ExecuteCommnad(cmd,ResponseLenght.Basic,CommandType.Initiliaze))
            {
                this.EvaluateStatus(this.buffer.ToArray(), fp);
                return true;
            }
            return false;
        }
        private void GetDisplay(Common.FuelPoint fp)
        {
            Common.Nozzle nz = fp.ActiveNozzle != null ? fp.ActiveNozzle : fp.LastActiveNozzle;
            if(nz == null)
                return;
            
            byte startByte = BitConverter.GetBytes(240 + fp.Address - 1)[0];
            byte startByteNeg = (byte)(255 - startByte);
           this.cmd = new byte[] { startByte, startByteNeg, 0xA1, 0x5E };
            if(ExecuteCommnad(cmd,ResponseLenght.Display,CommandType.Display))
            {
                EvaluateDisplayData(nz, this.buffer.ToArray());
            }
        }
        private void Authorize(Common.Nozzle nz)
        {
            byte startByte = BitConverter.GetBytes(240 + nz.ParentFuelPoint.Address - 1)[0];
            List<Byte> commandBuffer = new List<byte>();

            byte slowFlowOffsetStart = 0x08;

            commandBuffer.Add(startByte);
            commandBuffer.Add(0xA5);
            commandBuffer.Add(slowFlowOffsetStart);

            //price = price * (decimal)Math.Pow(10, this.PriceDecimalPlaces);

            ////
            string sprice = nz.UntiPriceInt.ToString();

            for(int k = sprice.Length; k < 4; k++)
            {
                sprice = "0" + sprice;
            }
            int p1 = Convert.ToInt32((sprice.Substring(0, 2)));
            int p2 = Convert.ToInt32((sprice.Substring(2, 2)));

            byte price1 = (byte)ith(p1);
            byte price2 = (byte)ith(p2);
            ///

            //byte[] upBuffer = this.GetDecimalBytes(nz.UnitPrice, nz.ParentFuelPoint.UnitPriceDecimalPlaces, 2);
            byte[] prBuffer = this.GetDecimalBytes((decimal)999999, nz.ParentFuelPoint.AmountDecimalPlaces, 3);
            byte[] volBuffer = this.GetDecimalBytes((decimal)999999, nz.ParentFuelPoint.VolumeDecimalPlaces, 3);
            //volBuffer = new byte[]{0x99, 0x09, 0x00};
            commandBuffer.Add(price2);
            commandBuffer.Add(price1);
            commandBuffer.AddRange(prBuffer.Reverse());
            commandBuffer.AddRange(volBuffer.Reverse());

           cmd = this.NormaliseBuffer(commandBuffer.ToArray());
            
            if(ExecuteCommnad(cmd,ResponseLenght.Basic,CommandType.Authorize))
            {
                if(this.buffer.ToArray().Length != 0)
                    nz.ParentFuelPoint.QueryAuthorize = false;
            }
        }
        private void SetPrice(Common.FuelPoint fp)
        {
            byte startByte = BitConverter.GetBytes(192 + fp.Address - 1)[0];
            List<Byte> commandBuffer = new List<byte>();
            byte suffix = 0xA3;
            commandBuffer.Add(startByte);
            commandBuffer.Add(suffix);

            int[] prices = new int[8];
            foreach (Common.Nozzle nz in fp.Nozzles)
            {
                prices[nz.NozzleIndex - 1] = nz.UntiPriceInt;
            }
            for (int i = 0; i < prices.Length; i++)
            {
                string sprice = prices[i].ToString();

                for (int k = sprice.Length; k < 4; k++)
                {
                    sprice = "0" + sprice;
                }
                int p1 = Convert.ToInt32((sprice.Substring(0, 2)));
                int p2 = Convert.ToInt32((sprice.Substring(2, 2)));

                byte price1 = (byte)ith(p1);
                byte price2 = (byte)ith(p2);
                commandBuffer.Add(price1);
                commandBuffer.Add(price2);
                commandBuffer.Add(price1);
                commandBuffer.Add(price2);
            }
            //for(int i = 0; i < fp.Nozzles.Length; i++)
            //{
            //    string sprice = fp.Nozzles[i].UntiPriceInt.ToString();

            //    for(int k = sprice.Length; k < 4; k++)
            //    {
            //        sprice = "0" + sprice;
            //    }
            //    int p1 = Convert.ToInt32((sprice.Substring(0, 2)));
            //    int p2 = Convert.ToInt32((sprice.Substring(2, 2)));

            //    byte price1 = (byte)ith(p1);
            //    byte price2 = (byte)ith(p2);
            //    commandBuffer.Add(price1);
            //    commandBuffer.Add(price2);
            //    commandBuffer.Add(price1);
            //    commandBuffer.Add(price2);
            //}
            //for(int i = fp.Nozzles.Length; i < 8; i++)
            //{
            //    byte[] price1 = new byte[] { 0x00, 0x00 };
            //    //byte[] price2 = price1;
            //    commandBuffer.AddRange(price1);
            //    commandBuffer.AddRange(price1);
            //    commandBuffer.AddRange(price1);
            //    commandBuffer.AddRange(price1);
            //}
            for(int i = 0; i < 4; i++)
            {
                commandBuffer.Add(0x00);
            }

            byte[] cmd = this.NormaliseBuffer(commandBuffer.ToArray());

            int spIndex = 0;
            try
            {
                spIndex = (int)fp.GetExtendedProperty("SetPriceIndex");
            }
            catch
            {
                fp.SetExtendedProperty("SetPriceIndex", 0);
                spIndex = 0;
            }
            spIndex++;

            if (ExecuteCommnad(cmd, ResponseLenght.Basic, CommandType.SetPrice))
            {
                if (this.buffer.ToArray().Length == 0 && spIndex < 4)
                {
                    fp.SetExtendedProperty("SetPriceIndex", spIndex);
                    fp.QuerySetPrice = true;
                }
                else
                {
                    fp.SetExtendedProperty("SetPriceIndex", 0);
                    fp.QuerySetPrice = false;
                }
                if (System.IO.File.Exists("Box69.log"))
                    System.IO.File.AppendAllText("Box69.log", "SetPriceIndex : " + spIndex.ToString() + "\r\n");
                //if(this.buffer.ToArray()[0] == 0xB0)
                //    fp.QuerySetPrice = false;

            }
            else
            {
                if (spIndex < 4)
                {
                    fp.SetExtendedProperty("SetPriceIndex", spIndex);
                    fp.QuerySetPrice = true;
                }
                else
                {
                    fp.SetExtendedProperty("SetPriceIndex", 0);
                    fp.QuerySetPrice = false;
                }
                if (System.IO.File.Exists("Box69.log"))
                    System.IO.File.AppendAllText("Box69.log", "SetPriceIndex : " + spIndex.ToString() + "\r\n");
            }
        }
        private void SetActiveNozzle(Common.FuelPoint fp)
        {
            byte startByte = BitConverter.GetBytes(192 + fp.Address - 1)[0];
            List<Byte> commandBuffer = new List<byte>();

            byte suffix = 0xA1;
            commandBuffer.Add(startByte);
            commandBuffer.Add(suffix);

            byte[] command = this.NormaliseBuffer(commandBuffer.ToArray());
            if(ExecuteCommnad(command, ResponseLenght.Basic, CommandType.GetActiveNozzle))
            {
                //This configure FP;
                this.EvaluateActiveNozzle(fp, this.buffer.ToArray());
            }


        }
        private void GetStatus(Common.FuelPoint fp)
        {
            byte startByte = BitConverter.GetBytes(240 + fp.Address - 1)[0];
            byte startByteNeg = (byte)(255 - startByte);
            cmd= new byte[] { startByte, startByteNeg, 0xA2, 0x5D };
            if (this.ExecuteCommnad(cmd, ResponseLenght.Basic, CommandType.Status))
            {
                this.EvaluateStatus(this.buffer.ToArray(), fp);
                
            }
            else
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
        
        private void AcknowledgeDeactivatedNozzle(Common.FuelPoint fp)
        {
            byte startByte = BitConverter.GetBytes(192 + fp.Address - 1)[0];
            byte startByteNeg = (byte)(255 - startByte);
            cmd = new byte[] { startByte, startByteNeg, 0xA2, 0x5D };
            if (this.ExecuteCommnad(cmd, ResponseLenght.Basic, CommandType.AcknowledgeDeactivatedNozzle))
            {
                this.EvaluateAcknowledgeDeactivatedNozzle(this.buffer.ToArray(), fp);
            }
            else
            {

            }
        }
        private void GetTotals(Common.Nozzle nz)
        {
            byte startByte = BitConverter.GetBytes(240 + nz.ParentFuelPoint.Address - 1)[0];
            List<Byte> commandBuffer = new List<byte>();
            byte suffix = 0xA9;
            commandBuffer.Add(startByte);
            commandBuffer.Add(suffix);
            //commandBuffer.Add(this.TotalNozzleDefinition(nz.Index - 1));
            commandBuffer.Add(this.TotalNozzleDefinition(nz.NozzleIndex - 1));
            cmd = this.NormaliseBuffer(commandBuffer.ToArray());
            if(ExecuteCommnad(cmd,ResponseLenght.Totals,CommandType.Totlas))
            {
                EvaluateTotals(nz, this.buffer.ToArray());
                //if ()
                //    nz.QueryTotals = false;
            }
            
        }
        
        #endregion 

        #region Evaluate
        
        private bool EvaluateDisplayData(Common.Nozzle nozzle, byte[] buffer)
        {
            try
            {
                byte[] unitPrice = buffer.Take(2).ToArray();
                unitPrice = unitPrice.Reverse().ToArray();
                byte[] priceBuffer = buffer.Skip(2).Take(3).ToArray();
                priceBuffer = priceBuffer.Reverse().ToArray();
                byte[] volumeBuffer = buffer.Skip(5).Take(3).ToArray();
                volumeBuffer = volumeBuffer.Reverse().ToArray();


                byte[] status = buffer.Skip(8).Take(1).ToArray();
                string upString = BitConverter.ToString(unitPrice).Replace("-", "");// unitPrice[0].ToString() + unitPrice[1].ToString();
                string volString = BitConverter.ToString(volumeBuffer).Replace("-", "");// volumeBuffer[0].ToString() + volumeBuffer[1].ToString() + volumeBuffer[2].ToString();
                string priceString = BitConverter.ToString(priceBuffer).Replace("-", "");// priceBuffer[0].ToString() + priceBuffer[1].ToString() + priceBuffer[2].ToString();

                //nozzle.ParentFuelPoint.DispenserStatus = this.EvaluateStatus(status, nozzle.ParentFuelPoint.DispenserStatus);
                //nozzle.UnitPrice = decimal.Parse(upString) / (decimal)Math.Pow(10, nozzle.ParentFuelPoint.UnitPriceDecimalPlaces);
                nozzle.ParentFuelPoint.DispensedVolume = decimal.Parse(volString) / (decimal)Math.Pow(10, nozzle.ParentFuelPoint.VolumeDecimalPlaces);
                nozzle.ParentFuelPoint.DispensedAmount = decimal.Parse(priceString) / (decimal)Math.Pow(10, nozzle.ParentFuelPoint.AmountDecimalPlaces);

                //if (Math.Round(nozzle.ParentFuelPoint.DispensedVolume / nozzle.ParentFuelPoint.DispensedAmount * nozzle.UnitPrice, 0) != 1)
                //{
                //    nozzle.ParentFuelPoint.DispensedVolume = nozzle.ParentFuelPoint.DispensedVolume / 10;
                //}
                
                if(this.DataChanged != null)
                {
                    Common.FuelPointValues values = new Common.FuelPointValues();
                    values.CurrentSalePrice = nozzle.UnitPrice;
                    values.CurrentPriceTotal = nozzle.ParentFuelPoint.DispensedAmount;
                    values.CurrentVolume = nozzle.ParentFuelPoint.DispensedVolume;


                    this.DataChanged(this, new Common.FuelPointValuesArgs()
                    {
                        CurrentFuelPoint = nozzle.ParentFuelPoint,
                        CurrentNozzleId = nozzle.Index,
                        Values = values
                    });
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        private bool EvaluateTotals(Common.Nozzle nz, byte[] buffer)
        {
            try
            {
                if ((buffer.Length - 1)  % 10 != 0)
                    return false;
                if (buffer.Length == 1 && buffer[0] == 0x2f)
                {
                    this.InitializeFuelPoint(nz.ParentFuelPoint);
                    System.Threading.Thread.Sleep(100);
                    return false;
                }
                byte[] volumeBuffer = buffer.Take(5).ToArray();
                volumeBuffer = volumeBuffer.Reverse().ToArray();
                byte[] priceBuffer = buffer.Skip(5).Take(5).ToArray();
                priceBuffer = priceBuffer.Reverse().ToArray();
                byte[] status = buffer.Skip(10).Take(1).ToArray();
                if (buffer.Length > 11)
                {
                    int skip = (nz.NozzleIndex - 1) * 10;
                    status = buffer.Skip(buffer.Length - 1).Take(1).ToArray();
                    volumeBuffer = buffer.Skip(skip).Take(5).ToArray();
                    volumeBuffer = volumeBuffer.Reverse().ToArray();
                    priceBuffer = buffer.Skip(skip + 5).Take(5).ToArray();
                    priceBuffer = priceBuffer.Reverse().ToArray();
                }
                
                string volString = BitConverter.ToString(volumeBuffer).Replace("-", "");// volumeBuffer[0].ToString() + volumeBuffer[1].ToString() + volumeBuffer[2].ToString() + volumeBuffer[3].ToString() + volumeBuffer[4].ToString();
                string priceString = BitConverter.ToString(priceBuffer).Replace("-", ""); //priceBuffer[0].ToString() + priceBuffer[1].ToString() + priceBuffer[2].ToString() + priceBuffer[3].ToString() + priceBuffer[4].ToString();

                this.GetDisplay(nz.ParentFuelPoint);

                nz.TotalPrice = decimal.Parse(priceString);
                nz.TotalVolume = decimal.Parse(volString);
                nz.ParentFuelPoint.Initialized = true;
                if (this.TotalsRecieved != null)
                    this.TotalsRecieved(this, new Common.TotalsEventArgs(
                        nz.ParentFuelPoint,
                        nz.Index,
                        nz.TotalVolume,
                        nz.TotalPrice));

                this.EvaluateStatus(status, nz.ParentFuelPoint);

                return true;
                
            }
            catch(Exception ex)
            {
                if (System.IO.File.Exists("Box69_Exception.log"))
                    System.IO.File.AppendAllText("Box69_Exception.log", "Exception : " + ex.Message + "\r\n");
                return false;
            }
               
             
        }
        private void EvaluateStatus(byte[] buffer, Common.FuelPoint fp)
        {
            if (buffer.Length == 0 && DateTime.Now.Subtract(fp.LastValidResponse).TotalSeconds > 5)
            {
                if (this.DispenserOffline != null)
                    this.DispenserOffline(fp, new EventArgs());
                return;
            }
            //if (buffer.Length == 0 && DateTime.Now.Subtract(fp.LastValidResponse).TotalSeconds > 5)
            //{
            //    fp.Status = Common.Enumerators.FuelPointStatusEnum.Offline;
            //    if (this.DispenserStatusChanged != null)
            //    {
            //        Common.FuelPointValues values = new Common.FuelPointValues();
            //        values.Status = Common.Enumerators.FuelPointStatusEnum.Offline;
            //        this.DispenserStatusChanged(this, new Common.FuelPointValuesArgs()
            //        {
            //            CurrentFuelPoint = fp,
            //            CurrentNozzleId = 0,
            //            Values = values
            //        });
            //    }
            //}
            if (buffer.Length == 0)
                return;
            fp.LastValidResponse = DateTime.Now;
            Common.Enumerators.FuelPointStatusEnum oldstatus = fp.Status;
            try
            {
                byte b1 = buffer[0];
                if(b1 == 0x2F)
                    fp.Status= Common.Enumerators.FuelPointStatusEnum.Offline;
                else if (b1 == 0x20)
                {
                    fp.Status = Common.Enumerators.FuelPointStatusEnum.Idle;
                    fp.QueryHalt = false;
                    fp.Halted = false;
                }
                else if (b1 == 0xA0)
                    fp.Status = Common.Enumerators.FuelPointStatusEnum.Nozzle;
                else if (b1 == 0x90)
                    fp.Status = Common.Enumerators.FuelPointStatusEnum.Ready;
                else if (b1 == 0xD0)
                    fp.Status = Common.Enumerators.FuelPointStatusEnum.Work;
                else if (b1 == 0xF0)
                    fp.Status = Common.Enumerators.FuelPointStatusEnum.Work;
                else if (b1 == 0x91)
                    fp.Status = Common.Enumerators.FuelPointStatusEnum.TransactionStopped;
                else if (b1 == 0x92)
                    fp.Status = Common.Enumerators.FuelPointStatusEnum.TransactionStopped;
                else if (b1 == 0x99)
                {
                    fp.Status = Common.Enumerators.FuelPointStatusEnum.TransactionStopped;
                    fp.QueryHalt = false;
                    fp.Halted = true;
                }

                fp.DispenserStatus = fp.Status;


                if(oldstatus != fp.Status && DispenserStatusChanged != null)
                {
                    Common.FuelPointValues values = new Common.FuelPointValues();
                    values.Status = fp.Status;

                    if(fp.Status != Common.Enumerators.FuelPointStatusEnum.Close && fp.Status != Common.Enumerators.FuelPointStatusEnum.Idle && fp.Status != Common.Enumerators.FuelPointStatusEnum.Offline)
                    {
                        SetActiveNozzle(fp);
                        values.ActiveNozzle = fp.ActiveNozzleIndex;
                    }
                    else
                    {
                        fp.ActiveNozzleIndex = -1;
                        values.ActiveNozzle = -1;
                    }

                    this.DispenserStatusChanged(this, new Common.FuelPointValuesArgs()
                    {
                        CurrentFuelPoint = fp,
                        CurrentNozzleId = fp.ActiveNozzleIndex,
                        Values = values
                    });
   
                }
            }
            catch
            {
            }
        }
        private void EvaluateAcknowledgeDeactivatedNozzle(byte[] buffer, Common.FuelPoint fp)
        {
            byte b1 = buffer[0];
            if (b1 == 0xB0)
            {
            }
        }
        private void EvaluateActiveNozzle(Common.FuelPoint fp, byte[] buffer)
        {
            try
            {

                BitArray answer = new BitArray(new byte[] { buffer[0] });
                BitArray nozzleNrArray = new BitArray(7);
                nozzleNrArray.Set(0, answer.Get(6));
                nozzleNrArray.Set(1, answer.Get(5));
                nozzleNrArray.Set(2, answer.Get(4));
                nozzleNrArray.Set(3, answer.Get(3));
                nozzleNrArray.Set(4, answer.Get(2));
                nozzleNrArray.Set(5, answer.Get(1));
                nozzleNrArray.Set(6, answer.Get(0));
                int nozzleIndex = this.ToNumeral(nozzleNrArray, 7);

                Common.Nozzle nz = fp.Nozzles.Where(n => n.NozzleIndex == nozzleIndex).FirstOrDefault();
                if (nz != null)
                {
                    fp.ActiveNozzle = nz;
                    fp.ActiveNozzleIndex = nz.Index - 1;
                }
                else
                {
                    fp.ActiveNozzle = null;
                    fp.ActiveNozzleIndex = -1;

                }
                //nozzleIndex = nozzleIndex - 1;
                //if(nozzleIndex < 0)
                //{
                //    fp.ActiveNozzle = null;
                //    fp.ActiveNozzleIndex = -1;
                //}
                //else
                //{
                //    fp.ActiveNozzleIndex = nozzleIndex;
                //    fp.ActiveNozzle = fp.Nozzles[nozzleIndex];
                //}
            }
            catch
            {
            }
         
        }
        
        #endregion

        #region helpers

        private byte[] GetDecimalBytes(decimal value, int decimalPlaces, int byteCount)
        {
            long valueInt = (long)((double)value * Math.Pow(10, decimalPlaces));
            List<Byte> bufferList = new List<byte>();
            int maxPow = 2 * (byteCount - 1);
            double restValue = (double)valueInt;
            string str = "";
            for(int i = 0; i < byteCount; i++)
            {
                int pow = maxPow - (2 * i);
                double byteVal = (int)(restValue / System.Math.Pow(10, pow));
                int bv = (int)byteVal;
                str = str + bv.ToString();
                //bufferList.Add(b);
                restValue = restValue - (byteVal * System.Math.Pow(10, pow));
            }
            return StringToByteArray(str).Take(byteCount).ToArray();
        }
        public byte[] StringToByteArray(string hex)
        {
            if(hex.Length % 2 != 0)
                hex = "0" + hex;
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
        private int ToNumeral(BitArray binary, int length)
        {
            int numeral = 0;
            for(int i = 0; i < length; i++)
            {
                if(binary[i])
                {
                    numeral = numeral | (((int)1) << (length - 1 - i));
                }
            }
            return numeral;
        }
        private bool ExecuteCommnad(byte[] cmd,ResponseLenght responseLength,CommandType commandType)
        {  
             int TimeOut = 100;//ms
           //Request from pump
           //if (validResponse) 
           //return true
           
            try
            {
                this.buffer.Clear();
                if (System.IO.File.Exists("Box69.log"))
                    System.IO.File.AppendAllText("Box69.log", "TX : " + BitConverter.ToString(cmd) + "\r\n");
                this.serialPort.Write(cmd, 0, cmd.Length);

                if(commandType == CommandType.SetPrice)
                {
                    System.Threading.Thread.Sleep((int)((double)(cmd.Length) * speed) + 35);
                    this.serialPort.Write(cmd, 0, cmd.Length);
                }
                else
                    System.Threading.Thread.Sleep((int)((double)(cmd.Length + (int)responseLength) * speed)+10);
                
                int waiting = 0;
                
                //TypeA
                //while(this.serialPort.BytesToRead > 0 && waiting < ((TimeOut/20)*10))
                //{
                //    Byte[] resp = new byte[this.serialPort.BytesToRead];
                //    this.serialPort.Read(resp, 0, resp.Length);
                //    this.buffer.AddRange(resp);
                //    waiting +=10;
                //    System.Threading.Thread.Sleep(20);
                //}

                //TypeB

                while (this.serialPort.BytesToRead < (int)responseLength && waiting < TimeOut)//((TimeOut / 20) * 10))
                {
                    waiting += 10;
                    System.Threading.Thread.Sleep(10);
                }
                Byte[] resp1 = new byte[this.serialPort.BytesToRead];
                this.serialPort.Read(resp1, 0, resp1.Length);
                this.buffer.AddRange(resp1);

                if (System.IO.File.Exists("Box69.log"))
                    System.IO.File.AppendAllText("Box69.log", "RX : " + BitConverter.ToString(resp1) + "\r\n");

                if (resp1.Length == 0)
                    return false;
                return this.CheckBuffer(buffer.ToArray());
     
            }
            catch
            {
                return false ;
            }
        }
        private byte[] NormaliseBuffer(byte[] buffer)
        {
            List<byte> newBuffer = new List<byte>();
            foreach(Byte b in buffer)
            {
                newBuffer.Add(b);
                newBuffer.Add((byte)(255 - b));
            }
            return newBuffer.ToArray();
        }
        private byte TotalNozzleDefinition(int nozzleIndex)
        {
            BitArray bitArr = new BitArray(8);
            byte nzi = (byte)nozzleIndex;
            BitArray baNzi = new BitArray(new Byte[] { nzi });
            bitArr.Set(0, false);
            bitArr.Set(1, baNzi.Get(0));
            bitArr.Set(2, baNzi.Get(1));
            bitArr.Set(3, baNzi.Get(2));
            bitArr.Set(4, false);
            bitArr.Set(5, false);
            bitArr.Set(6, true);
            bitArr.Set(7, false);

            byte[] bytes = new byte[1];
            bitArr.CopyTo(bytes, 0);
            return bytes[0];

            //BitArray bitArr = new BitArray(8);
            //byte nzi = (byte)nozzleIndex;
            //BitArray baNzi = new BitArray(new Byte[] { nzi });
            //bitArr.Set(0, false);
            //bitArr.Set(1, false);
            //bitArr.Set(2, false);
            //bitArr.Set(3, false);
            //bitArr.Set(4, false);
            //bitArr.Set(5, false);
            //bitArr.Set(6, false);
            //bitArr.Set(7, false);

            //byte[] bytes = new byte[1];
            //bitArr.CopyTo(bytes, 0);
            //return bytes[0];

        }
        private int ith(int num)
        {
            return 16 * (num / 10) + (num % 10);
        }
        private enum ResponseLenght
        {
            Basic = 2,//price,auth,halt,activeNozzle
            Totals = 88,
            Display = 18,     
        }
        private enum CommandType
        {
            Status = 1,
            SetPrice = 2,
            Totlas = 3,
            Initiliaze,
            Display,
            Authorize,
            GetActiveNozzle,
            AcknowledgeDeactivatedNozzle
        }
        private bool CheckBuffer(byte[] t)
        {
            if(t.Length == 0)
                return false;
            if(t.Length % 2 != 0)
                return false;
            for(int i = 0; i < t.Length / 2; i++)
            {
                int j = (2 * i);
                if(t[j] + t[j + 1] != 255)
                    return false;
            }
            this.buffer = GetBuffer(t).ToList();
            return true;
        }
        private byte[] GetBuffer(byte[] buffer)
        {
            if(buffer.Length % 2 != 0)
                return new byte[] { };
            List<byte> newBuffer = new List<byte>();
            for(int i = 0; i < buffer.Length / 2; i++)
            {
                int j = (2 * i);
                newBuffer.Add(buffer[j]);
            }
            return newBuffer.ToArray();
        }

        #endregion
    }
}

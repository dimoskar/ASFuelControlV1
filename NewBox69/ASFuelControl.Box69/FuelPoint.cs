using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using ASFuelControl.Common;
using ASFuelControl.Common.Enumerators;

namespace ASFuelControl.Box69
{

    public static class FuelPointExtensions
    {
        public static OpenCommand CreateNextCommand(this FuelPoint fp)
        {
            DateTime dtNow = DateTime.Now;
            OpenCommand nextCommand = null;
            try
            {
                if ((bool)fp.GetExtendedProperty("NoAnswer", false) && DateTime.Now.Subtract((DateTime)fp.GetExtendedProperty("LastOffline", DateTime.MinValue)).TotalSeconds < 10)
                    nextCommand = null;
                else if ((bool)fp.GetExtendedProperty("NoAnswer", false))
                    nextCommand = fp.GetFuelPointStatus();
                else if (fp.QueryStop)
                    nextCommand = fp.StopFuelPoint();
                else if (fp.QuerySetPrice)
                    nextCommand = fp.SetPrices();
                else if (fp.QueryTotals)
                {
                    if (fp.DispenserStatus != Common.Enumerators.FuelPointStatusEnum.Idle && fp.ActiveNozzle != null)
                        nextCommand = fp.GetNozzleTotals(fp.ActiveNozzle);
                    else
                    {
                        nextCommand = fp.GetId();
                        //nextCommand = this.GetNozzleTotals(this.LastNozzle);
                    }
                }
                else
                {
                    if (fp.DispenserStatus == Common.Enumerators.FuelPointStatusEnum.Offline)
                    {
                        fp.SetExtendedProperty("InitializeSent", false);
                        fp.SetExtendedProperty("PriceSetSent", false);
                    }
                    if (!(bool)fp.GetExtendedProperty("InitializeSent", false) || !(bool)fp.GetExtendedProperty("PriceSetSent", false))
                    {
                        if (dtNow.Subtract((DateTime)fp.GetExtendedProperty("LastCommand", DateTime.MinValue)).TotalMilliseconds < 500)
                            nextCommand = null;
                        if (!(bool)fp.GetExtendedProperty("InitializeSent", false))
                            nextCommand = fp.InitializeFuelPoint();
                        else if (!(bool)fp.GetExtendedProperty("PriceSetSent", false))
                            nextCommand = fp.SetPrices();
                        else
                            nextCommand = fp.GetFuelPointStatus();
                    }
                    else
                    {

                        if (fp.DispenserStatus == Common.Enumerators.FuelPointStatusEnum.Idle)
                        {
                            if (fp.ActiveNozzle != null)
                            {
                                fp.ActiveNozzle = null;
                            }
                            if (dtNow.Subtract((DateTime)fp.GetExtendedProperty("LastCommand", DateTime.MinValue)).TotalMilliseconds < 1000)
                                nextCommand = null;
                            if (fp.QueryNozzle)
                                nextCommand = fp.GetActiveNozzle();
                            else
                                nextCommand = fp.GetFuelPointStatus();
                        }
                        else if (fp.DispenserStatus == Common.Enumerators.FuelPointStatusEnum.Nozzle)
                        {
                            if (dtNow.Subtract((DateTime)fp.GetExtendedProperty("LastCommand", DateTime.MinValue)).TotalMilliseconds < 50)
                                nextCommand = null;

                            //if (this.ActiveNozzle == null)
                            //    nextCommand = this.GetActiveNozzle();
                            if (fp.QueryAuthorize)
                                nextCommand = fp.AuthoriseFuelPoint(fp.ActiveNozzle.UnitPrice, (decimal)999999, 999999);
                            else if (fp.QueryTotals)
                                nextCommand = fp.GetNozzleTotals(fp.ActiveNozzle);
                            else
                                nextCommand = fp.GetFuelPointStatus();
                        }
                        else if (fp.DispenserStatus == Common.Enumerators.FuelPointStatusEnum.Work || fp.DispenserStatus == Common.Enumerators.FuelPointStatusEnum.Ready)
                        {
                            if (dtNow.Subtract((DateTime)fp.GetExtendedProperty("LastCommand", DateTime.MinValue)).TotalMilliseconds < 50)
                                nextCommand = null;
                            nextCommand = fp.GetFuelingPointData(fp.ActiveNozzle);
                        }
                        else
                            nextCommand = fp.GetFuelPointStatus();
                    }
                }
                return nextCommand;
            }
            catch
            {
                nextCommand = null;
                return nextCommand;
            }
            finally
            {
                if (nextCommand != null)
                    fp.SetExtendedProperty("LastCommand", dtNow);

            }
        }

        private static OpenCommand GetFuelPointStatus(this FuelPoint fp)
        {
            byte startByte = BitConverter.GetBytes(240 + fp.Address - 1)[0];
            byte startByteNeg = (byte)(255 - startByte);
            byte[] command = new byte[] { startByte, startByteNeg, 0xA2, 0x5D };

            return new OpenCommand(command, CommandTypeEnum.RequestStatus, fp);
        }

        private static OpenCommand InitializeFuelPoint(this FuelPoint fp)
        {
            byte startByte = BitConverter.GetBytes(240 + fp.Address - 1)[0];
            List<Byte> commandBuffer = new List<byte>();
            fp.SetExtendedProperty("InitializeSent", true);
            byte cmd = 0xA6;
            commandBuffer.Add(startByte);
            commandBuffer.Add(cmd);

            for (int i = 0; i < fp.Nozzles.Length; i++)
            {
                byte[] price1 = fp.GetDecimalBytes(fp.Nozzles[i].UnitPrice, fp.UnitPriceDecimalPlaces, 2);
                byte[] price2 = fp.GetDecimalBytes(fp.Nozzles[i].UnitPrice, fp.UnitPriceDecimalPlaces, 2);
                commandBuffer.AddRange(price1);
                commandBuffer.AddRange(price2);
            }
            for (int i = fp.Nozzles.Length; i < 4; i++)
            {
                byte[] price1 = fp.GetDecimalBytes(0, fp.UnitPriceDecimalPlaces, 2);
                byte[] price2 = fp.GetDecimalBytes(0, fp.UnitPriceDecimalPlaces, 2);
                commandBuffer.AddRange(price1);
                commandBuffer.AddRange(price2);
            }

            byte[] command = fp.NormaliseBuffer(commandBuffer.ToArray());

            Console.WriteLine("InitializeFuelPoint Code : " + BitConverter.ToString(command));
            return new OpenCommand(command, CommandTypeEnum.SendMainDisplayData, fp);
        }

        public static OpenCommand SetPrices(this FuelPoint fp)
        {

            byte startByte = BitConverter.GetBytes(192 + fp.Address - 1)[0];
            List<Byte> commandBuffer = new List<byte>();
            fp.SetExtendedProperty("PriceSetSent", true);
            byte cmd = 0xA3;
            commandBuffer.Add(startByte);
            commandBuffer.Add(cmd);
            for (int i = 0; i < fp.Nozzles.Length; i++)
            {
                if(fp.Nozzles[i].UnitPrice == 0)
                    return null;
                byte[] price1 = fp.GetDecimalBytes(fp.Nozzles[i].UnitPrice, fp.UnitPriceDecimalPlaces, 2);
                byte[] price2 = fp.GetDecimalBytes(fp.Nozzles[i].UnitPrice, fp.UnitPriceDecimalPlaces, 2);
                commandBuffer.AddRange(price1.Take(2));
                commandBuffer.AddRange(price2.Take(2));
            }
            for (int i = fp.Nozzles.Length; i < 8; i++)
            {
                byte[] price1 = fp.GetDecimalBytes(0, fp.UnitPriceDecimalPlaces, 2);
                byte[] price2 = fp.GetDecimalBytes(0, fp.UnitPriceDecimalPlaces, 2);
                commandBuffer.AddRange(price1.Take(2));
                commandBuffer.AddRange(price2.Take(2));
            }
            for (int i = 0; i < 8; i++)
            {
                commandBuffer.Add(0x00);
            }

            byte[] command = fp.NormaliseBuffer(commandBuffer.ToArray());


            return new OpenCommand(command, CommandTypeEnum.SendPrices, fp);
        }

        public static OpenCommand GetId(this FuelPoint fp)
        {
            byte startByte = BitConverter.GetBytes(240 + fp.Address - 1)[0];
            List<Byte> commandBuffer = new List<byte>();
            fp.SetExtendedProperty("InitializeSent", true);
            byte cmd = 0xA0;
            commandBuffer.Add(startByte);
            commandBuffer.Add(cmd);
            byte[] command = fp.NormaliseBuffer(commandBuffer.ToArray());

            Console.WriteLine("Get ID : " + BitConverter.ToString(command));
            return new OpenCommand(command, CommandTypeEnum.RequestID, fp);
        }

        private static OpenCommand AuthoriseFuelPoint(this FuelPoint fp, decimal price, decimal presetMoney, decimal presetVolume)
        {
            byte startByte = BitConverter.GetBytes(240 + fp.Address - 1)[0];
            List<Byte> commandBuffer = new List<byte>();

            byte slowFlowOffsetStart = 0x20;

            commandBuffer.Add(startByte);
            commandBuffer.Add(0xA5);
            commandBuffer.Add(slowFlowOffsetStart);

            price = price * (decimal)Math.Pow(10, fp.UnitPriceDecimalPlaces);

            byte[] upBuffer = fp.GetDecimalBytes(price, fp.UnitPriceDecimalPlaces, 2);
            byte[] prBuffer = fp.GetDecimalBytes((decimal)999999, fp.DecimalPlaces, 3);
            byte[] volBuffer = fp.GetDecimalBytes((decimal)999999, fp.DecimalPlaces, 3);

            commandBuffer.AddRange(upBuffer.Take(2).Reverse());
            commandBuffer.AddRange(prBuffer.Reverse());
            commandBuffer.AddRange(volBuffer.Reverse());

            byte[] command = fp.NormaliseBuffer(commandBuffer.ToArray());
            Console.WriteLine("AuthoriseFuelPoint Code : " + BitConverter.ToString(command));
            return new OpenCommand(command, CommandTypeEnum.Authorize, fp);
        }

        private static OpenCommand GetActiveNozzle(this FuelPoint fp)
        {
            byte startByte = BitConverter.GetBytes(192 + fp.Address - 1)[0];
            List<Byte> commandBuffer = new List<byte>();

            byte cmd = 0xA1;

            commandBuffer.Add(startByte);
            commandBuffer.Add(cmd);

            byte[] command = fp.NormaliseBuffer(commandBuffer.ToArray());
            return new OpenCommand(command, CommandTypeEnum.RequestActiveNozzle, fp);
        }

        private static OpenCommand GetNozzleTotals(this FuelPoint fp, Nozzle nozzle)
        {
            byte startByte = BitConverter.GetBytes(240 + fp.Address - 1)[0];
            List<Byte> commandBuffer = new List<byte>();

            byte cmd = 0xA9;

            commandBuffer.Add(startByte);
            commandBuffer.Add(cmd);
            commandBuffer.Add(fp.TotalNozzleDefinition(nozzle.Index - 1));

            byte[] command = fp.NormaliseBuffer(commandBuffer.ToArray());
            return new OpenCommand(command, CommandTypeEnum.RequestTotals, nozzle);
        }

        private static OpenCommand GetFuelingPointData(this FuelPoint fp, Nozzle nozzle)
        {
            if (nozzle == null)
                return null;
            byte startByte = BitConverter.GetBytes(240 + nozzle.ParentFuelPoint.Address - 1)[0];
            byte startByteNeg = (byte)(255 - startByte);
            byte[] command = new byte[] { startByte, startByteNeg, 0xA1, 0x5E };
            return new OpenCommand(command, CommandTypeEnum.RequestDisplayData, nozzle);

        }

        private static OpenCommand StopFuelPoint(this FuelPoint fp)
        {
            byte startByte = BitConverter.GetBytes(240 + fp.Address - 1)[0];
            byte startByteNeg = (byte)(255 - startByte);
            byte[] command = new byte[] { startByte, startByteNeg, 0xA3, 0x5C };
            return new OpenCommand(command, CommandTypeEnum.Halt, fp);
        }

        private static byte TotalNozzleDefinition(this FuelPoint fp, int nozzleIndex)
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
        }

        public static byte[] StringToByteArray(this FuelPoint fp, string hex)
        {
            if (hex.Length % 2 != 0)
                hex = "0" + hex;
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        private static byte[] GetDecimalBytes(this FuelPoint fp, decimal value, int decimalPlaces, int byteCount)
        {
            long valueInt = (long)value;// ((double)value * Math.Pow(10, decimalPlaces));
            List<Byte> bufferList = new List<byte>();
            int maxPow = 2 * (byteCount - 1);
            double restValue = (double)valueInt;
            string str = "";
            for (int i = 0; i < byteCount; i++)
            {
                int pow = maxPow - (2 * i);
                double byteVal = (int)(restValue / System.Math.Pow(10, pow));
                int bv = (int)byteVal;
                str = str + bv.ToString();
                //bufferList.Add(b);
                restValue = restValue - (byteVal * System.Math.Pow(10, pow));
            }
            return fp.StringToByteArray(str);
        }

        private static byte[] NormaliseBuffer(this FuelPoint fp, byte[] buffer)
        {
            List<byte> newBuffer = new List<byte>();
            foreach (Byte b in buffer)
            {
                newBuffer.Add(b);
                newBuffer.Add((byte)(255 - b));
            }
            return newBuffer.ToArray();
        }
    }
}


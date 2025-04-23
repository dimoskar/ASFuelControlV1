using System;
using System.Collections.Generic;
using System.Text;
using PTSLib.PTS;
using System.IO;
using System.Collections;

namespace PTSLib.Unipump
{
    /// <summary>
    /// Contains methods for formation of messages to PTS controller in format of UniPump communication protocol specification. 
    /// </summary>
    internal static class UnipumpUtils
    {
        //UniPump protocol command codes
        public const byte uStatusRequest = 0x53;
        public const byte uAutorizeRequest = 0x41;
        public const byte uHaltRequest = 0x48;
        public const byte uCloseTransactionRequest = 0x43;
        public const byte uTotalRequest = 0x54;
        public const byte uLockRequest = 0x4c;
        public const byte uUnlockRequest = 0x55;
        public const byte uPricesSetRequest = 0x70;
        public const byte uPricesGetRequest = 0x50;
        public const byte uVersionRequest = 0x56;
        public const byte uPumpConfigSetRequest = 0x51;
        public const byte uPumpConfigGetRequest = 0x46;
        public const byte uAtgConfigSetRequest = 0x5A;
        public const byte uAtgConfigGetRequest = 0x59;
        public const byte uAtgMeasureRequest = 0x58;
        public const byte uParamSetRequest = 0x57;
        public const byte uParamGetRequest = 0x52;

        //UniPump protocol response codes
        public const byte uStatusResponse = 0x53;
        public const byte uUnlockStatusResponse = 0x55;
        public const byte uAmountInfoResponse = 0x41;
        public const byte uTransactionInfoResponse = 0x54;
        public const byte uTotalInfoResponse = 0x43;
        public const byte uPricesResponse = 0x50;
        public const byte uVersionResponse = 0x56;
        public const byte uPumpConfigResponse = 0x51;
        public const byte uAtgConfigResponse = 0x5A;
        public const byte uAtgMeasureResponse = 0x58;
        public const byte uParamResponse = 0x52;

        //UniPump protocol extended message code
        public const byte uExtended = 0x45;
        public const byte uExtendedSeparator = 0x3B;

        //UniPump protocol escape characters
        public const byte STX = 0x02;
        public const byte ETX = 0x03;
        public const byte DLE = 0x10;
        private static readonly byte[] _dle = { DLE, DLE };

        /// <summary>
        /// General method for preparation of the packet to be sent to the PTS controller.
        /// </summary>
        /// <param name="deviceId">ID of the device (fuel dispenser, ATG system probe, etc), to which command is addressed.</param>
        /// <param name="data">List of bytes of transferred command.</param>
        public static List<byte> CreateMessage(int deviceId, List<byte> data)
        {
            List<byte> result = new List<byte>();
            List<byte> crcDat = new List<byte>();
            int index = 0;
            byte address;

            if (data == null) 
                throw new NullReferenceException();
            if (deviceId < 0 || deviceId > PtsConfiguration.FuelPointQuantity) 
                throw new ArgumentOutOfRangeException("deviceId");

            if (deviceId == 0)
                address = (byte)deviceId;
            else
                address = (byte)(deviceId + 0x30);

            crcDat.Add(address);
            crcDat.AddRange(data);
            crcDat.AddRange(BitConverter.GetBytes(PtsCRC16.CalculateFast(crcDat.ToArray())));

            //Double DLE characters inside the packet data to avoid confusion with DLE escape characters 
            while (index >= 0)
            {
                index = crcDat.IndexOf(DLE, index);
                if (index >= 0)
                {
                    crcDat.Insert(index, DLE);
                    index += 2;
                }
            }

            result.Add(DLE);
            result.Add(STX);
            result.AddRange(crcDat);
            result.Add(DLE);
            result.Add(ETX);

            return result;
        }

        /// <summary>
        /// General method for formation of data field of command to be sent to PTS controller.
        /// </summary>
        /// <param name="deviceId">ID of the device (fuel dispenser, ATG system probe, etc), to which command is addressed.</param>
        /// <param name="requestCode">Code of the command to be executed.</param>
        /// <param name="data">List of data bytes of the commands.</param>
        public static List<byte> CreateRequestMessage(int deviceId, byte requestCode, List<byte> data)
        {
            List<byte> rData = new List<byte>();

            rData.Add(requestCode);
            if (data != null)
                rData.AddRange(data);

            return CreateMessage(deviceId, rData);
        }

        /// <summary>
        /// Method for formation of StatusRequest command.
        /// </summary>
        /// <param name="deviceId">ID of the device (fuel dispenser, ATG system probe, etc), to which command is addressed.</param>
        public static List<byte> CreateStatusRequestMessage(int deviceId)
        {
            return CreateRequestMessage(deviceId, uStatusRequest, null);
        }

        /// <summary>
        /// Method for formation of ExtendedStatusRequest command.
        /// </summary>
        /// <param name="deviceId">ID of the device (fuel dispenser, ATG system probe, etc), to which command is addressed.</param>
        public static List<byte> CreateExtendedStatusRequestMessage(int deviceId)
        {
            List<byte> rData = new List<byte>();

            rData.Add(uStatusRequest);

            return CreateRequestMessage(deviceId, uExtended, rData);
        }

        /// <summary>
        /// Method for formation of AuthorizeRequest command.
        /// </summary>
        /// <param name="deviceId">ID of the device (fuel dispenser, ATG system probe, etc), to which command is addressed.</param>
        /// <param name="nozzleId">ID of nozzle.</param>
        /// <param name="authorizeType">Type of authorization.</param>
        /// <param name="orderAmount">Amount of order (in volume or money amount depending on authorization type).</param>
        /// <param name="pricePerLiter">Price per liter (or other volume unit).</param>
        public static List<byte> CreateAuthorizeRequestMessage(int deviceId, byte nozzleId, AuthorizeType authorizeType, int orderAmount, int pricePerLiter)
        {
            List<byte> rData = new List<byte>();

            rData.Add(AsciiConversion.ByteToAscii(nozzleId));
            rData.Add((byte)authorizeType);
            rData.AddRange(AsciiConversion.IntToAscii(orderAmount, 8));
            rData.AddRange(AsciiConversion.IntToAscii(pricePerLiter, 4));

            return CreateRequestMessage(deviceId, uAutorizeRequest, rData);
        }

        /// <summary>
        /// Method for formation of ExtendedAuthorizeRequest command.
        /// </summary>
        /// <param name="deviceId">ID of the device (fuel dispenser, ATG system probe, etc), to which command is addressed.</param>
        /// <param name="nozzleId">ID of nozzle.</param>
        /// <param name="autorizeType">Type of authorization.</param>
        /// <param name="orderAmount">Amount of order (in volume or money amount depending on authorization type).</param>
        /// <param name="pricePerLiter">Price per liter (or other volume unit).</param>
        public static List<byte> CreateExtendedAuthorizeRequestMessage(int deviceId, byte nozzleId, AuthorizeType autorizeType, int orderAmount, int pricePerLiter)
        {
            List<byte> rData = new List<byte>();

            rData.Add(uAutorizeRequest);

            if (AsciiConversion.ByteToAsciiExt(nozzleId) != 0)
                rData.Add(AsciiConversion.ByteToAsciiExt(nozzleId));
            rData.Add(uExtendedSeparator);

            rData.Add((byte)autorizeType);
            rData.Add(uExtendedSeparator);

            if (AsciiConversion.IntToAsciiExt(orderAmount) != null)
                rData.AddRange(AsciiConversion.IntToAsciiExt(orderAmount));
            rData.Add(uExtendedSeparator);

            if (AsciiConversion.IntToAsciiExt(pricePerLiter) != null)
                rData.AddRange(AsciiConversion.IntToAsciiExt(pricePerLiter));
            rData.Add(uExtendedSeparator);

            return CreateRequestMessage(deviceId, uExtended, rData);
        }

        /// <summary>
        /// Method for formation of CloseTransactionRequest command.
        /// </summary>
        /// <param name="deviceId">ID of the device (fuel dispenser, ATG system probe, etc), to which command is addressed.</param>
        /// <param name="transactionId">ID of transaction.</param>
        /// <returns></returns>
        public static List<byte> CreateCloseTransactionRequestMessage(int deviceId, int transactionId)
        {
            List<byte> rData = new List<byte>();

            rData.AddRange(AsciiConversion.IntToAscii(transactionId, 2));

            return CreateRequestMessage(deviceId, uCloseTransactionRequest, rData);
        }

        /// <summary>
        /// Method for formation of CreateTotalRequest command.
        /// </summary>
        /// <param name="deviceId">ID of the device (fuel dispenser, ATG system probe, etc), to which command is addressed.</param>
        /// <param name="nozzleId">ID of nozzle.</param>
        public static List<byte> CreateTotalRequestMessage(int deviceId, byte nozzleId)
        {
            List<byte> rData = new List<byte>();

            rData.Add(AsciiConversion.ByteToAscii(nozzleId));

            return CreateRequestMessage(deviceId, uTotalRequest, rData);
        }

        /// <summary>
        /// Method for formation of CreateTotalRequest command.
        /// </summary>
        /// <param name="deviceId">ID of the device (fuel dispenser, ATG system probe, etc), to which command is addressed.</param>
        /// <param name="nozzleId">ID of nozzle.</param>
        public static List<byte> CreateExtendedTotalRequestMessage(int deviceId, byte nozzleId)
        {
            List<byte> rData = new List<byte>();

            rData.Add(uTotalRequest);
            rData.Add(AsciiConversion.ByteToAscii(nozzleId));

            return CreateRequestMessage(deviceId, uExtended, rData);
        }

        /// <summary>
        /// Method for formation of HaltRequest command.
        /// </summary>
        /// <param name="deviceId">ID of the device (fuel dispenser, ATG system probe, etc), to which command is addressed.</param>
        public static List<byte> CreateHaltRequestMessage(int deviceId)
        {
            return CreateRequestMessage(deviceId, uHaltRequest, null);
        }

        /// <summary>
        /// Method for formation of LockRequest command.
        /// </summary>
        /// <param name="deviceId">ID of the device (fuel dispenser, ATG system probe, etc), to which command is addressed.</param>
        public static List<byte> CreateLockRequestMessage(int deviceId)
        {
            return CreateRequestMessage(deviceId, uLockRequest, null);
        }

        /// <summary>
        /// Method for formation of UnlockRequest command.
        /// </summary>
        /// <param name="deviceId">ID of the device (fuel dispenser, ATG system probe, etc), to which command is addressed.</param>
        public static List<byte> CreateUnlockRequestMessage(int deviceId)
        {
            return CreateRequestMessage(deviceId, uUnlockRequest, null);
        }

        /// <summary>
        /// Method for formation of PricesSetRequest command.
        /// </summary>
        /// <param name="deviceId">ID of the device (fuel dispenser, ATG system probe, etc), to which command is addressed.</param>
        /// <param name="prices">Array of nozzle prices of a fuel point.</param>
        public static List<byte> CreatePricesSetRequestMessage(int deviceId, int[] prices)
        {
            List<byte> rData = new List<byte>();

            foreach (int price in prices)
                rData.AddRange(AsciiConversion.IntToAscii(price, 4));

            return CreateRequestMessage(deviceId, uPricesSetRequest, rData);
        }

        /// <summary>
        /// Method for formation of ExtendedPricesSetRequest command.
        /// </summary>
        /// <param name="deviceId">ID of the device (fuel dispenser, ATG system probe, etc), to which command is addressed.</param>
        /// <param name="prices">Array of nozzle prices of a fuel point.</param>
        public static List<byte> CreateExtendedPricesSetRequestMessage(int deviceId, int[] prices)
        {
            List<byte> rData = new List<byte>();

            rData.Add(uPricesSetRequest);

            foreach (int price in prices)
            {
                rData.AddRange(AsciiConversion.IntToAsciiExt(price));
                rData.Add(uExtendedSeparator);
            }

            return CreateRequestMessage(deviceId, uExtended, rData);
        }

        /// <summary>
        /// Method for formation of PricesGetRequest command.
        /// </summary>
        /// <param name="deviceId">ID of the device (fuel dispenser, ATG system probe, etc), to which command is addressed.</param>
        public static List<byte> CreatePricesGetRequestMessage(int deviceId)
        {
            return CreateRequestMessage(deviceId, uPricesGetRequest, null);
        }

        /// <summary>
        /// Method for formation of ParameterGetRequest command.
        /// </summary>
        /// <param name="parameterAddress">Address of the parameter requested.</param>
        /// <param name="parameterNumber">Number of the parameter requested.</param>
        public static List<byte> CreateParameterGetRequestMessage(short parameterAddress, int parameterNumber)
        {
            List<byte> rData = new List<byte>();

            rData.AddRange(AsciiConversion.IntToAscii(parameterNumber, 4));

            return CreateRequestMessage((int)parameterAddress, uParamGetRequest, rData);
        }

        /// <summary>
        /// Method for formation of ParameterSetRequest command.
        /// </summary>
        /// <param name="parameterAddress">Address of the parameter requested.</param>
        /// <param name="parameterNumber">Number of the parameter requested.</param>
        /// <param name="parameterValue">Value of the parameter requested.</param>
        public static List<byte> CreateParameterSetRequestMessage(short parameterAddress, int parameterNumber, byte[] parameterValue)
        {
            List<byte> rData = new List<byte>();

            rData.AddRange(AsciiConversion.IntToAscii(parameterNumber, 4));
            rData.AddRange(parameterValue);

            return CreateRequestMessage((int)parameterAddress, uParamSetRequest, rData);
        }

        /// <summary>
        /// Method for formation of VersionRequest command.
        /// </summary>
        public static List<byte> CreateVersionRequestMessage()
        {
            return CreateRequestMessage(0, uVersionRequest, null);
        }

        /// <summary>
        /// Method for formation of PumpConfigGetRequest command.
        /// </summary>
        public static List<byte> CreatePumpConfigGetRequestMessage()
        {
            return CreateRequestMessage(0, uPumpConfigGetRequest, null);
        }

        /// <summary>
        /// Method for formation of PumpConfigSetRequest command.
        /// </summary>
        /// <param name="settings">Configuration of PTS controller PTSLib.PTS.PtsConfiguration.</param>
        public static List<byte> CreatePumpConfigSetRequestMessage(PtsConfiguration settings)
        {
            List<byte> rData = new List<byte>();

            // FuelPoint channels
            for (int i = 0; i < PtsConfiguration.FuelPointChannelQuantity; i++)
            {
                rData.Add(AsciiConversion.ByteToAscii((byte)settings.FuelPointChannels[i].ID));
                rData.AddRange(AsciiConversion.IntToAscii((int)settings.FuelPointChannels[i].Protocol, 2));
                rData.Add(AsciiConversion.ByteToAscii((byte)settings.FuelPointChannels[i].BaudRate));
            }

            // FuelPoints
            for (int i = 0; i < PtsConfiguration.FuelPointQuantity; i++)
            {
                rData.AddRange(AsciiConversion.IntToAscii((settings.FuelPoints[i].ID), 2));
                rData.AddRange(AsciiConversion.IntToAscii((settings.FuelPoints[i].PhysicalAddress), 2));
                rData.Add(AsciiConversion.ByteToAscii((byte)settings.FuelPoints[i].ChannelID));
            }

            return CreateRequestMessage(0, uPumpConfigSetRequest, rData);
        }

        /// <summary>
        /// Method for formation of AtgConfigGetRequest command.
        /// </summary>
        public static List<byte> CreateAtgConfigGetRequestMessage()
        {
            return CreateRequestMessage(0, uAtgConfigGetRequest, null);
        }

        /// <summary>
        /// Method for formation of AtgConfigSetRequest command.
        /// </summary>
        /// <param name="settings">Configuration of PTS controller PTSLib.PTS.PtsConfiguration.</param>
        public static List<byte> CreateAtgConfigSetRequestMessage(PtsConfiguration settings)
        {
            List<byte> rData = new List<byte>();

            // ATG channels
            for (int i = 0; i < PtsConfiguration.AtgChannelQuantity; i++)
            {
                rData.Add(AsciiConversion.ByteToAscii((byte)settings.AtgChannels[i].ID));
                rData.AddRange(AsciiConversion.IntToAscii((int)settings.AtgChannels[i].Protocol, 2));
                rData.Add(AsciiConversion.ByteToAscii((byte)settings.AtgChannels[i].BaudRate));
            }

            // ATG
            for (int i = 0; i < PtsConfiguration.AtgQuantity; i++)
            {
                rData.AddRange(AsciiConversion.IntToAscii((settings.ATGs[i].ID), 2));
                rData.AddRange(AsciiConversion.IntToAscii((settings.ATGs[i].PhysicalAddress), 2));
                rData.Add(AsciiConversion.ByteToAscii((byte)settings.ATGs[i].ChannelID));
            }

            return CreateRequestMessage(0, uAtgConfigSetRequest, rData);
        }

        /// <summary>
        /// Method for formation of AtgDataRequest command.
        /// </summary>
        /// <param name="deviceId">ID of the device (fuel dispenser, ATG system probe, etc), to which command is addressed.</param>
        public static List<byte> CreateAtgDataRequestMessage(int deviceId)
        {
            return CreateRequestMessage(deviceId, uAtgMeasureRequest, null);
        }

        /// <summary>
        /// Method for checking whether message has format in accordance with UniPump communication protocol specification.
        /// </summary>
        /// <param name="message"></param>
        public static bool IsValidMessage(byte[] message)
        {
            if (message.Length < 8 || message.Length > 255) return false; //Check message length
            if (message[0] != DLE && message[1] != STX && message[message.Length - 1] != ETX && message[message.Length - 2] != DLE) return false;
            if (message[2] == 0) return true; //Check device ID
            else if (message[2] < 0x31 || message[2] > 0xFF) return false;

            return IsValidCRC(message);
        }

        /// <summary>
        /// Method for checking CRC of the message.
        /// </summary>
        /// <param name="message">Message with CRC to be checked.</param>
        public static bool IsValidCRC(byte[] message)
        {
            List<byte> body = new List<byte>();
            List<byte> messageCrc = new List<byte>();
            List<byte> recalcCrc = new List<byte>();
            ushort messageCrcValue;
            ushort calcCrcValue;

            //Check CRC of the packet: calculated  CRC  on  the  fields (<ADDR>, <DATA>, <CRC>) should be equal to 0
            //Ignore service symbols <DLE> in packet at calculation of CRC
            for (int i = 2; i < message.Length - 2; i++)
            {
                if (message[i] == DLE && message[i + 1] == DLE && i != message.Length - 3)
                    continue;

                body.Add(message[i]);
            }

            for (int i = body.Count - 2; i < body.Count; i++)
                messageCrc.Add(body[i]);

            messageCrcValue = BitConverter.ToUInt16(messageCrc.ToArray(), 0);

            for (int i = 0; i < body.Count - 2; i++)
                recalcCrc.Add(body[i]);

            calcCrcValue = PtsCRC16.CalculateFast(recalcCrc.ToArray());

            return messageCrcValue == calcCrcValue;
        }
    }
}

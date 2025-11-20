using ASFuelControl.Common;
using ASFuelControl.HongYang.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.HongYang
{
    public class DispenserClient
    {
        private readonly SerialTransport _transport;

        public DispenserClient(SerialTransport transport)
        {
            _transport = transport;
        }

        //private bool TrySendCommand(byte address, CommandCode cmd, byte[] param, out ResponsePacket response, out string diagnostic)
        //{
        //    response = null;
        //    diagnostic = string.Empty;

        //    try
        //    {
        //        Logger.Instance.Debug("Start TrySendCommand");
        //        var packet = new CommandPacket
        //        {
        //            Address = address,
        //            Command = cmd,
        //            Parameter = param ?? new byte[0]
        //        };
        //        Logger.Instance.Debug($"Sending command {BitConverter.ToString(packet.ToBytes())} to address {address}");
        //        _transport.Send(packet.ToBytes());
        //        response = _transport.ReceiveAndParse();

        //        diagnostic = "Response OK. State byte: 0x" + response.State.ToString("X2");
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Instance.Error($"Error in TrySendCommand: {ex.Message}");
        //        diagnostic = "Error: " + ex.Message;
        //        return false;
        //    }
        //}

        public bool IsConnected()
        {
            return _transport.IsConnected();
        }

        private ResponsePacket SendCommand(byte address, byte command, byte[] parameters)
        {
            if(parameters == null)
                parameters = new byte[] { };
            byte[] buffer = new byte[parameters.Length + 4];
            buffer[0] = address;
            buffer[1] = (byte)(buffer.Length - 1);
            buffer[2] = command;
            if (parameters != null && parameters.Length > 0)
            {
                for(var i=0; i < parameters.Length; i++)
                {
                    buffer[i + 3] = parameters[i];
                }
            }
            var checkSumBuffer = buffer.Take(buffer.Length - 1).ToArray();
            buffer[buffer.Length - 1] = ResponsePacket.CalculateChecksum(checkSumBuffer);
            var payload = buffer.Skip(1).ToArray();
            return _transport.Send(address, payload);
        }

        public Common.Enumerators.FuelPointStatusEnum GetStatus(byte address)
        {
            var responsePackage = SendCommand(address, (byte)(int)CommandCode.Status, null);
            var stateBytes = new byte[] { responsePackage.State };
            var text = (responsePackage == null ? "null" : BitConverter.ToString(stateBytes, 0));
            if (responsePackage == null)
                return Common.Enumerators.FuelPointStatusEnum.Offline;

            var status = responsePackage.ParseStatus();
            return status;
        }

        public Common.Enumerators.FuelPointStatusEnum AuthorizeDispenser(byte address)
        {
            var responsePackage = SendCommand(address, (byte)(int)CommandCode.Authorise, null);
            if (responsePackage == null)
                return Common.Enumerators.FuelPointStatusEnum.Offline;

            return responsePackage.ParseStatus();
        }
        public Common.Enumerators.FuelPointStatusEnum GetDisplay(byte address, ref int amount, ref int volume)
        {
            var responsePackage = SendCommand(address, (byte)(int)CommandCode.ReadFuelledAmount, null);
            if (responsePackage == null)
                return Common.Enumerators.FuelPointStatusEnum.Offline;
            //var amountBytes = responsePackage.Parameter.Skip(2).Take(4).ToArray();
            //var volumeBytes = responsePackage.Parameter.Skip(6).Take(4).ToArray();
            amount = (int)responsePackage.ConvertBytesToLong(0, 4, true);// BcdConverter.Decode(amountBytes);
            volume = (int)responsePackage.ConvertBytesToLong(4, 4, true);
            return responsePackage.ParseStatus();
        }
        public Common.Enumerators.FuelPointStatusEnum Stop(byte address)
        {
            var responsePackage = SendCommand(address, (byte)(int)CommandCode.Stop, null);
            if (responsePackage == null)
                return Common.Enumerators.FuelPointStatusEnum.Offline;
            return responsePackage.ParseStatus();
        }
        public Common.Enumerators.FuelPointStatusEnum HaltDispenser(byte address)
        {
            var responsePackage = SendCommand(address, (byte)(int)CommandCode.Stop, null);
            if (responsePackage == null)
                return Common.Enumerators.FuelPointStatusEnum.Offline;
            return responsePackage.ParseStatus();
        }

        public Common.Enumerators.FuelPointStatusEnum ChangePrice(byte address, int price)
        {
            var parameters = ResponsePacket.LongToBytes(price, 4, true);
            var responsePackage = SendCommand(address, (byte)(int)CommandCode.ChangePrice, parameters);
            if (responsePackage == null)
                return Common.Enumerators.FuelPointStatusEnum.Offline;

            return responsePackage.ParseStatus();
        }

        public Common.Enumerators.FuelPointStatusEnum GetPrice(byte address, ref int price)
        {
            var responsePackage = SendCommand(address, (byte)(int)CommandCode.GetPrice, null);
            if (responsePackage == null)
                return Common.Enumerators.FuelPointStatusEnum.Offline;
            price = (int)responsePackage.ConvertBytesToLong(0, 4, true);
            return responsePackage.ParseStatus();
        }

        public Common.Enumerators.FuelPointStatusEnum GetTotals(byte address, ref int volume, ref int amount)
        {
            volume = 0;
            amount = 0;
            var responsePackage = SendCommand(address, (byte)(int)CommandCode.GetTotals, null);
            if (responsePackage == null)
                return Common.Enumerators.FuelPointStatusEnum.Offline;
            //Logger.Instance.Debug($"GetTotals response parameters: {BitConverter.ToString(responsePackage.Parameter)}");
            //Logger.Instance.Debug($"GetTotals Volume part: {BitConverter.ToString(responsePackage.Parameter.Skip(0).Take(6).ToArray())}");
            //Logger.Instance.Debug($"GetTotals Amount part: {BitConverter.ToString(responsePackage.Parameter.Skip(6).Take(6).ToArray())}");
            
            volume = (int)responsePackage.DecodeDecimalFromReversedBytes(0, 6);
            amount = (int)responsePackage.DecodeDecimalFromReversedBytes(6, 6);
            return responsePackage.ParseStatus();
        }

        public bool IsDispenserOnline(byte address)
        {
            var responsePackage = SendCommand(address, (byte)(int)CommandCode.GetPrice, null);
            return responsePackage != null;
        }

        public Common.Enumerators.FuelPointStatusEnum ClearDisplay(byte address)
        {
            ResponsePacket response;
            return Common.Enumerators.FuelPointStatusEnum.Idle; 
        }

        //public bool InitializeDispenser(byte address, out string diagnostic)
        //{
        //    var log = new List<string>();
        //    string diag;

        //    if (!IsDispenserOnline(address, out diag))
        //    {
        //        diagnostic = "Offline — " + diag;
        //        return false;
        //    }
        //    log.Add("Online — " + diag);

        //    LockDispenser(address, out diag);
        //    log.Add("Lock → " + diag);

        //    double price;
        //    GetPrice(address, out price, out diag);
        //    log.Add("Price → " + diag);

        //    int vol, amt;
        //    GetTotals(address, out vol, out amt, out diag);
        //    log.Add("Totals → " + diag);

        //    diagnostic = string.Join(Environment.NewLine, log);
        //    return true;
        //}
    }
}

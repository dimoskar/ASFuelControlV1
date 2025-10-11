// File: TatsunoPdeClient.Requests.cs (net462)
using System;
using System.Threading;

namespace ASFuelControl.Tatsuno
{
    //public partial class TatsunoPdeClient
    //{
    //    // Common timeout for requests
    //    public int DefaultTimeoutMs { get; set; } = 250;

    //    // Initialize / ping the address
    //    public PdeResult<PdeAck> Initialize(int address, CancellationToken ct = default(CancellationToken))
    //    {
    //        byte addr = (byte)(32 + address);
    //        var rr = SendAndReceiveRaw(addr, PdeCommands.Initialize, new byte[0], DefaultTimeoutMs, ct);
    //        if (!rr.Success) return Fail<PdeAck>(rr.Address, rr.Command, !string.IsNullOrEmpty(rr.Error) ? rr.Error : "Init failed", rr.RawFrame, rr.Payload);
    //        var ack = ParseAck(rr.Payload);
    //        return Ok<PdeAck>(rr.Address, rr.Command, rr.RawFrame, rr.Payload, ack);
    //    }

    //    public PdeResult<PdeStatus> RequestStatus(int address, CancellationToken ct = default(CancellationToken))
    //    {
    //        byte addr = (byte)(32 + address);
    //        var rr = SendAndReceiveRaw(addr, PdeCommands.Status, new byte[0], DefaultTimeoutMs, ct);
    //        if (!rr.Success) return Fail<PdeStatus>(rr.Address, rr.Command, !string.IsNullOrEmpty(rr.Error) ? rr.Error : "Status failed", rr.RawFrame, rr.Payload);
    //        var status = ParseStatus(rr.Payload);
    //        return Ok<PdeStatus>(rr.Address, rr.Command, rr.RawFrame, rr.Payload, status);
    //    }

    //    public PdeResult<PdeDisplay> RequestDisplay(int address, CancellationToken ct = default(CancellationToken))
    //    {
    //        byte addr = (byte)(32 + address);
    //        var rr = SendAndReceiveRaw(addr, PdeCommands.Display, new byte[0], DefaultTimeoutMs, ct);
    //        if (!rr.Success) return Fail<PdeDisplay>(rr.Address, rr.Command, !string.IsNullOrEmpty(rr.Error) ? rr.Error : "Display failed", rr.RawFrame, rr.Payload);
    //        var disp = ParseDisplay(rr.Payload);
    //        return Ok<PdeDisplay>(rr.Address, rr.Command, rr.RawFrame, rr.Payload, disp);
    //    }

    //    public PdeResult<PdeHistory> RequestLastTransaction(int address, int index, CancellationToken ct = default(CancellationToken))
    //    {
    //        byte addr = (byte)(32 + address);
    //        var payload = new byte[2];
    //        payload[0] = (byte)(index & 0xFF);
    //        payload[1] = (byte)((index >> 8) & 0xFF);

    //        var rr = SendAndReceiveRaw(addr, PdeCommands.LastTransaction, payload, DefaultTimeoutMs, ct);
    //        if (!rr.Success) return Fail<PdeHistory>(rr.Address, rr.Command, !string.IsNullOrEmpty(rr.Error) ? rr.Error : "LastTransaction failed", rr.RawFrame, rr.Payload);
    //        var hist = ParseHistory(index, rr.Payload);
    //        return Ok<PdeHistory>(rr.Address, rr.Command, rr.RawFrame, rr.Payload, hist);
    //    }

    //    public PdeResult<PdeRegisters> RequestRegisters(int address, int nozzle, int type, CancellationToken ct = default(CancellationToken))
    //    {
    //        byte addr = (byte)(32 + address);
    //        var payload = new byte[2];
    //        payload[0] = (byte)nozzle;
    //        payload[1] = (byte)type; // device-specific meaning

    //        var rr = SendAndReceiveRaw(addr, PdeCommands.Registers, payload, DefaultTimeoutMs, ct);
    //        if (!rr.Success) return Fail<PdeRegisters>(rr.Address, rr.Command, !string.IsNullOrEmpty(rr.Error) ? rr.Error : "Registers failed", rr.RawFrame, rr.Payload);
    //        var regs = ParseRegisters(nozzle, rr.Payload);
    //        return Ok<PdeRegisters>(rr.Address, rr.Command, rr.RawFrame, rr.Payload, regs);
    //    }

    //    public PdeResult<PdeText> RequestTextMessage(int address, int number, CancellationToken ct = default(CancellationToken))
    //    {
    //        byte addr = (byte)(32 + address);
    //        var payload = new byte[1] { (byte)number };

    //        var rr = SendAndReceiveRaw(addr, PdeCommands.TextMessage, payload, DefaultTimeoutMs, ct);
    //        if (!rr.Success) return Fail<PdeText>(rr.Address, rr.Command, !string.IsNullOrEmpty(rr.Error) ? rr.Error : "TextMessage failed", rr.RawFrame, rr.Payload);
    //        var txt = ParseText(number, rr.Payload);
    //        return Ok<PdeText>(rr.Address, rr.Command, rr.RawFrame, rr.Payload, txt);
    //    }

    //    public PdeResult<PdeTextStatus> RequestTextStatus(int address, int number, CancellationToken ct = default(CancellationToken))
    //    {
    //        byte addr = (byte)(32 + address);
    //        var payload = new byte[1] { (byte)number };

    //        var rr = SendAndReceiveRaw(addr, PdeCommands.TextStatus, payload, DefaultTimeoutMs, ct);
    //        if (!rr.Success) return Fail<PdeTextStatus>(rr.Address, rr.Command, !string.IsNullOrEmpty(rr.Error) ? rr.Error : "TextStatus failed", rr.RawFrame, rr.Payload);
    //        var st = ParseTextStatus(number, rr.Payload);
    //        return Ok<PdeTextStatus>(rr.Address, rr.Command, rr.RawFrame, rr.Payload, st);
    //    }

    //    public PdeResult<PdeError> RequestError(int address, CancellationToken ct = default(CancellationToken))
    //    {
    //        byte addr = (byte)(32 + address);
    //        var rr = SendAndReceiveRaw(addr, PdeCommands.Error, new byte[0], DefaultTimeoutMs, ct);
    //        if (!rr.Success) return Fail<PdeError>(rr.Address, rr.Command, !string.IsNullOrEmpty(rr.Error) ? rr.Error : "Error request failed", rr.RawFrame, rr.Payload);
    //        var err = ParseError(rr.Payload);
    //        return Ok<PdeError>(rr.Address, rr.Command, rr.RawFrame, rr.Payload, err);
    //    }

    //    public PdeResult<PdeAck> Authorize(
    //        int address,
    //        int nozzle,
    //        PdePresetType presetType,
    //        decimal presetValue,
    //        CancellationToken ct = default(CancellationToken))
    //    {
    //        byte addr = (byte)(32 + address);

    //        // Payload example: [0]=nozzle, [1]=presetType, [2..5]=value minor unit (u32 LE)
    //        uint valueMinor = 0;
    //        if (presetType == PdePresetType.Amount)
    //            valueMinor = (uint)Math.Round(presetValue * _opt.AmountMinorUnit);
    //        else if (presetType == PdePresetType.Volume)
    //            valueMinor = (uint)Math.Round(presetValue * _opt.VolumeMinorUnit);

    //        var payload = new byte[6];
    //        payload[0] = (byte)nozzle;
    //        payload[1] = (byte)presetType;
    //        Le.WriteUInt32(payload, 2, valueMinor);

    //        var rr = SendAndReceiveRaw(addr, PdeCommands.Authorize, payload, DefaultTimeoutMs, ct);
    //        if (!rr.Success) return Fail<PdeAck>(rr.Address, rr.Command, !string.IsNullOrEmpty(rr.Error) ? rr.Error : "Authorize failed", rr.RawFrame, rr.Payload);
    //        var ack = ParseAck(rr.Payload);
    //        return Ok<PdeAck>(rr.Address, rr.Command, rr.RawFrame, rr.Payload, ack);
    //    }

    //    public PdeResult<PdeAck> Control(int address, int commandCode, CancellationToken ct = default(CancellationToken))
    //    {
    //        byte addr = (byte)(32 + address);
    //        var payload = new byte[1] { (byte)commandCode }; // device-specific (e.g., Stop, Resume, Lock, Unlock)
    //        var rr = SendAndReceiveRaw(addr, PdeCommands.Control, payload, DefaultTimeoutMs, ct);
    //        if (!rr.Success) return Fail<PdeAck>(rr.Address, rr.Command, !string.IsNullOrEmpty(rr.Error) ? rr.Error : "Control failed", rr.RawFrame, rr.Payload);
    //        var ack = ParseAck(rr.Payload);
    //        return Ok<PdeAck>(rr.Address, rr.Command, rr.RawFrame, rr.Payload, ack);
    //    }

    //    public PdeResult<PdeAck> SetUnitPrice(int address, int product, int priceMinorUnit, CancellationToken ct = default(CancellationToken))
    //    {
    //        byte addr = (byte)(32 + address);
    //        // Payload: [0]=product, [1..4]=price minor unit (u32 LE)
    //        var payload = new byte[5];
    //        payload[0] = (byte)product;
    //        Le.WriteUInt32(payload, 1, (uint)priceMinorUnit);

    //        var rr = SendAndReceiveRaw(addr, PdeCommands.SetUnitPrice, payload, DefaultTimeoutMs, ct);
    //        if (!rr.Success) return Fail<PdeAck>(rr.Address, rr.Command, !string.IsNullOrEmpty(rr.Error) ? rr.Error : "SetUnitPrice failed", rr.RawFrame, rr.Payload);
    //        var ack = ParseAck(rr.Payload);
    //        return Ok<PdeAck>(rr.Address, rr.Command, rr.RawFrame, rr.Payload, ack);
    //    }
    //}
}
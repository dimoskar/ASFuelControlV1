// File: Pde.Models.cs (net462)
using System;

namespace ASFuelControl.Tatsuno
{
    // Envelope so every request "includes the response"
    //public class PdeResult<T>
    //{
    //    public bool Success { get; set; }
    //    public string Error { get; set; }
    //    public byte Address { get; set; }
    //    public byte Command { get; set; }
    //    public byte[] RawFrame { get; set; }
    //    public byte[] Payload { get; set; }
    //    public T Data { get; set; }

    //    public PdeResult()
    //    {
    //        Error = string.Empty;
    //        RawFrame = new byte[0];
    //        Payload = new byte[0];
    //    }
    //}

    //// Basic ACK/NAK shape when device responds with status to a command
    //public class PdeAck
    //{
    //    public bool Ok { get; set; }
    //    public byte Code { get; set; }
    //    public string Message { get; set; }

    //    public PdeAck()
    //    {
    //        Message = string.Empty;
    //    }
    //}

    //public class PdeStatus
    //{
    //    public bool InService { get; set; }
    //    public bool Busy { get; set; }
    //    public bool Error { get; set; }
    //    public byte ActiveNozzle { get; set; }
    //    public ushort ErrorCode { get; set; }
    //}

    //public class PdeDisplay
    //{
    //    public int HoseNumber { get; set; }
    //    public decimal Volume { get; set; }       // liters
    //    public decimal Amount { get; set; }       // currency
    //    public decimal UnitPrice { get; set; }    // currency per liter
    //    public string Currency { get; set; }

    //    public PdeDisplay()
    //    {
    //        Currency = string.Empty;
    //    }
    //}

    //public class PdeHistory
    //{
    //    public int Index { get; set; }
    //    public int HoseNumber { get; set; }
    //    public DateTime Timestamp { get; set; }
    //    public decimal Volume { get; set; }
    //    public decimal Amount { get; set; }
    //    public decimal UnitPrice { get; set; }
    //    public string ReceiptId { get; set; }

    //    public PdeHistory()
    //    {
    //        ReceiptId = string.Empty;
    //    }
    //}

    //public class PdeRegisters
    //{
    //    public int HoseNumber { get; set; }
    //    public long TotalVolumeImpulses { get; set; }
    //    public long TotalAmountCents { get; set; }
    //}

    //public class PdeText
    //{
    //    public int Number { get; set; }
    //    public string Value { get; set; }

    //    public PdeText()
    //    {
    //        Value = string.Empty;
    //    }
    //}

    //public class PdeTextStatus
    //{
    //    public int Number { get; set; }
    //    public bool Displayed { get; set; }
    //    public bool Acknowledged { get; set; }
    //}

    //public class PdeError
    //{
    //    public ushort Code { get; set; }
    //    public string Description { get; set; }
    //    public byte Severity { get; set; }

    //    public PdeError()
    //    {
    //        Description = string.Empty;
    //    }
    //}

    //public enum PdePresetType : byte
    //{
    //    None = 0,
    //    Amount = 1,
    //    Volume = 2
    //}
}
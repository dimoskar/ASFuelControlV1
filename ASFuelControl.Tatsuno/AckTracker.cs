using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.Tatsuno
{
    public class AckTracker
    {
        private readonly Queue<byte> _pendingAckQueue = new Queue<byte>();
        private readonly object _lock = new object();

        public void RegisterPendingAck(byte address)
        {
            lock (_lock)
            {
                _pendingAckQueue.Enqueue(address);
            }
        }

        public AckResult ProcessAck(byte echoedAddr, byte ackByte, byte expectedAddr)
        {
            var result = new AckResult
            {
                Success = ackByte == Ctrl.ACK,
                ExpectedAddress = expectedAddr,
                EchoedAddress = echoedAddr
            };

            if (!result.Success)
            {
                Console.WriteLine($"NAK from {echoedAddr:X2} (expected ACK for {expectedAddr:X2})");
            }
            else if (!result.AddressMatched)
            {
                Console.WriteLine($"ACK mismatch: got {echoedAddr:X2}, expected {expectedAddr:X2}");
            }

            return result;
        }
    }
    public class AckResult
    {
        public bool Success { get; set; }             // Was ACK received?
        public byte ExpectedAddress { get; set; }     // Address we sent to
        public byte EchoedAddress { get; set; }       // Address we got back
        public bool AddressMatched => EchoedAddress == ExpectedAddress;
    }

}

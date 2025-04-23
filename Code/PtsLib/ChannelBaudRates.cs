using System;
using System.Collections.Generic;
using System.Text;

namespace PTSLib.PTS
{
    /// <summary>
    /// Specifies a list of supported baud ratea of FuelPointChannels and AtgChannels of PTS controller.
    /// </summary>
    public enum ChannelBaudRate
    {
        /// <summary>
        /// Baud rate is not set.
        /// </summary>
        None = 0,
        /// <summary>
        /// Baud rate 2400 baud.
        /// </summary>
        BR2400 = 1,
        /// <summary>
        /// Baud rate 4800 baud.
        /// </summary>
        BR4800 = 2,
        /// <summary>
        /// Baud rate 5758 baud.
        /// </summary>
        BR5787 = 3,
        /// <summary>
        /// Baud rate 9600 baud.
        /// </summary>
        BR9600 = 4,
        /// <summary>
        /// Baud rate 19200 baud.
        /// </summary>
        BR19200 = 5,
        /// <summary>
        /// Baud rate 1200 baud.
        /// </summary>
        BR1200 = 6,
        /// <summary>
        /// Baud rate 38400 baud.
        /// </summary>
        BR38400 = 7,
        /// <summary>
        /// Baud rate 115200 baud.
        /// </summary>
        BR115200 = 8,
        /// <summary>
        /// Baud rate 300 baud.
        /// </summary>
        BR300 = 9,
        /// <summary>
        /// Baud rate 600 baud.
        /// </summary>
        BR600 = 0xA
    }
}

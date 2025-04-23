using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace PTSLib.PTS
{
    /// <summary>
    /// Provides information about a FuelPoint channel of a PTS controller.
    /// </summary>
    //[ComVisible(true)]
    public class FuelPointChannel
    {
        private ChannelBaudRate baudRate;
        private FuelPoint[] dispensers;
        private FuelPointChannelProtocol protocol;
        private PTSController parent;

        /// <summary>
        /// Creates an exemplar of FuelPointChannel class.
        /// </summary>
        /// <param name="parent">Exemplar of parent PTS class.</param>
        internal FuelPointChannel(PTSController parent)
        {
            this.parent = parent;
        }

        /// <summary>
        /// Gets or sets baud rate of a channel.
        /// </summary>
        /// <remarks>
        /// To make this setting work it is necessary to call a method 
        /// UpdateFuelPointConfiguration() of PTS exemplar, to which a channel belongs to.
        /// </remarks>
        public ChannelBaudRate BaudRate
        {
            get
            {
                return baudRate;
            }
            set
            {
                baudRate = value;
            }
        }

        /// <summary>
        /// Gets an identifier of a channel.
        /// </summary>
        public int ID
        { get; set; }

        /// <summary>
        /// Gets or sets a communication protocol of a channel.
        /// </summary>
        /// <remarks>
        /// To make this setting work it is necessary to call a method 
        /// UpdateFuelPointConfiguration() of PTS exemplar, to which a channel belongs to.
        /// </remarks>
        public FuelPointChannelProtocol Protocol
        {
            get
            {
                return protocol;
            }
            set
            {
                protocol = value;
            }
        }

        /// <summary>
        /// Gets an array of objects FuelPoint, which belongs to given channel.
        /// </summary>
        public FuelPoint[] FuelPoints
        {
            get
            {
                return dispensers;
            }
            internal set
            {
                dispensers = value;

                if (value == null) return;

                foreach (FuelPoint dispenser in dispensers)
                {
                    dispenser.Channel = this;
                }
            }
        }

        /// <summary>
        /// Gets an a PTS exemplar, to which a channel belongs to.
        /// </summary>
        public PTSController PTS
        {
            get
            {
                return parent;
            }
        }

        public override string ToString()
        {
            return string.Format("Channel{0},Protocol:{1},Baud rate:{2}", ID, Protocol, BaudRate);
        }
    }
}

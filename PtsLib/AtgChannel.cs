using System;
using System.Collections.Generic;
using System.Text;

namespace PTSLib.PTS
{
    /// <summary>
    /// Provides information about an ATG channel of a PTS controller.
    /// </summary>
    public class AtgChannel
    {
        private ChannelBaudRate baudRate;
        private ATG[] atgs;
        private AtgChannelProtocol protocol;
        private PTSController parent;

        /// <summary>
        /// Creates an exemplar of AtgChannel class.
        /// </summary>
        /// <param name="parent">Exemplar of parent PTS class.</param>
        internal AtgChannel(PTSController parent)
        {
            this.parent = parent;
        }

        /// <summary>
        /// Gets or sets baud rate of a channel.
        /// </summary>
        /// <remarks>
        /// To make this setting work it is necessary to call a method 
        /// UpdateAtgConfiguration() of PTS exemplar, to which a channel belongs to.
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
        /// Gets or sets a communication protocol of a AtgChannel.
        /// </summary>
        public AtgChannelProtocol Protocol
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
        /// Gets an array of objects ATG, which belongs to given AtgChannel.
        /// </summary>
        public ATG[] ATGs
        {
            get
            {
                return atgs;
            }
            internal set
            {
                atgs = value;

                if (value == null) return;

                foreach (ATG atg in atgs)
                {
                    atg.Channel = this;
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

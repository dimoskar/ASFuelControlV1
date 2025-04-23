using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.IO.Ports;
using PTSLib.Unipump;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;

namespace PTSLib.PTS
{
    /// <summary>
    /// Provides information about a PTS controller configuration.
    /// </summary>
    public class PtsConfiguration
    {
        private FuelPointChannel[] fuelPointChannels;
        private FuelPoint[] fuelDispensers;
        private AtgChannel[] atgChannels;
        private ATG[] atgs;
        private PTSController pts;

        /// <summary>
        /// Quantity of FuelPoint channels in PTS controller.
        /// </summary>
        public const int FuelPointChannelQuantity = 4;

        /// <summary>
        /// Maximum quantity of FuelPoints, that can be connected to PTS controller.
        /// </summary>
        public const int FuelPointQuantity = 16;

        /// <summary>
        /// Maximum value of FuelPoint address.
        /// </summary>
        public const int FuelPointAddressQuantity = 99;

        /// <summary>
        /// Maximum possible quantity of nozzles in a FuelPoint.
        /// </summary>
        public const int NozzleQuantity = 6;

        /// <summary>
        /// Quantity of ATG channels in PTS controller.
        /// </summary>
        public const int AtgChannelQuantity = 3;

        /// <summary>
        /// Maximum quantity of ATGs, that can be connected to PTS controller.
        /// </summary>
        public const int AtgQuantity = 16;

        /// <summary>
        /// Maximum value of ATG address.
        /// </summary>
        public const int AtgAddressQuantity = 99999;

        /// <summary>
        /// Communication baud rate for PTS controller.
        /// </summary>
        public const int PtsBaudRate = 57600;

        /// <summary>
        /// Creates an exemplar of PtsConfiguration class.
        /// </summary>
        /// <param name="pts">Exemplar of parent PTS class.</param>
        internal PtsConfiguration(PTSController pts)
        {
            this.fuelPointChannels = new FuelPointChannel[PtsConfiguration.FuelPointChannelQuantity];
            this.fuelDispensers = new FuelPoint[PtsConfiguration.FuelPointQuantity];
            this.atgChannels = new AtgChannel[PtsConfiguration.AtgChannelQuantity];
            this.atgs = new ATG[PtsConfiguration.AtgQuantity];
            this.pts = pts;

            for (int i = 0; i < FuelPointChannelQuantity; i++) fuelPointChannels[i] = new FuelPointChannel(pts);
            for (int i = 0; i < FuelPointQuantity; i++) fuelDispensers[i] = new FuelPoint(pts);
            for (int i = 0; i < AtgChannelQuantity; i++) atgChannels[i] = new AtgChannel(pts);
            for (int i = 0; i < AtgQuantity; i++) atgs[i] = new ATG(pts);
        }

        /// <summary>
        /// Gets an array of objects FuelPointChannel of PTS controller.
        /// </summary>
        /// <remarks>At closed connection returns a value (Nothing в Visual Basic).</remarks>
        public FuelPointChannel[] FuelPointChannels
        {
            get
            {
                return fuelPointChannels;
            }
        }

        /// <summary>
        /// Gets an array of objects FuelPoint of PTS controller.
        /// </summary>
        /// <remarks>At closed connection returns a value (Nothing в Visual Basic).</remarks>
        public FuelPoint[] FuelPoints
        {
            get
            {
                return fuelDispensers;
            }
        }

        /// <summary>
        /// Provides initialization of FuelPoints.
        /// </summary>
        /// <param name="settResponse">Response on configuration of dispenser channels, received from a PTS controller.</param>
        internal void FuelPointInit(byte[] settResponse)
        {
            int index = 4;

            //Read FuelPoint channel settings
            for (int i = 0; i < PtsConfiguration.FuelPointChannelQuantity; i++)
            {
                this.FuelPointChannels[i].ID = AsciiConversion.AsciiToByte(settResponse[index++]);
                this.FuelPointChannels[i].Protocol = (FuelPointChannelProtocol)(AsciiConversion.AsciiToByte(settResponse[index++]) * 10 + AsciiConversion.AsciiToByte(settResponse[index++]));
                this.FuelPointChannels[i].BaudRate = (ChannelBaudRate)AsciiConversion.AsciiToByte(settResponse[index++]);
            }

            //Read FuelPoint settings
            for (int i = 0; i < PtsConfiguration.FuelPointQuantity; i++)
            {
                this.FuelPoints[i].ID = AsciiConversion.AsciiToInt(settResponse[index], settResponse[index + 1]);
                index += 2;
                this.FuelPoints[i].PhysicalAddress = AsciiConversion.AsciiToInt(settResponse[index], settResponse[index + 1]);
                index += 2;
                this.FuelPoints[i].ChannelID = AsciiConversion.AsciiToByte(settResponse[index++]);
            }

            //Assign FuelPoints to FuelPointChannels
            foreach (FuelPointChannel fuelPointChannel in FuelPointChannels)
            {
                List<FuelPoint> fuelPointsList = new List<FuelPoint>();

                foreach (FuelPoint fuelPoint in FuelPoints)
                    if (fuelPointChannel.ID == fuelPoint.ChannelID)
                        fuelPointsList.Add(fuelPoint);

                fuelPointChannel.FuelPoints = fuelPointsList.ToArray();
            }
        }

        /// <summary>
        /// Gets an array of objects AtgChannel of PTS controller.
        /// </summary>
        /// <remarks>At closed connection returns a value (Nothing в Visual Basic).</remarks>
        public AtgChannel[] AtgChannels
        {
            get
            {
                return atgChannels;
            }
        }

        /// <summary>
        /// Gets an array of objects ATG of PTS controller.
        /// </summary>
        /// <remarks>At closed connection returns a value (Nothing в Visual Basic).</remarks>
        public ATG[] ATGs
        {
            get
            {
                return atgs;
            }
        }

        /// <summary>
        /// Provides initialization of ATG.
        /// </summary>
        /// <param name="settResponse">Response on configuration of ATG channels, received from a PTS controller.</param>
        internal void AtgInit(byte[] settResponse)
        {
            int index = 4;

            //Read ATG channel settings
            for (int i = 0; i < PtsConfiguration.AtgChannelQuantity; i++)
            {
                this.AtgChannels[i].ID = AsciiConversion.AsciiToByte(settResponse[index++]);
                this.AtgChannels[i].Protocol = (AtgChannelProtocol)(AsciiConversion.AsciiToByte(settResponse[index++]) * 10 + AsciiConversion.AsciiToByte(settResponse[index++]));
                this.AtgChannels[i].BaudRate = (ChannelBaudRate)AsciiConversion.AsciiToByte(settResponse[index++]);
            }

            //Read ATG settings
            for (int i = 0; i < PtsConfiguration.AtgQuantity; i++)
            {
                this.ATGs[i].ID = AsciiConversion.AsciiToInt(settResponse[index], settResponse[index + 1]);
                index += 2;
                this.ATGs[i].PhysicalAddress = AsciiConversion.AsciiToInt(settResponse[index], settResponse[index + 1]);
                index += 2;
                this.ATGs[i].ChannelID = AsciiConversion.AsciiToByte(settResponse[index++]);
            }

            //Assign Atgs to AtgChannels
            foreach (AtgChannel atgChannel in AtgChannels)
            {
                List<ATG> atgsList = new List<ATG>();

                foreach (ATG atg in ATGs)
                    if (atgChannel.ID == atg.ChannelID)
                        atgsList.Add(atg);

                atgChannel.ATGs = atgsList.ToArray();
            }
        }
    }
}

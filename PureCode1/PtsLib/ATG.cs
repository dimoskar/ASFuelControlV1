using System;
using System.Collections.Generic;
using System.Text;

namespace PTSLib.PTS
{
    /// <summary>
    /// Provides data on measurement of an ATG (automatic tank gauge) system.
    /// </summary>
    public class ATG
    {
        private AtgChannel channel;
        private PTSController pts;
        private int channelId, physicalAddress;
        private bool isActive;

        public DateTime LastMeasurmentTime { set; get; }

        /// <summary>
        /// Creates exemplar of ATG class.
        /// </summary>
        /// <param name="pts">Exemplar of parent PTS class</param>
        internal ATG(PTSController pts)
        {
            this.pts = pts;
            this.isActive = false;
            
            this.ProductHeight = 0;
            this.ProductVolume = 0;
            this.WaterHeight = 0;
            this.WaterVolume = 0;
            this.Temperature = 0;
            this.ProductUllage = 0;
            this.ProductTCVolume = 0;
            this.ProductDensity = 0;
            this.ProductMass = 0;
            this.MaxTankHeight = 0;

            this.ProductHeightPresent = false;
            this.ProductVolumePresent = false;
            this.WaterHeightPresent = false;
            this.WaterVolumePresent = false;
            this.TemperaturePresent = false;
            this.ProductUllagePresent = false;
            this.ProductTCVolumePresent = false;
            this.ProductDensityPresent = false;
            this.ProductMassPresent = false;
        }

        /// <summary>
        /// Gets unique identifier of an ATG.
        /// </summary>
        public int ID
        { get; set; }

        /// <summary>
        /// Gets or sets physical address of an ATG.
        /// </summary>
        public int PhysicalAddress
        {
            get
            {
                return physicalAddress;
            }
            set
            {
                if (value < 0 || value > PtsConfiguration.AtgAddressQuantity) throw new ArgumentOutOfRangeException();

                physicalAddress = value;
            }
        }

        /// <summary>
        /// Gets or sets an identifier of a channel, to which an ATG is connected.
        /// </summary>
        /// <remarks>
        /// If an ATG is not connected to a channel then a value should be equal to zero.
        /// </remarks>
        public int ChannelID
        {
            get
            {
                return channelId;
            }
            set
            {
                if (value < 0 || value > PtsConfiguration.AtgChannelQuantity) throw new ArgumentOutOfRangeException();

                channelId = value;
                
                if (value > 0)
                    foreach (AtgChannel atgChannel in pts.AtgChannels)
                        if (atgChannel.ID == value)
                            channel = atgChannel;
            }
        }

        /// <summary>
        /// Gets an object AtgChannel, to which an ATG is connected.
        /// </summary>
        /// <remarks>
        /// If an ATG is not connected to a channel - returns a value null (Nothing in Visual Basic).
        /// </remarks>     
        public AtgChannel Channel
        {
            get
            {
                return channel;
            }
            internal set
            {
                channel = value;
                if (channel != null) channelId = channel.ID;
                else channel = null;
            }
        }

        /// <summary>
        /// Requests ATG measurement resuls.
        /// </summary>
        public void GetMeasurementData()
        {
            pts.RequestAtgMeasurementData(ID);
        }

        /// <summary>
        /// Gets or sets a value, which points if an ATG is active and it is necessary to query its state.
        /// </summary>
        public bool IsActive
        {
            get
            {
                return isActive;
            }
            set
            {
                isActive = value;
            }
        }

        /// <summary>
        /// Gets a value of product level (in 0.1 millimeters) in tank.   
        /// </summary>     
        /// <remarks>
        /// If ATG system does not return value of products level - returns null (Nothing in Visual Basic).
        /// </remarks>
        public double ProductHeight
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets a boolean value indicating whether an ATG provides measurement of product level.
        /// </summary>
        public bool ProductHeightPresent
        {
            get; 
            internal set;
        }
        
        /// <summary>
        /// Gets a value of product volume (in liters) in tank.  
        /// </summary>      
        /// <remarks>
        /// If ATG system does not return value of product volume - returns null (Nothing in Visual Basic).
        /// </remarks>
        public int? ProductVolume
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets a boolean value indicating whether an ATG provides measurement of product volume.
        /// </summary>
        public bool ProductVolumePresent
        {
            get;
            internal set;
        }
        
        /// <summary>
        /// Gets a value of water level (in 0.1 millimeters) in tank.
        /// </summary>
        /// <remarks>
        /// If ATG system does not return value of water level - returns null (Nothing in Visual Basic).
        /// </remarks>
        public double WaterHeight
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets a boolean value indicating whether an ATG provides measurement of water level.
        /// </summary>
        public bool WaterHeightPresent
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets a value of water volume (in liters) in tank.
        /// </summary>
        /// <remarks>
        /// If ATG system does not return value of water volume - returns null (Nothing in Visual Basic).
        /// </remarks>
        public int? WaterVolume
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets a boolean value indicating whether an ATG provides measurement of water volume.
        /// </summary>
        public bool WaterVolumePresent
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets a value of product temperature (in 0.1 degrees Celcium) in tank.
        /// </summary>
        /// <remarks>
        /// If ATG system does not return value of product temperature - returns null (Nothing in Visual Basic).
        /// </remarks>
        public double Temperature
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets a boolean value indicating whether an ATG provides measurement of product temperature.
        /// </summary>
        public bool TemperaturePresent
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets a value of product ullage volume (in liters) in tank. 
        /// </summary>
        /// <remarks>
        /// If ATG system does not return value of product ullage volume - returns null (Nothing in Visual Basic).
        /// </remarks>
        public int? ProductUllage
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets a boolean value indicating whether an ATG provides measurement of product ullage volume.
        /// </summary>
        public bool ProductUllagePresent
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets a value of product temperature compensated volume (in liters) in tank. 
        /// </summary>
        /// <remarks>
        /// If ATG system does not return value of product temperature compensated volume - returns null (Nothing in Visual Basic).
        /// </remarks>
        public int? ProductTCVolume
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets a boolean value indicating whether an ATG provides measurement of product temperature compensated volume.
        /// </summary>
        public bool ProductTCVolumePresent
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets a value of product density (in 0.1 kilograms devided on cubic meters) in tank. 
        /// </summary>
        /// <remarks>
        /// If ATG system does not return value of product density - returns null (Nothing in Visual Basic).
        /// </remarks>
        public double ProductDensity
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets a boolean value indicating whether an ATG provides measurement of product density.
        /// </summary>
        public bool ProductDensityPresent
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets a value of product mass (in 0.1 kilograms) in tank. 
        /// </summary>
        /// <remarks>
        /// If ATG system does not return value of product mass - returns null (Nothing in Visual Basic).
        /// </remarks>
        public double ProductMass
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets a boolean value indicating whether an ATG provides measurement of product mass.
        /// </summary>
        public bool ProductMassPresent
        {
            get;
            internal set;
        }

        /// <summary>
        /// Maximum tank height (should be equal to ATG probe height).
        /// </summary>
        public int MaxTankHeight
        {
            get;
            set;
        }

        internal protected void OnDataUpdated()
        {
            if (DataUpdated != null) DataUpdated(this, EventArgs.Empty);
        }

        public override string ToString()
        {
            return string.Format("ATG: ID={0}, Address={1}", ID, PhysicalAddress);
        }

        /// <summary>
        /// Event occures at update of measurements data from ATG.
        /// </summary>
        public event EventHandler DataUpdated;
    }
}

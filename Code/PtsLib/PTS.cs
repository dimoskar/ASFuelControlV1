using System;
using System.Collections;
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
    /// Provides instruments for access and control over PTS controller.
    /// </summary>
    public class PTSController : Component
    {
        private PtsConfiguration configuration;
        private PtsParameter parameter;
        private ReleaseInfo releaseInfo;
        private SerialPort port;
        private Thread refreshThread;
        private bool initializing;
        private AuthorizeType selectedAuthorizationType;
        private bool authorizationPolling;
        private bool useExtendedCommands;
        private bool requestTotalizers;
        private int uniPumpTimeout;
        private byte responseCode;
        private byte responseExtCode;
        private int totalsUpdateTimeout;
        private int maxPacketSize = 256;
        private int maxResponseTimeout = 2000;
        private bool automaticAuthorize;
        private int manualModeAuthorizeValue;
        private DateTime lastResponse;
        private bool runThread = false;
        
        /// <summary>
        /// Initializes a new exemplar of PTS class.
        /// </summary>
        public PTSController()
        {
            port = new SerialPort();
            port.BaudRate = 57600;
            port.ReadTimeout = 2000; 
            uniPumpTimeout = 50;
            configuration = null;
            initializing = false;
            parameter = new PtsParameter();
            totalsUpdateTimeout = 5000;
            requestTotalizers = true; 
            automaticAuthorize = false;
            manualModeAuthorizeValue = 99999900;
        }

        /// <summary>
        /// Opens a connection with a PTS controller.
        /// </summary>
        /// <exception cref="T:System.InvalidOperationException">
        /// Specified COM-port is already opened.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        /// COM-port name does not start with "COM".
        /// </exception>
        /// <exception cref="T:System.UnauthorizedAccessException">
        /// Access to specified COM-port is denied.
        /// </exception>
        public void Open()
        {
            OnOpening();
            port.Open();

            try
            {
                initializing = true;
                OnInitializing();
                InitializeConfig();
                RequestVersion();
                initializing = false;
                OnInitialized();
            }
            catch (TimeoutException)
            {
                port.Close();
                OnTimeoutExpired();
                throw;
            }
            runThread = true;
            startRefreshThread();
            OnOpened();
        }

        /// <summary>
        /// Closes connection with a PTS controller.
        /// </summary>
        /// <exception cref="T:System.InvalidOperationException">
        /// Specified COM-port is not opened.
        /// </exception>
        public void Close()
        {
            if (!IsOpen) return;
            OnClosing();
            Monitor.Enter(port);
            port.Close();
            

            foreach (FuelPoint fp in configuration.FuelPoints)
            {
                fp.DispenserStatus = FuelPointStatus.OFFLINE;
            }
            runThread = false;
            //stopRefreshThread();

            releaseInfo = null;
            configuration = null;
            Monitor.Exit(port);
            OnClosed();
        }
        
        /// <summary>
        /// Gets a value indicating a status of PTS controller connection - opened or closed.
        /// </summary>
        public bool IsOpen
        {
            get
            {
                //if (DateTime.Now.Subtract(this.lastResponse).TotalSeconds > 10)
                //    return false;
                return port.IsOpen;
            }
        }

        /// <summary>
        /// Gets or sets a COM-port name, to which PTS controller is connected.
        /// </summary>
        /// <remarks>A list of valid COM-port names can be received using a static method System.IO.Ports.SerialPort.GetPortNames().</remarks>
        /// <exception cref="System.ArgumentException">Invalid COM-port name.</exception>
        /// <exception cref="System.ArgumentNullException">Property PortName has a null value (Nothing in Visual Basic).</exception>
        /// <exception cref="System.InvalidOperationException">Specified COM-port is already opened.</exception>
        public string PortName
        {
            get
            {
                return port.PortName;
            }
            set
            {
                port.PortName = value;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (port.IsOpen)
            {
                refreshThread.Abort();
                port.Close();
            }
        }

        /// <summary>
        /// Gets response code of PTS controller.
        /// </summary>
        public byte ResponseCode
        {
            get
            {
                return responseCode;
            }
        }

        /// <summary>
        /// Gets response code of PTS controller.
        /// </summary>
        public byte ResponseExtCode
        {
            get
            {
                return responseExtCode;
            }
        }

        /// <summary>
        /// Gets or sets duration of expectation of a response from COM-port in milliseconds.
        /// </summary>
        public int ResponseTimeout
        {
            get
            {
                return port.ReadTimeout;
            }
            set
            {
                port.ReadTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets duration of expectation of a response from a PTS controller in milliseconds.
        /// </summary>
        public int UniPumpTimeout
        {
            get
            {
                return uniPumpTimeout;
            }
            set
            {
                uniPumpTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets duration of expectation of a total counters from the PTS controller in milliseconds.
        /// </summary>
        public int TotalsUpdateTimeout
        {
            get
            {
                return totalsUpdateTimeout;
            }
            set
            {
                totalsUpdateTimeout = value;
            }
        }

        /// <summary>
        /// Starts a new thread, which updates statuses of connected FuelPoints.
        /// </summary>
        private void startRefreshThread()
        {
            int loopCounter = 0;
            int activeFuelPointsCount = 0;
            this.lastResponse = DateTime.Now;
            refreshThread = new Thread(_ =>
            {
                while (runThread)
                {
                    activeFuelPointsCount = 0;
                    if (DateTime.Now.Subtract(this.lastResponse).TotalSeconds > 10)
                        this.Close();
                    if (!this.IsOpen)
                    {
                        Thread.Sleep(500);
                        continue;
                    }
                    foreach (FuelPoint fuelPoint in FuelPoints)
                    {
                        if (fuelPoint.IsActive && fuelPoint.ChannelID != 0)
                        {
                            fuelPoint.GetStatus();
                            activeFuelPointsCount++;
                            Thread.Sleep(UniPumpTimeout);
                            for (int i = 0; i < fuelPoint.Nozzles.Length; i++)
                            {
                                if (fuelPoint.Nozzles[i].NozzleAlive && !fuelPoint.Nozzles[i].NozzleInitialized)
                                {
                                    fuelPoint.GetTotals(fuelPoint.Nozzles[i].ID);
                                }
                            }
                        }
                    }

                    //Delay between querying FuelPoints.
                    Thread.Sleep(UniPumpTimeout);

                    if (--loopCounter <= 0)
                    {
                        foreach (ATG atg in ATGs)
                            if (atg.IsActive && atg.ChannelID != 0)
                                atg.GetMeasurementData();

                        if (activeFuelPointsCount == 0)
                        {
                            //If only ATG systems are queried - delay 1.5 seconds
                            Thread.Sleep(UniPumpTimeout * 30);
                            loopCounter = 0;
                        }
                        else
                        {
                            //Query ATG with intervals
                            loopCounter = 10;
                            Thread.Sleep(UniPumpTimeout);
                        }
                    }
                }
            });
            refreshThread.Start();
        }

        /// <summary>
        /// Stops operation of a thread, which updates statuses of connected FuelPoints.
        /// </summary>
        private void stopRefreshThread()
        {
            try
            {
                if (refreshThread == null) return;

                refreshThread.Abort();
                refreshThread.Join();
                refreshThread = null;
            }
            catch(Exception ex)
            {

            }
        }

        /// <summary>
        /// Gets configuration of PTS controller.
        /// </summary>
        public PtsConfiguration Configuration
        {
            get
            {
                return configuration;
            }
        }

        /// <summary>
        /// Gets access to an object PtsParameter, which provides information about a parameter of PTS controller.
        /// </summary>
        public PtsParameter Parameter
        {
            get
            {
                return parameter;
            }
        }

        /// <summary>
        /// Gets an array of objects FuelPointChannel for a PTS controller.
        /// </summary>
        /// <remarks>At closed connection returns value null (Nothing in Visual Basic).</remarks>
        public FuelPointChannel[] FuelPointChannels
        {
            get
            {
                if (configuration == null) return null;

                return configuration.FuelPointChannels;
            }
        }

        /// <summary>
        /// Gets an array of objects FuelPoint for a PTS controller.
        /// </summary>
        /// <remarks>At closed connection returns value null (Nothing in Visual Basic).</remarks>
        public FuelPoint[] FuelPoints
        {
            get
            {
                if (configuration == null) return null;

                return configuration.FuelPoints;
            }
        }

        /// <summary>
        /// Returns FuelPoint by its ID value.
        /// </summary>
        /// <param name="fuelPointId">Identifier of a FuelPoint.</param>
        /// <remarks>
        /// Value of parameter <paramref name="fuelPointId"/> should be in ranges from 1 to PtsConfiguration.FuelPointCount.
        /// At closed connection returns value null (Nothing in Visual Basic).
        /// </remarks>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// Value of parameter <paramref name="fuelPointId"/> has invalid value.
        /// </exception>
        /// <returns>Object FuelPoint of a requested FuelPoint.</returns>
        public FuelPoint GetFuelPointByID(int fuelPointId)
        {
            if (fuelPointId < 1 || fuelPointId > PtsConfiguration.FuelPointQuantity)
                throw new ArgumentOutOfRangeException("fuelPointIdfuelPointId");
            if (configuration == null)
                return null;

            foreach (FuelPoint fp in configuration.FuelPoints)
                if (fp.ID == fuelPointId)
                    return fp;

            return null;
        }

        /// <summary>
        /// Gets an array of objects AtgChannel for a PTS controller.
        /// </summary>
        /// <remarks>At closed connection returns value null (Nothing in Visual Basic).</remarks>
        public AtgChannel[] AtgChannels
        {
            get
            {
                if (configuration == null) return null;

                return configuration.AtgChannels;
            }
        }

        /// <summary>
        /// Gets an array of objects ATG for a PTS controller.
        /// </summary>
        /// <remarks>At closed connection returns value null (Nothing in Visual Basic).</remarks>
        public ATG[] ATGs
        {
            get
            {
                if (configuration == null) return null;

                return configuration.ATGs;
            }
        }

        /// <summary>
        /// Returns ATG by its ID value.
        /// </summary>
        /// <param name="atgId">Identifier of an ATG.</param>
        /// <remarks>
        /// Value of parameter <paramref name="atgId"/> should be in ranges from 1 to PtsConfiguration.AtgCount.
        /// At closed connection returns value null (Nothing in Visual Basic).
        /// </remarks>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Value of parameter <paramref name="atgId"/> has invalid value.
        /// </exception>
        /// <returns>Object ATG of a requested ATG.</returns>
        public ATG GetAtgByID(int atgId)
        {
            if (atgId < 1 || atgId > PtsConfiguration.AtgQuantity) throw new ArgumentOutOfRangeException("atgId");
            if (configuration == null) return null;

            foreach (ATG atg in configuration.ATGs)
                if (atg.ID == atgId)
                    return atg;

            return null;
        }

        /// <summary>
        /// Gets an object ReleaseInfo, which provides information about a firmware version of PTS controller.
        /// </summary>
        /// <remarks>At closed connection returns value null (Nothing in Visual Basic).</remarks>
        public ReleaseInfo ReleaseInfo
        {
            get
            {
                return releaseInfo;
            }
        }

        /// <summary>
        /// Initializes FuelPoint and ATG configuration of PTS controller.
        /// </summary>
        internal void InitializeConfig()
        {
            configuration = new PtsConfiguration(this);
            byte[] settResponse;

            settResponse = RequestFuelPointConfigurationGet();
            configuration.FuelPointInit(settResponse);

            settResponse = RequestAtgConfigurationGet();
            configuration.AtgInit(settResponse);
        }

        /// <summary>
        /// Gets status of a FuelPoint.
        /// </summary>
        /// <param name="deviceId">Identifier of a FuelPoint.</param>
        /// <remarks>
        /// Value of parameter <paramref name="deviceId"/> should be in range from 1 to PtsConfiguration.FuelPointQuantity.
        /// </remarks>
        internal void RequestStatus(int deviceId)
        {
            if (deviceId < 1 || deviceId > PtsConfiguration.FuelPointQuantity)
                throw new Exception("Error: value of parameter 'deviceId' should be in range from 1 to " + PtsConfiguration.FuelPointQuantity.ToString());

            byte[] message = UnipumpUtils.CreateStatusRequestMessage(deviceId).ToArray();
            sendMessage(deviceId, message);
        }

        /// <summary>
        /// Gets extended status of a FuelPoint.
        /// </summary>
        /// <param name="deviceId">Identifier of a FuelPoint.</param>
        /// <remarks>
        /// Value of parameter <paramref name="deviceId"/> should be in range from 1 to PtsConfiguration.FuelPointQuantity.
        /// </remarks>
        public void RequestExtendedStatus(int deviceId)
        {
            if (deviceId < 1 || deviceId > PtsConfiguration.FuelPointQuantity)
                throw new Exception("Error: value of parameter 'deviceId' should be in range from 1 to " + PtsConfiguration.FuelPointQuantity.ToString());

            byte[] message = UnipumpUtils.CreateExtendedStatusRequestMessage(deviceId).ToArray();
            sendMessage(deviceId, message);
        }

        /// <summary>
        /// Sets lock on control over a FuelPoint in a multi POS system (several POS systems each with a PTS controller).
        /// </summary>
        /// <param name="deviceId">Identifier of a FuelPoint.</param>
        /// <remarks>
        /// Value of parameter <paramref name="deviceId"/> should be in ranges from 1 to PtsConfiguration.FuelPointQuantity.
        /// </remarks>
        internal void RequestLock(int deviceId)
        {
            if (deviceId < 1 || deviceId > PtsConfiguration.FuelPointQuantity)
                throw new Exception("Error: value of parameter 'deviceId' should be in range from 1 to " + PtsConfiguration.FuelPointQuantity.ToString());

            byte[] message = UnipumpUtils.CreateLockRequestMessage(deviceId).ToArray();
            sendMessage(deviceId, message);
        }

        /// <summary>
        /// Sets unlock on control over a FuelPoint in a multi POS system (several POS systems each with a PTS controller).
        /// </summary>
        /// <param name="deviceId">Identifier of a FuelPoint.</param>
        /// <remarks>
        /// Value of parameter <paramref name="deviceId"/> should be in ranges from 1 to PtsConfiguration.FuelPointQuantity.
        /// </remarks>
        internal void RequestUnlock(int deviceId)
        {
            if (deviceId < 1 || deviceId > PtsConfiguration.FuelPointQuantity)
                throw new Exception("Error: value of parameter 'deviceId' should be in range from 1 to " + PtsConfiguration.FuelPointQuantity.ToString());

            byte[] message = UnipumpUtils.CreateUnlockRequestMessage(deviceId).ToArray();
            sendMessage(deviceId, message);
        }

        /// <summary>
        /// Sets authorization of a FuelPoint.
        /// </summary>
        /// <param name="deviceId">Identifier of a FuelPoint.</param>
        /// <param name="nozzleId">Identifier of a nozzle.</param>
        /// <param name="autorizeType">Type of authorization (by volume or by money amount).</param>
        /// <param name="orderAmount">Amount of authorization (volume or money amount).</param>
        /// <param name="pricePerLiter">Fuel price per liter.</param>
        /// <remarks>
        /// Value of parameter <paramref name="deviceId"/> should be in ranges from 1 to PtsConfiguration.FuelPointQuantity.
        /// Value of parameter <paramref name="nozzleId"/> should be in ranges from 1 to PtsConfiguration.NozzleQuantity.
        /// </remarks>
        internal void RequestAuthorize(int deviceId, byte nozzleId, AuthorizeType autorizeType, int orderAmount, int pricePerLiter)
        {
            if (deviceId < 1 || deviceId > PtsConfiguration.FuelPointQuantity)
                throw new Exception("Error: value of parameter 'deviceId' should be in range from 1 to " + PtsConfiguration.FuelPointQuantity.ToString());
            if (nozzleId < 1 || nozzleId > PtsConfiguration.NozzleQuantity)
                throw new Exception("Error: value of parameter 'nozzleId' should be in range from 1 to " + PtsConfiguration.NozzleQuantity.ToString());
            if (orderAmount < 0)
                throw new Exception("Error: value of parameter 'orderAmount' should not be negative");
            if (pricePerLiter < 0)
                throw new Exception("Error: value of parameter 'pricePerLiter' should be positive");

            byte[] message = UnipumpUtils.CreateAuthorizeRequestMessage(deviceId, nozzleId, autorizeType, orderAmount, pricePerLiter).ToArray();
            sendMessage(deviceId, message);
        }

        /// <summary>
        /// Sets extended authorization of a FuelPoint.
        /// </summary>
        /// <param name="deviceId">Identifier of a FuelPoint.</param>
        /// <param name="nozzleId">Identifier of a nozzle.</param>
        /// <param name="autorizeType">Type of authorization (by volume or by money amount).</param>
        /// <param name="orderAmount">Amount of authorization (volume or money amount).</param>
        /// <param name="pricePerLiter">Fuel price per liter.</param>
        /// <remarks>
        /// Value of parameter <paramref name="deviceId"/> should be in ranges from 1 to PtsConfiguration.FuelPointQuantity.
        /// Value of parameter <paramref name="nozzleId"/> should be in ranges from 1 to PtsConfiguration.NozzleQuantity.
        /// </remarks>
        public void RequestExtendedAuthorize(int deviceId, byte nozzleId, AuthorizeType autorizeType, int orderAmount, int pricePerLiter)
        {
            if (deviceId < 1 || deviceId > PtsConfiguration.FuelPointQuantity)
                throw new Exception("Error: value of parameter 'deviceId' should be in range from 1 to " + PtsConfiguration.FuelPointQuantity.ToString());
            if (nozzleId < 1 || nozzleId > PtsConfiguration.NozzleQuantity)
                throw new Exception("Error: value of parameter 'nozzleId' should be in range from 1 to " + PtsConfiguration.NozzleQuantity.ToString());
            if (orderAmount < 0)
                throw new Exception("Error: value of parameter 'orderAmount' should not be negative");
            if (pricePerLiter < 0)
                throw new Exception("Error: value of parameter 'pricePerLiter' should be positive");

            byte[] message = UnipumpUtils.CreateExtendedAuthorizeRequestMessage(deviceId, nozzleId, autorizeType, orderAmount, pricePerLiter).ToArray();
            sendMessage(deviceId, message);
        }

        /// <summary>
        /// Stops fuel dispensing through a specified FuelPoint.
        /// </summary>
        /// <param name="deviceId">Identifier of a FuelPoint.</param>
        /// <remarks>
        /// Value of parameter <paramref name="deviceId"/> should be in ranges from 1 to PtsConfiguration.FuelPointQuantity.
        /// </remarks>
        internal void RequestHalt(int deviceId)
        {
            if (deviceId < 1 || deviceId > PtsConfiguration.FuelPointQuantity)
                throw new Exception("Error: value of parameter 'deviceId' should be in range from 1 to " + PtsConfiguration.FuelPointQuantity.ToString());

            byte[] message = UnipumpUtils.CreateHaltRequestMessage(deviceId).ToArray();
            sendMessage(deviceId, message);
        }

        /// <summary>
        /// Closes a transaction of PTS controller.
        /// </summary>
        /// <param name="deviceId">Identifier of a FuelPoint.</param>
        /// <param name="transactionId">Identifier of a transaction.</param>
        /// <remarks>
        /// Value of parameter <paramref name="deviceId"/> should be in ranges from 1 to PtsConfiguration.FuelPointQuantity.
        /// Value of parameter <paramref name="transactionId"/> should be in ranges from 0 to 99.
        /// </remarks>
        internal void RequestCloseTransaction(int deviceId, int transactionId)
        {
            if (deviceId < 1 || deviceId > PtsConfiguration.FuelPointQuantity)
                throw new Exception("Error: value of parameter 'deviceId' should be in range from 1 to " + PtsConfiguration.FuelPointQuantity.ToString());
            if (transactionId < 0 || transactionId > 99)
                throw new Exception("Error: value of parameter 'transactionId' should be in range from 0 to 99");

            byte[] message = UnipumpUtils.CreateCloseTransactionRequestMessage(deviceId, transactionId).ToArray();
            sendMessage(deviceId, message);
        }

        /// <summary>
        /// Gets a value of electronic totalizer of a nozzle of a FuelPoint.
        /// </summary>
        /// <param name="deviceId">Identifier of a FuelPoint.</param>
        /// <param name="nozzleId">Identifier of a nozzle.</param>
        /// <remarks>
        /// Value of parameter <paramref name="deviceId"/> should be in ranges from 1 to PtsConfiguration.FuelPointQuantity.
        /// Value of parameter <paramref name="nozzleId"/> should be in ranges from 1 to PtsConfiguration.NozzleQuantity.
        /// </remarks>
        public void RequestTotals(int deviceId, byte nozzleId)
        {
            if (deviceId < 1 || deviceId > PtsConfiguration.FuelPointQuantity)
                throw new Exception("Error: value of parameter 'deviceId' should be in range from 1 to " + PtsConfiguration.FuelPointQuantity.ToString());

            if (nozzleId < 1 || nozzleId > PtsConfiguration.NozzleQuantity)
                throw new Exception("Error: value of parameter 'nozzleId' should be in range from 1 to " + PtsConfiguration.NozzleQuantity.ToString());

            byte[] message = UnipumpUtils.CreateTotalRequestMessage(deviceId, nozzleId).ToArray();
            sendMessage(deviceId, message);
        }

        /// <summary>
        /// Gets a value of electronic totalizer of a nozzle of a FuelPoint.
        /// </summary>
        /// <param name="deviceId">Identifier of a FuelPoint.</param>
        /// <param name="nozzleId">Identifier of a nozzle.</param>
        /// <remarks>
        /// Value of parameter <paramref name="deviceId"/> should be in ranges from 1 to PtsConfiguration.FuelPointQuantity.
        /// Value of parameter <paramref name="nozzleId"/> should be in ranges from 1 to PtsConfiguration.NozzleQuantity.
        /// </remarks>
        public void RequestExtendedTotals(int deviceId, byte nozzleId)
        {
            if (deviceId < 1 || deviceId > PtsConfiguration.FuelPointQuantity)
                throw new Exception("Error: value of parameter 'deviceId' should be in range from 1 to " + PtsConfiguration.FuelPointQuantity.ToString());

            if (nozzleId < 1 || nozzleId > PtsConfiguration.NozzleQuantity)
                throw new Exception("Error: value of parameter 'nozzleId' should be in range from 1 to " + PtsConfiguration.NozzleQuantity.ToString());

            byte[] message = UnipumpUtils.CreateExtendedTotalRequestMessage(deviceId, nozzleId).ToArray();
            sendMessage(deviceId, message);
        }

        /// <summary>
        /// Gets fuel prices for nozzles of a FuelPoint.
        /// </summary>
        /// <param name="deviceId">Identifier of a FuelPoint.</param>
        /// <remarks>
        /// Value of parameter <paramref name="deviceId"/> should be in ranges from 1 to PtsConfiguration.FuelPointQuantity.
        /// </remarks>
        internal void RequestPricesGet(int deviceId)
        {
            if (deviceId < 1 || deviceId > PtsConfiguration.FuelPointQuantity)
                throw new Exception("Error: value of parameter 'deviceId' should be in range from 1 to " + PtsConfiguration.FuelPointQuantity.ToString());

            byte[] message = UnipumpUtils.CreatePricesGetRequestMessage(deviceId).ToArray();
            sendMessage(deviceId, message);
        }

        /// <summary>
        /// Sets fuel prices for nozzles of a FuelPoint.
        /// </summary>
        /// <param name="deviceId">Identifier of a FuelPoint.</param>
        /// <param name="prices">
        /// Array of fuel prices for nozzles in cents. If length of the array is shorter than 
        /// required for a total quantity of nozzles in a FuelPoint (prices for not all nozzles 
        /// are specified) then prices will be set only for the first nozzles, if length of the 
        /// array is longer than required for a total quantity of nozzles in a FuelPoint - then only 
        /// first elements of the array will be used as fuel prices for nozzles.  If fuel price for a 
        /// nozzle equals to zero - price is ignored.
        /// </param>
        /// <remarks>
        /// Value of parameter <paramref name="deviceId"/> should be in ranges from 1 to PtsConfiguration.FuelPointQuantity.
        /// Value of parameter <paramref name="nozzleId"/> should be in ranges from 1 to PtsConfiguration.NozzleQuantity.
        /// </remarks>
        internal void RequestPricesSet(int deviceId, int[] prices)
        {
            if (deviceId < 1 || deviceId > PtsConfiguration.FuelPointQuantity)
                throw new Exception("Error: value of parameter 'deviceId' should be in range from 1 to " + PtsConfiguration.FuelPointQuantity.ToString());

            if (prices == null || prices.Length == 0) return;

            foreach (int price in prices)
                if (price < 0 || price > 999999999)
                    throw new Exception("Error: value of each price in parameter 'prices' should be in range from 1 to 999999999");

            byte[] message = UnipumpUtils.CreatePricesSetRequestMessage(deviceId, prices).ToArray();
            sendMessage(deviceId, message);
        }

        /// <summary>
        /// Sets extended fuel prices for nozzles of a FuelPoint.
        /// </summary>
        /// <param name="deviceId">Identifier of a FuelPoint.</param>
        /// <param name="prices">
        /// Array of fuel prices for nozzles in cents. If length of the array is shorter than 
        /// required for a total quantity of nozzles in a FuelPoint (prices for not all nozzles 
        /// are specified) then prices will be set only for the first nozzles, if length of the 
        /// array is longer than required for a total quantity of nozzles in a FuelPoint - then only 
        /// first elements of the array will be used as fuel prices for nozzles.  If fuel price for a 
        /// nozzle equals to zero - price is ignored.
        /// </param>
        /// <remarks>
        /// Value of parameter <paramref name="deviceId"/> should be in ranges from 1 to PtsConfiguration.FuelPointQuantity.
        /// </remarks>
        public void RequestExtendedPricesSet(int deviceId, int[] prices)
        {
            if (deviceId < 1 || deviceId > PtsConfiguration.FuelPointQuantity)
                throw new Exception("Error: value of parameter 'deviceId' should be in range from 1 to " + PtsConfiguration.FuelPointQuantity.ToString());

            if (prices == null || prices.Length == 0) return;

            foreach (int price in prices)
                if (price < 0 || price > 999999999)
                    throw new Exception("Error: value of each price in parameter 'prices' should be in range from 1 to 999999999");

            byte[] message = UnipumpUtils.CreateExtendedPricesSetRequestMessage(deviceId, prices).ToArray();
            sendMessage(deviceId, message);
        }

        /// <summary>
        /// Gets data on measurements by ATG probe.
        /// </summary>
        /// <param name="deviceId">Identifier of an ATG.</param>
        /// <remarks>
        /// Value of parameter <paramref name="deviceId"/> should be in range from 1 to PtsConfiguration.AtgQuantity.
        /// </remarks>
        internal void RequestAtgMeasurementData(int deviceId)
        {
            if (deviceId < 1 || deviceId > PtsConfiguration.FuelPointQuantity)
                throw new Exception("Error: value of parameter 'deviceId' should be in range from 1 to " + PtsConfiguration.AtgQuantity.ToString());

            byte[] message = UnipumpUtils.CreateAtgDataRequestMessage(deviceId).ToArray();
            sendMessage(deviceId, message);
        }

        /// <summary>
        /// Gets firmware version information from PTS controller.
        /// </summary>
        public void RequestVersion()
        {
            byte[] message = UnipumpUtils.CreateVersionRequestMessage().ToArray();
            byte[] buffer;

            int year, month, day;
            List<string> fuelPointProtocols = new List<string>();
            List<string> atgProtocols = new List<string>();

            Monitor.Enter(port);
            port.Write(message, 0, message.Length);
            readResponseMessage(out buffer);
            Monitor.Exit(port);

            releaseInfo = new ReleaseInfo();
            year = 2000 + AsciiConversion.AsciiToInt(buffer[4], buffer[5]);
            month = AsciiConversion.AsciiToInt(buffer[6], buffer[7]);
            day = AsciiConversion.AsciiToInt(buffer[8], buffer[9]);
            releaseInfo.ReleaseDate = new DateTime(year, month, day);
            releaseInfo.ReleaseNum = (short)AsciiConversion.AsciiToInt(buffer[10], buffer[11]);
            releaseInfo.ReleaseVersion = AsciiConversion.AsciiToInt(buffer[4], buffer[5], buffer[6], buffer[7], buffer[8], buffer[9], buffer[10], buffer[11]).ToString();

            for (int i = 12; i < 162; i += 2) //previous value was 72 (30 protocols for fuel dispensers)
            {
                int protocol = AsciiConversion.AsciiToInt(buffer[i], buffer[i + 1]);

                if (protocol == 0) continue;
                if (Enum.IsDefined(typeof(FuelPointChannelProtocol), (FuelPointChannelProtocol)protocol))
                    fuelPointProtocols.Add(((FuelPointChannelProtocol)protocol).ToString());
            }
            releaseInfo.SupportedFuelPointProtocols = fuelPointProtocols.ToArray();

            for (int i = 162; i < 212; i += 2) //previous value was 92 (10 protocols for ATG systems)
            {
                int protocol = AsciiConversion.AsciiToInt(buffer[i], buffer[i + 1]);

                if (protocol == 0) continue;
                if (Enum.IsDefined(typeof(AtgChannelProtocol), (AtgChannelProtocol)protocol))
                    atgProtocols.Add(((AtgChannelProtocol)protocol).ToString());
            }
            releaseInfo.SupportedAtgProtocols = atgProtocols.ToArray();
        }

        /// <summary>
        /// Gets a value of PTS controller parameter.
        /// </summary>
        /// <param name="parameterAddress">Address of the parameter requested</param>
        /// <param name="parameterNumber">Number of the parameter requested</param>
        /// <returns>Value of a parameter.</returns>
        /// <remarks>
        /// Value of parameter <paramref name="parameterAddress"/> should be in ranges from 0 to PtsConfiguration.FuelPointQuantity, at this value 0 corresponds to PTS.
        /// Value of parameter <paramref name="parameterNumber"/> should be in range from 0 to 9999.
        /// </remarks>
        public void RequestParameterGet(short parameterAddress, int parameterNumber)
        {
            byte[] buffer; 
            
            if (parameterAddress < 0 || parameterAddress > PtsConfiguration.FuelPointQuantity)
                throw new Exception("Error: value of parameter 'parameterAddress' should be in range from 0 to " + PtsConfiguration.FuelPointQuantity.ToString());
            if (parameterNumber < 0 || parameterNumber > 9999)
                throw new Exception("Error: value of parameter 'parameterNumber' should be in range from 0 to 9999");
            
            byte[] message = UnipumpUtils.CreateParameterGetRequestMessage(parameterAddress, parameterNumber).ToArray();
            
            Monitor.Enter(port);
            port.Write(message, 0, message.Length);
            readResponseMessage(out buffer);
            Monitor.Exit(port);

            if (!UnipumpUtils.IsValidMessage(buffer))
                throw new Exception("Error: parameter response message has incorrect format");

            if (buffer[3] == UnipumpUtils.uUnlockStatusResponse)
                throw new Exception("Error: pump side is switched off and parameters can not be read or written");

            List<byte> byteList = new List<byte>();

            parameter.ParameterAddress = AsciiConversion.AsciiToByte(buffer[2]);
            parameter.ParameterNumber = AsciiConversion.AsciiToInt(buffer[4], buffer[5], buffer[6], buffer[7]);

            for (int i = 8; i <= 15; i++)
                byteList.Add(buffer[i]);
            parameter.ParameterValue = byteList.ToArray();
        }

        /// <summary>
        /// Sets a value of PTS controller parameter.
        /// </summary>
        /// <param name="parameterAddress">Address of the parameter requested</param>
        /// <param name="parameterNumber">Number of the parameter requested</param>
        /// <param name="parameterValue">Value of the parameter requested</param>
        /// <remarks>
        /// Value of parameter <paramref name="parameterAddress"/> should be in ranges from 0 to PtsConfiguration.FuelPointQuantity, at this value 0 corresponds to PTS. 
        /// Value of parameter <paramref name="parameterNumber"/> should be in ranges from 0 to 9999.
        /// Writing of a parameter to a FuelPoint with identifier 0 will set the parameter for PTS itself.  
        /// Writing of a parameter with a identifier 0 will cause nulling of all parameters for the specified  FuelPoint
        /// or PTS itself in case of writing to broadcasting FuelPoint with identifier 0.
        /// </remarks>
        public void RequestParameterSet(short parameterAddress, int parameterNumber, byte[] parameterValue)
        {
            byte[] buffer;
            
            if (parameterAddress < 0 || parameterAddress > PtsConfiguration.FuelPointQuantity)
                throw new Exception("Error: value of parameter 'deviceId' should be in range from 0 to " + PtsConfiguration.FuelPointQuantity.ToString());
            if (parameterNumber < 0 || parameterNumber > 9999)
                throw new Exception("Error: value of parameter 'parameterId' should be in range from 0 to 9999"); 
            if(parameterValue.Length > 8)
                throw new Exception("Error: value of parameter 'parameterValue' should be in range from 0 to 0xFFFFFFFF");

            byte[] message = UnipumpUtils.CreateParameterSetRequestMessage(parameterAddress, parameterNumber, parameterValue).ToArray();
            sendMessage(parameterAddress, message);
            
            Monitor.Enter(port);
            port.Write(message, 0, message.Length);
            readResponseMessage(out buffer);
            Monitor.Exit(port);

            if (!UnipumpUtils.IsValidMessage(buffer))
                throw new Exception("Error: parameter response message has incorrect format");

            if (buffer[3] == UnipumpUtils.uUnlockStatusResponse)
                throw new Exception("Error: pump side is switched off and parameters can not be read or written");

            List<byte> byteList = new List<byte>();

            parameter.ParameterAddress = AsciiConversion.AsciiToByte(buffer[2]);
            parameter.ParameterNumber = AsciiConversion.AsciiToInt(buffer[4], buffer[5], buffer[6], buffer[7]);

            for (int i = 8; i <= 15; i++)
                byteList.Add(buffer[i]);
            parameter.ParameterValue = byteList.ToArray();
        }

        /// <summary>
        /// Sets configuration of FuelPoint channels to PTS controller.
        /// </summary>
        public byte[] RequestFuelPointConfigurationSet()
        {
            byte[] message = UnipumpUtils.CreatePumpConfigSetRequestMessage(configuration).ToArray();
            byte[] buffer;

            Monitor.Enter(port);
            this.runThread = false;
            //stopRefreshThread();
            port.Write(message, 0, message.Length);
            readResponseMessage(out buffer);
            Monitor.Exit(port);
            startRefreshThread();

            return buffer;
        }

        /// <summary>
        /// Gets configuration of FuelPoint channels from PTS controller.
        /// </summary>
        internal byte[] RequestFuelPointConfigurationGet()
        {
            byte[] message = UnipumpUtils.CreatePumpConfigGetRequestMessage().ToArray();
            byte[] buffer;

            Monitor.Enter(port);
            port.Write(message, 0, message.Length);
            readResponseMessage(out buffer);
            Monitor.Exit(port);

            configuration.FuelPointInit(buffer);

            return buffer;
        }

        /// <summary>
        /// Reads and applies configuration of objects FuelPoint of PTS controller.
        /// </summary>
        public void GetFuelPointConfiguration()
        {
            byte[] settResponse = RequestFuelPointConfigurationGet();
            configuration.FuelPointInit(settResponse);
        }

        /// <summary>
        /// Writes and applies configuration of objects FuelPoint of PTS controller.
        /// </summary>
        public void SetFuelPointConfiguration()
        {
            byte[] settResponse = RequestFuelPointConfigurationSet();
            configuration.FuelPointInit(settResponse);
        }

        /// <summary>
        /// Sets configuration of ATG probe channels to PTS controller.
        /// </summary>
        /// <remarks>
        /// Value of parameter <paramref name="configuration"/> should be in ranges ... .
        /// </remarks>
        public byte[] RequestAtgConfigurationSet()
        {
            byte[] message = UnipumpUtils.CreateAtgConfigSetRequestMessage(configuration).ToArray();
            byte[] buffer;

            Monitor.Enter(port);
            stopRefreshThread();
            port.Write(message, 0, message.Length);
            readResponseMessage(out buffer);
            Monitor.Exit(port);
            startRefreshThread();

            return buffer;
        }

        /// <summary>
        /// Gets configuration of ATG probe channels from PTS controller.
        /// </summary>
        public byte[] RequestAtgConfigurationGet()
        {
            byte[] message = UnipumpUtils.CreateAtgConfigGetRequestMessage().ToArray();
            byte[] buffer;

            Monitor.Enter(port);
            port.Write(message, 0, message.Length);
            readResponseMessage(out buffer);
            Monitor.Exit(port);

            return buffer;
        }

        /// <summary>
        /// Reads and applies configuration of objects ATG of PTS controller.
        /// </summary>
        public void GetAtgConfiguration()
        {
            byte[] settResponse = RequestAtgConfigurationGet();
            configuration.AtgInit(settResponse);
        }

        /// <summary>
        /// Writes and applies configuration of objects ATG of PTS controller.
        /// </summary>
        public void SetAtgConfiguration()
        {
            byte[] settResponse = RequestAtgConfigurationSet();
            configuration.AtgInit(settResponse);
        }

        /// <summary>
        /// Processes a response from a PTS controller.
        /// </summary>
        private void processResponseMessage(int address, byte[] buffer)
        {
            this.lastResponse = DateTime.Now;

            FuelPoint fuelPoint = new FuelPoint(this);
            foreach (FuelPoint fp in FuelPoints)
                if (fp.ID == address)
                    fuelPoint = fp;
            
            responseCode = buffer[3];

            switch (responseCode)
            {
                case UnipumpUtils.uStatusResponse: //StatusResponse
                {
                    fuelPoint.ID = AsciiConversion.AsciiToByte(buffer[2]);
                    fuelPoint.ActiveNozzleID = AsciiConversion.AsciiToByte(buffer[4]);
                    fuelPoint.Locked = true;

                    if (fuelPoint.DispenserStatus == FuelPointStatus.READY && ((FuelPointStatus)AsciiConversion.AsciiToByte(buffer[5]) == FuelPointStatus.IDLE || (FuelPointStatus)AsciiConversion.AsciiToByte(buffer[5]) == FuelPointStatus.NOZZLE))
                        fuelPoint.DispenserStatus = FuelPointStatus.READY;
                    else
                        fuelPoint.DispenserStatus = (FuelPointStatus)AsciiConversion.AsciiToByte(buffer[5]);
                    
                    fuelPoint.CurrentPendingCommand = buffer[6];

                    if (fuelPoint.PreviousPendingCommand != fuelPoint.CurrentPendingCommand)
                    {
                        fuelPoint.OnPendingCommandChanged(new PendingCommandChangedEventArgs(fuelPoint.CurrentPendingCommand, fuelPoint.PreviousPendingCommand));
                        fuelPoint.PreviousPendingCommand = fuelPoint.CurrentPendingCommand;
                    }
                } break;

                case UnipumpUtils.uUnlockStatusResponse: //UnlockStatusResponse
                {
                    fuelPoint.ID = AsciiConversion.AsciiToByte(buffer[2]);
                    fuelPoint.ActiveNozzleID = AsciiConversion.AsciiToByte(buffer[4]);
                    fuelPoint.Locked = false;
                    //if (fuelPoint.Status != FuelPointStatus.READY && fuelPoint.Status != FuelPointStatus.ERROR)
                    fuelPoint.DispenserStatus = (FuelPointStatus)AsciiConversion.AsciiToByte(buffer[5]);
                    fuelPoint.CurrentPendingCommand = buffer[6];

                    if (fuelPoint.PreviousPendingCommand != fuelPoint.CurrentPendingCommand)
                        fuelPoint.PreviousPendingCommand = fuelPoint.CurrentPendingCommand;
                } break;

                case UnipumpUtils.uAmountInfoResponse: //AmountInfoResponse
                {
                    //Console.WriteLine("Amount Info :" + BitConverter.ToString(buffer));
                    
                    fuelPoint.ID = AsciiConversion.AsciiToByte(buffer[2]);
                    fuelPoint.TransactionID = AsciiConversion.AsciiToInt(buffer[4], buffer[5]);
                    fuelPoint.ActiveNozzleID = AsciiConversion.AsciiToByte(buffer[6]);
                    int dispVol = AsciiConversion.AsciiToInt(buffer[7], buffer[8], buffer[9], buffer[10], buffer[11], buffer[12], buffer[13], buffer[14]);
                    fuelPoint.DispensedVolume = dispVol;
                    fuelPoint.DispensedAmount = fuelPoint.DispensedVolume * ((float)fuelPoint.ActiveNozzle.PricePerLiter / 100);
                    fuelPoint.DispenserStatus = FuelPointStatus.WORK;
                } break;

                case UnipumpUtils.uTransactionInfoResponse: //TransactionInfoResponse
                {
                    fuelPoint.ID = AsciiConversion.AsciiToByte(buffer[2]);
                    fuelPoint.TransactionID = AsciiConversion.AsciiToInt(buffer[4], buffer[5]);
                    byte actId =  AsciiConversion.AsciiToByte(buffer[6]);
                    fuelPoint.ActiveNozzleID = actId;
                    fuelPoint.TransactionNozzleID = AsciiConversion.AsciiToByte(buffer[6]);
                    fuelPoint.DispensedVolume = AsciiConversion.AsciiToInt(buffer[7], buffer[8], buffer[9], buffer[10], buffer[11], buffer[12], buffer[13], buffer[14]);
                    fuelPoint.DispensedAmount = AsciiConversion.AsciiToInt(buffer[19], buffer[20], buffer[21], buffer[22], buffer[23], buffer[24], buffer[25], buffer[26]);
                    fuelPoint.Nozzles[fuelPoint.ActiveNozzleID - 1].PricePerLiter = Convert.ToInt16(AsciiConversion.AsciiToInt(buffer[15], buffer[16], buffer[17], buffer[18]));
                    if (fuelPoint.DispenserStatus != FuelPointStatus.TRANSACTIONSTOPPED)     
                        fuelPoint.DispenserStatus = FuelPointStatus.TRANSACTIONCOMPLETED;
                    fuelPoint.OnTransactionFinished(new TransactionEventArgs(fuelPoint.TransactionID, actId, fuelPoint.DispensedAmount, fuelPoint.DispensedVolume, fuelPoint.Nozzles[actId - 1].PricePerLiter));
                } break;

                case UnipumpUtils.uTotalInfoResponse: //TotalInfoResponse
                {
                    fuelPoint.ID = AsciiConversion.AsciiToByte(buffer[2]);
                    byte nozzleId = AsciiConversion.AsciiToByte(buffer[6]);
                    fuelPoint.ActiveNozzleID = AsciiConversion.AsciiToByte(buffer[6]);
                    fuelPoint.TransactionID = AsciiConversion.AsciiToInt(buffer[4], buffer[5]);
                    fuelPoint.Nozzles[nozzleId - 1].TotalDispensedAmount = AsciiConversion.AsciiToInt(buffer[7], buffer[8], buffer[9], buffer[10], buffer[11], buffer[12], buffer[13], buffer[14], buffer[15], buffer[16]);
                    fuelPoint.Nozzles[nozzleId - 1].TotalDispensedVolume = AsciiConversion.AsciiToInt(buffer[17], buffer[18], buffer[19], buffer[20], buffer[21], buffer[22], buffer[23], buffer[24], buffer[25], buffer[26]);

                    Console.WriteLine("FuelPoint : {2}, Nozzle : {0},  Totals {1}", nozzleId, fuelPoint.Nozzles[nozzleId - 1].TotalDispensedVolume, fuelPoint.ID);

                    OnTotalsUpdated(new TotalsEventArgs(fuelPoint.ID, nozzleId, fuelPoint.Nozzles[nozzleId - 1]));
                } break;

                case UnipumpUtils.uPricesResponse: //PricesResponse
                {
                    fuelPoint.ID = AsciiConversion.AsciiToByte(buffer[2]);
                    fuelPoint.Nozzles[0].PricePerLiter = AsciiConversion.AsciiToInt(buffer[4], buffer[5], buffer[6], buffer[7]);
                    fuelPoint.Nozzles[1].PricePerLiter = AsciiConversion.AsciiToInt(buffer[8], buffer[9], buffer[10], buffer[11]);
                    fuelPoint.Nozzles[2].PricePerLiter = AsciiConversion.AsciiToInt(buffer[12], buffer[13], buffer[14], buffer[15]);
                    fuelPoint.Nozzles[3].PricePerLiter = AsciiConversion.AsciiToInt(buffer[16], buffer[17], buffer[18], buffer[19]);
                    fuelPoint.Nozzles[4].PricePerLiter = AsciiConversion.AsciiToInt(buffer[20], buffer[21], buffer[22], buffer[23]);
                    fuelPoint.Nozzles[5].PricePerLiter = AsciiConversion.AsciiToInt(buffer[24], buffer[25], buffer[26], buffer[27]);
                    OnPricesGet(new PricesEventArgs(fuelPoint.ID));
                } break;

                case UnipumpUtils.uAtgMeasureResponse: //AtgMeasurementDataResponse
                {
                    ATG atg = GetAtgByID(AsciiConversion.AsciiToByte(buffer[2]));

                    int value;

                    // Measurements of product level
                    if ((buffer[4] & 0x80) > 0)
                    {
                        atg.ProductHeightPresent = true;
                        value = AsciiConversion.AsciiToInt(buffer[6], buffer[7], buffer[8], buffer[9], buffer[10], buffer[11]);
                        atg.ProductHeight = (double)value / 10; // Normalize to mm
                    }
                    else
                    {
                        atg.ProductHeightPresent = false;
                        atg.ProductHeight = 0;
                    }

                    // Measurements of water level
                    if ((buffer[4] & 0x40) > 0)
                    {
                        atg.WaterHeightPresent = true;

                        value = AsciiConversion.AsciiToInt(buffer[12], buffer[13], buffer[14], buffer[15], buffer[16]);
                        atg.WaterHeight = (double)value / 10; // Normalize to mm
                    }
                    else
                    {
                        atg.WaterHeightPresent = false;
                        atg.WaterHeight = 0;
                    }

                    // Measurements of temperature
                    if ((buffer[4] & 0x20) > 0)
                    {
                        atg.TemperaturePresent = true;

                        value = AsciiConversion.AsciiToInt(buffer[18], buffer[19], buffer[20]);
                        atg.Temperature = (double)value / 10; // Normalize to degree Celcium

                        // Temperature sign
                        if (buffer[17] == 0x2D)
                            atg.Temperature = -atg.Temperature;
                    }
                    else
                    {
                        atg.TemperaturePresent = false;
                        atg.Temperature = 0;
                    }

                    // Measurements of product volume
                    if ((buffer[4] & 0x10) > 0)
                    {
                        atg.ProductVolumePresent = true;

                        value = AsciiConversion.AsciiToInt(buffer[21], buffer[22], buffer[23], buffer[24], buffer[25], buffer[26]);
                        atg.ProductVolume = value / 1; // Normalize to l
                    }
                    else
                    {
                        atg.ProductVolumePresent = false;
                        atg.ProductVolume = 0;
                    }

                    // Measurements of water volume
                    if ((buffer[4] & 0x08) > 0)
                    {
                        atg.WaterVolumePresent = true;

                        value = AsciiConversion.AsciiToInt(buffer[27], buffer[28], buffer[29], buffer[30], buffer[31]);
                        atg.WaterVolume = value / 1; // Normalize to l
                    }
                    else
                    {
                        atg.WaterVolumePresent = false;
                        atg.WaterVolume = 0;
                    }

                    // Measurements of product ullage
                    if ((buffer[4] & 0x04) > 0)
                    {
                        atg.ProductUllagePresent = true;

                        value = AsciiConversion.AsciiToInt(buffer[32], buffer[33], buffer[34], buffer[35], buffer[36], buffer[37]);
                        atg.ProductUllage = value / 1; // Normalize to l
                    }
                    else
                    {
                        atg.ProductUllagePresent = false;
                        atg.ProductUllage = 0;
                    }

                    // Measurements of product temperature compensated volume
                    if ((buffer[4] & 0x02) > 0)
                    {
                        atg.ProductTCVolumePresent = true;

                        value = AsciiConversion.AsciiToInt(buffer[38], buffer[39], buffer[40], buffer[41], buffer[42], buffer[43]);
                        atg.ProductTCVolume = value / 1; // Normalize to l
                    }
                    else
                    {
                        atg.ProductTCVolumePresent = false;
                        atg.ProductTCVolume = 0;
                    }

                    // Measurements of product density
                    if ((buffer[4] & 0x01) > 0)
                    {
                        atg.ProductDensityPresent = true;

                        value = AsciiConversion.AsciiToInt(buffer[44], buffer[45], buffer[46], buffer[47]);
                        atg.ProductDensity = (double)value / 10; // Normalize to kg/m3
                    }
                    else
                    {
                        atg.ProductDensityPresent = false;
                        atg.ProductDensity = 0;
                    }

                    // Measurements of product mass
                    if ((buffer[5] & 0x80) > 0)
                    {
                        atg.ProductMassPresent = true;

                        value = AsciiConversion.AsciiToInt(buffer[48], buffer[49], buffer[50], buffer[51], buffer[52], buffer[53], buffer[54]);
                        atg.ProductMass = (double)value / 10; // Normalize to kg
                    }
                    else
                    {
                        atg.ProductMassPresent = false;
                        atg.ProductMass = 0;
                    }
                    
                    atg.OnDataUpdated();
                } break;

                case UnipumpUtils.uExtended: //Extended responses
                    {
                        responseExtCode = buffer[4];

                        List<byte> dataBytes = new List<byte>();
                        string strBuffer = string.Empty;
                        string[] strData;

                        switch (responseExtCode)
                        {
                            case UnipumpUtils.uTransactionInfoResponse: //ExtendedTransactionInfoResponse
                                {
                                    fuelPoint.ID = AsciiConversion.AsciiToByte(buffer[2]);

                                    for (int i = 5; i < buffer.Length - 4; i++)
                                        dataBytes.Add(buffer[i]);

                                    strData = AsciiConversion.BytesArrayToString(dataBytes.ToArray()).Split(';');

                                    if (strData.Length < 5)
                                        throw new Exception("Incorrect data format in uExtendedTransactionInfoResponse");

                                    for (int i = 0; i < strData.Length; i++)
                                        if (strData[i] == "")
                                            strData[i] = "0";

                                    fuelPoint.TransactionID = Convert.ToInt32(strData[0]);
                                    fuelPoint.ActiveNozzleID = Convert.ToByte(strData[1]);
                                    fuelPoint.TransactionNozzleID = Convert.ToByte(strData[1]);
                                    fuelPoint.DispensedVolume = Convert.ToInt32(strData[2]);
                                    fuelPoint.Nozzles[fuelPoint.ActiveNozzleID - 1].PricePerLiter = Convert.ToInt32(strData[3]);
                                    fuelPoint.DispensedAmount = Convert.ToInt32(strData[4]);

                                    if (fuelPoint.DispenserStatus != FuelPointStatus.TRANSACTIONSTOPPED) 
                                        fuelPoint.DispenserStatus = FuelPointStatus.TRANSACTIONCOMPLETED;
                                    fuelPoint.OnTransactionFinished(new TransactionEventArgs(fuelPoint.TransactionID, fuelPoint.ActiveNozzleID, fuelPoint.DispensedAmount, fuelPoint.DispensedVolume, fuelPoint.Nozzles[fuelPoint.ActiveNozzleID - 1].PricePerLiter));
                                } break;

                            case UnipumpUtils.uTotalInfoResponse: //TotalInfoResponse
                                {
                                    fuelPoint.ID = AsciiConversion.AsciiToByte(buffer[2]);

                                    for (int i = 5; i < buffer.Length - 4; i++)
                                        dataBytes.Add(buffer[i]);

                                    strData = AsciiConversion.BytesArrayToString(dataBytes.ToArray()).Split(';');

                                    if (strData.Length < 4)
                                        throw new Exception("Incorrect data format in uExtendedTotalInfoResponse");

                                    for (int i = 0; i < strData.Length; i++)
                                        if (strData[i] == "")
                                            strData[i] = "0";

                                    byte nozzleId = Convert.ToByte(strData[1]);
                                    fuelPoint.ActiveNozzleID = Convert.ToByte(strData[1]);
                                    fuelPoint.TransactionID = Convert.ToInt32(strData[0]);
                                    fuelPoint.Nozzles[nozzleId - 1].TotalDispensedAmount = Convert.ToInt64(strData[2]);
                                    fuelPoint.Nozzles[nozzleId - 1].TotalDispensedVolume = Convert.ToInt64(strData[3]);
                                    OnTotalsUpdated(new TotalsEventArgs(fuelPoint.ID, nozzleId, fuelPoint.Nozzles[nozzleId - 1]));
                                } break;

                            case UnipumpUtils.uPricesResponse: //ExtendedPricesResponse
                                {
                                    fuelPoint.ID = AsciiConversion.AsciiToByte(buffer[2]);

                                    for (int i = 5; i < buffer.Length - 4; i++)
                                        dataBytes.Add(buffer[i]);

                                    strData = AsciiConversion.BytesArrayToString(dataBytes.ToArray()).Split(';');

                                    if (strData.Length < 6)
                                        throw new Exception("Incorrect data format in uExtendedPricesResponse");

                                    for (int i = 0; i < strData.Length; i++)
                                        if (strData[i] == "")
                                            strData[i] = "0";

                                    fuelPoint.Nozzles[0].PricePerLiter = Convert.ToInt32(strData[0]);
                                    fuelPoint.Nozzles[1].PricePerLiter = Convert.ToInt32(strData[1]);
                                    fuelPoint.Nozzles[2].PricePerLiter = Convert.ToInt32(strData[2]);
                                    fuelPoint.Nozzles[3].PricePerLiter = Convert.ToInt32(strData[3]);
                                    fuelPoint.Nozzles[4].PricePerLiter = Convert.ToInt32(strData[4]);
                                    fuelPoint.Nozzles[5].PricePerLiter = Convert.ToInt32(strData[5]);
                                    
                                    OnPricesGet(new PricesEventArgs(fuelPoint.ID));
                                } break;
                        }
                    } break;
            }
        }

        /// <summary>
        /// Sends a request to a PTS controller and checks validity of a response.
        /// </summary>
        private void sendMessage(int address, byte[] message)
        {
            try
            {
                byte[] buffer = null;

                Monitor.Enter(port);
                port.Write(message, 0, message.Length);
                readResponseMessage(out buffer);
                Monitor.Exit(port);

                if (buffer == null) return;

                //Check CRC of the received packet
                if (!UnipumpUtils.IsValidMessage(buffer))
                {
                    OnMessageError(new MessageErrorEventArgs(buffer));
                    return;
                }

                try
                {
                    processResponseMessage(address, buffer);
                }
                catch (ArgumentOutOfRangeException)
                {
                    OnMessageError(new MessageErrorEventArgs(buffer));
                }
            }
            catch
            {

            }
        }

        /// <summary>
        /// Receives a response from a PTS controller.
        /// </summary>
        private void readResponseMessage(out byte[] response)
        {
            List<byte> buffer = new List<byte>();
            byte currentByte = 0;
            byte prevByte = 0;
            bool messageStart = false;
            response = null;
            bool responseReceived = false;
            int currentTimeSpan = DateTime.Now.Millisecond;

            try
            {
                while (responseReceived == false)
                {
                    prevByte = currentByte;
                    currentByte = (byte)port.ReadByte();

                    if (prevByte == UnipumpUtils.DLE && currentByte == UnipumpUtils.STX && messageStart == false)
                    {
                        buffer.Add(prevByte);
                        messageStart = true;
                    }

                    if (messageStart == false)
                        continue;

                    buffer.Add(currentByte);

                    if (buffer.Capacity > maxPacketSize)
                        throw new Exception("Error: Max receive packet size exceeded");

                    if (prevByte == UnipumpUtils.DLE && currentByte == UnipumpUtils.ETX)
                        responseReceived = true; // End of packet

                    // No response received during maximum response timeout
                    if (DateTime.Now.Millisecond > maxResponseTimeout + currentTimeSpan)
                        responseReceived = true; // Finish wait for response
                }
            }
            catch (TimeoutException)
            {
                if (initializing)
                {
                    Monitor.Exit(port);
                    throw;
                }

                OnTimeoutExpired();
                return;
            }

            response = buffer.ToArray();
        }

        /// <summary>
        /// Gets and sets authorization type of a PTS controller over FuelPoints.
        /// </summary>
        public AuthorizeType SelectedAuthorizationType
        {
            get
            {
                return selectedAuthorizationType;
            }
            set
            {
                if (value == AuthorizeType.Volume || value == AuthorizeType.Amount)
                    selectedAuthorizationType = value;
                else
                    throw new InvalidOperationException("AuthorizeType enumeration out of range");
            }
        }

        /// <summary>
        /// Gets and sets whether polling authorization should be constantly made or once once if pump is in READY state.
        /// </summary>
        public bool AuthorizationPolling
        {
            get
            {
                return authorizationPolling;
            }
            set
            {
                authorizationPolling = value;
            }
        }

        /// <summary>
        /// Gets and sets whether to use general commands or extended commands
        /// </summary>
        public bool UseExtendedCommands
        {
            get
            {
                return useExtendedCommands;
            }
            set
            {
                useExtendedCommands = value;
            }
        }

        /// <summary>
        /// Gets or sets whether it is necessary to request totalizers after each dispensing performed or not.
        /// </summary>
        public bool RequestTotalizers
        {
            get
            {
                return requestTotalizers;
            }
            set
            {
                requestTotalizers = value;
            }
        }

        /// <summary>
        /// Gets or sets whether it is necessary to automatically authorize a dispenser when nozzle is up in postpayment mode or manually
        /// </summary>
        public bool AutomaticAuthorize
        {
            get
            {
                return automaticAuthorize;
            }
            set
            {
                automaticAuthorize = value;
            }
        }

        /// <summary>
        /// Gets or sets a value of volume to authorize a dispenser when manual mode is selected
        /// </summary>
        public int ManualModeAuthorizeValue
        {
            get
            {
                return manualModeAuthorizeValue;
            }
            set
            {
                manualModeAuthorizeValue = value;
            }
        }

        internal void OnMessageError(MessageErrorEventArgs e)
        {
        }

        protected void OnOpening()
        {
            if (Opening != null) 
                Opening(this, EventArgs.Empty);
        }

        protected void OnOpened()
        {
            if (Opened != null) 
                Opened(this, EventArgs.Empty);
        }

        protected void OnClosing()
        {
            if (Closing != null) 
                Closing(this, EventArgs.Empty);
        }

        protected void OnClosed()
        {
            if (Closed != null) 
                Closed(this, EventArgs.Empty);
        }

        protected void OnInitializing()
        {
            if (Initializing != null) 
                Initializing(this, EventArgs.Empty);
        }

        protected void OnInitialized()
        {
            if (Initialized != null) 
                Initialized(this, EventArgs.Empty);
        }

        internal void OnTimeoutExpired()
        {
            if (TimeoutExpired != null) 
                TimeoutExpired(this, EventArgs.Empty);
        }

        internal void OnTotalsUpdated(TotalsEventArgs e)
        {
            FuelPoint fp = new FuelPoint(this);

            foreach (FuelPoint fuelPoint in FuelPoints)
                if (fuelPoint.ID == e.Address)
                    fp = fuelPoint;

            if (TotalsUpdated != null)
                TotalsUpdated(this, e);

            if (fp != null)
                fp.OnTotalsUpdated(e);
        }

        internal void OnPricesGet(PricesEventArgs e)
        {
            FuelPoint fp = new FuelPoint(this);

            foreach (FuelPoint fuelPoint in FuelPoints)
                if (fuelPoint.ID == e.Address)
                    fp = fuelPoint;

            if (PricesGet != null)
                PricesGet(this, e);

            if (fp != null)
                fp.OnPricesGet(e);
        }

        /// <summary>
        /// Event occures before closing a COM-port.
        /// </summary>
        public event EventHandler Closing;

        /// <summary>
        /// Event occures after closing a COM-port.
        /// </summary>
        public event EventHandler Closed;

        /// <summary>
        /// Event occures before beginning of initialization.
        /// </summary>
        public event EventHandler Initializing;

        /// <summary>
        /// Event occures after of initialization.
        /// </summary>
        public event EventHandler Initialized;

        /// <summary>
        /// Event occures before opening a COM-port.
        /// </summary>
        public event EventHandler Opening;

        /// <summary>
        /// Event occures after opening a COM-port.
        /// </summary>
        public event EventHandler Opened;

        /// <summary>
        /// Event occures after calling a method UpdateTotals(), in which data of an electronic totalizer is received.
        /// </summary>
        public event EventHandler<TotalsEventArgs> TotalsUpdated;

        /// <summary>
        /// Event occures after calling a method UpdatePrices(), in which data of a prices of FuelPoint is received.
        /// </summary>
        public event EventHandler<PricesEventArgs> PricesGet;

        /// <summary>
        /// Event occures when at request of data from a PTS controller duration of a response 
        /// exceeds a value set in ResponseTimeout.
        /// </summary>
        public event EventHandler TimeoutExpired;
    }

    /// <summary>
    /// Provides information on errors in transmitted messages.
    /// </summary>
    internal class MessageErrorEventArgs : EventArgs
    {
        byte[] message;
        int errorCode;

        public MessageErrorEventArgs(byte[] message)
        {
            this.errorCode = -1;
            this.message = message;
        }

        public MessageErrorEventArgs(int errorCode, byte[] message)
        {
            this.errorCode = errorCode;
            this.message = message;
        }

        public int ErrorCode
        {
            get
            {
                return errorCode;
            }
        }

        public byte[] Message
        {
            get
            {
                return message;
            }
        }        
    }

    /// <summary>
    /// Provides information for an event TotalsUpdated.
    /// </summary>
    //[ComVisible(true)]
    public class TotalsEventArgs : EventArgs
    {
        private int address;
        private byte nozzleId;
        private Nozzle nozzle;
        
        /// <summary>
        /// Creates an exemplar of TotalsEventArgs class.
        /// </summary>
        /// <param name="address">Physical address of a FuelPoint.</param>
        /// <param name="nozzleId">Identifier of a nozzle, for which electronic totalizer was updated.</param>
        /// <param name="nozzle">Nozzle, for which electronic totalizer was updated.</param>
        public TotalsEventArgs(int address, byte nozzleId, Nozzle nozzle)
        {
            this.address = address;
            this.nozzleId = nozzleId;
            this.nozzle = nozzle;
        }

        /// <summary>
        /// Gets physical address of a FuelPoint.
        /// </summary>
        public int Address
        {
            get
            {
                return address;
            }
        }

        /// <summary>
        /// Gets identifier of a nozzle, for which electronic totalizer was updated.
        /// </summary>
        public byte NozzleID
        {
            get
            {
                return nozzleId;
            }
        }

        /// <summary>
        /// Gets nozzle, for which electronic totalizer was updated.
        /// </summary>
        public Nozzle Nozzle
        {
            get
            {
                return nozzle;
            }
        }
    }

    /// <summary>
    /// Provides information for an event PricesUpdated.
    /// </summary>
    //[ComVisible(true)]
    public class PricesEventArgs : EventArgs
    {
        private int address;

        /// <summary>
        /// Creates an exemplar of PricesEventArgs class.
        /// </summary>
        /// <param name="address">Physical address of a FuelPoint.</param>
        public PricesEventArgs(int address)
        {
            this.address = address;
        }

        /// <summary>
        /// Gets physical address of a FuelPoint.
        /// </summary>
        public int Address
        {
            get
            {
                return address;
            }
        }
    }

    /// <summary>
    /// Provides information for an event TransactionFinished.
    /// </summary>
    //[ComVisible(true)]
    public class TransactionEventArgs : EventArgs
    {
        private int transactionId;
        private byte nozzle;
        private float dispensedAmount;
        private int dispensedVolume;
        private float dispensedPrice;

        /// <summary>
        /// Creates an exemplar of TransactionEventArgs class.
        /// </summary>
        /// <param name="transactionId">Identifier of a transaction.</param>
        /// <param name="nozzle">Identifier of a nozzle.</param>
        /// <param name="dispensedAmount">Dispensed amount.</param>
        /// <param name="dispensedVolume">Dispensed volume.</param>
        public TransactionEventArgs(int transactionId, byte nozzle, float dispensedAmount, int dispensedVolume, float dispensedPrice)
        {
            this.transactionId = transactionId;
            this.nozzle = nozzle;
            this.dispensedAmount = dispensedAmount;
            this.dispensedVolume = dispensedVolume;
            this.dispensedPrice = dispensedPrice;
        }

        /// <summary>
        /// Gets identifier of a transaction.
        /// </summary>
        public int TransactionID
        {
            get
            {
                return transactionId;
            }
        }

        /// <summary>
        /// Gets identifier of a nozzle.
        /// </summary>
        public byte Nozzle
        {
            get
            {
                return nozzle;
            }
        }

        /// <summary>
        /// Gets dispensed amount.
        /// </summary>
        public float DispensedAmount
        {
            get
            {
                return dispensedAmount;
            }
        }

        /// <summary>
        /// Gets dispensed volume.
        /// </summary>
        public int DispensedVolume
        {
            get
            {
                return dispensedVolume;
            }
        }

        /// <summary>
        /// Gets dispensed price.
        /// </summary>
        public float DispensedPrice
        {
            get
            {
                return dispensedPrice;
            }
        }
    }

    public enum OrderModes
    {
        /// <summary>
        /// Mode of dispensing with previous setting of order in system
        /// </summary>
        Preset = 1,
        /// <summary>
        /// Mode of dispensing with setting of order in dispenser or without setting of order
        /// </summary>
        Manual = 2
    }   
}

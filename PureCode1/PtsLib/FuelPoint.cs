using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PTSLib.PTS
{
    /// <summary>
    /// Provides control over a FuelPoint connected to a PTS controller.
    /// </summary>
    public class FuelPoint
    {
        private FuelPointStatus dispenserStatus;
        private FuelPointStatus status;
        private FuelPointChannel channel;
        private PTSController pts;
        private Nozzle[] nozzles;
        private byte activeNozzleId;
        private byte lastActiveNozzleId;
        private byte transactionNozzleId;
        private int channelId, physicalAddress;
        private int dispensedVolume = 0;
        private bool transEventRised;
        private bool isActive;
        private bool transFinished;
        private byte currentPendingCommand;
        private byte previousPendingCommand;
        private OrderModes orderMode;
        private bool totalRecieved = false;
        private byte totalNozzle = 0;
        private bool authorized = false;
        private byte authorizeNozzle = 0;
        private AuthorizeType lastType = AuthorizeType.Amount;
        private int lastAuthAmount = 0;

        

        private System.Threading.Thread thTotals = null;
        private System.Threading.Thread thAuthorize;


        public bool WorkingEntered { set; get; }

        /// <summary>
        /// Creates exemplar of ATG class.
        /// </summary>
        /// <param name="pts">Exemplar of parent PTS class.</param>
        internal FuelPoint(PTSController pts)
        {
            this.AutocloseTransaction = true;
            this.Locked = false;
            this.nozzles = new Nozzle[PtsConfiguration.NozzleQuantity];
            this.pts = pts;
            this.status = FuelPointStatus.OFFLINE;
            this.transEventRised = false;
            this.isActive = false;
            this.transFinished = false;
            this.transactionNozzleId = 0;
            this.orderMode = OrderModes.Preset;

            for (int i = 0; i < PtsConfiguration.NozzleQuantity; i++)
            {
                nozzles[i] = new Nozzle(this, (byte)(i + 1));
            }
        }

        /// <summary>
        /// Gets a currently taken up nozzle. 
        /// </summary>
        /// <remarks>
        /// If there is no taken up nozzle - then returns null (Nothing in Visual Basic).
        /// </remarks>
        public Nozzle ActiveNozzle
        {
            get
            {
                if (ActiveNozzleID == 0) return null;

                return nozzles[ActiveNozzleID - 1];
            }
        }

        /// <summary>
        /// Gets an identifier of a taken up nozzle. 
        /// </summary>
        /// <remarks>
        /// If there is no taken up nozzle - then returns 0.
        /// </remarks>
        public byte ActiveNozzleID
        {
            get
            {
                return activeNozzleId;
            }
            internal set
            {
                byte prevValue = activeNozzleId;

                this.lastActiveNozzleId = activeNozzleId;
                activeNozzleId = value;

                OnNozzleChanged(new NozzleChangedEventArgs(activeNozzleId, prevValue));
            }
        }

        /// <summary>
        /// Gets an identifier of a nozzle, through which fuel was dispensed in last ransaction. 
        /// </summary>
        /// <remarks>
        /// If there was no transaction - then returns 0.
        /// </remarks>
        public byte TransactionNozzleID
        {
            get
            {
                return transactionNozzleId;
            }
            internal set
            {
                transactionNozzleId = value;
            }
        }

        /// <summary>
        /// Gets code of command currently executed by PTS controller.
        /// </summary>
        public byte CurrentPendingCommand
        {
            get
            {
                return currentPendingCommand;
            }
            internal set
            {
                currentPendingCommand = value;
            }
        }

        /// <summary>
        /// Gets code of previous command currently executed by PTS controller.
        /// </summary>
        public byte PreviousPendingCommand
        {
            get
            {
                return previousPendingCommand;
            }
            internal set
            {
                previousPendingCommand = value;
            }
        }

        /// <summary>
        /// Gets or sets a value, which points if a FuelPoint is active and it is necessary to query its state.
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
        /// Gets or sets a value, which points that previous transaction has just finished (used in order to request totalizers at once after transaction is finished and closed).
        /// </summary>
        public bool TransFinished
        {
            get
            {
                return transFinished;
            }
            set
            {
                transFinished = value;
            }
        }

        /// <summary>
        /// Gets or sets a value, which points whether transaction should be closed automatically or it is necessary 
        /// to close transaction manually after it is finished.
        /// </summary>
        public bool AutocloseTransaction
        { get; set; }

        /// <summary>
        /// Gets unique identifier of a FuelPoint.
        /// </summary>
        public int ID
        { get; set; }

        public bool Initialized { set; get; }

        /// <summary>
        /// Gets or sets physical address of a FuelPoint.
        /// </summary>
        public int PhysicalAddress
        {
            get
            {
                return physicalAddress;
            }
            set
            {
                if (value < 0 || value > PtsConfiguration.FuelPointAddressQuantity) throw new ArgumentOutOfRangeException();

                physicalAddress = value;
            }
        }

        /// <summary>
        /// Gets or sets an identifier of a channel, to which a FuelPoint is connected.
        /// </summary>
        /// <remarks>
        /// If a FuelPoint is not connected to a channel then a value should be equal to zero.
        /// </remarks>
        public int ChannelID
        {
            get
            {
                return channelId;
            }
            set
            {
                if (value < 0 || value > PtsConfiguration.FuelPointChannelQuantity) throw new ArgumentOutOfRangeException();

                channelId = value;

                if (value > 0)
                    foreach (FuelPointChannel fuelPointChannel in pts.FuelPointChannels)
                        if (fuelPointChannel.ID == value)
                            channel = fuelPointChannel;
            }
        }

        /// <summary>
        /// Gets an object FuelPointChannel, to which a FuelPoint is connected.
        /// </summary>
        /// <remarks>
        /// If a FuelPoint is not connected to a channel - returns a value null (Nothing in Visual Basic).
        /// </remarks>     
        public FuelPointChannel Channel
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
        /// Gets or sets whether dispensing should be done when preset with previous setting of order in system or in dispenser
        /// </summary>     
        public OrderModes OrderMode
        {
            get
            {
                return orderMode;
            }
            set
            {
                orderMode = value;
            }
        }

        /// <summary>
        /// Gets an amount (in cents) for a current (in a case of an active transaction) or last fuel dispense.
        /// </summary>
        [Browsable(false)]
        public float DispensedAmount
        { get; set; }

        /// <summary>
        /// Gets a volume (in 10 ml units) for a current (in a case of an active transaction) or last fuel dispense.
        /// </summary>
        [Browsable(false)]
        public int DispensedVolume
        {
            get { return this.dispensedVolume; }
            set 
            {
                if (value == this.dispensedVolume)
                    return;
                this.dispensedVolume = value;
                if(this.DispensedValuesChanged != null)
                    this.DispensedValuesChanged(this, new EventArgs());
            }
        }

        /// <summary>
        /// Gets a value indicating whether a FuelPoint is locked.
        /// </summary>
        public bool Locked
        { get; internal set; }

        /// <summary>
        ///  Gets an array of objects Nozzle connected to given a FuelPoint.
        /// </summary>
        public Nozzle[] Nozzles
        {
            get
            {
                return nozzles;
            }
        }        

        /// <summary>
        /// Gets a status of a FuelPoint.
        /// </summary>
        public FuelPointStatus DispenserStatus
        {
            get
            {
                return dispenserStatus;
            }
            set
            {
                try
                {
                    if (dispenserStatus == value)
                        return;
                    if (!this.Initialized)
                    {
                        return;
                    }
                    this.dispenserStatus = value;
                    //if (this.dispenserStatus == FuelPointStatus.WORK)
                    //{
                    //    this.DispensedVolume = 0;
                    //    this.DispensedAmount = 0;
                    //    this.WorkingEntered = true;
                    //}
                    //if (this.dispenserStatus == FuelPointStatus.NOZZLE && (this.status == FuelPointStatus.TRANSACTIONCOMPLETED || this.status == FuelPointStatus.TRANSACTIONSTOPPED))
                    //{
                    //    this.WorkingEntered = true;
                    //}
                    //else if (this.dispenserStatus == FuelPointStatus.NOZZLE)
                    //{
                    //    this.WorkingEntered = false;
                    //    this.Status = this.dispenserStatus;
                    //}
                    //else if (this.dispenserStatus == FuelPointStatus.IDLE && (this.status == FuelPointStatus.WORK ||
                    //    this.status == FuelPointStatus.TRANSACTIONCOMPLETED || this.status == FuelPointStatus.TRANSACTIONSTOPPED ||
                    //    this.status == FuelPointStatus.READY))
                    //{
                    //    //if (this.ActiveNozzle != null)
                    //    //{
                    //    //    this.totalNozzle = this.ActiveNozzleID;
                    //    //    this.totalRecieved = false;
                    //    //    this.thTotals = new System.Threading.Thread(new System.Threading.ThreadStart(this.TotalsThread));
                    //    //    this.thTotals.Start();
                    //    //}
                    //    //else if (this.lastActiveNozzleId > 0)
                    //    //{
                    //    //    this.totalNozzle = this.lastActiveNozzleId;
                    //    //    this.totalRecieved = false;
                    //    //    this.thTotals = new System.Threading.Thread(new System.Threading.ThreadStart(this.TotalsThread));
                    //    //    this.thTotals.Start();
                    //    //}
                    //    this.Status = this.dispenserStatus;
                    //}
                    //if (this.dispenserStatus == FuelPointStatus.NOZZLE && (this.status == FuelPointStatus.IDLE || this.status == FuelPointStatus.OFFLINE))
                    ////else if (this.dispenserStatus == FuelPointStatus.NOZZLE && (this.status == FuelPointStatus.IDLE))
                    //{
                    //    if (this.ActiveNozzle != null)
                    //    {
                    //        this.ActiveNozzle.QueryTotals = true;
                    //        this.thTotals = new System.Threading.Thread(new System.Threading.ThreadStart(this.TotalsThread));
                    //        this.thTotals.Start();
                    //    }
                    //    else if (this.lastActiveNozzleId > 0)
                    //    {
                    //        this.Nozzles[(int)lastActiveNozzleId - 1].QueryTotals = true;
                    //        this.thTotals = new System.Threading.Thread(new System.Threading.ThreadStart(this.TotalsThread));
                    //        this.thTotals.Start();
                    //    }
                    //}
                    //else if (this.dispenserStatus == FuelPointStatus.NOZZLE && (this.status == FuelPointStatus.TRANSACTIONCOMPLETED || this.status == FuelPointStatus.TRANSACTIONSTOPPED))
                    //{
                    //    this.dispenserStatus = FuelPointStatus.IDLE;
                    //    this.status = FuelPointStatus.IDLE;
                    //}
                    //else
                    //{
                    //    this.Status = this.dispenserStatus;
                    //}

                    if (this.dispenserStatus != FuelPointStatus.NOZZLE)
                    {
                        this.authorized = true;
                        this.authorizeNozzle = 0;
                    }
                    this.Status = this.dispenserStatus;
                }
                finally
                {
                    //ASFuelControl.Logging.Logger.Instance.LogToFile("PTS STATUSES", string.Format("DispenserStatus : {0}, Status : {1}", this.dispenserStatus, this.status), 3);
                }
                
            }
        }

        public FuelPointStatus Status
        {
            get
            {
                return status;
            }
            set
            {
                FuelPointStatus prevStatus;
                if (status != value)
                {
                    prevStatus = dispenserStatus;
                    status = value;
                    OnStatusChanged(new StatusChangedEventArgs(dispenserStatus, prevStatus));
                }
            }
        }

        /// <summary>
        /// Gets or sets an identifier of a current transaction.
        /// </summary>
        public int TransactionID
        { 
            get; 
            internal set; 
        }

        private void TotalsThread()
        {
            bool noTotals = false;
            while (!noTotals)
            {
                int wait = 0;
                noTotals = true;
                foreach (Nozzle nozzle in this.nozzles)
                {
                    if (!nozzle.NozzleAlive)
                        continue;
                    if (nozzle.QueryTotals)
                    {
                        noTotals = false;
                        this.GetTotals(nozzle.ID);
                        if (this.Initialized)
                        {
                            System.Threading.Thread.Sleep(250);
                            wait = wait + 250;
                        }
                        else
                        {
                            System.Threading.Thread.Sleep(1500);
                            wait = wait + 1500;
                        }
                        
                    }
                }
                if(wait < 1500)
                    System.Threading.Thread.Sleep(1500 - wait);
            }
        }

        private void AuthorizeThread()
        {
            System.Threading.Thread.Sleep(1000);
            while (!authorized && this.authorizeNozzle > 0)
            {
                this.Authorize(this.lastType, this.lastAuthAmount, this.authorizeNozzle);
                System.Threading.Thread.Sleep(1000);
            }
            this.authorized = true;
            this.authorizeNozzle = 0;
        }

        public void GetTotalsByThread(byte nozzleId)
        {
            this.Nozzles[(int)nozzleId - 1].QueryTotals = true;
            //this.totalNozzle = nozzleId;
            //this.totalRecieved = false;
            this.thTotals = new System.Threading.Thread(new System.Threading.ThreadStart(this.TotalsThread));
            this.thTotals.Start();
        }


        /// <summary>
        /// Sends a command on authorization to a FuelPoint for a currently taken up nozzle and opens a transaction.
        /// </summary>
        /// <param name="autorizeType">Type of authorization.</param>
        /// <param name="orderAmount">Amount of order.</param>
        /// <param name="nozzleId">Identifier of authorized nozzle.</param>
        /// <remarks>
        /// In case of authorization is done by amount (AuthorizeType.Amount) <paramref name="orderAmmount"/>
        /// will have value in cents, in case of authorization is done by volume (AuthorizeType.Volume) 
        /// <paramref name="orderAmount"/> will have value in 10 ml units.
        /// Given method will work only when a property Status equals to FuelPointStatus.Nozzle and a FuelPoint is locked by a method Lock() (property Locked equals to true).
        /// After closing of a transaction it is necessary to call a method Unlock().
        /// </remarks>
        public void Authorize(AuthorizeType autorizeType, int orderAmount, byte nozzleId)
        {
            this.lastType = autorizeType;
            this.lastAuthAmount = orderAmount;
            this.authorizeNozzle = nozzleId;
            if (pts.UseExtendedCommands == false)
            {
                if (ActiveNozzleID < 1)
                    pts.RequestAuthorize(ID, nozzleId, autorizeType, orderAmount, Nozzles[nozzleId - 1].PricePerLiter);
                else
                    pts.RequestAuthorize(ID, ActiveNozzleID, autorizeType, orderAmount, ActiveNozzle.PricePerLiter);
            }
            else
            {
                if (ActiveNozzleID < 1)
                    pts.RequestExtendedAuthorize(ID, nozzleId, autorizeType, orderAmount, Nozzles[nozzleId - 1].PricePerLiter);
                else
                    pts.RequestExtendedAuthorize(ID, ActiveNozzleID, autorizeType, orderAmount, ActiveNozzle.PricePerLiter);
            }
            this.authorized = false;
            this.thAuthorize = new System.Threading.Thread(new System.Threading.ThreadStart(this.AuthorizeThread));
            this.thAuthorize.Start();
        }

        /// <summary>
        /// Closes current tranaction.
        /// </summary>
        /// <remarks>
        /// Given method needs to be called in a case if value of property Status 
        /// equals to FuelPointStatus.TransactionCompleted or FuelPointStatus.TransactionStopped.
        /// </remarks>
        public void CloseTransaction()
        {
            pts.RequestCloseTransaction(ID, TransactionID);
        }

        /// <summary>
        /// Stops dispensing of fuel through a FuelPoint.
        /// </summary>
        /// <remarks>
        /// If AutocloseTransaction equals to true then transaction closes automatically, at this
        /// property Status will be equal to FuelPointStatus.TransactionStopped.
        /// </remarks>
        public void Halt()
        {
            pts.RequestHalt(ID);
            DispenserStatus = FuelPointStatus.TRANSACTIONSTOPPED;
        }

        /// <summary>
        /// Locks control over a FuelPoint in a multi POS system (each POS system having a PTS controller connected).
        /// </summary>
        /// <remarks>
        /// Given method needs to be called before calling methods Autorize and UpdatePrices.
        /// </remarks>
        public void Lock()
        {
            pts.RequestLock(ID);
        }

        /// <summary>
        /// Unlocks control over a FuelPoint in a multi POS system (each POS system having a PTS controller connected).
        /// </summary>
        /// <remarks>
        /// Given method needs to be called after calling methods Autorize and UpdatePrices.
        /// </remarks>
        public void Unlock()
        {
            pts.RequestUnlock(ID);
        }

        /// <summary>
        /// Sets prices on fuel for nozzles in a FuelPoint in accordance with prices set by
        /// properties PricePerLiter of connected objects Nozzle.
        /// </summary>
        /// <remarks>
        /// Before calling a given method it is necessary to call a method Lock, and after it to call a method Unlock. 
        /// Before calling a method Unlock it is necessary to wait for a pause for prices to be updated (duration of a 
        /// pause may vary depending on fuel dispenser, in average up to 3 sec max).
        /// </remarks>
        public void SetPrices()
        {
            int[] prices = new int[PtsConfiguration.NozzleQuantity];
            for (int i = 0; i < PtsConfiguration.NozzleQuantity; i++)
                prices[i] = this.Nozzles[i].PricePerLiter;

            if(pts.UseExtendedCommands == false)
                pts.RequestPricesSet(ID, prices);
            else
                pts.RequestExtendedPricesSet(ID, prices);
        }

        /// <summary>
        /// Gets prices on fuel for nozzles in a FuelPoint.
        /// </summary>
        /// <remarks>
        /// Before calling a given method it is necessary to call a method Lock, and after it to call a method Unlock. 
        /// </remarks>
        public void GetPrices()
        {
            pts.RequestPricesGet(ID);
        }

        /// <summary>
        /// Gets total counters from the FuelPoint.
        /// </summary>
        /// <remarks>
        /// Before calling a given method it is necessary to call a method Lock, and after it to call a method Unlock. 
        /// </remarks>
        /// <param name="nozzleId">Identifier of the nozzle.</param>
        public void GetTotals(byte nozzleId)
        {
            if (pts.UseExtendedCommands == false)
                pts.RequestTotals(ID, nozzleId);
            else
                pts.RequestExtendedTotals(ID, nozzleId);
        }

        /// <summary>
        /// Gets status of the FuelPoint.
        /// </summary>
        /// <remarks>
        /// Returns current status of the FuelPoint. 
        /// </remarks>
        internal void GetStatus()
        {
            try
            {
                if (pts.UseExtendedCommands == false)
                    pts.RequestStatus(ID);
                else
                    pts.RequestExtendedStatus(ID);
            }
            catch
            {
            }
        }

        protected void OnNozzleChanged(NozzleChangedEventArgs e)
        {
            if (NozzleChanged != null) 
                NozzleChanged(this, e);
        }

        protected void OnStatusChanged(StatusChangedEventArgs e)
        {
            if (StatusChanged != null) 
                StatusChanged(this, e);
        }

        protected internal void OnPendingCommandChanged(PendingCommandChangedEventArgs e)
        {
            if (PendingCommandChanged != null)
                PendingCommandChanged(this, e);
        }

        protected internal void OnTotalsUpdated(TotalsEventArgs e)
        {
            if (e.Address != ID)
                return;
            e.Nozzle.QueryTotals = false;

            if (!this.Initialized)
            {
                if (e.Nozzle.TotalDispensedVolume > 0)
                {
                    e.Nozzle.NozzleInitialized = true;
                    e.Nozzle.LastTotalDispensedAmount = e.Nozzle.TotalDispensedVolume;
                    e.Nozzle.TotalGetAttempIndex = 0;
                }
                else
                {
                    e.Nozzle.TotalGetAttempIndex++;
                    if (e.Nozzle.TotalGetAttempIndex > 5)
                    {
                        e.Nozzle.NozzleInitialized = true;
                        e.Nozzle.LastTotalDispensedAmount = e.Nozzle.TotalDispensedVolume;
                        e.Nozzle.TotalGetAttempIndex = 0;
                    }
                }
            }
            else
            {
                if (e.Nozzle.TotalDispensedVolume == e.Nozzle.LastTotalDispensedVolume && e.Nozzle.TotalGetAttempIndex < 5)
                {
                    e.Nozzle.TotalGetAttempIndex++;
                    e.Nozzle.QueryTotals = true;
                    return;
                }
                e.Nozzle.TotalGetAttempIndex = 0;
            }
            for (int i = 0; i < this.nozzles.Length; i++)
            {
                if (!this.nozzles[i].NozzleAlive)
                    continue;
                if (!this.nozzles[i].NozzleInitialized)
                    continue;
            }
            if (this.dispenserStatus == FuelPointStatus.OFFLINE && !this.Initialized)
            {
                this.DispenserStatus = FuelPointStatus.IDLE;
                this.Initialized = true;
            }
            this.Status = this.DispenserStatus;

            if (TotalsUpdated != null) 
                TotalsUpdated(this, e);
        }

        protected internal void OnPricesGet(PricesEventArgs e)
        {
            if (PricesGet != null && e.Address == ID) 
                PricesGet(this, e);
        }

        protected internal void OnTransactionFinished(TransactionEventArgs e)
        {
            if (AutocloseTransaction) 
                CloseTransaction();

            transFinished = true;

            if (TransactionFinished != null)
            {
                transEventRised = true;
                TransactionFinished(this, e);                
            }
        }

        public override string ToString()
        {
            return string.Format("FuelPoint: ID={0}, Address={1}", ID, PhysicalAddress);
        }

        /// <summary>
        /// Event occures when another nozzle at FuelPoint is taken up.
        /// </summary>
        public event EventHandler<NozzleChangedEventArgs> NozzleChanged;
        
        /// <summary>
        /// Event occures when status of FuelPoint is changed.
        /// </summary>
        public event EventHandler<StatusChangedEventArgs> StatusChanged;

        /// <summary>
        /// Event occures when status of FuelPoint is changed.
        /// </summary>
        public event EventHandler<PendingCommandChangedEventArgs> PendingCommandChanged;
        
        /// <summary>
        /// Event occures when information about an electronic totalizer of one of the nozzles is updated.
        /// </summary>
        public event EventHandler<TotalsEventArgs> TotalsUpdated;

        /// <summary>
        /// Event occures when information about a price of one of the nozzles is updated.
        /// </summary>
        public event EventHandler<PricesEventArgs> PricesGet;

        /// <summary>
        /// Event occures every time when a transaction is finished normally or as a result of stoppage using a method Stop().
        /// </summary>
        public event EventHandler<TransactionEventArgs> TransactionFinished;

        public event EventHandler DispensedValuesChanged;
    }

    public class NozzleChangedEventArgs : EventArgs
    {
        byte previousNozzleId;
        byte currentNozzleId;

        public NozzleChangedEventArgs(byte currentNozzleId, byte previousNozzleId)
        {
            this.currentNozzleId = currentNozzleId;
            this.previousNozzleId = previousNozzleId;
        }

        public byte CurrentNozzleID
        {
            get
            {
                return currentNozzleId;
            }
        }

        public byte PreviousNozzleID
        {
            get
            {
                return previousNozzleId;
            }
        }
    }

    public class StatusChangedEventArgs : EventArgs
    {
        private FuelPointStatus previousStatus;
        private FuelPointStatus currentStatus;

        public StatusChangedEventArgs(FuelPointStatus currentStatus, FuelPointStatus previousStatus)
        {
            this.currentStatus = currentStatus;
            this.previousStatus = previousStatus;
            Console.WriteLine(currentStatus.ToString());
        }

        public FuelPointStatus CurrentStatus
        {
            get
            {
                
                return currentStatus;
            }
        }

        public FuelPointStatus PreviousStatus
        {
            get
            {
                return previousStatus;
            }
        }
    }

    public class PendingCommandChangedEventArgs : EventArgs
    {
        private byte previousPendingCommand;
        private byte currentPendingCommand;

        public PendingCommandChangedEventArgs(byte currentPendingCommand, byte previousPendingCommand)
        {
            this.currentPendingCommand = currentPendingCommand;
            this.previousPendingCommand = previousPendingCommand;
        }

        public byte CurrentPendingCommand
        {
            get
            {
                return currentPendingCommand;
            }
        }

        public byte PreviousPendingCommand
        {
            get
            {
                return previousPendingCommand;
            }
        }
    }
}

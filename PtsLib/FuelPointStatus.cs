using System;
using System.Collections.Generic;
using System.Text;

namespace PTSLib.PTS
{
    /// <summary>
    /// Specifies a list of supported statuses of a FuelPoint.
    /// </summary>
    public enum FuelPointStatus
    {
        /// <summary>
        /// FuelPoint is not active (locked or manual mode of control is switched on).
        /// </summary>
        OFFLINE = 0,
        /// <summary>
        /// All nozzles are hanged down (idle mode).
        /// </summary>
        IDLE = 1,
        /// <summary>
        /// Nozzle is taken up, waiting for authorization.
        /// </summary>
        NOZZLE = 3,
        /// <summary>
        /// Dispensing is allowed, indicator is being tested.
        /// </summary>
        READY = 4,
        /// <summary>
        /// Fuel is being dispensed through a nozzle.
        /// </summary>
        WORK = 5,
        /// <summary>
        /// Transaction is finished normally, waiting for a nozzle to be hanged down.
        /// </summary>
        TRANSACTIONCOMPLETED = 6,
        /// <summary>
        /// Transaction is finished abnormally, waiting for a nozzle to be hanged down.
        /// </summary>
        TRANSACTIONSTOPPED = 7,
        /// <summary>
        /// Status of an error.
        /// </summary>
        ERROR = 8
    };
}

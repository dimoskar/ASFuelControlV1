using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace PTSLib.PTS
{
    /// <summary>
    /// Provides information about a nozzle of a FuelPoint.
    /// </summary>
    public class Nozzle
    {
        private FuelPoint parent;
        private int pricePerLiter;
        private byte id;
        private bool queryTotals = false;
        private Int64 totalDispensedAmount;
        private Int64 totalDispensedVolume;


        /// <summary>
        /// Creates an exemplar of Nozzle class.
        /// </summary>
        /// <param name="parent">Exemplar of parent FuelPoint class.</param>
        /// <param name="id">Identifier of a nozzle.</param>
        internal Nozzle(FuelPoint parent, byte id)
        {
            this.parent = parent;
            this.id = id;
        }

        /// <summary>
        /// Gets an object FuelPoint, to which a nozzle belongs to.
        /// </summary>
        public FuelPoint FuelPoint
        {
            get
            {
                return parent;
            }
        }

        public bool QueryTotals 
        {
            set { this.queryTotals = value; }
            get { return this.queryTotals; }
        }

        public int TotalGetAttempIndex { set; get; }

        public bool SaleCompleted { set; get; }
        /// <summary>
        /// Gets an identifier of a nozzle.
        /// </summary>
        public byte ID
        {
            get
            {
                return id;
            }
        }

        /// <summary>
        /// Gets or sets price of fuel per liter/galon.
        /// </summary>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// Set value is less than zero.
        /// </exception>
        public int PricePerLiter
        {
            get
            {
                return pricePerLiter;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException();

                pricePerLiter = value;
            }
        }

        public bool NozzleInitialized { set; get; }
        public bool NozzleAlive { set; get; }


        public Int64 LastTotalDispensedAmount { internal set; get; }

        /// <summary>
        /// Gets a value of totally dispensed amount (in cents) of electronic totalizer.
        /// </summary>
        public Int64 TotalDispensedAmount
        {
            get { return this.totalDispensedAmount; }
            internal set
            {
                if (value == this.totalDispensedAmount)
                    return;
                this.LastTotalDispensedAmount = this.totalDispensedAmount;
                this.totalDispensedAmount = value;
            }
        }

        public Int64 LastTotalDispensedVolume { internal set; get; }

        /// <summary>
        /// Gets a value of totally dispensed volume (in 10 ml units) of electronic totalizer.
        /// </summary>
        public Int64 TotalDispensedVolume
        {
            get { return this.totalDispensedVolume; }
            internal set
            {
                if (value == this.totalDispensedVolume)
                    return;
                if(!this.NozzleInitialized)
                    this.LastTotalDispensedVolume = value;
                else
                    this.LastTotalDispensedVolume = this.totalDispensedVolume;
                this.totalDispensedVolume = value;
            }
        }

        /// <summary>
        /// Requests a FuelPoint on values of electronic totalizer.
        /// </summary>
        public void UpdateTotals()
        {
            parent.GetTotals(id);
        }

        public override string ToString()
        {
            return string.Format("Nozzle{0}", ID);
        }
    }
}

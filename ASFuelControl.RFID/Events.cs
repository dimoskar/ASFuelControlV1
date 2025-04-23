using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.RFID
{
    public delegate void RfidHasEnrollNumber(object sender, RFIDEvent Data);
    public class RFIDEvent : EventArgs
    {
        public int EnrollNumber
        {
            get;
            set;
        }

        public DateTime EnrollDateTime { set; get; }

        public RFIDEvent(int Value, DateTime dt)
        {
            this.EnrollNumber = Value;
            this.EnrollDateTime = dt;
        }
    }
}

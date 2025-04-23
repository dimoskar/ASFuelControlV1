using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.WorkFlow
{
    public interface IFuelPumpWorkFlow
    {
        event EventHandler QueryCheckState;
        event EventHandler<SaleCompletedArgs> SaleCompleted;

        VirtualDevices.VirtualDispenser Dispenser { set; get; }
        Common.IController Controller { set; get; }
        ASFuelControl.WorkFlow.WorkFlowProcess Process { set; get; }
        Guid CurrentShiftId { set; get; }

        void SetValues(object _values);
        void SetSale(Common.Sales.SaleData sale);
        void CloseSale(Common.Sales.SaleData sale);
        void ValidateTransitions();
    }

    public class SaleCompletedArgs : EventArgs
    {
        public Common.Sales.SaleData Sale { private set; get; }
        public Guid InvoiceLineId { set; get; }

        public SaleCompletedArgs(Common.Sales.SaleData sale)
        {
            this.Sale = sale;
        }
    }
}

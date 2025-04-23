using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.Windows.ViewModels
{
    public partial class VehicleViewModel
    {
        public string TraderName { set; get; }

        public string TraderTaxRegistration { set; get; }

        public string TraderDeliveryAddress { set; get; }

        public Guid DefaultInvoiceTypeId { set; get; }

        public int Odometer { set; get; }
    }
}

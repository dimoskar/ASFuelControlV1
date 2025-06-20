using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exedron.Invoicing.Models
{

    public class InvoiceCreationResponse
    {
        public Invoicemarking invoiceMarking { get; set; }
        public Error[] errors { get; set; }
    }

    public class Invoicemarking
    {
        public string mark { get; set; }
        public string providerUrl { get; set; }
        public string qrCode { get; set; }
        public string invoiceId { get; set; }
        public string verificationHash { get; set; }
        public string invoiceIdentifier { get; set; }
        public bool previouslySubmitted { get; set; }
    }

    public class Error
    {
        public string code { get; set; }
        public string defaultMessage { get; set; }
        public bool fatal { get; set; }
    }

}

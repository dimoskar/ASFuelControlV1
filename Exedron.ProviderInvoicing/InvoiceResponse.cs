using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exedron.ProviderInvoicing
{
    public class InvoiceResponse
    {
        public string Mark { set; get; }
        public string Uid { set; get; }
        public string Url { set; get; }
        public string InvoiceUrl { set; get; }
        public string QrCodeUrl { set; get; }
        public string VerificationHash { set; get; }
        public string ProviderUrl { set; get; }
        public string InvoiceSignature { set; get; }
        public bool HasErrors
        {
            get { return this.Errors != null && Errors.Length > 0; }
        }
        public string[] Errors { set; get; }
        public string[] Warnings { set; get; }
        public bool HasWarnings
        {
            get { return this.Warnings != null && Warnings.Length > 0; }
        }
    }
}

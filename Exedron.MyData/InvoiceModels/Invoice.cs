using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Exedron.MyData.Interfaces;

namespace Exedron.MyData.InvoiceModels
{
    public class Invoice : Interfaces.IInvoice
    {
        public string Uid { set; get; }
        public long Mark { set; get; }
        public long CanceledByMark { set; get; }
        public string AuthenticationCode { set; get; }
        public IPartyType Issuer { set; get; }
        public IPartyType CounterPart { set; get; }
        public IInvoiceHeader InvoiceHeader { set; get; }
        public IPaymentMethodDetailType[] PaymentMethods { set; get; }
        public IInvoiceRowType[] InvoiceDetails { set; get; }
        public ITaxTotal[] TaxesTotals { set; get; }
        public IInvoiceSummary InvoiceSummary { set; get; }

        public string AsXml()
        {
            string xml = Properties.Resources.Invoice;
            if(!string.IsNullOrEmpty(this.Uid))
                xml = xml.Replace("[uid]", this.Uid);
            else
                xml = xml.Replace("[uid]", "");
            if (Mark > 0)
                xml = xml.Replace("[mark]", this.Mark.ToString());
            else
                xml = xml.Replace("[mark]", "");
            if (CanceledByMark > 0)
                xml = xml.Replace("[canceledbymark]", this.CanceledByMark.ToString());
            else
                xml = xml.Replace("[canceledbymark]", "");

            if(this.CounterPart != null)
                xml = xml.Replace("[counterpart]", this.CounterPart.AsXml());
            else
                xml = xml.Replace("[counterpart]", "");

            xml = xml.Replace("[issuer]", this.Issuer.AsXml());
            xml = xml.Replace("[invoiceheader]", this.InvoiceHeader.AsXml());
            string pcXml = "";
            if (this.PaymentMethods != null)
            {
                foreach (var pc in this.PaymentMethods)
                {
                    if (pcXml == "")
                        pcXml = pc.AsXml();
                    else
                        pcXml = pcXml + "\r\n" + pc.AsXml();
                }
            }
            xml = xml.Replace("[paymentmethoddetails]", pcXml);
            string detailsXml = "";
            List<IIncomeClassification> incomes = new List<IIncomeClassification>();
            foreach(var id in this.InvoiceDetails)
            {
                if(detailsXml == "")
                    detailsXml = id.AsXml();
                else
                    detailsXml = detailsXml + "\r\n" + id.AsXml();
                if(id.IncomeClassification != null)
                {
                    var ic = incomes.FirstOrDefault(i =>
                        i.ClassificationCategory == id.IncomeClassification.ClassificationCategory &&
                        i.ClassificationType == id.IncomeClassification.ClassificationType);
                    if (ic != null)
                        ic.Amount = ic.Amount + decimal.Round(id.IncomeClassification.Amount, 2);
                    else
                    {
                        ic = new IncomeClassification();
                        ic.Amount = decimal.Round(id.IncomeClassification.Amount, 2);
                        ic.ClassificationCategory = id.IncomeClassification.ClassificationCategory;
                        ic.ClassificationType = id.IncomeClassification.ClassificationType;
                        incomes.Add(ic);
                    }
                }
                if(id.IncomeClassifications != null && id.IncomeClassifications.Length > 0)
                {
                    foreach (var idIc in id.IncomeClassifications)
                    {
                        var ic = incomes.FirstOrDefault(i =>
                            i.ClassificationCategory == idIc.ClassificationCategory &&
                            i.ClassificationType == idIc.ClassificationType);
                        if (ic != null)
                            ic.Amount = ic.Amount + decimal.Round(idIc.Amount, 2);
                        else
                        {
                            ic = new IncomeClassification();
                            ic.Amount = decimal.Round(idIc.Amount, 2);
                            ic.ClassificationCategory = idIc.ClassificationCategory;
                            ic.ClassificationType = idIc.ClassificationType;
                            incomes.Add(ic);
                        }
                    }
                }
            }
            this.InvoiceSummary.IncomeClassification = incomes.ToArray();

            xml = xml.Replace("[invoicedetails]", detailsXml);
            xml = xml.Replace("[invoicesummary]", this.InvoiceSummary.AsXml());
            return xml;
        }
    }
}

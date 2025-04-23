using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Exedron.MyData.Interfaces;

namespace Exedron.MyData.InvoiceModels
{
    public class InvoiceSummary : Interfaces.IInvoiceSummary
    {
        public decimal TotalNetValue { get; set; }
        public decimal TotalVATAmount { get; set; }
        public decimal TotalWithheldAmount { get; set; }
        public decimal TotalFeesAmount { get; set; }
        public decimal TotalStampDutyAmount { get; set; }
        public decimal TotalOtherTaxesAmount { get; set; }
        public decimal TotalDeductionsAmount { get; set; }
        public decimal TotalGrossValue { get; set; }
        public IIncomeClassification[] IncomeClassification { get; set; }
        public IExpenseClassification[] ExpenseClassification { get; set; }
        

        public string AsXml()
        {
            string xml = Properties.Resources.InvoiceSummary;
            xml = xml.Replace("[totaldeductionsamount]", RequestHelpers.ToString(this.TotalDeductionsAmount));
            xml = xml.Replace("[totalfeesamount]", RequestHelpers.ToString(this.TotalFeesAmount));
            xml = xml.Replace("[totalgrossvalue]", RequestHelpers.ToString(this.TotalGrossValue));
            xml = xml.Replace("[totalnetvalue]", RequestHelpers.ToString(this.TotalNetValue));
            xml = xml.Replace("[totalothertaxesamount]", RequestHelpers.ToString(this.TotalOtherTaxesAmount));
            xml = xml.Replace("[totalstampdutyamount]", RequestHelpers.ToString(this.TotalStampDutyAmount));
            xml = xml.Replace("[totalvatamount]", RequestHelpers.ToString(this.TotalVATAmount));
            xml = xml.Replace("[totalwithheldamount]", RequestHelpers.ToString(this.TotalWithheldAmount));
            string clXml = "";
            foreach(var ic in this.IncomeClassification)
            {
                if(clXml == "")
                    clXml = ic.AsXml();
                else
                    clXml = clXml + "\r\n" + ic.AsXml();
            }
            xml = xml.Replace("[incomeclassification]", clXml);
            return xml;
        }
    }
}

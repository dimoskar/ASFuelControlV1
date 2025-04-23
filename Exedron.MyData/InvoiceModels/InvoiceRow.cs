using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Exedron.MyData.Interfaces;

namespace Exedron.MyData.InvoiceModels
{
    public class InvoiceRow : IInvoiceRowType
    {
        public int LineNumber { get; set; }
        public decimal Quantity { get; set; }
        public MeasurementUnitEnum MeasurementUnit { get; set; }
        public InvoiceDetailTypeEnum InvoiceDetailType { get; set; }
        public decimal NetValue { get; set; }
        public VATCategoryEnum VATCategory { get; set; }
        public decimal VATAmount { get; set; }
        public VATExemptionCategoryEnum VATExemptionCategory { get; set; }
        public IShipType Conduct { get; set; }
        public bool DiscountOption { get; set; }
        public decimal WithheldAmount { get; set; }
        public WithheldPercentCategoryEnum WithheldPercentCategory { get; set; }
        public decimal StampDutyAmount { get; set; }
        public StampDutyPercentCategoryEnum StampDutyPercentCategory { get; set; }
        public decimal FeesAmount { get; set; }
        public FeesPercentCategoryEnum FeesPercentCategory { get; set; }
        public decimal OtherTaxesAmount { get; set; }
        public OtherTaxesCategoryEnum OtherTaxesCategory { get; set; }
        public decimal DeductionsAmount { get; set; }
        public string LineComments { get; set; }
        public string ItemDescription { get; set; }
        public IIncomeClassification IncomeClassification { get; set; }
        public IIncomeClassification[] IncomeClassifications { get; set; }
        public IExpenseClassification ExpenseClassification { get; set; }

        public string AsXml()
        {
            string xml = Properties.Resources.InvoiceDetails;
            xml = xml.Replace("[deductionsamount]", RequestHelpers.ToString(this.DeductionsAmount));
            xml = xml.Replace("[discountoption]", this.DiscountOption.ToString());
            if (this.FeesPercentCategory == FeesPercentCategoryEnum.None)
            {
                xml = xml.Replace("[feespercentcategory]", "");
                xml = xml.Replace("[feesamount]", "");
            }
            else
            {
                xml = xml.Replace("[feespercentcategory]", ((int)this.FeesPercentCategory).ToString());
                xml = xml.Replace("[feesamount]", RequestHelpers.ToString(this.FeesAmount));
            }

            if (this.OtherTaxesCategory == OtherTaxesCategoryEnum.None)
            {
                xml = xml.Replace("[othertaxescategory]", "");
                xml = xml.Replace("[othertaxesamount]", "");
            }
            else
            {
                xml = xml.Replace("[othertaxescategory]", ((int)this.OtherTaxesCategory).ToString());
                xml = xml.Replace("[othertaxesamount]", RequestHelpers.ToString(this.OtherTaxesAmount));
            }
            if (this.StampDutyPercentCategory == StampDutyPercentCategoryEnum.None)
            {
                xml = xml.Replace("[stampdutypercentcategory]", "");
                xml = xml.Replace("[stampdutyamount]", "");
            }
            else
            {
                xml = xml.Replace("[stampdutypercentcategory]", ((int)this.StampDutyPercentCategory).ToString());
                xml = xml.Replace("[stampdutyamount]", RequestHelpers.ToString(this.StampDutyAmount));
            }
            if (this.VATExemptionCategory == VATExemptionCategoryEnum.None)
                xml = xml.Replace("<vatExemptionCategory>[vatexemptioncategory]</vatExemptionCategory>", "");
            else
                xml = xml.Replace("[vatexemptioncategory]", ((int)this.VATExemptionCategory).ToString());
            if (this.InvoiceDetailType == InvoiceDetailTypeEnum.None)
                xml = xml.Replace("[invoicedetailtype]", "");
            else
                xml = xml.Replace("[invoicedetailtype]", ((int)this.InvoiceDetailType).ToString());

            if (string.IsNullOrEmpty(this.LineComments))
                xml = xml.Replace("[linecomments]", "");
            else
                xml = xml.Replace("[linecomments]", this.LineComments);

            
            xml = xml.Replace("[linenumber]", this.LineNumber.ToString());
            if(this.MeasurementUnit != MeasurementUnitEnum.None)
                xml = xml.Replace("[measurementunit]", ((int)this.MeasurementUnit).ToString());
            else
                xml = xml.Replace("<measurementUnit>[measurementunit]</measurementUnit>", "");

            xml = xml.Replace("[netvalue]", RequestHelpers.ToString(this.NetValue));
            if (this.MeasurementUnit != MeasurementUnitEnum.None)
                xml = xml.Replace("[quantity]", RequestHelpers.ToString(this.Quantity));
            else
                xml = xml.Replace("<quantity>[quantity]</quantity>", "");
            xml = xml.Replace("[vatamount]", RequestHelpers.ToString(this.VATAmount));
            xml = xml.Replace("[vatcategory]", ((int)this.VATCategory).ToString());

            if(string.IsNullOrEmpty(ItemDescription))
                xml = xml.Replace("<itemDescr>[itemDescr]</itemDescr>", "");
            else
                xml = xml.Replace("[itemDescr]", this.ItemDescription);
            

            if (this.IncomeClassifications != null && this.IncomeClassifications.Length > 0)
            {
                string xmlic = "";
                foreach (var ic in this.IncomeClassifications)
                {
                    if (xmlic != "")
                        xmlic = xmlic + "\r\n" + ic.AsXml();
                    else
                        xmlic = ic.AsXml();
                }
                xml = xml.Replace("[incomeclassification]", xmlic);
            }
            else
                xml = xml.Replace("[incomeclassification]", this.IncomeClassification.AsXml());
            return xml;
        }
    }
}

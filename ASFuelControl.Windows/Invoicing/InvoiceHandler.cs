using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.Windows.Invoicing
{
    public class InvoiceHandler
    {
        public Exedron.Invoicing.Models.Invoice CreateInvoice(Data.Invoice dbInvoice)
        {
            Exedron.Invoicing.Models.Invoice invoice = new Exedron.Invoicing.Models.Invoice();

            string paymentTerms = GetPaymentTerms(dbInvoice);
            CountryModeEnum countryMode = GetCountryMode(dbInvoice);
            bool vatExemption = false;
            if (dbInvoice.Trader != null && dbInvoice.Trader.VatExemption.HasValue)
                vatExemption = dbInvoice.Trader.VatExemption.Value;

            invoice.creationDate = DateTime.Now;
            invoice.invoiceCurrencyCode = "EUR";
            invoice.invoiceIssueDate = dbInvoice.TransactionDate;
            invoice.paymentTerms = paymentTerms;
            invoice.serialNumber = dbInvoice.Number.ToString();
            invoice.seriesNumber = string.IsNullOrEmpty(dbInvoice.Series) ? "0" : dbInvoice.Series;
            invoice.invoiceTypeCode = "380";

            invoice.aadeData = new Exedron.Invoicing.Models.AadeData();
            invoice.aadeData.aadeInvoiceTypeCode = GetInvoiceTypeCode(dbInvoice, countryMode);
            invoice.aadeData.aadeFuelInvoice = true;
            invoice.aadeData.aadeVatPaymentSuspension = vatExemption;
            int classificationType = GetClassificationType(dbInvoice, countryMode);
            if (classificationType > -1)
            {
                invoice.aadeData.incomeClassifications = new Exedron.Invoicing.Models.Incomeclassification[]
                {
                new Exedron.Invoicing.Models.Incomeclassification()
                {
                    classificationType = GetClassificationType(dbInvoice, countryMode),
                    classificationCategory = GetClassificationCategory(dbInvoice),
                    amount = 100
                }
                };
            }

            invoice.aadeData.invoiceRowTypes = new Exedron.Invoicing.Models.Invoicerowtype[]
            {
                new Exedron.Invoicing.Models.Invoicerowtype()
                {
                    vatAmount = 24,
                    lineNumber = 1,
                    netValue = 100,
                    vatCategory = 1,
                    incomeClassification = classificationType == -1 ? null : new Exedron.Invoicing.Models.Incomeclassification[]
                    {
                        new Exedron.Invoicing.Models.Incomeclassification()
                        {
                            classificationType = 8,
                            classificationCategory = GetClassificationCategory(dbInvoice),
                            amount = 100
                        }
                    }
                }
            };

            if (dbInvoice.Trader != null)
            {
                var trader = dbInvoice.Trader;
                invoice.buyer = new Exedron.Invoicing.Models.Buyer();
                invoice.buyer.buyerVatIdentifier = trader.TaxRegistrationNumber;
                invoice.buyer.buyerName = trader.Name;
                invoice.buyer.buyerBranch = 0;
                invoice.buyer.buyerPostalAddress = new Exedron.Invoicing.Models.Buyerpostaladdress();
                invoice.buyer.buyerPostalAddress.buyerAddressLine1 = trader.Address;
                invoice.buyer.buyerPostalAddress.buyerCity = trader.City;
                invoice.buyer.buyerPostalAddress.buyerPostCode = trader.ZipCode;
                invoice.buyer.buyerPostalAddress.buyerCountryCode = trader.Country;
            }
            invoice.docTotal = new Exedron.Invoicing.Models.Doctotal();
            if (classificationType > 0)
            {
                invoice.docTotal.amountDueForPayment = (float)dbInvoice.TotalAmount.Value;
                invoice.docTotal.invoiceLinesNetAmountSum = (float)dbInvoice.NettoAmount.Value;
                invoice.docTotal.invoiceTotalAmountWithVat = (float)dbInvoice.TotalAmount.Value;
                invoice.docTotal.invoiceTotalVatAmount = (float)dbInvoice.VatAmount.Value;
                invoice.docTotal.invoiceTotalWithoutVat = (float)(dbInvoice.TotalAmount.Value - dbInvoice.VatAmount.Value);
                invoice.docTotal.paidAmount = (new int[] { 1, 2 }).Contains(dbInvoice.PaymentType.Value) ? (float)dbInvoice.TotalAmount.Value : 0;
                invoice.docTotal.roundingAmount = 0;
            }
            else
            {
                invoice.docTotal.amountDueForPayment = 0;
                invoice.docTotal.invoiceLinesNetAmountSum = 0;
                invoice.docTotal.invoiceTotalAmountWithVat = 0;
                invoice.docTotal.invoiceTotalVatAmount = 0;
                invoice.docTotal.invoiceTotalWithoutVat = 0;
                invoice.docTotal.paidAmount = 0;
                invoice.docTotal.roundingAmount = 0;
            }
            invoice.docTotal.aadeDocTotals = new Exedron.Invoicing.Models.AadeDocTotals()
            {
                aadeTotalDeductionsAmount = 0,
                aadeTotalFeesAmount = 0,
                aadeTotalGrossValue = invoice.docTotal.invoiceTotalAmountWithVat,
                aadeTotalNetValue = invoice.docTotal.invoiceTotalWithoutVat,
                aadeTotalOtherTaxesAmount = 0,
                aadeTotalStampDutyAmount = 0,
                aadeTotalVatAmount = invoice.docTotal.invoiceTotalVatAmount,
                aadeTotalWitheldAmount = 0
            };

            Data.CompanyData companyData = new Data.CompanyData();

            invoice.seller = new Exedron.Invoicing.Models.Seller();
            invoice.seller.sellerName = companyData.CompanyName;
            invoice.seller.sellerPostalAddress = new Exedron.Invoicing.Models.Sellerpostaladdress();
            invoice.seller.sellerPostalAddress.sellerAddressLine1 = companyData.CompanyMainAddress;
            //invoice.seller.sellerPostalAddress.sellerAddressLine2 = "Αμπελόκηποι";
            invoice.seller.sellerPostalAddress.sellerCity = companyData.CompanyCity;
            invoice.seller.sellerPostalAddress.sellerCountryCode = "GR";
            //invoice.seller.sellerPostalAddress.sellerCountrySubdivision = "Αττική";
            invoice.seller.sellerPostalAddress.sellerPostCode = companyData.CompanyPostalCode;
            invoice.seller.sellerVatIdentifier = companyData.CompanyTIN;
            invoice.seller.branch = 0;

            if (classificationType > 0)
            {
                var vatBreakDown = new Exedron.Invoicing.Models.Vatbreakdown();
                vatBreakDown.aadeVatData = new Exedron.Invoicing.Models.Aadevatdata();
                vatBreakDown.aadeVatData.aadeVatCategory = 1;
                vatBreakDown.categoryCode = "1";
                vatBreakDown.categoryRate = 24;
                vatBreakDown.categoryTaxableAmount = 100;
                vatBreakDown.categoryTaxAmount = 24;
                vatBreakDown.exemptionReasonCode = null;
                vatBreakDown.exemptionReasonText = null;
                invoice.vatBreakdowns = new Exedron.Invoicing.Models.Vatbreakdown[] { vatBreakDown };
            }

            Exedron.Invoicing.Models.Invoiceline invLine = new Exedron.Invoicing.Models.Invoiceline();
            invLine.discountAmount = 0;
            invLine.discountPercentage1 = 0;
            invLine.discountTotalAmount = 0;
            invLine.invoicedQuantity = 100;
            invLine.invoicedQuantityUnits = "LTR";
            invLine.lineNumber = 1;
            invLine.lineVatInfo = new Exedron.Invoicing.Models.Linevatinfo();
            invLine.lineVatInfo.vatRate = 24;
            invLine.lineVatInfo.vatAmount = 24;
            invLine.lineVatInfo.vatCategoryCode = "1";
            invLine.lineVatInfo.aadeVatData = new Exedron.Invoicing.Models.Aadevatdata1();
            invLine.lineVatInfo.aadeVatData.aadeVatCategory = 1;
            invLine.netAmount = 100;
            invLine.priceDetails = new Exedron.Invoicing.Models.Pricedetails();
            invLine.priceDetails.itemGrossPrice = 124;
            invLine.priceDetails.itemNetPrice = 100;
            invLine.priceDetails.itemPriceBaseQuantity = 1;
            invLine.priceDetails.itemPriceBaseQuantityUnitsCode = "LTR";
            invLine.priceDetails.itemPriceDiscount = 0;

            invoice.invoiceLines = new Exedron.Invoicing.Models.Invoiceline[] { invLine };

            invoice.paymentMethods = new Exedron.Invoicing.Models.PaymentMethod[]
            {
                new Exedron.Invoicing.Models.PaymentMethod()
                {
                    amount = invoice.docTotal.invoiceTotalAmountWithVat,
                    type = 5
                }
            };
            return null;
        }
        private string GetPaymentTerms(Data.Invoice dbInvoice)
        {
            string paymentTerms = "Μετρητά";
            if (dbInvoice.PaymentType.HasValue)
            {
                switch (dbInvoice.PaymentType.Value)
                {
                    case 0:
                        paymentTerms = "Επί Πιστώσει"; break;
                    case 1:
                        paymentTerms = "Μετρητά"; break;
                    case 2:
                        paymentTerms = "Κάρτα"; break;
                    case 3:
                        paymentTerms = "Κάρτα Στόλου"; break;
                }
            }
            return paymentTerms;
        }
        private string GetInvoiceTypeCode(Data.Invoice dbInvoice, CountryModeEnum countryMode)
        {
            int officialInvType = dbInvoice.InvoiceType.OfficialEnumerator;
            switch (officialInvType)
            {
                case 158:
                case 160:
                    return "15.3";
                case 165:
                    if (countryMode == CountryModeEnum.Greece)
                        return "1.1";
                    else if(countryMode == CountryModeEnum.Eu)
                        return "1.2";
                    else if (countryMode == CountryModeEnum.NonEu)
                        return "1.3";
                    return "1.1";
                case 168:
                    return "5.1";
                case 173:
                    return "11.1";
                case 175:
                    return "11.4";
                case 181:
                    break;
                case 215:
                    if(dbInvoice.ParentInvoiceRelations != null && dbInvoice.ParentInvoiceRelations.Count > 0)
                    {
                        var parentInvoiceType = dbInvoice.ParentInvoiceRelations.Where(i => i.RelationType == (int)Common.Enumerators.InvoiceRelationTypeEnum.Cancel).First().ParentInvoice.InvoiceType;
                        List<int> wholesaleTypes = new List<int>();
                        wholesaleTypes.AddRange(new int[] { 158, 160, 165, 222 });
                        if (wholesaleTypes.Contains(parentInvoiceType.OfficialEnumerator))
                            return "5.1";
                        return "11.4";
                    }
                    return "11.4";
                case 222:
                    return "1.1";
                case 316:
                    break;
            }
            return "";
        }
        private int GetClassificationCategory(Data.Invoice dbInvoice)
        {
            if (dbInvoice.InvoiceType.OfficialEnumerator == 158)
                return 11;
            return 0;
        }
        private int GetClassificationType(Data.Invoice dbInvoice, CountryModeEnum countryMode)
        {
            int officialInvType = dbInvoice.InvoiceType.OfficialEnumerator;
            switch (officialInvType)
            {
                case 158:
                    return -1;
                case 173:
                case 175:
                    return 8;
                case 165:
                case 168:
                case 222:
                    if (countryMode == CountryModeEnum.Greece)
                        return 6;
                    else if (countryMode == CountryModeEnum.Eu)
                        return 10;
                    else if (countryMode == CountryModeEnum.NonEu)
                        return 11;
                    return 6;
                default:
                    return -1;
            }
        }
        private CountryModeEnum GetCountryMode(Data.Invoice invoice)
        {
            string country = "GR";
            if (invoice.Trader != null && !string.IsNullOrEmpty(invoice.Trader.Country))
            {
                country = invoice.Trader.Country;
            }
            else
            {
                if (invoice.Trader != null && !char.IsDigit(invoice.Trader.TaxRegistrationNumber[0]))
                {
                    country = "";
                }
            }
            if (country == "GR")
                return CountryModeEnum.Greece;
            else
            {
                //vatExemption = true;
                if (Data.Implementation.Country.IsEuCountry(country))
                    return CountryModeEnum.Eu;
                else
                    return CountryModeEnum.NonEu;
            }
        }
    }

    public enum CountryModeEnum
    {
        Greece = 0,
        Eu = 1,
        NonEu = 2
    }
}


using Exedron.Invoicing.Core.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ProviderTrader = Exedron.Invoicing.Core.Models.Trader;

namespace Exedron.Invoicing.Core
{
    public sealed class InvoiceParent
    {
        public InvoiceInput Invoice { get; set; }
        public TransmissionEvidence Transmission { get; set; }
    }

    // No database writes or provider calls. Also used by the regression tests.
    public sealed class InvoiceBuilder : IInvoiceBuilder
    {
        private readonly IInvoiceTypeResolver resolver;
        public InvoiceBuilder(IInvoiceTypeResolver resolver) { this.resolver = resolver ?? throw new ArgumentNullException(nameof(resolver)); }
        public InvoiceModel Build(InvoiceBuildRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            return BuildCore(request.Invoice, request.Company, request.Country, request.Parents,
                request.RequireIssuedIdentity, request.Movement, request.Delivery, request.SaleCorrelations);
        }
        public CountryInput ResolveCountry(IEnumerable<CountryInput> countries, string value)
        {
            string key = Clean(value);
            Require(key != null, "The customer's country is missing.");
            var matches = countries.Where(c => string.Equals(Clean(c.CountryCode), key, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length == 0)
                matches = countries.Where(c => string.Equals(Clean(c.DisplayMember), key, StringComparison.OrdinalIgnoreCase)).ToArray();
            Require(matches.Length == 1, "The customer's country is unknown or ambiguous: " + key);
            Require(Clean(matches[0].CountryCode) != null, "The customer's country has no ISO code.");
            return matches[0];
        }

        public InvoiceModel Build(InvoiceInput invoice, CompanySettings company, CountryInput country,
            IEnumerable<InvoiceParent> parents, bool requireIssuedIdentity = true, MovementContext tanker = null)
        {
            return BuildCore(invoice, company, country, parents, requireIssuedIdentity, tanker, null, null);
        }

        private InvoiceModel BuildCore(InvoiceInput invoice, CompanySettings company, CountryInput country,
            IEnumerable<InvoiceParent> parents, bool requireIssuedIdentity, MovementContext tanker,
            DeliveryContext dispatch, IEnumerable<InvoiceParent> saleCorrelations)
        {
            var linked = (parents ?? Enumerable.Empty<InvoiceParent>()).ToArray();
            string invoiceType = resolver.Resolve(invoice, country, linked, tanker);
            Require(company != null && Clean(company.CompanyTaxId) != null, "The company VAT number is missing.");
            if (requireIssuedIdentity)
                Require(invoice.Number > 0 && invoice.PrintedDateTime != default(DateTime), "The invoice number or issue date is missing.");
            var type = invoice.InvoiceSeries.InvoiceType;
            bool retail = type.IsRetail;
            bool movementOnly = invoiceType == "9.1" || invoiceType == "9.3" || invoiceType == "9.2" || invoiceType == "10.1";
            Require((invoiceType != "9.2" && invoiceType != "10.1") || tanker != null,
                "This document must be created through the tanker route workflow.");
            bool delivery = movementOnly || invoice.InvoiceSeries.IsDeliveryNote;
            Require(dispatch == null || tanker == null, "Choose ordinary delivery or tanker movement, not both.");
            Require(dispatch == null || delivery, "Delivery details require a delivery document or an invoice with delivery.");
            Require(!delivery || type.MyDataDocumentKind != (int)MyDataDocumentKind.Services,
                "A service invoice cannot also be a delivery note. Check the series configuration.");
            var customer = invoice.Trader;
            string countryCode = country.CountryCode.Trim().ToUpperInvariant();
            Require(retail || Clean(customer.TaxRegistrationId) != null,
                "The customer's VAT number is required for this document.");
            int branch = 0;
            Require(Clean(company.CompanyBranch) == null || int.TryParse(company.CompanyBranch, out branch), "The company branch must be a non-negative integer.");
            Require(branch >= 0 && customer.Branch >= 0, "Company and customer branches must be non-negative.");

            var issuerAddress = MapAddress(company.CompanyAddress, company.CompanyCity, company.CompanyZipCode);
            var customerAddress = MapAddress(customer.Address, customer.City, customer.PostalCode);
            if (dispatch != null)
            {
                Require(Clean(dispatch.Vehicle) != null && dispatch.Departure != default(DateTime),
                    "The delivery vehicle or dispatch date is missing.");
                issuerAddress = dispatch.LoadingAddress;
                customerAddress = dispatch.DeliveryAddress;
            }
            if (tanker != null)
            {
                issuerAddress = tanker.LoadingAddress;
                customerAddress = tanker.DeliveryAddress;
                Require(tanker.Role == "Load" ? invoiceType == "9.2" : tanker.Role == "Return" ? invoiceType == "10.1" : invoiceType == "9.1" || invoiceType == "9.3" || invoiceType == "1.1" || invoiceType == "11.1",
                    "The tanker document series does not match its role.");
                Require(tanker.Role != "Load" || Clean(customer.TaxRegistrationId) == "000000000", "A 9.2 route requires recipient VAT number 000000000.");
                Require(tanker.Role == "Load" || tanker.ParentMark.GetValueOrDefault() > 0, "The parent tanker document has no accepted MARK.");
                Require(!string.IsNullOrWhiteSpace(tanker.Vehicle) && tanker.Departure != default(DateTime), "The tanker or dispatch date is missing.");
            }
            if (delivery)
            {
                ValidateAddress(issuerAddress, "Loading");
                ValidateAddress(customerAddress, "Delivery");
            }
            else if (!retail && countryCode != "GR") ValidateAddress(customerAddress, "Customer");
            var model = new InvoiceModel
            {
                Issuer = new ProviderTrader
                {
                    Country = "GR", VATNumber = company.CompanyTaxId.Trim(), Branch = branch,
                    Name = Clean(company.CompanyName), TaxOffice = Clean(company.CompanyTaxOffice),
                    Email = Clean(company.CompanyEmail), Phone = Clean(company.CompanyPhone), Job = Clean(company.CompanyJob),
                    Address = MapAddress(company.CompanyAddress, company.CompanyCity, company.CompanyZipCode)
                },
                CounterPart = Clean(customer.TaxRegistrationId) == null ? null : new ProviderTrader
                {
                    Name = Clean(customer.LastName + " " + customer.FirstName), Country = countryCode,
                    VATNumber = customer.TaxRegistrationId.Trim(), TaxOffice = Clean(customer.TaxRegistrationOffice),
                    Branch = customer.Branch, Email = Clean(customer.Email), Phone = Clean(customer.Phone), Job = Clean(customer.Job),
                    Address = MapAddress(customer.Address, customer.City, customer.PostalCode)
                },
                InvoiceHeader = new InvoiceHeader
                {
                    AA = invoice.Number.ToString(CultureInfo.InvariantCulture), Currency = "EUR",
                    IssueDate = invoice.PrintedDateTime, InvoiceType = invoiceType,
                    Series = Clean(invoice.Series) ?? Clean(invoice.InvoiceSeries.Series) ?? "0",
                    // isDeliveryNote denotes an invoice with delivery, not a standalone 9.3.
                    DeliveryNote = !movementOnly && invoice.InvoiceSeries.IsDeliveryNote,
                    VATPaymentSuspension = invoice.VATPaymentSuspension
                }
            };
            var sales = (saleCorrelations ?? Enumerable.Empty<InvoiceParent>()).ToArray();
            if (sales.Length > 0)
            {
                Require(invoiceType == "1.1" && tanker == null,
                    "Explicit sale correlations are supported only for ordinary domestic wholesale goods invoices.");
                Require(invoice.InvoiceId != Guid.Empty, "A correlated sale requires a saved invoice ID.");
                foreach (var parent in sales)
                {
                    Require(parent?.Invoice?.InvoiceSeries?.InvoiceType != null && parent.Invoice.Trader != null &&
                        parent.Invoice.InvoiceId != Guid.Empty && parent.Invoice.InvoiceId != invoice.InvoiceId,
                        "A sale correlation must identify a different original document.");
                    Require(parent.Invoice.InvoiceSeries.InvoiceType.InvoiceTypeMode == InvoiceMode.Normal &&
                        !parent.Invoice.InvoiceSeries.InvoiceType.IsRetail,
                        "A sale correlation must reference a normal wholesale document.");
                    int parentKind = parent.Invoice.InvoiceSeries.InvoiceType.MyDataDocumentKind;
                    Require(parentKind == (int)MyDataDocumentKind.Goods || parentKind == (int)MyDataDocumentKind.Services ||
                        parentKind == (int)MyDataDocumentKind.Delivery,
                        "A sale correlation requires an original sale or ordinary delivery document.");
                    Require(Clean(parent.Invoice.Trader.TaxRegistrationId) == Clean(customer.TaxRegistrationId),
                        "A sale correlation must have the same customer VAT number.");
                    Require(parent.Transmission != null && parent.Transmission.Status == 3 &&
                        parent.Transmission.Mark.GetValueOrDefault() > 0 && !parent.Transmission.CancelationMark.HasValue &&
                        !parent.Transmission.CanceledByMark.HasValue,
                        "Every sale correlation requires an accepted, uncancelled MARK.");
                }
                model.InvoiceHeader.CorrelatedInvoices = sales.Select(p => p.Transmission.Mark.Value).Distinct().ToArray();
            }
            if (dispatch != null)
            {
                model.InvoiceHeader.VehicleNumber = dispatch.Vehicle.Trim();
                model.InvoiceHeader.DispatchDateTime = dispatch.Departure;
            }
            if (invoiceType == "5.1")
                model.InvoiceHeader.CorrelatedInvoices = linked.Select(p => p.Transmission.Mark.Value).Distinct().ToArray();
            if (tanker != null)
            {
                if (invoiceType == "10.1" && tanker.Role == "Return")
                    ReceivingNotePurposes.Set(model.InvoiceHeader, ReceivingNotePurposes.UndeliveredReturn);
                // A 10.1 records receipt, not the start of a new dispatch.
                if (invoiceType != "10.1")
                {
                    model.InvoiceHeader.VehicleNumber = tanker.Vehicle;
                    model.InvoiceHeader.DispatchDateTime = tanker.Departure;
                }
                // Retail links remain in the route ledger; correlatedInvoices is not accepted for 11.x.
                if (tanker.ParentMark.HasValue && !retail)
                    model.InvoiceHeader.CorrelatedInvoices = new[] { tanker.ParentMark.Value };
            }
            if (delivery)
            {
                // Quantitative receipts do not declare a new movement purpose.
                model.InvoiceHeader.MovePurpose = invoiceType == "10.1" ? (MovePurposeEnum)0 :
                    type.InvoiceTypeMode == InvoiceMode.Credit ? (MovePurposeEnum)5 : MovePurposeEnum.Sales;
                if (invoiceType != "10.1")
                    model.InvoiceHeader.OtherDeliveryNoteHeader = new OtherDeliveryNoteHeader
                    {
                        LoadingAddress = CopyAddress(issuerAddress), DeliveryAddress = CopyAddress(customerAddress),
                        StartShippingBranch = branch, CompleteShippingBranch = customer.Branch
                    };
            }

            var lines = invoice.InvoiceLines.ToArray();
            Require(lines.Length > 0, "The invoice has no lines.");
            if (tanker != null)
            {
                Require(lines.Length == 1, "A tanker delivery requires one Heating Diesel line.");
                Require(lines.Sum(l => l.Quantity) == tanker.Quantity, "The invoice quantity does not match the tanker allocation.");
                foreach (var line in lines)
                    Require((line.OfficialFuelCode ?? line.SalesItem?.OfficialFuelCode) == 30 && line.MeasurementUnit?.OfficialType == 3, "Tanker deliveries require Heating Diesel (fuel code 30), in litres.");
            }
            var details = new List<InvoiceDetail>();
            foreach (var line in lines)
            {
                string label = "Line " + (details.Count + 1).ToString(CultureInfo.InvariantCulture) + ": ";
                Require(line.SalesItem != null, label + "the item is missing.");
                Require(line.NetAmount.HasValue && line.VatAmount.HasValue, label + "net or VAT amount is missing.");
                Require(line.NetAmount >= 0 && line.VatAmount >= 0 && line.DiscountAmount.GetValueOrDefault() >= 0 &&
                    line.DiscountAmount.GetValueOrDefault() <= line.NetAmount, label + "amounts must be non-negative and discount cannot exceed net amount.");
                Require(line.MeasurementUnit != null, label + "the measurement unit is missing.");
                int unit = line.MeasurementUnit.OfficialType;
                Require(Enum.IsDefined(typeof(MeasurementUnitEnum), unit) &&
                    (unit != 0 || type.MyDataDocumentKind == (int)MyDataDocumentKind.Services), label + "the measurement unit is not supported by the provider library.");
                Require(line.Quantity > 0, label + "quantity must be positive.");
                Require(!delivery || !string.IsNullOrWhiteSpace(line.ItemDescription), label + "itemDescr is required for a delivery invoice.");
                decimal net = line.NetAmount.Value - line.DiscountAmount.GetValueOrDefault();
                var detail = new InvoiceDetail
                {
                    LineNumber = details.Count + 1, ItemDescription = line.ItemDescription, ItemCode = line.ItemCode,
                    MeasurementUnit = (MeasurementUnitEnum)unit, Quantity = line.Quantity,
                    NetValue = net, VATAmount = line.VatAmount.Value
                };
                if (movementOnly)
                {
                    Require(net == 0 && detail.VATAmount == 0, label + "standalone delivery notes require zero financial values; use a goods invoice with delivery for a sale.");
                    detail.VATCategory = VATCategoryEnum.NoVATEntry;
                    detail.IncomeClassification = new IncomeClassification { ClassificationCategory = "category3", Amount = 0 };
                }
                else
                {
                    detail.VATCategory = ResolveVatCategory(line.VATPercentage, label);
                    if (line.VATPercentage == 0)
                    {
                        int exemption = line.SalesItem.VATExcemptionArticle.GetValueOrDefault();
                        Require(exemption > 0 && Enum.IsDefined(typeof(VATExemptionCategoryEnum), exemption),
                            label + "zero VAT requires an explicit exemption reason supported by the provider library.");
                        Require(detail.VATAmount == 0, label + "a zero VAT rate cannot have a VAT amount.");
                        detail.VATExemptionCategory = (VATExemptionCategoryEnum)exemption;
                        detail.VATInvoicingCategory = VATInvoicingCategoryEnum.Excemption;
                    }
                    Require(line.SalesItem.SalesItemType >= 0 && line.SalesItem.SalesItemType <= 2, label + "the item's income classification is not supported.");
                    detail.IncomeClassification = new IncomeClassification
                    {
                        Amount = net,
                        ClassificationCategory = "category1_" + (line.SalesItem.SalesItemType + 1).ToString(CultureInfo.InvariantCulture),
                        ClassificationType = countryCode == "GR" ? (retail ? "E3_561_003" : "E3_561_001")
                            : country.IsEu ? "E3_561_005" : "E3_561_006"
                    };
                }
                int? officialFuelCode = line.OfficialFuelCode ?? line.SalesItem.OfficialFuelCode;
                string fuelCode = officialFuelCode.HasValue ? officialFuelCode.Value.ToString(CultureInfo.InvariantCulture) : null;
                // Preserve the established fallback for legacy fuel items.
                if (fuelCode == null && !string.IsNullOrEmpty(line.SalesItem.FactoryCode) && line.SalesItem.FactoryCode.Length == 2)
                    fuelCode = line.SalesItem.FactoryCode;
                fuelCode = InvoiceValidation.TransmissionFuelCode(fuelCode);
                if (!string.IsNullOrEmpty(fuelCode))
                {
                    model.InvoiceHeader.FuelInvoice = true;
                    detail.FuelCode = fuelCode;
                    // Keep the invoice's saved supply number even if the trader is edited later.
                    var supplyCode = Clean(invoice.SupplyCode) ?? Clean(customer.StringField1);
                    Require(supplyCode == null || model.CounterPart != null,
                        "The customer's VAT number is required to transmit the fuel invoice's electricity supply code.");
                    if (model.CounterPart != null)
                        model.CounterPart.SupplyAccountNo = supplyCode;
                }
                details.Add(detail);
                if (tanker != null) MovementQuantities.Set(detail, line.Quantity);
            }
            decimal totalNet = details.Sum(d => d.NetValue);
            decimal totalVat = details.Sum(d => d.VATAmount);
            Require(invoice.NettoAmount == lines.Sum(l => l.NetAmount.Value) &&
                invoice.DiscountAmount == lines.Sum(l => l.DiscountAmount.GetValueOrDefault()) &&
                invoice.NettoAmount - invoice.DiscountAmount == totalNet && invoice.VATAmount == totalVat &&
                invoice.TotalAmount == totalNet + totalVat, "Invoice totals do not agree with the line totals. Correct the invoice before submitting it.");
            model.InvoiceDetails = details.ToArray();
            model.InvoiceSummary = new InvoiceSummary { TotalNetValue = totalNet, TotalVATAmount = totalVat, TotalGrossValue = invoice.TotalAmount };
            model.PaymentMethods = movementOnly ? new PaymentMethod[0] : new[]
            {
                new PaymentMethod { Amount = invoice.TotalAmount, Type = ResolvePaymentType(invoice.PaymentType, company.ProviderBankPaymentType) }
            };
            InvoiceValidation.Validate(model);
            return model;
        }

        private static VATCategoryEnum ResolveVatCategory(decimal rate, string label)
        {
            switch (rate)
            {
                case 24: return VATCategoryEnum.VAT24;
                case 13: return VATCategoryEnum.VAT13;
                case 6: return VATCategoryEnum.VAT06;
                case 17: return VATCategoryEnum.VAT17;
                case 9: return VATCategoryEnum.VAT09;
                case 4: return VATCategoryEnum.VAT04;
                case 0: return VATCategoryEnum.NoVAT;
                default: throw new ProviderInvoiceValidationException(label + "the VAT rate is not supported by the provider library.");
            }
        }

        private static PaymentMethodEnum ResolvePaymentType(InvoicePaymentType payment, int bankType)
        {
            switch (payment)
            {
                case InvoicePaymentType.Bank:
                    Require(bankType == 1 || bankType == 2 || bankType == 6,
                        "Configure the default bank-payment method in provider settings before issuing a bank-paid invoice.");
                    return (PaymentMethodEnum)bankType;
                case InvoicePaymentType.Card: return PaymentMethodEnum.POS;
                case InvoicePaymentType.IRIS: return PaymentMethodEnum.IRIS;
                case InvoicePaymentType.Cash: return PaymentMethodEnum.Cash;
                case InvoicePaymentType.Check: return PaymentMethodEnum.Check;
                case InvoicePaymentType.Credit: return PaymentMethodEnum.Credit;
                default: throw new ProviderInvoiceValidationException("The invoice payment method is not supported.");
            }
        }

        private static Address MapAddress(string street, string city, string postalCode)
        {
            var address = new Address { Street = Clean(street) ?? "", City = Clean(city), PostalCode = Clean(postalCode) };
            if (address.Street.Length > 0) address.ReplaceNumber();
            return address;
        }

        private static void ValidateAddress(Address address, string label)
        {
            Require(address != null && Clean(address.Street) != null && Clean(address.City) != null && Clean(address.PostalCode) != null,
                label + " address requires a street, city and postal code.");
        }

        private static Address CopyAddress(Address address)
        {
            return new Address { Street = address.Street, Number = address.Number, City = address.City, PostalCode = address.PostalCode };
        }

        private static string Clean(string value) { return string.IsNullOrWhiteSpace(value) ? null : value.Trim(); }
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new ProviderInvoiceValidationException(message);
        }
    }
}

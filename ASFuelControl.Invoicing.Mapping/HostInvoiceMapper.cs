using System;
using System.Globalization;
using System.Linq;
using Exedron.Invoicing.Core;
using Exedron.Invoicing.Core.Models;
using Entity = ASFuelControl.Data.Invoice;

namespace ASFuelControl.Invoicing.Mapping
{
    // Read entities while their host context is alive; the result contains no ORM objects.
    // This method never allocates a number, saves an entity or dispatches a document.
    public sealed class HostInvoiceMapper
    {
        private readonly InvoiceBuilder builder = new InvoiceBuilder(new InvoiceTypeResolver());

        // The legacy IsCancelation flag also identifies financial credit notes.
        // 215 is the special cancellation document used by the host cancellation flow.
        public static bool IsCancellationDocument(Data.InvoiceType type)
        { return type != null && type.OfficialEnumerator == 215; }

        public InvoiceBuildRequest Map(Entity source, MappingContext context)
        {
            Require(source != null && source.InvoiceId != Guid.Empty, "A saved invoice is required.");
            Require(context != null && context.Company != null && context.WholesaleCodes != null && context.Countries != null,
                "Company, country catalog and wholesale-code settings are required.");
            Require(context.Environment == FiscalEnvironment.Test || context.Environment == FiscalEnvironment.Production,
                "The issuance environment is required.");
            var company = CopyCompany(context.Company);
            var country = Country(source, context);
            var input = Identity(source, context);
            bool delivery = input.InvoiceSeries.IsDeliveryNote;
            Require(Clean(company.CompanyTaxId) != null, "Company VAT is missing.");
            Require(Clean(input.Trader.TaxRegistrationId) == null ||
                !string.Equals(Clean(input.Trader.TaxRegistrationId), Clean(company.CompanyTaxId), StringComparison.OrdinalIgnoreCase),
                "Same-VAT/self-supply documents require the separate 6.1 policy and are not enabled.");
            Require(source.InvoiceLines.Count > 0, "The invoice has no lines.");
            Require(source.InvoiceLines.All(l => l != null && l.InvoiceLineId != Guid.Empty && l.InvoiceId == source.InvoiceId) &&
                source.InvoiceLines.Select(l => l.InvoiceLineId).Distinct().Count() == source.InvoiceLines.Count,
                "Invoice lines require unique saved identities.");
            foreach (var line in source.InvoiceLines.OrderBy(l => l.InvoiceLineId))
            {
                Require(line != null && line.FuelType != null, "A line has no item.");
                var item = Item(line.FuelType, context);
                Require(item.SalesItemType >= 0 && item.SalesItemType <= 2, "Unsupported item classification.");
                Require(!item.OfficialFuelCode.HasValue || (item.OfficialFuelCode > 0 && item.SalesItemType != 2 && item.MeasurementUnit == 3),
                    "Fuel items must be goods measured in litres with a positive fuel code.");
                Require(line.VatPercentage >= 0 && line.TotalPrice >= 0 && line.VatAmount >= 0 && line.TotalPrice >= line.VatAmount &&
                    line.DiscountAmount >= 0 && line.DiscountAmountWhole >= 0, "Negative or inconsistent saved line amounts.");
                // DiscountAmount is gross; DiscountAmountWhole is the saved net discount.
                // TotalPrice and VatAmount already include the discount: never subtract it twice.
                decimal discount = line.DiscountAmountWhole;
                Require(delivery || discount == decimal.Round(line.DiscountAmount * 100m / (100m + line.VatPercentage), 2, MidpointRounding.ToEven),
                    "Saved gross and net discounts disagree; reconcile the invoice before mapping.");
                int? exemption = item.VatExemptionArticle;
                if (!delivery && line.VatPercentage == 0 && !exemption.HasValue)
                {
                    if (country.CountryCode != "GR") exemption = (int)(country.IsEu ? VATExemptionCategoryEnum.Article28 : VATExemptionCategoryEnum.Article24);
                    else if (source.Trader != null && source.Trader.VatExemption == true)
                        exemption = VatExemptionReasonCatalog.GetCodeOrDefault(source.Trader.VatExemptionReason, (int)VATExemptionCategoryEnum.Article27);
                }
                input.VATPaymentSuspension |= !delivery && country.CountryCode == "GR" &&
                    source.Trader != null && source.Trader.VatExemption == true && line.VatPercentage == 0;
                input.InvoiceLines.Add(new InvoiceLineInput
                {
                    ItemDescription = Clean(line.FuelType.Name), ItemCode = Clean(line.FuelType.Code) ?? line.FuelType.EnumeratorValue.ToString(CultureInfo.InvariantCulture),
                    OfficialFuelCode = item.OfficialFuelCode, Quantity = line.Volume,
                    MeasurementUnit = new MeasurementUnitInput { OfficialType = item.MeasurementUnit },
                    SalesItem = new SalesItemInput { SalesItemType = item.SalesItemType, VATExcemptionArticle = exemption },
                    NetAmount = delivery ? 0 : line.TotalPrice - line.VatAmount + discount,
                    DiscountAmount = delivery ? 0 : discount, VatAmount = delivery ? 0 : line.VatAmount,
                    VATPercentage = line.VatPercentage
                });
            }
            bool services = input.InvoiceLines.All(l => l.SalesItem.SalesItemType == 2);
            Require(services || input.InvoiceLines.All(l => l.SalesItem.SalesItemType != 2),
                "Mixed goods and services require an explicit document policy and are not enabled.");
            Require(!delivery || !services, "A service cannot be issued as a delivery note.");
            if (services) input.InvoiceSeries.InvoiceType.MyDataDocumentKind = (int)MyDataDocumentKind.Services;
            input.NettoAmount = input.InvoiceLines.Sum(l => l.NetAmount.Value);
            input.DiscountAmount = input.InvoiceLines.Sum(l => l.DiscountAmount.Value);
            input.VATAmount = delivery ? 0 : Required(source.VatAmount, "VAT total");
            input.TotalAmount = delivery ? 0 : Required(source.TotalAmount, "gross total");
            if (!delivery)
            {
                Require(source.DiscountAmountWhole == input.DiscountAmount &&
                    source.DiscountAmount == source.InvoiceLines.Sum(l => l.DiscountAmount), "Saved invoice discounts disagree with its lines.");
                // Older records may have net-after-discount in NettoAmount. Both documented
                // conventions are accepted only if the saved gross/VAT and line totals agree.
                decimal savedNet = Required(source.NettoAmount, "net total");
                Require(savedNet == input.NettoAmount || savedNet == input.NettoAmount - input.DiscountAmount,
                    "Saved invoice net total disagrees with its lines.");
                input.PaymentType = Payment(source.PaymentType, context.FleetCardPayment);
            }
            var linked = source.ParentInvoiceRelations.ToArray();
            bool credit = input.InvoiceSeries.InvoiceType.InvoiceTypeMode == InvoiceMode.Credit;
            Require(credit || linked.Length == 0 || context.IncludeSaleCorrelations,
                "Linked sales require an explicit correlation policy.");
            var parents = linked.Select(r =>
            {
                Require(r != null && r.ParentInvoice != null, "A linked parent invoice is missing.");
                var parent = r.ParentInvoice;
                ParentEvidence evidence;
                Require(context.Parents.TryGetValue(parent.InvoiceId, out evidence) && evidence != null &&
                    evidence.InvoiceId == parent.InvoiceId && evidence.Transmission != null, "Reconciled parent evidence is missing.");
                int branch;
                Require(int.TryParse(company.CompanyBranch ?? "0", NumberStyles.Integer, CultureInfo.InvariantCulture, out branch) &&
                    evidence.IssuerBranch == branch && string.Equals(Clean(evidence.IssuerVat), Clean(company.CompanyTaxId), StringComparison.OrdinalIgnoreCase) &&
                    evidence.Environment == context.Environment, "Parent issuer, branch or environment does not match this issuance.");
                var parentInput = Identity(parent, context);
                Require(parent.InvoiceId != Guid.Empty && parent.Number > 0 && parent.TransactionDate != default(DateTime), "Parent identity is incomplete.");
                Require(parent.InvoiceLines.Count > 0, "Load the original parent lines before resolving its document kind.");
                if (parent.InvoiceLines.Count > 0 && parent.InvoiceLines.All(l => l.FuelType != null && Item(l.FuelType, context).SalesItemType == 2))
                    parentInput.InvoiceSeries.InvoiceType.MyDataDocumentKind = (int)MyDataDocumentKind.Services;
                var t = evidence.Transmission;
                return new InvoiceParent { Invoice = parentInput, Transmission = new TransmissionEvidence
                    { Status = t.Status, Mark = t.Mark, CancelationMark = t.CancelationMark, CanceledByMark = t.CanceledByMark } };
            }).ToArray();
            var request = new InvoiceBuildRequest
            {
                Invoice = input, Company = company, Country = country,
                Parents = credit ? parents : new InvoiceParent[0],
                SaleCorrelations = !credit && parents.Length > 0 ? parents : null,
                Delivery = delivery ? CopyDelivery(context.Delivery) : null
            };
            Require(delivery || context.Delivery == null, "Dispatch details require a delivery document.");
            if (delivery) Require(request.Delivery != null, "Provide explicit ordinary delivery vehicle, departure and structured addresses.");
            Require(request.Delivery == null || request.Delivery.Departure.Kind != DateTimeKind.Utc,
                "Dispatch time must be fiscal wall-clock time, not UTC.");
            builder.Build(request); // One shared validation path before returning a dispatchable snapshot.
            return request;
        }

        public InvoiceModel Build(Entity source, MappingContext context) { return builder.Build(Map(source, context)); }

        private CountryInput Country(Entity source, MappingContext context)
        {
            string code = Clean(source.Trader == null ? null : source.Trader.Country);
            string vat = Clean(source.Trader == null ? null : source.Trader.TaxRegistrationNumber);
            if (code == null)
            {
                Require(vat == null || vat.All(c => c >= '0' && c <= '9'), "A foreign VAT number requires an explicit country.");
                code = "GR";
            }
            var result = builder.ResolveCountry(context.Countries, code);
            return new CountryInput { CountryCode = result.CountryCode.Trim().ToUpperInvariant(), DisplayMember = result.DisplayMember, IsEu = result.IsEu };
        }

        private static InvoiceInput Identity(Entity source, MappingContext context)
        {
            Require(source.InvoiceType != null, "Invoice type is missing.");
            Require(source.TransactionDate.Kind != DateTimeKind.Utc, "Issue time must be fiscal wall-clock time, not UTC.");
            var t = source.Trader;
            bool delivery = source.InvoiceType.OfficialEnumerator == 158 || source.InvoiceType.OfficialEnumerator == 159;
            return new InvoiceInput
            {
                InvoiceId = source.InvoiceId, TraderId = source.TraderId ?? (t == null ? Guid.Empty : t.TraderId),
                Number = source.Number, Series = Clean(source.Series), PrintedDateTime = source.TransactionDate,
                SupplyCode = Clean(source.SupplyNumber),
                Trader = new TraderInput { TaxRegistrationId = Clean(t == null ? null : t.TaxRegistrationNumber),
                    LastName = t == null ? null : t.Name, Branch = t == null ? 0 : t.Branch,
                    TaxRegistrationOffice = t == null ? null : t.TaxRegistrationOffice,
                    Address = t == null ? null : t.Address, City = t == null ? null : t.City, PostalCode = t == null ? null : t.ZipCode,
                    Email = t == null ? null : t.Email, Phone = t == null ? null : t.Phone1, Job = t == null ? null : t.Occupation },
                // Do not fall back to the trader's current supply number for a saved invoice.
                InvoiceSeries = new InvoiceSeriesInput { Series = Clean(source.Series), IsDeliveryNote = delivery,
                    InvoiceType = new InvoiceTypeInput { IsRetail = !context.WholesaleCodes.Contains(source.InvoiceType.OfficialEnumerator),
                        MyDataDocumentKind = (int)(delivery ? MyDataDocumentKind.Delivery : MyDataDocumentKind.Goods),
                        InvoiceTypeMode = IsCancellationDocument(source.InvoiceType) ? InvoiceMode.Cancel :
                            source.InvoiceType.IsCancelation == true ? InvoiceMode.Credit : InvoiceMode.Normal } }
            };
        }

        private static ItemMapping Item(Data.FuelType item, MappingContext context)
        {
            ItemMapping mapping;
            if (context.Items.TryGetValue(item.FuelTypeId, out mapping))
            { Require(mapping != null, "Item mapping is missing."); return mapping; }
            Require(item.EnumeratorValue > 0, "An item without a fuel code needs an explicit item mapping.");
            return new ItemMapping { SalesItemType = 0, MeasurementUnit = 3, OfficialFuelCode = item.EnumeratorValue };
        }

        private static InvoicePaymentType Payment(int? value, InvoicePaymentType? fleet)
        {
            switch (value)
            {
                case 0: return InvoicePaymentType.Credit;
                case 1: return InvoicePaymentType.Cash;
                case 2: return InvoicePaymentType.Card;
                case 4: return InvoicePaymentType.IRIS;
                case 3:
                    Require(fleet.HasValue && Enum.IsDefined(typeof(InvoicePaymentType), fleet.Value), "FleetCard requires an explicit payment mapping.");
                    return fleet.Value;
                default: throw new ProviderInvoiceValidationException("Missing or unsupported saved payment method.");
            }
        }
        private static CompanySettings CopyCompany(CompanySettings c) => new CompanySettings
        {
            CompanyTaxId = Clean(c.CompanyTaxId), CompanyBranch = Clean(c.CompanyBranch) ?? "0", CompanyName = c.CompanyName,
            CompanyAddress = c.CompanyAddress, CompanyCity = c.CompanyCity, CompanyZipCode = c.CompanyZipCode,
            CompanyTaxOffice = c.CompanyTaxOffice, CompanyEmail = c.CompanyEmail, CompanyPhone = c.CompanyPhone,
            CompanyJob = c.CompanyJob, ProviderBankPaymentType = c.ProviderBankPaymentType
        };
        private static DeliveryContext CopyDelivery(DeliveryContext d) => d == null ? null : new DeliveryContext
        { Vehicle = d.Vehicle, Departure = d.Departure, LoadingAddress = CopyAddress(d.LoadingAddress), DeliveryAddress = CopyAddress(d.DeliveryAddress) };
        private static Address CopyAddress(Address a) => a == null ? null : new Address
        { Street = a.Street, Number = a.Number, City = a.City, PostalCode = a.PostalCode };
        private static decimal Required(decimal? value, string label)
        { Require(value.HasValue, "Missing saved " + label + "."); return value.Value; }
        private static string Clean(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        private static void Require(bool condition, string message)
        { if (!condition) throw new ProviderInvoiceValidationException(message); }
    }
}

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
        private const int ReplaceRelationType = 1;
        private readonly InvoiceBuilder builder = new InvoiceBuilder(new InvoiceTypeResolver());

        // The legacy IsCancelation flag also identifies financial credit notes.
        // 215 is the special cancellation document used by the host cancellation flow.
        public static bool IsCancellationDocument(Data.InvoiceType type)
        { return type != null && type.OfficialEnumerator == 215; }

        // Official code 160 is also present in old databases on the litre-check
        // document.  The delivery type is therefore identified by its saved
        // business flags as well as the official code.
        public static bool IsSelfDeliveryDocument(Data.InvoiceType type)
        {
            return type != null && type.OfficialEnumerator == 160 &&
                type.NeedsVehicle == true && type.HasFinancialTransactions == false &&
                type.TransactionType == 0 && type.IsCancelation != true;
        }

        public static bool IsDeliveryDocument(Data.InvoiceType type)
        {
            return type != null && (type.OfficialEnumerator == 158 ||
                type.OfficialEnumerator == 159 || IsSelfDeliveryDocument(type));
        }

        // Legacy official type 222 is "Invoice - Delivery Note (Sales)".  Type
        // 165 is the ordinary sales invoice.  Both resolve to myDATA 1.1, but
        // only 222 carries isDeliveryNote=true.  NeedsVehicle is transport-data
        // validation and must not change the fiscal document classification.
        public static bool IsInvoiceWithDelivery(Data.InvoiceType type)
        {
            return type != null && !IsDeliveryDocument(type) &&
                type.OfficialEnumerator == 222 && type.TransactionType == 0 &&
                type.HasFinancialTransactions != false && !IsCancellationDocument(type);
        }

        public static bool HasDelivery(Data.InvoiceType type)
        { return IsDeliveryDocument(type) || IsInvoiceWithDelivery(type); }

        public InvoiceBuildRequest Map(Entity source, MappingContext context)
        { return Map(source, context, false); }

        // A type correction cancels the already-issued MARK and creates a new
        // standalone fiscal document.  Incoming host transformation relations
        // remain in the host database as provenance, but must not be promoted to
        // AADE sale correlations on a retail 11.1/1.1 correction.
        public InvoiceBuildRequest MapForTypeCorrection(Entity source, MappingContext context)
        { return Map(source, context, true); }

        private InvoiceBuildRequest Map(Entity source, MappingContext context,
            bool ignoreParentRelations)
        {
            Require(source != null && source.InvoiceId != Guid.Empty, "A saved invoice is required.");
            Require(context != null && context.Company != null && context.WholesaleCodes != null && context.Countries != null,
                "Company, country catalog and wholesale-code settings are required.");
            Require(context.Environment == FiscalEnvironment.Test || context.Environment == FiscalEnvironment.Production,
                "The issuance environment is required.");
            var company = CopyCompany(context.Company);
            var country = Country(source, context);
            var input = Identity(source, context);
            bool movementOnly = IsDeliveryDocument(source.InvoiceType);
            bool delivery = movementOnly || input.InvoiceSeries.IsDeliveryNote;
            bool selfDelivery = IsSelfDeliveryDocument(source.InvoiceType);
            if (source.InvoiceType.NeedsVehicle == true)
                Require(source.VehicleId.HasValue && source.VehicleId.Value != Guid.Empty,
                    "The invoice type requires a selected vehicle.");
            bool sameVat = Clean(input.Trader.TaxRegistrationId) != null &&
                string.Equals(Clean(input.Trader.TaxRegistrationId), Clean(company.CompanyTaxId),
                    StringComparison.OrdinalIgnoreCase);
            Require(Clean(company.CompanyTaxId) != null, "Company VAT is missing.");
            Require(!sameVat || selfDelivery,
                "Same-VAT/self-supply documents require the separate 6.1 policy and are not enabled.");
            Require(!selfDelivery || sameVat,
                "The internal delivery document requires the counterpart VAT number to match the issuer VAT number.");
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
                    line.DiscountAmount >= 0 && line.DiscountAmountWhole >= 0 && line.DiscountAmountRetail >= 0,
                    "Negative or inconsistent saved line amounts.");
                // DiscountAmountRetail is the explicit gross discount and
                // DiscountAmountWhole is the explicit net discount.  Older direct
                // pump sales stored the net value in the generic DiscountAmount
                // column, so that ambiguous compatibility column must not override
                // the explicit pair.
                // TotalPrice and VatAmount already include the discount: never subtract it twice.
                decimal discount = line.DiscountAmountWhole;
                decimal grossDiscount = GrossDiscount(line.DiscountAmountRetail,
                    line.DiscountAmount, discount, line.VatPercentage);
                Require(movementOnly || discount == decimal.Round(grossDiscount * 100m / (100m + line.VatPercentage), 2, MidpointRounding.ToEven),
                    "Saved gross and net discounts disagree; reconcile the invoice before mapping.");
                int? exemption = item.VatExemptionArticle;
                if (!movementOnly && line.VatPercentage == 0 && !exemption.HasValue)
                {
                    if (country.CountryCode != "GR") exemption = (int)(country.IsEu ? VATExemptionCategoryEnum.Article28 : VATExemptionCategoryEnum.Article24);
                    else if (source.Trader != null && source.Trader.VatExemption == true)
                        exemption = VatExemptionReasonCatalog.GetCodeOrDefault(source.Trader.VatExemptionReason, (int)VATExemptionCategoryEnum.Article27);
                }
                input.VATPaymentSuspension |= !movementOnly && country.CountryCode == "GR" &&
                    source.Trader != null && source.Trader.VatExemption == true && line.VatPercentage == 0;
                input.InvoiceLines.Add(new InvoiceLineInput
                {
                    ItemDescription = Clean(line.FuelType.Name), ItemCode = Clean(line.FuelType.Code) ?? line.FuelType.EnumeratorValue.ToString(CultureInfo.InvariantCulture),
                    OfficialFuelCode = item.OfficialFuelCode, Quantity = line.Volume,
                    MeasurementUnit = new MeasurementUnitInput { OfficialType = item.MeasurementUnit },
                    SalesItem = new SalesItemInput { SalesItemType = item.SalesItemType, VATExcemptionArticle = exemption },
                    NetAmount = movementOnly ? 0 : line.TotalPrice - line.VatAmount + discount,
                    DiscountAmount = movementOnly ? 0 : discount, VatAmount = movementOnly ? 0 : line.VatAmount,
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
            input.VATAmount = movementOnly ? 0 : Required(source.VatAmount, "VAT total");
            input.TotalAmount = movementOnly ? 0 : Required(source.TotalAmount, "gross total");
            if (!movementOnly)
            {
                decimal lineGrossDiscount = source.InvoiceLines.Sum(l => GrossDiscount(
                    l.DiscountAmountRetail, l.DiscountAmount, l.DiscountAmountWhole,
                    l.VatPercentage));
                decimal invoiceGrossDiscount = GrossDiscount(source.DiscountAmountRetail,
                    source.DiscountAmount, source.DiscountAmountWhole,
                    source.InvoiceLines.Select(l => l.VatPercentage).Distinct().Count() == 1
                        ? source.InvoiceLines[0].VatPercentage
                        : 0);
                Require(source.DiscountAmountWhole == input.DiscountAmount &&
                    invoiceGrossDiscount == lineGrossDiscount,
                    "Saved invoice discounts disagree with its lines.");
                // Older records may have net-after-discount in NettoAmount. Both documented
                // conventions are accepted only if the saved gross/VAT and line totals agree.
                decimal savedNet = Required(source.NettoAmount, "net total");
                Require(savedNet == input.NettoAmount || savedNet == input.NettoAmount - input.DiscountAmount,
                    string.Format(CultureInfo.InvariantCulture,
                        "Saved invoice net total disagrees with its lines (saved {0:0.00}, expected {1:0.00} before or {2:0.00} after discount).",
                        savedNet, input.NettoAmount, input.NettoAmount - input.DiscountAmount),
                    IsPermittedDirectPumpNetRounding(source, savedNet,
                        input.NettoAmount, input.NettoAmount - input.DiscountAmount));
                input.PaymentType = Payment(source.PaymentType);
            }
            // LocalCopy preserves host-side transformation provenance only. It
            // must not become a myDATA or invoicing-provider correlation.
            var linked = ignoreParentRelations
                ? new Data.InvoiceRelation[0]
                : source.ParentInvoiceRelations.Where(r => r.IsExternalFiscalCorrelation &&
                    !IsLocalDeliveryToSaleTransformation(source, r)).ToArray();
            bool credit = input.InvoiceSeries.InvoiceType.InvoiceTypeMode == InvoiceMode.Credit;
            bool cancellation = input.InvoiceSeries.InvoiceType.InvoiceTypeMode == InvoiceMode.Cancel;
            bool financialCredit = credit || cancellation;
            Require(financialCredit || linked.Length == 0 || context.IncludeSaleCorrelations,
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
            if (cancellation)
            {
                Require(parents.Length == 1,
                    "A cancellation document must reference exactly one original invoice.");
                // Type 215 is a host cancellation document. For provider/myDATA
                // transmission it becomes the financial reversal that matches the
                // original document (5.1 wholesale or 11.4 retail), not a request
                // to cancel the original provider submission.
                var originalType = parents[0].Invoice.InvoiceSeries.InvoiceType;
                input.InvoiceSeries.InvoiceType.IsRetail = originalType.IsRetail;
                input.InvoiceSeries.InvoiceType.MyDataDocumentKind = originalType.MyDataDocumentKind;
                input.PaymentType = InvoicePaymentType.Credit;
            }
            var deliveryContext = delivery
                ? context.Delivery ?? InvoiceDelivery(source, company)
                : context.Delivery;
            if (delivery && deliveryContext != null &&
                Clean(deliveryContext.Vehicle) == null)
                Require(source.InvoiceType.NeedsVehicle == true &&
                    source.VehicleId.HasValue && source.VehicleId.Value != Guid.Empty,
                    "A delivery without a registration number requires a selected vehicle on a vehicle-required invoice type.");
            var request = new InvoiceBuildRequest
            {
                Invoice = input, Company = company, Country = country,
                Parents = financialCredit ? parents : new InvoiceParent[0],
                SaleCorrelations = !financialCredit && parents.Length > 0 ? parents : null,
                Delivery = delivery ? CopyDelivery(deliveryContext) : null
            };
            Require(delivery || deliveryContext == null, "Dispatch details require a delivery document.");
            if (delivery) Require(request.Delivery != null, "Provide explicit ordinary delivery vehicle, departure and structured addresses.");
            Require(request.Delivery == null || request.Delivery.Departure.Kind != DateTimeKind.Utc,
                "Dispatch time must be fiscal wall-clock time, not UTC.");
            builder.Build(request); // One shared validation path before returning a dispatchable snapshot.
            return request;
        }

        private static bool IsLocalDeliveryToSaleTransformation(Entity child,
            Data.InvoiceRelation relation)
        {
            if (child == null || child.InvoiceType == null || relation == null ||
                relation.RelationType != ReplaceRelationType ||
                relation.ParentInvoice == null)
                return false;

            // A delivery note is movement evidence, not a previously issued sale
            // to correlate on the financial invoice created from it. Older host
            // databases saved this local transformation as Replace (1); treat it
            // as LocalCopy without mutating the historical row.
            return IsDeliveryDocument(relation.ParentInvoice.InvoiceType) &&
                !IsDeliveryDocument(child.InvoiceType) &&
                child.InvoiceType.HasFinancialTransactions != false &&
                child.InvoiceType.IsCancelation != true &&
                !IsCancellationDocument(child.InvoiceType);
        }

        public InvoiceModel Build(Entity source, MappingContext context) { return builder.Build(Map(source, context)); }

        private CountryInput Country(Entity source, MappingContext context)
        {
            return TraderCountryResolver.Resolve(context.Countries,
                source.Trader == null ? null : source.Trader.Country,
                source.Trader == null ? null : source.Trader.TaxRegistrationNumber);
        }

        private static InvoiceInput Identity(Entity source, MappingContext context)
        {
            Require(source.InvoiceType != null, "Invoice type is missing.");
            Require(source.TransactionDate.Kind != DateTimeKind.Utc, "Issue time must be fiscal wall-clock time, not UTC.");
            var t = source.Trader;
            bool movementOnly = IsDeliveryDocument(source.InvoiceType);
            bool invoiceWithDelivery = IsInvoiceWithDelivery(source.InvoiceType);
            return new InvoiceInput
            {
                InvoiceId = source.InvoiceId,
                TraderId = source.TraderId ?? (t == null ? Guid.Empty : t.TraderId),
                VehicleId = source.VehicleId,
                Number = source.Number, Series = Clean(source.Series), PrintedDateTime = source.TransactionDate,
                SupplyCode = Clean(source.SupplyNumber),
                Trader = new TraderInput { TaxRegistrationId = Clean(t == null ? null : t.TaxRegistrationNumber),
                    LastName = t == null ? null : t.Name, Branch = t == null ? 0 : t.Branch,
                    TaxRegistrationOffice = t == null ? null : t.TaxRegistrationOffice,
                    Address = t == null ? null : t.Address, City = t == null ? null : t.City, PostalCode = t == null ? null : t.ZipCode,
                    Email = t == null ? null : t.Email, Phone = t == null ? null : t.Phone1, Job = t == null ? null : t.Occupation },
                // Do not fall back to the trader's current supply number for a saved invoice.
                InvoiceSeries = new InvoiceSeriesInput { Series = Clean(source.Series), IsDeliveryNote = invoiceWithDelivery,
                    InvoiceType = new InvoiceTypeInput { IsRetail = !context.WholesaleCodes.Contains(source.InvoiceType.OfficialEnumerator),
                        MyDataDocumentKind = (int)(movementOnly ? MyDataDocumentKind.Delivery : MyDataDocumentKind.Goods),
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

        private static InvoicePaymentType Payment(int? value)
        {
            switch (value)
            {
                case 0: return InvoicePaymentType.Credit;
                case 1: return InvoicePaymentType.Cash;
                case 2: return InvoicePaymentType.Card;
                // A fleet card settles on the customer's account. It is not a
                // bank-card POS transaction and therefore maps to AADE credit (5).
                case 3: return InvoicePaymentType.Credit;
                case 4: return InvoicePaymentType.IRIS;
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
        private static DeliveryContext InvoiceDelivery(Entity source, CompanySettings company)
        {
            var trader = source.Trader;
            var savedPlate = Clean(source.VehiclePlateNumber);
            var currentPlate = source.Vehicle == null ? null : Clean(source.Vehicle.PlateNumber);
            return new DeliveryContext
            {
                Vehicle = savedPlate ?? currentPlate,
                Departure = source.TransactionDate,
                LoadingAddress = DeliveryAddress(company.CompanyAddress,
                    company.CompanyCity, company.CompanyZipCode),
                DeliveryAddress = trader == null ? null : DeliveryAddress(trader.Address,
                    trader.City, trader.ZipCode)
            };
        }
        private static Address DeliveryAddress(string street, string city, string postalCode)
        {
            var result = new Address
            {
                Street = Clean(street) ?? "",
                City = Clean(city),
                PostalCode = Clean(postalCode)
            };
            if (result.Street.Length != 0) result.ReplaceNumber();
            return result;
        }
        private static Address CopyAddress(Address a) => a == null ? null : new Address
        { Street = a.Street, Number = a.Number, City = a.City, PostalCode = a.PostalCode };
        private static decimal Required(decimal? value, string label)
        { Require(value.HasValue, "Missing saved " + label + "."); return value.Value; }
        private static decimal GrossDiscount(decimal explicitGross, decimal compatibility,
            decimal net, decimal vatPercentage)
        {
            explicitGross = decimal.Round(explicitGross, 2, MidpointRounding.ToEven);
            compatibility = decimal.Round(compatibility, 2, MidpointRounding.ToEven);
            net = decimal.Round(net, 2, MidpointRounding.ToEven);
            if (explicitGross > 0 || compatibility == 0) return explicitGross;

            // Before the explicit Retail/Whole columns were populated, the generic
            // column normally held gross.  A few direct-pump records instead copied
            // the net value into it; reconstruct gross only for that exact legacy
            // shape and let the normal net/gross validation prove the result.
            if (compatibility == net && vatPercentage > 0)
                return decimal.Round(net * (100m + vatPercentage) / 100m, 2,
                    MidpointRounding.ToEven);
            return compatibility;
        }
        private static bool IsPermittedDirectPumpNetRounding(Entity source, decimal savedNet,
            decimal expectedBeforeDiscount, decimal expectedAfterDiscount)
        {
            if (source.InvoiceLines.Count != 1 || !source.TotalAmount.HasValue)
                return false;
            var line = source.InvoiceLines[0];
            if (line.VatPercentage < 0 || source.DiscountAmount <= 0)
                return false;

            // Samtec transmits fuel prices with three or five decimals. Multiplying
            // that wire price by a three-decimal quantity and then splitting VAT and
            // discount can legitimately put the saved pre-discount net one cent away
            // from the independently rounded line totals. Accept only that one-cent
            // case for a fuel line that actually uses sub-cent pricing. Manually
            // entered and restored invoices do not necessarily retain a sale link.
            decimal price = line.VatPercentage == 0
                ? Math.Abs(line.UnitPriceWhole)
                : Math.Max(Math.Abs(line.UnitPrice), Math.Abs(line.UnitPriceRetail));
            int priceDecimals = decimal.Round(price, 3, MidpointRounding.ToEven) ==
                decimal.Round(price, 5, MidpointRounding.ToEven) ? 3 : 5;
            decimal wirePrice = decimal.Round(price, priceDecimals, MidpointRounding.ToEven);
            bool subCentWirePrice = wirePrice != decimal.Round(wirePrice, 2,
                MidpointRounding.ToEven);
            if (subCentWirePrice &&
                (Math.Abs(savedNet - expectedBeforeDiscount) <= 0.01m ||
                 Math.Abs(savedNet - expectedAfterDiscount) <= 0.01m))
                return true;

            // The remaining compatibility shapes are specific to the old direct-
            // pump creator and therefore still require the physical sale relation.
            if (!line.SaleTransactionId.HasValue || line.SaleTransactionId.Value == Guid.Empty ||
                source.DiscountAmount != source.DiscountAmountWhole ||
                source.DiscountAmountRetail <= source.DiscountAmount ||
                line.DiscountAmount != line.DiscountAmountWhole ||
                line.DiscountAmountRetail <= line.DiscountAmount)
                return false;

            // The pump editor calculates the invoice-level pre-discount net and
            // the final line net through separate rounded paths.  Their legitimate
            // difference is at most one cent; all dispatchable line totals are
            // still validated by InvoiceBuilder below.
            if (Math.Abs(savedNet - expectedBeforeDiscount) <= 0.01m ||
                Math.Abs(savedNet - expectedAfterDiscount) <= 0.01m)
                return true;

            // The old direct-pump creator added the net discount to the payable
            // gross and then removed VAT.  Preserve that value only as recognised
            // historical input; mapping still derives and transmits the final net,
            // VAT and gross amounts from the internally balanced line.
            decimal legacyNet = decimal.Round(
                (source.TotalAmount.Value + source.DiscountAmount) * 100m /
                (100m + line.VatPercentage), 2, MidpointRounding.ToEven);
            return savedNet == legacyNet;
        }
        private static string Clean(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        private static void Require(bool condition, string message)
        { if (!condition) throw new ProviderInvoiceValidationException(message); }
        private static void Require(bool condition, string message, bool compatibility)
        { if (!condition && !compatibility) throw new ProviderInvoiceValidationException(message); }
    }
}

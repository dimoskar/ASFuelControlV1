using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using Exedron.Invoicing.Core;
using Exedron.Invoicing.Core.Models;
using Exedron.Invoicing.Adapters;
using Newtonsoft.Json.Linq;

internal static partial class Program
{
    private static int passed, failed;
    private static readonly InvoiceBuilder Builder = new InvoiceBuilder(new InvoiceTypeResolver());
    private static readonly InvoiceAdapterFactory Factory = new InvoiceAdapterFactory(new InvoiceTypeResolver());
    private static readonly XNamespace Ns = "http://www.aade.gr/myDATA/invoice/v1.0";
    private static readonly DateTime AuditTime = new DateTime(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc);

    private static int Main()
    {
        Contracts();
        Transitions();
        ITimologiaTokenCache();
        foreach (var culture in new[] { "en-US", "el-GR" })
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            Mapping(culture);
            HeatingAllowance(culture);
        }
        Console.WriteLine("{0} passed, {1} failed", passed, failed);
        return failed == 0 ? 0 : 1;
    }

    private static void Contracts()
    {
        Test("Core has no host, vendor, persistence or UI dependency", () => {
            var allowed = new[] { "mscorlib", "System", "System.Core", "System.Net.Http", "System.Xml", "System.Xml.Linq" };
            foreach (var reference in typeof(IFiscalSigner).Assembly.GetReferencedAssemblies())
                Check(allowed.Contains(reference.Name), "Unexpected dependency: " + reference.Name);
        });
        Test("Request owns exact bytes and stable SHA-256", () => {
            var bytes = Encoding.UTF8.GetBytes("abc");
            var request = Request(bytes);
            bytes[0] = 0;
            request.Payload[1] = 0;
            Equal("abc", Encoding.UTF8.GetString(request.Payload));
            Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", request.PayloadSha256);
        });
        Test("Default document, environment and missing scope cannot be issued", () => {
            Throws<ArgumentException>(() => new FiscalDocumentReference(FiscalDocumentKind.Unspecified, Guid.NewGuid()));
            Throws<ArgumentException>(() => new FiscalDocumentReference(FiscalDocumentKind.Invoice, Guid.Empty));
            Throws<ArgumentException>(() => new FiscalSigningScope("samtec", "device-1", FiscalEnvironment.Unspecified, "099999999", 0));
            Throws<ArgumentException>(() => new FiscalSigningScope("samtec", "", FiscalEnvironment.Test, "099999999", 0));
            Throws<ArgumentException>(() => new FiscalSigningScope("samtec", "device-1", FiscalEnvironment.Test, "099999999", -1));
            Throws<ArgumentException>(() => Request(new byte[0]));
        });
        Test("Capabilities are explicit and cannot be mutated by caller", () => {
            var kinds = new[] { FiscalDocumentKind.Invoice, FiscalDocumentKind.Balance, FiscalDocumentKind.TankFilling };
            var capabilities = new FiscalSignerCapabilities("samtec", kinds);
            kinds[0] = FiscalDocumentKind.Alert;
            Check(!capabilities.DocumentKinds.Contains(FiscalDocumentKind.Alert), "Unimplemented alert signing advertised.");
            Throws<NotSupportedException>(() => ((System.Collections.Generic.IList<FiscalDocumentKind>)capabilities.DocumentKinds)[0] = FiscalDocumentKind.Alert);
            Throws<ArgumentException>(() => new FiscalSignerCapabilities("samtec", new[] { FiscalDocumentKind.Unspecified }));
        });
        Test("Signing success requires real signature and acceptance evidence", () => {
            var id = Guid.NewGuid();
            foreach (var signature in new[] { null, "", "-", "Sign Device A" })
                Throws<ArgumentException>(() => FiscalSigningResult.Signed(id, signature, null, null, "accepted"));
            Throws<ArgumentException>(() => FiscalSigningResult.Signed(id, "signature", null, null, ""));
            var result = FiscalSigningResult.Signed(id, " signature ", "qr", "device-ref", "raw reply");
            Equal(" signature ", result.Signature); // exact device output, not normalized printable text
            Equal("raw reply", result.Response);
        });
        Test("Uncertain replies retain evidence without claiming a signature", () => {
            var result = FiscalSigningResult.Unknown(Guid.NewGuid(), "partial reply", "Connection lost");
            Equal(FiscalSigningState.Unknown, result.State);
            Equal("partial reply", result.Response);
            Check(result.Signature == null, "Unknown outcome contains a signature.");
            Throws<ArgumentException>(() => FiscalSigningResult.Rejected(Guid.NewGuid(), "", "Not found"));
        });
        Test("Attempt binds outcome to request identity and state", () => {
            var request = Request();
            var result = FiscalSigningResult.Signed(request.AttemptId, "sig", null, "ref", "accepted");
            var attempt = new FiscalSigningAttempt(request, FiscalSigningState.Signed, 2, AuditTime, AuditTime.AddSeconds(1), result);
            Equal(request.AttemptId, attempt.Result.AttemptId);
            Throws<ArgumentException>(() => new FiscalSigningAttempt(Request(), FiscalSigningState.Signed, 2, AuditTime, AuditTime, result));
            Throws<ArgumentException>(() => new FiscalSigningAttempt(request, FiscalSigningState.Prepared, 0, AuditTime, AuditTime, result));
            Throws<ArgumentException>(() => new FiscalSigningAttempt(request, FiscalSigningState.Signed, 2, AuditTime, AuditTime));
            Throws<ArgumentException>(() => new FiscalSigningAttempt(request, FiscalSigningState.Unknown, 2, AuditTime, AuditTime, result));
        });
        Test("Audit time and local failure reason are required", () => {
            var request = Request();
            Throws<ArgumentException>(() => new FiscalSigningAttempt(request, FiscalSigningState.Prepared, 0, DateTime.SpecifyKind(AuditTime, DateTimeKind.Unspecified), AuditTime));
            Throws<ArgumentException>(() => new FiscalSigningAttempt(request, FiscalSigningState.Prepared, 0, AuditTime, AuditTime.AddSeconds(-1)));
            Throws<ArgumentException>(() => new FiscalSigningAttempt(request, FiscalSigningState.Prepared, -1, AuditTime, AuditTime));
            Throws<ArgumentException>(() => new FiscalSigningAttempt(request, FiscalSigningState.FailedBeforeDispatch, 1, AuditTime, AuditTime));
        });
        Test("Core signage policy preserves every existing routing combination", () => {
            IInvoiceSignagePolicy policy = new FiscalSignagePolicy();
            for (int bits = 0; bits < 16; bits++)
            {
                bool official = (bits & 1) != 0, signed = (bits & 2) != 0, provider = (bits & 4) != 0, device = (bits & 8) != 0;
                bool expected = official && !signed && (provider || device);
                Equal(expected, policy.NeedsSignature(official, signed, provider, device));
                Equal(expected, InvoiceSignageRouter.NeedsSignature(official, signed, provider, device));
                Equal(provider && device, policy.UsesInvoiceProvider(provider, device));
            }
        });
    }

    private static void Transitions()
    {
        Test("Signing: only persist-before-dispatch, outcome and reconciliation transitions", () => {
            Matrix(Enum.GetValues(typeof(FiscalSigningState)).Cast<FiscalSigningState>().ToArray(),
                FiscalStateTransitions.CanTransition,
                "Prepared>Signing,Prepared>FailedBeforeDispatch,Signing>Signed,Signing>Rejected,Signing>Unknown,Unknown>Signed,Unknown>Rejected,Unknown>Unknown");
            Check(!FiscalStateTransitions.CanTransition((FiscalSigningState)99, FiscalSigningState.Signing), "Invalid state accepted.");
        });
        Test("Transmission cannot retry an unknown or accepted attempt", () => {
            Matrix(Enum.GetValues(typeof(FiscalTransmissionState)).Cast<FiscalTransmissionState>().ToArray(),
                FiscalStateTransitions.CanTransition,
                "Prepared>Sending,Prepared>FailedBeforeDispatch,Sending>Accepted,Sending>Rejected,Sending>Unknown,Unknown>Accepted,Unknown>Rejected,Unknown>Unknown");
        });
        Test("Payment cannot reauthorize an unknown or accepted operation", () => {
            Matrix(Enum.GetValues(typeof(FiscalPaymentState)).Cast<FiscalPaymentState>().ToArray(),
                FiscalStateTransitions.CanTransition,
                "Prepared>Processing,Prepared>FailedBeforeDispatch,Processing>Accepted,Processing>Rejected,Processing>Unknown,Unknown>Accepted,Unknown>Rejected,Unknown>Unknown");
        });
        Test("Print handoff is distinct from confirmation and cannot silently redispatch", () => {
            Matrix(Enum.GetValues(typeof(FiscalPrintState)).Cast<FiscalPrintState>().ToArray(),
                FiscalStateTransitions.CanTransition,
                "Prepared>Dispatching,Prepared>FailedBeforeDispatch,Dispatching>HandedOff,Dispatching>Confirmed,Dispatching>Rejected,Dispatching>Unknown,HandedOff>Confirmed,HandedOff>Unknown,Unknown>Confirmed,Unknown>Rejected,Unknown>Unknown");
        });
    }

    private static void Mapping(string culture)
    {
        Action<string, Action> test = (name, action) => Test(culture + ": " + name, action);
        test("Existing builder overload keeps default behavior", () => {
            var request = Invoice();
            var viaRequest = Builder.Build(request);
            var viaLegacyOverload = Builder.Build(request.Invoice, request.Company, request.Country, request.Parents);
            Equal(new InvoiceXmlSerializer().Serialize(viaRequest, true), new InvoiceXmlSerializer().Serialize(viaLegacyOverload, true));
            Equal("1.1", viaRequest.InvoiceHeader.InvoiceType);
            Check(Header(viaRequest).Element(Ns + "vatPaymentSuspension") == null, "Default XML changed.");
        });
        test("VAT suspension reaches direct XML and iTimologia; Ilyda fails explicitly", () => {
            var request = Invoice();
            request.Invoice.VATPaymentSuspension = true;
            var model = Builder.Build(request);
            Equal("true", Header(model).Element(Ns + "vatPaymentSuspension").Value);
            var headerNames = Header(model).Elements().Select(e => e.Name.LocalName).ToArray();
            Check(Array.IndexOf(headerNames, "invoiceType") < Array.IndexOf(headerNames, "vatPaymentSuspension") &&
                Array.IndexOf(headerNames, "vatPaymentSuspension") < Array.IndexOf(headerNames, "currency"), "Incorrect header order.");
            var json = JObject.Parse(Factory.CreatePayloadMapper(InvoiceProviderCatalog.ITimologiaKey).Serialize(model, request.Invoice.InvoiceId));
            Equal(true, json.SelectTokens("$..vat_payment_suspension").Single().Value<bool>());
            Throws<InvalidOperationException>(() => Factory.CreatePayloadMapper(InvoiceProviderCatalog.DirectIlydaKey).Serialize(model, request.Invoice.InvoiceId));
        });
        test("Customer and issuer contact snapshots are mapped independently", () => {
            var request = Invoice();
            request.Company.CompanyEmail = " issuer@example.invalid ";
            request.Company.CompanyPhone = "100";
            request.Company.CompanyJob = "Fuel seller";
            request.Invoice.Trader.Email = " buyer@example.invalid ";
            request.Invoice.Trader.Phone = "200";
            request.Invoice.Trader.Job = "Transport";
            var model = Builder.Build(request);
            Equal("buyer@example.invalid", model.CounterPart.Email);
            Equal("200", model.CounterPart.Phone);
            Equal("Transport", model.CounterPart.Job);
            Equal("issuer@example.invalid", model.Issuer.Email);
            Equal("100", model.Issuer.Phone);
            Equal("Fuel seller", model.Issuer.Job);
            request.Invoice.Trader.Email = null;
            Check(Builder.Build(request).CounterPart.Email == null, "Issuer email leaked into customer.");
        });
        test("Ordinary multi-line delivery retains dispatch and copies addresses", () => {
            var request = Invoice();
            request.Invoice.InvoiceSeries.IsDeliveryNote = true;
            request.Delivery = Delivery();
            request.Invoice.InvoiceLines.Add(Line());
            request.Invoice.NettoAmount *= 2; request.Invoice.VATAmount *= 2; request.Invoice.TotalAmount *= 2;
            var model = Builder.Build(request);
            Equal(2, model.InvoiceDetails.Length);
            Equal("TEST-123", Header(model).Element(Ns + "vehicleNumber").Value);
            Equal("2026-10-15", Header(model).Element(Ns + "dispatchDate").Value);
            Equal("13:25:00", Header(model).Element(Ns + "dispatchTime").Value);
            Equal("Depot", model.InvoiceHeader.OtherDeliveryNoteHeader.LoadingAddress.Street);
            Equal("Issuer", model.Issuer.Address.Street);
            request.Delivery.LoadingAddress.Street = "Changed later";
            Equal("Depot", model.InvoiceHeader.OtherDeliveryNoteHeader.LoadingAddress.Street);
        });
        test("Ordinary delivery rejects conflicting or incomplete context", () => {
            var request = Invoice(); request.Delivery = Delivery();
            Throws<ProviderInvoiceValidationException>(() => Builder.Build(request));
            request.Invoice.InvoiceSeries.IsDeliveryNote = true;
            request.Movement = new MovementContext();
            Throws<ProviderInvoiceValidationException>(() => Builder.Build(request));
            request.Movement = null; request.Delivery.Vehicle = "";
            Throws<ProviderInvoiceValidationException>(() => Builder.Build(request));
            request.Delivery = Delivery(); request.Delivery.LoadingAddress = null;
            Throws<ProviderInvoiceValidationException>(() => Builder.Build(request));
            request.Delivery = Delivery(); request.Delivery.Departure = default(DateTime);
            Throws<ProviderInvoiceValidationException>(() => Builder.Build(request));
        });
        test("Explicit linked sale preserves distinct accepted MARKs in XML and providers", () => {
            var request = Invoice(); var parent = Parent();
            request.SaleCorrelations = new[] { parent, parent };
            var model = Builder.Build(request);
            Equal(1, model.InvoiceHeader.CorrelatedInvoices.Length);
            Equal(123456L, model.InvoiceHeader.CorrelatedInvoices[0]);
            Equal("123456", Header(model).Element(Ns + "correlatedInvoices").Value);
            foreach (var provider in new[] { InvoiceProviderCatalog.ITimologiaKey, InvoiceProviderCatalog.DirectIlydaKey })
                Check(Factory.CreatePayloadMapper(provider).Serialize(model, request.Invoice.InvoiceId).Contains("123456"), "Provider lost correlation.");
        });
        test("Sale correlation rejects cancelled, unaccepted and missing MARK evidence", () => {
            foreach (Action<InvoiceParent> mutate in new Action<InvoiceParent>[] {
                p => p.Transmission.Status = 0, p => p.Transmission.Mark = 0,
                p => p.Transmission.CancelationMark = 999, p => p.Transmission.CanceledByMark = 999,
                p => p.Transmission = null })
            {
                var request = Invoice(); var parent = Parent(); mutate(parent);
                request.SaleCorrelations = new[] { parent };
                Throws<ProviderInvoiceValidationException>(() => Builder.Build(request));
            }
        });
        test("Sale correlation rejects self, other customer, credit and retail originals", () => {
            foreach (Action<InvoiceParent, InvoiceBuildRequest> mutate in new Action<InvoiceParent, InvoiceBuildRequest>[] {
                (p, r) => p.Invoice.InvoiceId = r.Invoice.InvoiceId,
                (p, r) => p.Invoice.Trader.TaxRegistrationId = "111111111",
                (p, r) => p.Invoice.InvoiceSeries.InvoiceType.InvoiceTypeMode = InvoiceMode.Credit,
                (p, r) => p.Invoice.InvoiceSeries.InvoiceType.IsRetail = true,
                (p, r) => p.Invoice.InvoiceSeries.InvoiceType.MyDataDocumentKind = 0 })
            {
                var request = Invoice(); var parent = Parent(); mutate(parent, request);
                request.SaleCorrelations = new[] { parent };
                Throws<ProviderInvoiceValidationException>(() => Builder.Build(request));
            }
        });
        test("Credit parents and sale correlations cannot override each other", () => {
            var request = Invoice(); request.Invoice.InvoiceSeries.InvoiceType.InvoiceTypeMode = InvoiceMode.Credit;
            request.Parents = new[] { Parent() }; request.SaleCorrelations = new[] { Parent() };
            Throws<ProviderInvoiceValidationException>(() => Builder.Build(request));
            request.SaleCorrelations = null;
            Equal("5.1", Builder.Build(request).InvoiceHeader.InvoiceType);
            request.Parents.First().Transmission.CanceledByMark = 999;
            Throws<ProviderInvoiceValidationException>(() => Builder.Build(request));
        });
        test("Discount is applied once and inconsistent host totals are rejected", () => {
            var request = Invoice(); request.Invoice.InvoiceLines[0].DiscountAmount = 10;
            request.Invoice.InvoiceLines[0].VatAmount = 21.6m;
            request.Invoice.DiscountAmount = 10; request.Invoice.VATAmount = 21.6m; request.Invoice.TotalAmount = 111.6m;
            var model = Builder.Build(request);
            Equal(90m, model.InvoiceDetails[0].NetValue);
            Equal(111.6m, model.InvoiceSummary.TotalGrossValue);
            request.Invoice.NettoAmount = 90;
            Throws<ProviderInvoiceValidationException>(() => Builder.Build(request));
        });
        test("New fields do not infer self-supply or accept host payment enum casts", () => {
            var request = Invoice(); request.Invoice.Trader.TaxRegistrationId = request.Company.CompanyTaxId;
            Equal("1.1", Builder.Build(request).InvoiceHeader.InvoiceType);
            request.Invoice.PaymentType = (InvoicePaymentType)999;
            Throws<ProviderInvoiceValidationException>(() => Builder.Build(request));
        });
        test("Structured XML preserves special characters", () => {
            var request = Invoice(); request.Country.CountryCode = "DE"; request.Country.IsEu = true;
            request.Invoice.Trader.LastName = "A & B <Export>";
            var xml = XDocument.Parse(new InvoiceXmlSerializer().Serialize(Builder.Build(request), true));
            Equal("A & B <Export>", xml.Descendants(Ns + "counterpart").Single().Element(Ns + "name").Value);
        });
    }

    private static FiscalSigningRequest Request(byte[] payload = null)
    {
        return new FiscalSigningRequest(Guid.NewGuid(), new FiscalDocumentReference(FiscalDocumentKind.Invoice, Guid.NewGuid()),
            new FiscalSigningScope("samtec", "device-1", FiscalEnvironment.Test, "099999999", 0),
            "samtec.invoice.v1; charset=utf-8", payload ?? Encoding.UTF8.GetBytes("fixture request"));
    }
    private static InvoiceBuildRequest Invoice()
    {
        return new InvoiceBuildRequest {
            Invoice = new InvoiceInput { InvoiceId = Guid.NewGuid(), TraderId = Guid.NewGuid(), Number = 42,
                PrintedDateTime = new DateTime(2026, 10, 15, 13, 0, 0), PaymentType = InvoicePaymentType.Cash,
                InvoiceSeries = new InvoiceSeriesInput { Series = "A", InvoiceType = new InvoiceTypeInput { MyDataDocumentKind = (int)MyDataDocumentKind.Goods } },
                Trader = new TraderInput { TaxRegistrationId = "088888888", LastName = "Customer", Address = "Customer 2", City = "Athens", PostalCode = "10000" },
                InvoiceLines = new[] { Line() }.ToList(), NettoAmount = 100, VATAmount = 24, TotalAmount = 124 },
            Company = new CompanySettings { CompanyTaxId = "099999999", CompanyName = "Issuer", CompanyAddress = "Issuer 1", CompanyCity = "Athens", CompanyZipCode = "10000" },
            Country = new CountryInput { CountryCode = "GR", IsEu = true }
        };
    }
    private static InvoiceLineInput Line()
    {
        return new InvoiceLineInput { SalesItem = new SalesItemInput(), MeasurementUnit = new MeasurementUnitInput { OfficialType = 1 },
            ItemDescription = "Item", Quantity = 1, NetAmount = 100, VatAmount = 24, VATPercentage = 24 };
    }
    private static InvoiceParent Parent()
    {
        return new InvoiceParent { Invoice = Invoice().Invoice, Transmission = new TransmissionEvidence { Status = 3, Mark = 123456 } };
    }
    private static DeliveryContext Delivery()
    {
        return new DeliveryContext { Vehicle = "TEST-123", Departure = new DateTime(2026, 10, 15, 13, 25, 0),
            LoadingAddress = new Address { Street = "Depot", Number = "1", City = "Athens", PostalCode = "10000" },
            DeliveryAddress = new Address { Street = "Site", Number = "2", City = "Athens", PostalCode = "10000" } };
    }
    private static XElement Header(InvoiceModel model)
    {
        return XDocument.Parse(new InvoiceXmlSerializer().Serialize(model, true)).Descendants(Ns + "invoiceHeader").Single();
    }
    private static void Matrix<T>(T[] states, Func<T, T, bool> transition, string allowed)
    {
        var edges = allowed.Split(',');
        foreach (var from in states) foreach (var to in states)
            Equal(edges.Contains(from + ">" + to), transition(from, to));
    }
    private static void Test(string name, Action action)
    {
        try { action(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception ex) { failed++; Console.WriteLine("FAIL " + name + ": " + ex); }
    }
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Equal<T>(T expected, T actual) { Check(object.Equals(expected, actual), "Expected " + expected + ", got " + actual); }
    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new Exception("Expected " + typeof(T).Name);
    }
}

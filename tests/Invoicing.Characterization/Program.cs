using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Xml.Linq;
using ASFuelControl.Data;
using ASFuelControl.Data.Implementation;
using ASFuelControl.Windows.Threads;
using Exedron.MyData.Models.Responses;
using Provider = Exedron.ProviderInvoicing;

namespace Invoicing.Characterization
{
    internal sealed class CaseFixture
    {
        public string Id { get; set; }
        public int OfficialCode { get; set; } = 100;
        public int Payment { get; set; } = 1;
        public string Country { get; set; } = "GR";
        public decimal Gross { get; set; } = 124m;
        public decimal Vat { get; set; } = 24m;
        public decimal Rate { get; set; } = 24m;
        public decimal ExpectedNet { get; set; } = 100m;
        public bool VatExemption { get; set; }
        public bool Credit { get; set; }
        public bool SameVat { get; set; }
        public bool TwoLines { get; set; }
        public long? ParentMark { get; set; }
        public bool ParentCancelled { get; set; }
        public string ExpectedDirectType { get; set; } = "1.1";
        public string ExpectedProviderType { get; set; } = "1.1";
        public int ExpectedDirectPayment { get; set; } = 3;
        public int ExpectedProviderPayment { get; set; } = 3;
        public bool ExpectedDirectCorrelation { get; set; }
        public bool ExpectedProviderCorrelation { get; set; }
        public string ExpectedExemption { get; set; } = "None";
        public string ExpectedClassification { get; set; }
    }

    internal static class Program
    {
        private static string fixtures;
        private static bool record;
        private static int passed, failed;
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer();
        private static readonly Guid InvoiceId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        private static int Main(string[] args)
        {
            try
            {
                fixtures = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures");
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "--fixtures" && i + 1 < args.Length) fixtures = Path.GetFullPath(args[++i]);
                    else if (args[i] == "--record-snapshots") record = true;
                    else throw new ArgumentException("Unknown or incomplete argument: " + args[i]);
                }
                var cases = Json.Deserialize<CaseFixture[]>(File.ReadAllText(Path.Combine(fixtures, "cases.json")));
                if (cases.Length == 0 || cases.Select(c => c.Id).Distinct().Count() != cases.Length)
                    throw new InvalidOperationException("Fixture IDs must be nonempty and unique.");
                Console.WriteLine("OFFLINE legacy characterization. Known defects are observations, not acceptance criteria.");
                foreach (var culture in new[] { "en-US", "el-GR" })
                {
                    Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                    foreach (var item in cases)
                    {
                        Run(culture + "/direct/" + item.Id, () => CheckMapping(item, false));
                        Run(culture + "/provider/" + item.Id, () => CheckMapping(item, true));
                    }
                }
                RunFailureCases();
                RunSigningCases();
                Console.WriteLine("RESULT: " + passed + " passed, " + failed + " failed.");
                Console.WriteLine("Limits: in-memory data/transport; no ORM transactions, real SDK, device, printer, concurrency or AADE schema validation.");
                return failed == 0 ? 0 : 1;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        }

        private static void Run(string name, Action test)
        {
            try { test(); passed++; Console.WriteLine("PASS " + name); }
            catch (Exception ex) { failed++; Console.Error.WriteLine("FAIL " + name + ": " + ex.Message); }
        }
        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!object.Equals(expected, actual)) throw new Exception(message + ": expected " + expected + ", got " + actual);
        }
        private static void True(bool value, string message) { if (!value) throw new Exception(message); }

        private static Invoice Reset(CaseFixture c = null)
        {
            c = c ?? new CaseFixture();
            DatabaseModel.InvoiceRows.Clear(); DatabaseModel.TransmissionRows.Clear(); DatabaseModel.SavedStates.Clear();
            DatabaseModel.BalanceRows.Clear(); DatabaseModel.AlertRows.Clear(); DatabaseModel.TitrimetryRows.Clear(); DatabaseModel.FillingRows.Clear();
            PrintAgent.SelectProvider(false);
            PrintAgent.PrintInvoiceBoundary = value => { };
            PrintAgent.PrintOtherBoundary = kind => { };
            Samtec.WebService.HttpClient.Calls = 0;
            Samtec.WebService.HttpClient.Reply = () => new Samtec.WebService.Models.Response
            { ResultCode = "ΔΦΣΣ,SYNTHETIC-SIGNATURE,https://example.invalid/device-qr" };
            var options = OptionHandler.Instance.Values;
            options.Clear();
            options["CompanyTIN"] = "000000000";
            options["MyDataIsActive"] = "true";
            options["MyDataUserName"] = "offline-user";
            options["MyDataSubscriptionKey"] = "offline-key";
            options["MyDataUrl"] = "https://example.invalid/mydata";
            Exedron.MyData.Settings.IsActive = true;
            Exedron.MyData.InvoiceSender.Calls = 0;
            Exedron.MyData.InvoiceSender.LastXml = null;
            Exedron.MyData.InvoiceSender.Reply = () => DirectReply("mydata-accepted.xml");
            Exedron.MyData.InvoiceCancelation.Calls = 0;
            Exedron.MyData.InvoiceCancelation.Reply = () => DirectReply("mydata-cancelled.xml");
            Provider.InvoiceHandler.Instance.Calls = 0;
            Provider.InvoiceHandler.Instance.Errors = new string[0];
            Provider.InvoiceHandler.Instance.LastInvoice = null;
            Provider.InvoiceHandler.Instance.Reply = () => ProviderReply("provider-accepted.json");
            var invoice = new Invoice
            {
                InvoiceId = InvoiceId, Number = 42, Series = "TEST",
                TransactionDate = new DateTime(2026, 10, 15, 10, 30, 0, DateTimeKind.Unspecified),
                InvoiceType = new InvoiceType { OfficialEnumerator = c.OfficialCode, IsCancelation = c.Credit },
                Trader = new Trader
                {
                    TaxRegistrationNumber = c.SameVat ? "000000000" : "111111111", Country = c.Country,
                    Name = "SYNTHETIC CUSTOMER", Address = "Sample Road 20", City = "Test City", ZipCode = "20000",
                    Email = "customer@example.invalid", TaxRegistrationOffice = "TEST", Occupation = "TEST",
                    Phone1 = "0000000000", Branch = 2, VatExemption = c.VatExemption
                },
                Vehicle = new Vehicle { PlateNumber = "TEST1234" },
                PaymentType = c.Payment, TotalAmount = c.Gross, VatAmount = c.Vat
            };
            int count = c.TwoLines ? 2 : 1;
            for (int i = 0; i < count; i++) invoice.InvoiceLines.Add(new InvoiceLine
            {
                FuelTypeName = "Heating Diesel", FuelType = new FuelType { EnumeratorValue = 30, Name = "Heating Diesel" },
                Volume = 50m, TotalPrice = c.Gross / count, VatAmount = c.Vat / count, VatPercentage = c.Rate
            });
            DatabaseModel.InvoiceRows.Add(invoice);
            if (c.ParentMark.HasValue)
            {
                var parent = new Invoice { InvoiceId = Guid.Parse("22222222-2222-2222-2222-222222222222"), InvoiceType = invoice.InvoiceType };
                invoice.ParentInvoiceRelations.Add(new InvoiceRelation { ParentInvoice = parent });
                DatabaseModel.TransmissionRows.Add(new MyDataInvoice
                {
                    MyDataInvoiceId = Guid.Parse("33333333-3333-3333-3333-333333333333"), InvoiceId = parent.InvoiceId,
                    Status = 3, Mark = c.ParentMark, CanceledByMark = c.ParentCancelled ? (long?)900002 : null
                });
            }
            return invoice;
        }
        private static MyDataInvoice Current => DatabaseModel.TransmissionRows.First(r => r.InvoiceId == InvoiceId);
        private static ResponseDoc DirectReply(string file) => ResponseDoc.Deserialize(File.ReadAllText(Path.Combine(fixtures, "Responses", file)));
        private static Provider.InvoiceResponse ProviderReply(string file) => Json.Deserialize<Provider.InvoiceResponse>(File.ReadAllText(Path.Combine(fixtures, "Responses", file)));

        private static void CheckMapping(CaseFixture c, bool provider)
        {
            Reset(c);
            string error;
            using (var db = new DatabaseModel())
            {
                if (provider)
                {
                    var model = InvoiceHelper.CreateProviderInvoice(db, InvoiceId, out error);
                    Equal("", error, "mapping error");
                    Equal(c.ExpectedProviderType, model.InvoiceHeader.InvoiceType, "invoice type");
                    Equal(c.ExpectedProviderPayment, model.PaymentMethods == null ? 0 : (int)model.PaymentMethods[0].Type, "payment");
                    Equal(c.ExpectedProviderCorrelation, model.InvoiceHeader.CorrelatedInvoices != null, "correlation");
                    Equal(c.ExpectedNet, model.InvoiceDetails.Sum(l => l.NetValue), "net total");
                    Equal(c.ExpectedExemption, model.InvoiceDetails[0].VATExemptionCategory.ToString(), "exemption");
                    if (c.ExpectedClassification != null) Equal(c.ExpectedClassification, model.InvoiceDetails[0].IncomeClassification.ClassificationType, "classification");
                }
                else
                {
                    var model = InvoiceHelper.CreateInvoice(db, InvoiceId, out error);
                    Equal("", error, "mapping error");
                    Equal(c.ExpectedDirectType, model.InvoiceHeader.InvoiceType, "invoice type");
                    Equal(c.ExpectedDirectPayment, model.PaymentMethods == null ? 0 : (int)model.PaymentMethods[0].Type, "payment");
                    Equal(c.ExpectedDirectCorrelation, model.InvoiceHeader.CorrelatedInvoices != null, "correlation");
                    Equal(c.ExpectedNet, model.InvoiceDetails.Sum(l => l.NetValue), "net total");
                    Equal(c.ExpectedExemption, model.InvoiceDetails[0].VATExemptionCategory.ToString(), "exemption");
                    if (c.ExpectedClassification != null) Equal(c.ExpectedClassification, model.InvoiceDetails[0].IncomeClassification.ClassificationType, "classification");
                }
            }
            True(PrintAgent.RunSend(InvoiceId, provider), "synthetic accepted response should succeed");
            Equal(3, Current.Status, "accepted status");
            Equal((long?)900010, Current.Mark, "MARK persistence");
            if (!provider) Equal(Exedron.MyData.InvoiceSender.LastXml, Current.Data, "saved request equals submitted XML");
            Snapshot(c.Id + (provider ? ".provider-model.xml" : ".mydata.xml"), Current.Data);
        }

        private static void Snapshot(string name, string payload)
        {
            True(!string.IsNullOrWhiteSpace(payload), "payload cannot be empty");
            // Compare XML structure, ignoring formatting and XML-declaration encoding only.
            string normalized = XDocument.Parse(payload).ToString(SaveOptions.DisableFormatting);
            string path = Path.Combine(fixtures, "Expected", name);
            if (record && Thread.CurrentThread.CurrentCulture.Name == "en-US")
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, XDocument.Parse(payload).ToString() + "\n", new UTF8Encoding(false));
            }
            True(File.Exists(path), "Missing reviewed snapshot " + name + "; normal runs never generate expected output");
            string expected = XDocument.Parse(File.ReadAllText(path)).ToString(SaveOptions.DisableFormatting);
            if (expected != normalized)
            {
                string actual = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, name + ".actual.xml");
                File.WriteAllText(actual, payload);
                throw new Exception("Payload differs from " + name + "; actual: " + actual);
            }
        }

        private static void RunFailureCases()
        {
            foreach (bool provider in new[] { false, true })
            {
                string channel = provider ? "provider" : "direct";
                Run(channel + "/accepted-is-not-resent", () =>
                {
                    Reset(); True(PrintAgent.RunSend(InvoiceId, provider), "first send");
                    True(PrintAgent.RunSend(InvoiceId, provider), "repeat accepted");
                    Equal(1, Calls(provider), "transport calls");
                });
                Run(channel + "/request-saved-before-transport", () =>
                {
                    Reset();
                    Action check = () => { Equal(0, Current.Status, "pending before send"); True(Current.Data.Length > 0, "request saved"); True(DatabaseModel.SavedStates.Count > 0, "SaveChanges called"); };
                    if (provider) Provider.InvoiceHandler.Instance.Reply = () => { check(); return ProviderReply("provider-accepted.json"); };
                    else Exedron.MyData.InvoiceSender.Reply = () => { check(); return DirectReply("mydata-accepted.xml"); };
                    True(PrintAgent.RunSend(InvoiceId, provider), "send");
                });
                Run(channel + "/explicit-rejection", () =>
                {
                    Reset();
                    if (provider) Provider.InvoiceHandler.Instance.Reply = () => ProviderReply("provider-rejected.json");
                    else Exedron.MyData.InvoiceSender.Reply = () => DirectReply("mydata-rejected.xml");
                    Equal(false, PrintAgent.RunSend(InvoiceId, provider), "rejected result");
                    Equal(-1, Current.Status, "rejected status"); True(Current.Errors.Contains("rejection"), "error persisted");
                });
                Run("KNOWN-DEFECT " + channel + "/unknown-is-sent-again", () =>
                {
                    Reset();
                    if (provider) Provider.InvoiceHandler.Instance.Reply = () => null;
                    else Exedron.MyData.InvoiceSender.Reply = () => null;
                    Equal(false, PrintAgent.RunSend(InvoiceId, provider), "unknown result"); Equal(0, Current.Status, "unknown status");
                    Equal(false, PrintAgent.RunSend(InvoiceId, provider), "second unknown result");
                    Equal(2, Calls(provider), "legacy repeats transport without recovery");
                });
                Run("KNOWN-DEFECT " + channel + "/timeout-clears-existing-request", () =>
                {
                    Reset(); DatabaseModel.TransmissionRows.Add(new MyDataInvoice { InvoiceId = InvoiceId, MyDataInvoiceId = Guid.NewGuid(), Status = 0 });
                    if (provider) Provider.InvoiceHandler.Instance.Reply = () => { throw new TimeoutException("synthetic timeout"); };
                    else Exedron.MyData.InvoiceSender.Reply = () => { throw new TimeoutException("synthetic timeout"); };
                    Equal(false, PrintAgent.RunSend(InvoiceId, provider), "timeout result");
                    Equal(-1, Current.Status, "legacy misclassifies uncertain transport as rejected");
                    Equal("", Current.Data, "legacy destroys request evidence");
                });
                Run("KNOWN-DEFECT " + channel + "/first-timeout-creates-second-summary", () =>
                {
                    Reset();
                    if (provider) Provider.InvoiceHandler.Instance.Reply = () => { throw new TimeoutException("synthetic timeout"); };
                    else Exedron.MyData.InvoiceSender.Reply = () => { throw new TimeoutException("synthetic timeout"); };
                    Equal(false, PrintAgent.RunSend(InvoiceId, provider), "timeout result");
                    Equal(2, DatabaseModel.TransmissionRows.Count, "outer catch retains null reference and adds another summary; actual SQL constraints may reject it");
                });
            }
            Run("KNOWN-DEFECT provider/disabled-mydata-reports-success", () =>
            {
                Reset(); Exedron.MyData.Settings.IsActive = false;
                True(PrintAgent.RunSend(InvoiceId, true), "legacy skipped result"); Equal(0, Calls(true), "provider never called");
            });
            Run("KNOWN-DEFECT provider/missing-key-reports-success", () =>
            {
                Reset(); OptionHandler.Instance.Values["MyDataSubscriptionKey"] = "";
                True(PrintAgent.RunSend(InvoiceId, true), "legacy skipped result"); Equal(0, Calls(true), "provider never called");
            });
            Run("KNOWN-DEFECT direct/missing-user-is-not-validated", () =>
            {
                Reset(); OptionHandler.Instance.Values["MyDataUserName"] = "";
                True(PrintAgent.RunSend(InvoiceId, false), "fake accepts invalid credentials"); Equal(1, Calls(false), "transport still reached");
            });
            Run("provider/type-disabled-skips-send", () =>
            {
                Reset().InvoiceType.SendToMyData = false;
                True(PrintAgent.RunSend(InvoiceId, true), "legacy skipped result"); Equal(0, Calls(true), "no transport");
            });
            Run("direct/cancellation-records-mark", () =>
            {
                Reset(); PrintAgent.RunSend(InvoiceId, false);
                using (var db = new DatabaseModel()) PrintAgent.CancelInvoice(db, Current);
                Equal(900010L, Exedron.MyData.InvoiceCancelation.LastMark, "original MARK");
                Equal((long?)900020, Current.CanceledByMark, "cancellation MARK");
            });
            Run("KNOWN-DEFECT direct/cancellation-no-response-throws", () =>
            {
                Reset(); PrintAgent.RunSend(InvoiceId, false); Exedron.MyData.InvoiceCancelation.Reply = () => null;
                bool threw = false;
                try { using (var db = new DatabaseModel()) PrintAgent.CancelInvoice(db, Current); }
                catch (NullReferenceException) { threw = true; }
                True(threw, "legacy dereferences missing cancellation response");
            });
            Run("KNOWN-DEFECT direct/unescaped-name-fails-before-transport", () =>
            {
                Reset(new CaseFixture { Country = "DE" }).Trader.Name = "A & B";
                Equal(false, PrintAgent.RunSend(InvoiceId, false), "XML generation failure");
                Equal(0, Exedron.MyData.InvoiceSender.Calls, "no transport");
                Equal(-1, Current.Status, "failure status"); Equal("", Current.Data, "no saved payload");
                True(!string.IsNullOrWhiteSpace(Current.Errors), "failure recorded");
            });
        }
        private static int Calls(bool provider) => provider ? Provider.InvoiceHandler.Instance.Calls : Exedron.MyData.InvoiceSender.Calls;

        private static void RunSigningCases()
        {
            Run("signing/samtec-accepted", () =>
            {
                var invoice = Reset(); int prints = 0;
                PrintAgent.PrintInvoiceBoundary = value => { prints++; Equal("SYNTHETIC-SIGNATURE", value.InvoiceSignature, "signature at print boundary"); };
                using (var db = new DatabaseModel()) new PrintAgent().SignInvoice(db, invoice);
                Equal(1, Samtec.WebService.HttpClient.Calls, "device calls"); Equal(1, prints, "print handoff");
                Equal("https://example.invalid/device-qr", invoice.QRCodeData, "QR saved");
            });
            Run("signing/samtec-rejected", () =>
            {
                var invoice = Reset(); int prints = 0;
                Samtec.WebService.HttpClient.Reply = () => new Samtec.WebService.Models.Response { ResultCode = "Error: synthetic rejection" };
                PrintAgent.PrintInvoiceBoundary = value => prints++;
                using (var db = new DatabaseModel()) new PrintAgent().SignInvoice(db, invoice);
                Equal(0, prints, "no print handoff"); Equal(null, invoice.InvoiceSignature, "no signature");
            });
            Run("KNOWN-DEFECT signing/marks-printed-before-print-handoff", () =>
            {
                var invoice = Reset(); bool entered = false;
                PrintAgent.PrintInvoiceBoundary = value => { entered = true; Equal((bool?)true, value.IsPrinted, "premature print state"); throw new IOException("synthetic printer failure"); };
                using (var db = new DatabaseModel()) new PrintAgent().SignInvoice(db, invoice);
                True(entered, "printer boundary reached"); Equal((bool?)true, invoice.IsPrinted, "failure leaves printed flag");
            });
            Run("KNOWN-DEFECT signing/mydata-failure-leaves-signed-and-printed", () =>
            {
                var invoice = Reset(); bool submitted = false; int prints = 0;
                Exedron.MyData.InvoiceSender.Reply = () => DirectReply("mydata-rejected.xml");
                // Models the real PrintToPrinter boundary's early return; its rendering is excluded.
                PrintAgent.PrintInvoiceBoundary = value => { submitted = true; if (PrintAgent.RunSend(value.InvoiceId, false)) prints++; };
                using (var db = new DatabaseModel()) new PrintAgent().SignInvoice(db, invoice);
                True(submitted, "transmission attempted"); Equal(0, prints, "no print");
                Equal((bool?)true, invoice.IsPrinted, "legacy leaves printed flag");
                Equal("SYNTHETIC-SIGNATURE", invoice.InvoiceSignature, "signature remains");
            });
            Run("KNOWN-DEFECT signing/provider-path-uses-empty-signature", () =>
            {
                var invoice = Reset(); PrintAgent.SelectProvider(true); bool entered = false;
                PrintAgent.PrintInvoiceBoundary = value => { entered = true; Equal("", value.InvoiceSignature, "placeholder signature"); Equal((bool?)true, value.IsPrinted, "premature print state"); };
                using (var db = new DatabaseModel()) new PrintAgent().SignInvoice(db, invoice);
                True(entered, "print boundary reached"); Equal(0, Samtec.WebService.HttpClient.Calls, "provider bypasses device");
            });
            Run("signing/balance-and-tank-filling", () =>
            {
                Reset(); int prints = 0; PrintAgent.PrintOtherBoundary = kind => prints++;
                var balance = new Balance { BalanceId = Guid.NewGuid() }; DatabaseModel.BalanceRows.Add(balance);
                var filling = new TankFilling { TankFillingId = Guid.NewGuid() }; DatabaseModel.FillingRows.Add(filling);
                using (var db = new DatabaseModel())
                {
                    new PrintAgent().RunBalance(db, balance);
                    new PrintAgent().RunFilling(db, filling);
                }
                Equal("SYNTHETIC-SIGNATURE", balance.DocumentSign, "balance sign");
                Equal("SYNTHETIC-SIGNATURE", filling.SignSignature, "filling sign");
                Equal(2, Samtec.WebService.HttpClient.Calls, "two device calls"); Equal(2, prints, "two print handoffs");
            });
            Run("signing/alerts-and-titrimetry-use-placeholder", () =>
            {
                Reset();
                var alert = new ASFuelControl.Data.SystemEvent { EventId = Guid.NewGuid() }; DatabaseModel.AlertRows.Add(alert);
                var titrimetry = new Titrimetry { TitrimetryId = Guid.NewGuid() }; DatabaseModel.TitrimetryRows.Add(titrimetry);
                using (var db = new DatabaseModel())
                {
                    var agent = new PrintAgent(); agent.RunAlert(db, alert); agent.RunTitrimetry(db, titrimetry);
                }
                Equal("-", alert.DocumentSign, "alert placeholder"); Equal("-", titrimetry.DocumentSign, "titrimetry placeholder");
                Equal(0, Samtec.WebService.HttpClient.Calls, "no real signing");
            });
        }
    }
}

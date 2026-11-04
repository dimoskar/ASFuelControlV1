using System;
using System.Text;
using ASFuelControl.Invoicing;
using ASFuelControl.Invoicing.Mapping;
using ASFuelControl.Invoicing.Persistence;
using Exedron.Invoicing.Core;
using Newtonsoft.Json;

internal static partial class Program
{
    static InvoiceTypeCorrectionPlan TypeCorrectionPlan(string provider)
    {
        var source = new Source();
        var route = Route(provider);
        var request = source.Prepare(source.Id, route);
        request.Invoice.InvoiceSeries.InvoiceType.IsRetail = true;
        return new InvoiceTypeCorrectionPlan {
            OriginalId = source.Id,
            CreditId = Guid.NewGuid(),
            ReplacementId = Guid.NewGuid(), OriginalTypeId = Guid.NewGuid(),
            CreditTypeId = Guid.NewGuid(),
            OriginalAttemptId = Guid.NewGuid(), OriginalMark = 900010,
            CreditSeries = "CR", ReplacementSeries = "TEST", OriginalDescription = "TEST 42",
            DirectCancellation = provider == InvoiceProviderCatalog.MyDataKey,
            Original = new InvoiceWorkflowSnapshot { InvoiceId = source.Id, Invoice = request, Provider = route }
        };
    }

    static void InvoiceTypeCorrectionTests()
    {
        Test("type correction host guard survives JSON numeric round-trip but detects real changes", () => {
            var frozen = Newtonsoft.Json.Linq.JObject.FromObject(new {
                Invoice = new { TotalAmount = 300.01m, PaymentType = (byte)1,
                    TransactionDate = new DateTime(2026,11,1,9,35,0) },
                Lines = new[] { new { TotalPrice = 300.01m, VatPercentage = (short)24 } }
            });
            var persisted = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(
                JsonConvert.SerializeObject(frozen));
            Check(SqlHeatingCorrectionStore.SameHost(frozen,persisted),
                "JSON persistence changed an unchanged host snapshot");
            persisted["Invoice"]["TotalAmount"] = 300.02m;
            Check(!SqlHeatingCorrectionStore.SameHost(frozen,persisted),
                "host guard accepted a changed total amount");
        });
        foreach (string provider in new[] { InvoiceProviderCatalog.MyDataKey,
            InvoiceProviderCatalog.ITimologiaKey, InvoiceProviderCatalog.DirectIlydaKey })
            Test("11.1 correction creates the provider-specific reversal and a 1.1 replacement: " + provider, () => {
                var plan = TypeCorrectionPlan(provider);
                string before = JsonConvert.SerializeObject(plan);
                InvoiceTypeCorrectionPolicy.Validate(plan);
                var builder = new InvoiceBuilder(new InvoiceTypeResolver());
                Equal("1.1", builder.Build(plan.Request(false, 44, "TEST", new DateTime(2026, 11, 1))).InvoiceHeader.InvoiceType);
                Equal(plan.Original.Invoice.Invoice.PaymentType,
                    plan.Request(false, 44, "TEST", DateTime.Now).Invoice.PaymentType);
                var credit = plan.Request(true, 43, "CR", new DateTime(2026, 11, 1));
                Equal("11.4", builder.Build(credit).InvoiceHeader.InvoiceType);
                Equal(InvoicePaymentType.Credit, credit.Invoice.PaymentType);
                Equal(1, System.Linq.Enumerable.Count(credit.Parents));
                Equal(plan.OriginalMark, System.Linq.Enumerable.First(credit.Parents).Transmission.Mark.Value);
                Equal(before, JsonConvert.SerializeObject(plan));
            });

        Test("type correction rejects an invoice that was not frozen as 11.1", () => {
            var plan = TypeCorrectionPlan(InvoiceProviderCatalog.ITimologiaKey);
            plan.Original.Invoice.Invoice.InvoiceSeries.InvoiceType.IsRetail = false;
            Throws(() => InvoiceTypeCorrectionPolicy.Validate(plan));
        });

        Test("accepted fiscal-device signing does not block a direct-myDATA MARK correction", () => {
            var plan = TypeCorrectionPlan(InvoiceProviderCatalog.MyDataKey);
            plan.Original.Signing = new FiscalSigningRequest(Guid.NewGuid(),
                new FiscalDocumentReference(FiscalDocumentKind.Invoice, plan.OriginalId),
                new FiscalSigningScope("Samtec", "device-1", FiscalEnvironment.Test,
                    "000000000", 0), "samtec/test", Encoding.UTF8.GetBytes("signed original"));
            InvoiceTypeCorrectionPolicy.Validate(plan);

            var providerPlan = TypeCorrectionPlan(InvoiceProviderCatalog.ITimologiaKey);
            providerPlan.Original.Signing = plan.Original.Signing;
            Throws(() => InvoiceTypeCorrectionPolicy.Validate(providerPlan));
        });

        Test("type correction mapping keeps incoming transformation parents as host-only provenance", () => {
            var source = new Source();
            var parent = new ASFuelControl.Data.Invoice { InvoiceId = Guid.NewGuid(), Number = 41,
                TransactionDate = source.Invoice.TransactionDate.AddDays(-1), InvoiceType = source.Invoice.InvoiceType };
            source.Invoice.ParentInvoiceRelations.Add(new ASFuelControl.Data.InvoiceRelation {
                InvoiceRelationId = Guid.NewGuid(), ParentInvoiceId = parent.InvoiceId,
                ChildInvoiceId = source.Invoice.InvoiceId,
                RelationType = 1, // InvoiceRelationTypeEnum.Replace
                ParentInvoice = parent, ChildInvoice = source.Invoice
            });
            var request = new HostInvoiceMapper().MapForTypeCorrection(source.Invoice, new MappingContext {
                Environment = FiscalEnvironment.Test,
                Company = new CompanySettings { CompanyTaxId = "000000000", CompanyBranch = "0",
                    CompanyName = "Issuer", CompanyAddress = "Test Road 1", CompanyCity = "Athens", CompanyZipCode = "10000" },
                WholesaleCodes = new System.Collections.Generic.HashSet<int>(new[] { 100 }),
                Countries = new[] { new CountryInput { CountryCode = "GR", IsEu = true } }
            });
            Equal("1.1", new InvoiceBuilder(new InvoiceTypeResolver()).Build(request).InvoiceHeader.InvoiceType);
            Check(request.SaleCorrelations == null, "host transformation parent became an AADE sale correlation");
            Equal(1, source.Invoice.ParentInvoiceRelations.Count);
        });

        Test("direct correction cancels before issuing the replacement and honours pause", () => {
            var plan = TypeCorrectionPlan(InvoiceProviderCatalog.MyDataKey);
            var http = new Http(plan.ReplacementId, InvoiceProviderCatalog.MyDataKey);
            var printer = new Printer(); int cancellations = 0, replacements = 0; string state = ""; bool pause = false;
            Func<bool, bool, InvoiceWorkflowResult> issue = (credit, newFiscalDocument) => {
                Check(!credit, "direct correction attempted a credit invoice"); replacements++;
                Check(!newFiscalDocument, "successful MARK cancellation treated the replacement as a second fiscal-device document");
                return Workflow(http, printer).Process(plan.ReplacementId,
                    new WorkflowSource { Snapshot = new InvoiceWorkflowSnapshot {
                        InvoiceId = plan.ReplacementId, Provider = plan.Original.Provider,
                        Invoice = plan.Request(false, 44, "TEST", DateTime.Now),
                        PrintDestination = "test", PrintFormat = "test/v1", PrintContent = Encoding.UTF8.GetBytes("correction")
                    } }, false);
            };
            InvoiceTypeCorrectionRunner.Run(true, () => { cancellations++; return 700010; }, issue,
                (saved, message) => { state = saved; pause = saved == "MarkCancelled"; }, () => pause);
            Equal(1, cancellations); Equal(0, replacements); Equal("MarkCancelled", state);
            pause = false; int finalizations = 0;
            InvoiceTypeCorrectionRunner.Run(true, () => { cancellations++; return 700010; }, issue,
                (saved, message) => state = saved, () => pause,
                replacement => { finalizations++; return "myDiesel MARK pending"; });
            Equal(2, cancellations); Equal(1, replacements); Equal(1,finalizations); Equal("MyDieselPending", state);
            InvoiceTypeCorrectionRunner.Run(true, () => { cancellations++; return 700010; }, issue,
                (saved, message) => state = saved, () => pause,
                replacement => { finalizations++; return null; });
            Equal(3,cancellations); Equal(2,replacements); Equal(2,finalizations); Equal("Completed",state);
        });

        Test("direct correction falls back to an 11.4 credit only after a definite cancellation rejection", () => {
            var plan = TypeCorrectionPlan(InvoiceProviderCatalog.MyDataKey);
            int credits = 0, replacements = 0; string state = "";
            Func<bool, bool, InvoiceWorkflowResult> issue = (credit, newFiscalDocument) => {
                Check(newFiscalDocument, "credit fallback did not identify its children as new fiscal documents");
                if (credit) credits++; else replacements++;
                Guid id = credit ? plan.CreditId : plan.ReplacementId;
                return Workflow(new Http(id, InvoiceProviderCatalog.MyDataKey), new Printer()).Process(id,
                    new WorkflowSource { Snapshot = new InvoiceWorkflowSnapshot {
                        InvoiceId = id, Provider = plan.Original.Provider,
                        Invoice = plan.Request(credit, credit ? 43 : 44, credit ? "CR" : "TEST", DateTime.Now),
                        PrintDestination = "test", PrintFormat = "test/v1",
                        PrintContent = Encoding.UTF8.GetBytes(credit ? "credit" : "replacement")
                    } }, false);
            };
            InvoiceTypeCorrectionRunner.Run(true,
                () => { throw new InvoiceMarkCancellationRejectedException("249 - cancellation rejected"); }, issue,
                (saved, message) => state = saved, () => false);
            Equal(1, credits); Equal(1, replacements); Equal("Completed", state);
        });

        Test("direct correction does not issue a credit when the cancellation outcome is unknown", () => {
            int issues = 0;
            Throws(() => InvoiceTypeCorrectionRunner.Run(true,
                () => { throw new InvalidOperationException("connection lost"); },
                (credit, newFiscalDocument) => { issues++; return null; },
                (saved, message) => { }, () => false));
            Equal(0, issues);
        });

        Test("type correction reservation protects both provider children and verifies its plan hash", () => {
            var plan = TypeCorrectionPlan(InvoiceProviderCatalog.ITimologiaKey);
            var store = new SqlInvoiceTypeCorrectionStore(database);
            using (var lease = database.EnterInvoice(plan.OriginalId))
                store.Reserve(lease, new InvoiceTypeCorrectionRecord {
                    OriginalInvoiceId = plan.OriginalId, BatchId = Guid.NewGuid(), CreditInvoiceId = plan.CreditId,
                    ReplacementInvoiceId = plan.ReplacementId, CreatedBy = Guid.NewGuid(), PlanJson = JsonConvert.SerializeObject(plan)
                });
            Check(store.IsChild(plan.CreditId, true), "credit reservation was not protected");
            Check(store.IsChild(plan.ReplacementId, true), "replacement reservation was not protected");
            using (var lease = database.EnterInvoice(plan.ReplacementId))
                Check(new SqlFiscalOwnershipStore(database).Owns(lease), "ownership missed type-correction child");
            using (var lease = database.EnterInvoice(plan.OriginalId)) store.Progress(lease, "Completed", "done");
            Check(!store.IsChild(plan.ReplacementId, true) && store.IsChild(plan.ReplacementId), "completed child protection mismatch");
            Execute(connection, "DELETE dbo.FiscalInvoiceTypeCorrection WHERE OriginalInvoiceId='" + plan.OriginalId + "';");
        });
    }
}

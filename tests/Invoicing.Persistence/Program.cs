using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Threading.Tasks;
using ASFuelControl.Invoicing.Persistence;
using Exedron.Invoicing.Core;
using Exedron.Invoicing.Core.Models;
using Exedron.Invoicing.Adapters;

internal static class Program
{
    private static SqlFiscalDatabase db;
    private static string connectionString, instance, databaseName;
    private static int passed, failed;
    private static int Main(string[] args)
    {
        if (args.Length == 0 || !Regex.IsMatch(args[0], "^ASFC_InvoicingStep3_[0-9a-f]{32}$")) throw new ArgumentException("Dedicated test instance required.");
        instance = args[0];
        databaseName = args.Length > 1 ? args[1] : "ASFC_Step3_" + Guid.NewGuid().ToString("N");
        if (!Regex.IsMatch(databaseName, "^ASFC_Step3_[0-9a-f]{32}$")) throw new ArgumentException("Dedicated test database required.");
        connectionString = Connection(databaseName);
        db = new SqlFiscalDatabase(connectionString);
        if (args.Length > 2) return Child(args[2], Guid.Parse(args[3]));
        bool created = false;
        try
        {
            Execute(Connection("master"), "CREATE DATABASE [" + databaseName + "];"); created = true;
            Execute(Connection("master"), "ALTER DATABASE [" + databaseName + "] SET COMPATIBILITY_LEVEL = 100;");
            SchemaTests(); SigningTests(); SubmissionTests(); PaymentTests(); OperationTests(); EvidenceTests();
            Console.WriteLine("RESULT: {0} passed, {1} failed", passed, failed);
            return failed == 0 ? 0 : 1;
        }
        finally
        {
            SqlConnection.ClearAllPools();
            if (created) Execute(Connection("master"), "ALTER DATABASE [" + databaseName + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [" + databaseName + "];");
        }
    }
    private static string Connection(string catalog)
    {
        return new SqlConnectionStringBuilder { DataSource = "(localdb)\\" + instance, InitialCatalog = catalog,
            IntegratedSecurity = true, Pooling = false, ConnectTimeout = 15 }.ConnectionString;
    }
    private static void SchemaTests()
    {
        Test("All active application SQL scripts pass the SQL Server 2008 compatibility checks", () => {
            var scripts = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../ASFuelControl.Windows/DBScripts"));
            var files = Directory.GetFiles(scripts, "*.sql");
            Check(files.Length >= 57, "Active migration scripts were not found.");
            foreach (var file in files)
            {
                try { ParseSql100(File.ReadAllText(file)); }
                catch (Exception ex) { throw new Exception(Path.GetFileName(file) + ": " + ex.Message, ex); }
            }
        });
        Test("Migration and every production SQL batch parse with SQL Server 2008 grammar", () => {
            var assembly = typeof(SqlFiscalDatabase).Assembly;
            using (var reader = new StreamReader(assembly.GetManifestResourceStream("FiscalSchema.sql"))) ParseSql100(reader.ReadToEnd());
            using (var reader = new StreamReader(assembly.GetManifestResourceStream("HeatingCorrectionSchema.sql"))) ParseSql100(reader.ReadToEnd());
            foreach (var type in new[] { "SqlStatements", "SqlFiscalConfigurationStore", "SqlFiscalCatalogStore", "SqlFiscalOwnershipStore", "SqlFiscalMigrationStore" })
            foreach (var statement in assembly.GetType("ASFuelControl.Invoicing.Persistence." + type).GetFields(BindingFlags.NonPublic | BindingFlags.Static).Where(f => f.IsLiteral && f.FieldType == typeof(string)))
                ParseSql100((string)statement.GetRawConstantValue());
            Equal(100, Convert.ToInt32(Scalar("SELECT compatibility_level FROM sys.databases WHERE name=DB_NAME();")));
            // Negative control: the older grammar must reject a SQL Server 2012-only statement.
            Throws<Exception>(() => ParseSql100("THROW 50000, 'not SQL 2008', 1;"));
        });
        Test("Migration failure rolls back the entire schema", () => {
            Execute(connectionString, "CREATE TABLE dbo.FiscalAttempt(TestCollision int);");
            Throws<SqlException>(() => db.InstallSchema());
            Equal(0, Convert.ToInt32(Scalar("SELECT COUNT(*) FROM sys.tables WHERE name IN ('FiscalSchemaVersion','FiscalDocumentLease');")));
            Execute(connectionString, "DROP TABLE dbo.FiscalAttempt;");
        });
        Test("Migration reruns without duplicating or deleting data", () => {
            db.InstallSchema(); db.InstallSchema();
            Equal(1, Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.FiscalSchemaVersion WHERE Version=1;")));
            Equal(10, Convert.ToInt32(Scalar("SELECT COUNT(*) FROM sys.tables WHERE name LIKE 'Fiscal%' AND name <> 'FiscalSchemaVersion';")));
        });
    }
    private static void SigningTests()
    {
        Test("Outer workflow shares its lease across signing and submission stores", () => {
            var request = Request(Document()); var signing = new SqlFiscalSigningStore(db); var submissions = new SqlMyDataSubmissionStore(db);
            using (var outer = db.EnterInvoice(request.Document.DocumentId))
            {
                using (var lease = signing.Acquire(request.Document))
                {
                    Equal(outer.FencingToken, lease.FencingToken);
                    var a = signing.Begin(lease, request, DateTime.UtcNow); a = Next(a, FiscalSigningState.Signing); signing.Save(lease, 0, a);
                    signing.Save(lease, 1, Next(a, FiscalSigningState.Signed, FiscalSigningResult.Signed(request.AttemptId, "SIG", null, "ref", "accepted")));
                }
                using (submissions.Acquire("scope", request.Document.DocumentId))
                {
                    var a = MyData(); a.InvoiceId = request.Document.DocumentId; submissions.Save(a);
                    a.State = "Accepted"; a.ResponseXml = "accepted"; submissions.Save(a);
                }
                Equal(1, new SqlFiscalHistoryStore(db).Load(outer, FiscalJournalFamily.Signing).Count);
                Equal(1, new SqlFiscalHistoryStore(db).Load(outer, FiscalJournalFamily.Submission).Count);
            }
        });
        Test("Document lease excludes another thread and another process", () => {
            var document = Document();
            using (var lease = db.Acquire(document))
            {
                Task.Run(() => Throws<InvalidOperationException>(() => new SqlFiscalDatabase(connectionString).Acquire(document))).GetAwaiter().GetResult();
                Equal(0, RunChild("contend", document.DocumentId));
            }
            using (var lease = db.Acquire(document)) Check(lease.FencingToken >= 2, "Fencing token did not advance.");
        });
        Test("Kinds isolate documents with the same GUID", () => {
            var id = Guid.NewGuid();
            using (var one = db.Acquire(new FiscalDocumentReference(FiscalDocumentKind.Invoice, id)))
            using (var two = db.Acquire(new FiscalDocumentReference(FiscalDocumentKind.Balance, id))) Check(two != null, "Kinds collide.");
        });
        Test("Signing preserves exact request, every outcome revision, QR and scope", () => {
            var store = new SqlFiscalSigningStore(db); var request = Request(Document());
            using (var lease = store.Acquire(request.Document))
            {
                var a = store.Begin(lease, request, DateTime.UtcNow);
                a = Next(a, FiscalSigningState.Signing); store.Save(lease, 0, a);
                a = Next(a, FiscalSigningState.Unknown, FiscalSigningResult.Unknown(request.AttemptId, "partial", "timeout")); store.Save(lease, 1, a);
                a = Next(a, FiscalSigningState.Signed, FiscalSigningResult.Signed(request.AttemptId, "SIG", "QR", "device-ref", "accepted")); store.Save(lease, 2, a);
                var all = store.LoadHistory(lease, request.AttemptId);
                Equal(4, all.Count); Equal("partial", all[2].Result.Response); Equal("QR", all[3].Result.QrCode);
                Equal("device-1", all[3].Request.Scope.DeviceId); Equal(request.PayloadSha256, all[0].Request.PayloadSha256);
                Throws<Exception>(() => store.Begin(lease, Request(request.Document, "device-2"), DateTime.UtcNow));
                db.InstallSchema(); Equal(4, store.LoadHistory(lease, request.AttemptId).Count);
            }
        });
        Test("CAS, immutable request and document ownership reject stale writes", () => {
            var store = new SqlFiscalSigningStore(db); var request = Request(Document());
            using (var lease = store.Acquire(request.Document))
            {
                var prepared = store.Begin(lease, request, DateTime.UtcNow);
                var sending = Next(prepared, FiscalSigningState.Signing); store.Save(lease, 0, sending);
                Throws<InvalidOperationException>(() => store.Save(lease, 0, sending));
                var changed = new FiscalSigningRequest(request.AttemptId, request.Document, request.Scope, request.PayloadFormat, Encoding.UTF8.GetBytes("changed"));
                var unknown = new FiscalSigningAttempt(changed, FiscalSigningState.Unknown, 2, prepared.CreatedUtc, DateTime.UtcNow,
                    FiscalSigningResult.Unknown(request.AttemptId, null, "timeout"));
                Throws<InvalidOperationException>(() => store.Save(lease, 1, unknown));
                Throws<InvalidOperationException>(() => store.Begin(lease, Request(Document()), DateTime.UtcNow));
                Equal(2, store.LoadHistory(lease, request.AttemptId).Count);
            }
        });
        Test("Stale fencing token and disposed lease cannot commit", () => {
            var store = new SqlFiscalSigningStore(db); var request = Request(Document());
            var lease = store.Acquire(request.Document);
            var prepared = store.Begin(lease, request, DateTime.UtcNow);
            // Fault injection confined to the generated database: simulate a replaced owner token.
            Execute(connectionString, "UPDATE dbo.FiscalDocumentLease SET FencingToken=FencingToken+1 WHERE DocumentId='" + request.Document.DocumentId + "';");
            Throws<InvalidOperationException>(() => store.Save(lease, 0, Next(prepared, FiscalSigningState.Signing)));
            lease.Dispose();
            Throws<InvalidOperationException>(() => store.Load(lease));
        });
        Test("Process crash after dispatch/remote acceptance cannot authorize resend", () => {
            var document = Document(); Equal(17, RunChild("crash", document.DocumentId));
            var store = new SqlFiscalSigningStore(db);
            using (var lease = store.Acquire(document))
            {
                var saved = store.Load(lease).Single(); Equal(FiscalSigningState.Signing, saved.State);
                Throws<Exception>(() => store.Begin(lease, Request(document, "changed-device"), DateTime.UtcNow));
                var unknown = Next(saved, FiscalSigningState.Unknown, FiscalSigningResult.Unknown(saved.Request.AttemptId, null, "Interrupted process"));
                store.Save(lease, saved.Revision, unknown);
                Throws<Exception>(() => store.Begin(lease, Request(document), DateTime.UtcNow));
            }
        });
        Test("Definitive rejection permits an explicit new attempt while retaining history", () => {
            var store = new SqlFiscalSigningStore(db); var request = Request(Document());
            using (var lease = store.Acquire(request.Document))
            {
                var a = store.Begin(lease, request, DateTime.UtcNow); a = Next(a, FiscalSigningState.Signing); store.Save(lease, 0, a);
                store.Save(lease, 1, Next(a, FiscalSigningState.Rejected, FiscalSigningResult.Rejected(request.AttemptId, "reject", "Invalid input")));
                store.Begin(lease, Request(request.Document), DateTime.UtcNow);
                Equal(2, store.Load(lease).Count); Equal(3, store.LoadHistory(lease, request.AttemptId).Count);
            }
        });
    }
    private static void SubmissionTests()
    {
        Test("Real myDATA adapter journals before fake transport and never resends accepted invoice", () => {
            var id = Guid.NewGuid(); var calls = 0;
            var handler = new FakeHttp(() => {
                calls++;
                Equal("Sending", Convert.ToString(Scalar("SELECT r.State FROM dbo.FiscalAttempt a JOIN dbo.FiscalAttemptRevision r ON a.AttemptId=r.AttemptId AND a.CurrentRevision=r.Revision WHERE a.DocumentId='" + id + "' AND a.Family='Submission';")));
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<ResponseDoc><response><statusCode>Success</statusCode><invoiceUid>uid</invoiceUid><invoiceMark>123</invoiceMark></response></ResponseDoc>") };
            });
            using (var client = new MyDataClient(new MyDataOptions { BaseUrl = "https://fixture.invalid/", UserId = "fixture", SubscriptionKey = "fixture",
                TestMode = true, Store = new SqlMyDataSubmissionStore(db), Logger = new SqlEvidenceLogger(db) }, new InvoiceXmlSerializer(), handler))
            {
                var model = new InvoiceModel { Issuer = new Trader { VATNumber = "099999999", Country = "GR" },
                    InvoiceHeader = new InvoiceHeader { AA = "1", Series = "A", InvoiceType = "11.1", Currency = "EUR", IssueDate = new DateTime(2026, 10, 15) },
                    InvoiceDetails = new[] { new InvoiceDetail { LineNumber = 1, Quantity = 1, NetValue = 100, VATAmount = 24, VATCategory = VATCategoryEnum.VAT24,
                        IncomeClassification = new IncomeClassification { Amount = 100, ClassificationCategory = "category1_1", ClassificationType = "E3_561_003" } } },
                    InvoiceSummary = new InvoiceSummary { TotalNetValue = 100, TotalVATAmount = 24, TotalGrossValue = 124 } };
                Check(!client.SendInvoice(model, id, null).HasErrors, "Fake acceptance was not persisted.");
                Check(!client.SendInvoice(model, id, null).HasErrors, "Stored acceptance was not returned."); Equal(1, calls);
            }
        });
        Test("Latest attempt uses insertion order even with identical timestamps", () => {
            var store = new SqlMyDataSubmissionStore(db); var a = MyData();
            using (store.Acquire(a.Scope, a.InvoiceId))
            {
                store.Save(a); a.State = "Rejected"; a.ResponseXml = "rejected"; store.Save(a);
                var b = MyData(); b.InvoiceId = a.InvoiceId; b.StartedUtc = a.StartedUtc;
                store.Save(b); Equal(b.AttemptId, store.Load(b.Scope, b.InvoiceId).AttemptId);
            }
        });
        Test("myDATA stores unknown evidence and blocks changed routes/environments", () => {
            var store = new SqlMyDataSubmissionStore(db); var a = MyData();
            using (store.Acquire(a.Scope, a.InvoiceId))
            {
                store.Save(a); a.State = "Unknown"; a.ResponseXml = "partial"; store.Save(a);
                var loaded = store.Load(a.Scope, a.InvoiceId); Equal("partial", loaded.ResponseXml);
                Throws<InvalidOperationException>(() => store.Load("other-environment", a.InvoiceId));
                var ilyda = new SqlIlydaSubmissionStore(db);
                Throws<InvalidOperationException>(() => ilyda.Load(a.InvoiceId));
                Throws<Exception>(() => ilyda.Save(new IlydaSubmission { AttemptId = Guid.NewGuid(), InvoiceId = a.InvoiceId,
                    TestMode = true, IssuerVat = "099999999", RequestJson = "{}", State = "Sending" }));
            }
        });
        Test("Submission rejects stale DTOs and changes to original payload", () => {
            var store = new SqlMyDataSubmissionStore(db); var a = MyData();
            using (store.Acquire(a.Scope, a.InvoiceId))
            {
                store.Save(a); var stale = store.Load(a.Scope, a.InvoiceId);
                a.State = "Unknown"; store.Save(a);
                stale.State = "Rejected"; stale.ResponseXml = "rejection";
                Throws<InvalidOperationException>(() => store.Save(stale));
                a.RequestXml = "changed"; Throws<InvalidOperationException>(() => store.Save(a));
            }
        });
        Test("Ilyda accepts queued recovery and PDF flag without replacing acceptance evidence", () => {
            var store = new SqlIlydaSubmissionStore(db); var id = Guid.NewGuid();
            using (store.Acquire(id))
            {
                var a = new IlydaSubmission { AttemptId = Guid.NewGuid(), InvoiceId = id, TestMode = true, IssuerVat = "099999999", RequestJson = "{}", State = "Sending" };
                store.Save(a); a.State = "Queued"; a.InvoiceUid = "queued-id"; a.ResponseJson = "queue"; store.Save(a);
                a.State = "Accepted"; a.ResponseJson = "accepted"; store.Save(a);
                a.PdfUploaded = true; store.Save(a); Equal(true, store.Load(id).PdfUploaded);
                a.ResponseJson = "changed"; Throws<InvalidOperationException>(() => store.Save(a));
            }
        });
        Test("iTimologia begin/complete survives restart and refuses scope switching", () => {
            var store = new SqlITimologiaSubmissionStore(db); var id = Guid.NewGuid();
            using (store.Acquire(id))
            {
                var a = new ITimologiaSubmissionAttempt { AttemptId = Guid.NewGuid(), InvoiceId = id, TestMode = true, IssuerVat = "099999999",
                    Endpoint = "https://fixture.invalid/invoices", RequestJson = "{}", State = "Sending", CreatedUtc = DateTime.UtcNow };
                store.Begin(a); a.State = "Unknown"; a.TransportError = "timeout"; a.CompletedUtc = DateTime.UtcNow; store.Complete(a);
                var other = new SqlITimologiaSubmissionStore(db);
                Equal("Unknown", other.LoadLatest(id, true, a.IssuerVat).State);
                Throws<InvalidOperationException>(() => other.LoadLatest(id, false, a.IssuerVat));
                a.State = "Accepted"; a.ResponseJson = "accepted"; store.Complete(a);
                Equal("Accepted", other.LoadLatest(id, true, a.IssuerVat).State);
            }
        });
        Test("Ilyda POS session retains original signature and cancellation history", () => {
            var store = new SqlIlydaPosStore(db); var id = Guid.NewGuid();
            using (store.Acquire(id))
            {
                var a = new IlydaPosSession { Id = Guid.NewGuid(), InvoiceId = id, TestMode = true, IssuerVat = "099999999",
                    Endpoint = "https://fixture.invalid/sign", RequestJson = "{}", State = "Requesting" };
                store.Save(a); a.ResponseJson = "ready"; store.Save(a); a.State = "Ready"; store.Save(a);
                a.State = "CancelPending"; a.CancelReason = "TRANSACTION_FAILED"; store.Save(a);
                a.State = "Cancelled"; store.Save(a); Equal("Cancelled", store.Load(id).State);
                Equal(5, HistoryCount(a.Id));
            }
        });
    }
    private static void PaymentTests()
    {
        Test("Payment requires an outer workflow lease", () => {
            var store = new SqlProviderPaymentStore(db, "Test|issuer|gateway");
            Throws<InvalidOperationException>(() => store.GetOrCreate(PaymentRequest(Guid.NewGuid())));
        });
        Test("Payment recovery reuses UID and never turns unknown into new authorization", () => {
            var store = new SqlProviderPaymentStore(db, "Test|issuer|gateway"); var id = Guid.NewGuid();
            using (db.EnterInvoice(id))
            {
                var request = PaymentRequest(id); var a = store.GetOrCreate(request);
                store.SaveSignature(a.Id, Signature()); store.MarkPending(a.Id); store.MarkFailure(a.Id, "Unknown", "timeout", "reply lost");
                Equal(a.Id, store.GetOrCreate(request).Id); store.MarkPending(a.Id); Equal("Unknown", store.LoadLatest(id).State);
                store.MarkFailure(a.Id, "Failed", "exception", "legacy caller"); Equal("Unknown", store.LoadLatest(id).State);
                request.TerminalId = "another"; Throws<InvalidOperationException>(() => store.GetOrCreate(request));
                store.MarkApproved(a.Id, Authorization(a)); store.MarkInvoiceSubmitted(a.Id);
                Equal("Invoiced", store.LoadLatest(id).State); Throws<InvalidOperationException>(() => store.MarkPending(a.Id));
            }
        });
        Test("Mismatched authorization cannot become approved", () => {
            var store = new SqlProviderPaymentStore(db, "Test|issuer|gateway"); var id = Guid.NewGuid();
            using (db.EnterInvoice(id))
            {
                var a = store.GetOrCreate(PaymentRequest(id)); store.SaveSignature(a.Id, Signature()); store.MarkPending(a.Id);
                var response = Authorization(a); response.AmountMinorUnits++;
                Throws<InvalidOperationException>(() => store.MarkApproved(a.Id, response)); Equal("Pending", store.LoadLatest(id).State);
            }
        });
        Test("Void recovery preserves its UID; new payment follows confirmed void only", () => {
            var store = new SqlProviderPaymentStore(db, "Test|issuer|gateway"); var id = Guid.NewGuid();
            using (db.EnterInvoice(id))
            {
                var request = PaymentRequest(id); var a = store.GetOrCreate(request);
                store.SaveSignature(a.Id, Signature()); store.MarkPending(a.Id); store.MarkApproved(a.Id, Authorization(a));
                store.MarkVoidPending(a.Id, "void-1"); store.MarkVoidFailed(a.Id, "timeout");
                Equal(a.Id, store.GetOrCreate(request).Id);
                Throws<InvalidOperationException>(() => store.MarkVoidPending(a.Id, "void-2"));
                store.MarkVoidPending(a.Id, "void-1"); store.MarkVoided(a.Id);
                var next = store.GetOrCreate(request); Check(next.Id != a.Id && next.TransactionUid != a.TransactionUid, "Payment identity reused.");
                Equal(a.Id, next.OriginalPaymentAttemptId.Value); Equal(2, next.AttemptNumber);
            }
        });
    }
    private static void OperationTests()
    {
        Test("Print handoff is separate; explicit reprint uses saved fiscal evidence only", () => {
            var signing = new SqlFiscalSigningStore(db); var request = Request(Document()); var jobs = new SqlFiscalOperationStore(db);
            using (var lease = signing.Acquire(request.Document))
            {
                var a = signing.Begin(lease, request, DateTime.UtcNow); a = Next(a, FiscalSigningState.Signing); signing.Save(lease, 0, a);
                signing.Save(lease, 1, Next(a, FiscalSigningState.Signed, FiscalSigningResult.Signed(request.AttemptId, "SIG", "QR", "ref", "accepted")));
                var job = jobs.BeginPrint(lease, request.AttemptId, "printer-1", "pdf", Encoding.UTF8.GetBytes("rendered"));
                jobs.SavePrint(lease, job, FiscalPrintState.Dispatching, "spool request"); jobs.SavePrint(lease, job, FiscalPrintState.HandedOff, "spool-id");
                Equal("HandedOff", jobs.Load(lease, true).Single().State);
                Throws<InvalidOperationException>(() => jobs.BeginPrint(lease, request.AttemptId, "printer-1", "pdf", new byte[] { 1 }));
                var reprint = jobs.BeginPrint(lease, request.AttemptId, "printer-1", "pdf", Encoding.UTF8.GetBytes("rendered"), "Operator requested copy");
                jobs.SavePrint(lease, reprint, FiscalPrintState.Dispatching, "write started"); jobs.SavePrint(lease, reprint, FiscalPrintState.Unknown, "connection lost");
                Throws<Exception>(() => jobs.BeginPrint(lease, request.AttemptId, "printer-1", "pdf", new byte[] { 1 }, "retry"));
                Equal(3, signing.LoadHistory(lease, request.AttemptId).Count);
            }
        });
        Test("Cancellation retains historical scope and cannot reset accepted submission", () => {
            var store = new SqlMyDataSubmissionStore(db); var a = MyData();
            using (store.Acquire(a.Scope, a.InvoiceId)) { store.Save(a); a.State = "Accepted"; a.ResponseXml = "accepted"; store.Save(a); }
            var jobs = new SqlFiscalOperationStore(db);
            using (var lease = db.Acquire(new FiscalDocumentReference(FiscalDocumentKind.Invoice, a.InvoiceId)))
            {
                var cancel = jobs.BeginCancellation(lease, a.AttemptId, "cancel.v1", Encoding.UTF8.GetBytes("mark=123"));
                Check(cancel.Scope.Contains(a.Scope), "Cancellation lost historical scope.");
                jobs.SaveCancellation(lease, cancel, FiscalTransmissionState.Sending, "sending");
                jobs.SaveCancellation(lease, cancel, FiscalTransmissionState.Unknown, "timeout");
                Throws<Exception>(() => jobs.BeginCancellation(lease, a.AttemptId, "cancel.v1", new byte[] { 1 }));
                jobs.SaveCancellation(lease, cancel, FiscalTransmissionState.Accepted, "cancellationMark=456");
            }
            using (store.Acquire(a.Scope, a.InvoiceId)) Equal("Accepted", store.Load(a.Scope, a.InvoiceId).State);
        });
    }
    private static void EvidenceTests()
    {
        Test("HTTP evidence is written before response and never overwritten", () => {
            var logger = new SqlEvidenceLogger(db); var id = Guid.NewGuid();
            logger.RequestStarted(new InvoiceRequestLog { ExchangeId = id, Integration = "fixture", StartedUtc = DateTimeOffset.UtcNow, Body = "request" });
            var reply = new InvoiceResponseLog { ExchangeId = id, CompletedUtc = DateTimeOffset.UtcNow, HttpStatus = 200, Body = "response" };
            logger.ResponseReceived(reply); Throws<InvalidOperationException>(() => logger.ResponseReceived(reply));
            reply.ExchangeId = Guid.NewGuid(); Throws<InvalidOperationException>(() => logger.ResponseReceived(reply));
            logger.Record(new PaymentGatewayEvent { TransactionUid = "fixture-uid", ResponseJson = "first" });
            logger.Record(new PaymentGatewayEvent { TransactionUid = "fixture-uid", ResponseJson = "second" });
            Equal(2, Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.FiscalPaymentEvent WHERE TransactionUid='fixture-uid';")));
        });
    }
    private static int Child(string mode, Guid id)
    {
        var doc = new FiscalDocumentReference(FiscalDocumentKind.Invoice, id);
        if (mode == "contend") { try { using (db.Acquire(doc)) return 2; } catch (InvalidOperationException) { return 0; } }
        var store = new SqlFiscalSigningStore(db); var lease = store.Acquire(doc); var request = Request(doc);
        var a = store.Begin(lease, request, DateTime.UtcNow); store.Save(lease, 0, Next(a, FiscalSigningState.Signing));
        Console.WriteLine("Simulated external acceptance; process exits before saving response.");
        Environment.Exit(17); return 17;
    }
    private static int RunChild(string mode, Guid id)
    {
        var start = new ProcessStartInfo(typeof(Program).Assembly.Location, instance + " " + databaseName + " " + mode + " " + id) {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        using (var child = Process.Start(start))
        {
            if (!child.WaitForExit(30000)) { child.Kill(); throw new Exception("Child test timed out."); }
            string error = child.StandardError.ReadToEnd(); if (error.Length > 0) throw new Exception(error);
            return child.ExitCode;
        }
    }
    private static FiscalDocumentReference Document() { return new FiscalDocumentReference(FiscalDocumentKind.Invoice, Guid.NewGuid()); }
    private static FiscalSigningRequest Request(FiscalDocumentReference document, string device = "device-1")
    { return new FiscalSigningRequest(Guid.NewGuid(), document, new FiscalSigningScope("samtec", device, FiscalEnvironment.Test, "099999999", 0), "fixture.v1", Encoding.UTF8.GetBytes("exact request αβ")); }
    private static FiscalSigningAttempt Next(FiscalSigningAttempt a, FiscalSigningState state, FiscalSigningResult result = null)
    { return new FiscalSigningAttempt(a.Request, state, a.Revision + 1, a.CreatedUtc, DateTime.UtcNow, result); }
    private static MyDataSubmission MyData()
    { return new MyDataSubmission { AttemptId = Guid.NewGuid(), InvoiceId = Guid.NewGuid(), Scope = "https://fixture.invalid|user", RequestXml = "<invoice />", State = "Sending", StartedUtc = DateTimeOffset.UtcNow }; }
    private static ProviderPaymentAttemptRequest PaymentRequest(Guid id)
    { return new ProviderPaymentAttemptRequest { InvoiceId = id, Gateway = "GBridge", InvoiceProvider = "iTimologia", ProviderId = 34, PaymentType = 0, TerminalId = "terminal-1", Amount = 12.34m, AmountMinorUnits = 1234 }; }
    private static ProviderPaymentSignature Signature() { return new ProviderPaymentSignature { ProviderInput = "input", ProviderSignature = "signature", ProviderUid = "uid" }; }
    private static PaymentAuthorizationResult Authorization(ProviderPaymentAttempt a)
    { return new PaymentAuthorizationResult { TransactionId = "gateway-id", ResponseTransactionUid = a.TransactionUid, TerminalId = a.TerminalId, AmountMinorUnits = a.AmountMinorUnits, PaymentType = a.PaymentType, TransactionType = (int)a.Operation }; }
    private static int HistoryCount(Guid id) { return Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.FiscalAttemptRevision WHERE AttemptId='" + id + "';")); }
    private static void ParseSql100(string sql)
    {
        IList<ParseError> errors;
        var fragment = new TSql100Parser(true).Parse(new StringReader(sql), out errors);
        if (errors.Count > 0) throw new Exception(string.Join("; ", errors.Select(e => e.Message)));
        // Some new keywords parse as ordinary procedure/function names in the old grammar.
        var code = string.Concat(fragment.ScriptTokenStream.Where(t => t.TokenType != TSqlTokenType.AsciiStringLiteral &&
            t.TokenType != TSqlTokenType.UnicodeStringLiteral && t.TokenType != TSqlTokenType.SingleLineComment && t.TokenType != TSqlTokenType.MultilineComment).Select(t => t.Text));
        var newer = Regex.Match(code, @"(?<![@\w])(THROW|OFFSET)\b|\b(OPENJSON|JSON_VALUE|JSON_QUERY|JSON_MODIFY|STRING_AGG|STRING_SPLIT|TRY_CONVERT|TRY_CAST|DATEFROMPARTS|DATETIMEFROMPARTS|EOMONTH|FORMAT|IIF|CONCAT)\s*\(|\bCREATE\s+(SEQUENCE|OR\s+ALTER)\b|\bDROP\s+TABLE\s+IF\s+EXISTS\b", RegexOptions.IgnoreCase);
        if (newer.Success) throw new Exception("Post-SQL Server 2008 construct detected: " + newer.Value);
        foreach(var token in fragment.ScriptTokenStream.Where(t=>t.TokenType==TSqlTokenType.UnicodeStringLiteral))
        {
            var inner=token.Text.Substring(2,token.Text.Length-3).Replace("''","'");
            if(inner.TrimStart().StartsWith("ALTER VIEW",StringComparison.OrdinalIgnoreCase))ParseSql100(inner);
        }
    }
    private sealed class FakeHttp : HttpMessageHandler
    {
        private readonly Func<HttpResponseMessage> response;
        internal FakeHttp(Func<HttpResponseMessage> response) { this.response = response; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        { return Task.FromResult(response()); }
    }
    private static object Scalar(string sql)
    { using (var c = new SqlConnection(connectionString)) { c.Open(); using (var cmd = new SqlCommand(sql, c)) return cmd.ExecuteScalar(); } }
    private static void Execute(string connection, string sql)
    { using (var c = new SqlConnection(connection)) { c.Open(); using (var cmd = new SqlCommand(sql, c)) { cmd.CommandTimeout = 60; cmd.ExecuteNonQuery(); } } }
    private static void Test(string name, Action action)
    { try { action(); passed++; Console.WriteLine("PASS " + name); } catch (Exception ex) { failed++; Console.WriteLine("FAIL " + name + ": " + ex); } }
    private static void Equal<T>(T expected, T actual) { Check(object.Equals(expected, actual), "Expected " + expected + ", got " + actual); }
    private static void Check(bool value, string error) { if (!value) throw new Exception(error); }
    private static void Throws<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new Exception("Expected " + typeof(T).Name); }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ASFuelControl.Data;
using ASFuelControl.Windows.ViewModels;

internal static class Program
{
    private static int passed;
    private static string connection;
    private static readonly Guid type = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid admin = Guid.Parse("00000000-0000-0000-0000-000000000010");
    private static readonly Dictionary<Guid, int> types = new Dictionary<Guid, int>
    {
        { type, 0 }, { Guid.Parse("00000000-0000-0000-0000-000000000002"), 1 },
        { Guid.Parse("00000000-0000-0000-0000-000000000003"), 2 }
    };

    private static int Main(string[] args)
    {
        if (args.Length < 2 || args.Length > 4 || !Regex.IsMatch(args[0], "^ASFC_Catalog_[0-9a-f]{32}$"))
            throw new ArgumentException("Private catalog test instance and repository path required.");
        string name = "Catalog_" + Guid.NewGuid().ToString("N");
        var cs = new SqlConnectionStringBuilder { DataSource = "(localdb)\\" + args[0], InitialCatalog = "master", IntegratedSecurity = true, Pooling = false };
        string master = cs.ConnectionString;
        cs.InitialCatalog = name; connection = cs.ConnectionString;
        bool created = false;
        try
        {
            Execute(master, "CREATE DATABASE [" + name + "];"); created = true;
            Execute(master, "ALTER DATABASE [" + name + "] SET COMPATIBILITY_LEVEL = 100;");
            string repo = args[1];
            Execute(connection, File.ReadAllText(Path.Combine(repo, "tests/InvoiceCatalog/Schema.sql")));
            string old35 = File.ReadAllText(Path.Combine(repo, "ASFuelControl.Windows/DBScripts/UpdateDatabase35.sql"));
            string function = Regex.Match(old35, @"CREATE FUNCTION GetInvoiceFuelTypes[\s\S]+?^GO", RegexOptions.Multiline).Value;
            Execute(connection, Regex.Replace(function, @"\bGO\s*$", ""));
            string old49 = File.ReadAllText(Path.Combine(repo, "ASFuelControl.Windows/DBScripts/UpdateDatabase49.sql"));
            Script(old49.Replace("ALTER VIEW", "CREATE VIEW"));
            Script(old49.Replace("ALTER VIEW", "CREATE VIEW").Replace("[dbo].[InvoiceCatalogView]", "[dbo].[LegacyInvoiceCatalogView]"));
            string migration = File.ReadAllText(Path.Combine(repo, "ASFuelControl.Windows/DBScripts/UpdateDatabase60.sql"));
            Script(migration);
            int indexes = Convert.ToInt32(Scalar("SELECT COUNT(*) FROM sys.indexes WHERE name LIKE 'IX_InvoiceCatalog_%'"));
            Script(migration);
            Check(indexes == 3 && indexes == Convert.ToInt32(Scalar("SELECT COUNT(*) FROM sys.indexes WHERE name LIKE 'IX_InvoiceCatalog_%'")), "migration is repeatable and reuses existing date/line indexes");
            string heatingMigration=File.ReadAllText(Path.Combine(repo,"ASFuelControl.Windows/DBScripts/UpdateDatabase61.sql"));
            Script(heatingMigration);Script(heatingMigration);
            string fiscalMigration=File.ReadAllText(Path.Combine(repo,"ASFuelControl.Windows/DBScripts/UpdateDatabase57.sql"));
            Script(fiscalMigration);Script(fiscalMigration);
            string workflowMigration=File.ReadAllText(Path.Combine(repo,"ASFuelControl.Windows/DBScripts/UpdateDatabase58.sql"));
            Script(workflowMigration);Script(workflowMigration);
            string configurationMigration=File.ReadAllText(Path.Combine(repo,"ASFuelControl.Windows/DBScripts/UpdateDatabase59.sql"));
            Script(configurationMigration);Script(configurationMigration);
            Check(Convert.ToInt32(Scalar("SELECT compatibility_level FROM sys.databases WHERE name = DB_NAME()")) == 100, "database compatibility level 100");
            Check(Convert.ToInt32(Scalar("SELECT COUNT(*) FROM (SELECT * FROM dbo.LegacyInvoiceCatalogView EXCEPT SELECT * FROM dbo.InvoiceCatalogView) d")) == 0 &&
                  Convert.ToInt32(Scalar("SELECT COUNT(*) FROM (SELECT * FROM dbo.InvoiceCatalogView EXCEPT SELECT * FROM dbo.LegacyInvoiceCatalogView) d")) == 0,
                  "all SQL view columns match the legacy view, in both directions (2,405 invoices)");
            Check(Convert.ToInt32(Scalar("SELECT MAX(LEN(FuelTypes)) FROM dbo.InvoiceCatalogView")) == 1000, "legacy fuel-list length limit preserved");
            if (args.Length < 4 || args[3] != "--native-only")
            {
                Filters();
                CancellationPairTotals();
                SnapshotAndTotals();
                AsyncLoads().GetAwaiter().GetResult();
                Benchmark();
                HeatingCorrectionTotals();
            }
            if (args.Length >= 3)
            {
                using (var native = Process.Start(new ProcessStartInfo(args[2], "\"" + connection + "\"") {
                    UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true,
                    WorkingDirectory = Path.GetDirectoryName(args[2]) }))
                {
                    var stdout = native.StandardOutput.ReadToEndAsync();
                    var stderr = native.StandardError.ReadToEndAsync();
                    if (!native.WaitForExit(60000)) { native.Kill(); throw new TimeoutException("Native catalog probe timed out."); }
                    Console.Write(stdout.Result); Console.Write(stderr.Result);
                    Check(native.ExitCode == 0, "native WinForms catalog integration");
                }
            }
            Console.WriteLine("RESULT: " + passed + " passed.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally
        {
            SqlConnection.ClearAllPools();
            if (created) Execute(master, "ALTER DATABASE [" + name + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [" + name + "];");
        }
    }

    private static void Filters()
    {
        var day = new DateTime(2026, 10, 18);
        foreach (var search in new[] { null, "", "12", "-12", "0012", "12,13", "0012,13", "12, 13", "12,", ",", "ΠΕΛΑΤΗΣ", "AB-1234", "SUP-1", "no match", "%", "_" })
        {
            using (var db = new DatabaseModel(connection))
            {
                var filter = new InvoiceCatalogFilter(day.AddHours(15), day.AddHours(3), Guid.Empty, search);
                var expected = LegacyFilter(db.InvoiceCatalogViews, filter).Select(i => i.InvoiceId).ToArray();
                var actual = filter.Apply(db.InvoiceCatalogViews).Select(i => i.InvoiceId).ToArray();
                Check(expected.OrderBy(i => i).SequenceEqual(actual.OrderBy(i => i)), "filter matches legacy: " + (search ?? "<null>"));
            }
        }
        foreach (var filter in new[] {
            new InvoiceCatalogFilter(day, day, type, ""),
            new InvoiceCatalogFilter(day, day, Guid.NewGuid(), ""),
            new InvoiceCatalogFilter(day.AddDays(1), day, Guid.Empty, ""),
            new InvoiceCatalogFilter(day.AddDays(-1), day.AddDays(1), Guid.Empty, ""),
            new InvoiceCatalogFilter(day, DateTime.MaxValue, Guid.Empty, "") })
        {
            using (var db = new DatabaseModel(connection))
            {
                var expected = LegacyFilter(db.InvoiceCatalogViews, filter).Select(i => i.InvoiceId).ToArray();
                var actual = filter.Apply(db.InvoiceCatalogViews).ToArray();
                Check(expected.OrderBy(i => i).SequenceEqual(actual.Select(i => i.InvoiceId).OrderBy(i => i)), "date/type filter matches legacy: " + filter.To.ToString("yyyy-MM-dd") + "/" + filter.InvoiceTypeId);
                Check(actual.Select(i => i.TransactionDate).SequenceEqual(actual.Select(i => i.TransactionDate).OrderByDescending(i => i)), "descending database ordering");
            }
        }
    }
    private static void HeatingCorrectionTotals()
    {
        var original=Guid.NewGuid();var credit=Guid.NewGuid();var replacement=Guid.NewGuid();
        Execute(connection,"INSERT dbo.Invoice(InvoiceId,InvoiceTypeId,TransactionDate,Number,PaymentType,TotalAmount) VALUES('"+original+"','"+type+"','20261022',9001,1,124),('"+credit+"','"+type+"','20261022',9002,0,124),('"+replacement+"','"+type+"','20261022',9003,0,124);"+
            "INSERT dbo.FiscalHeatingCorrection(OriginalInvoiceId,BatchId,CreditInvoiceId,ReplacementInvoiceId,PlanJson,PlanSha256,State,Message,CreatedUtc,UpdatedUtc,CreatedBy) VALUES('"+original+"',NEWID(),'"+credit+"','"+replacement+"','{}',REPLICATE('0',64),'Completed','test',GETUTCDATE(),GETUTCDATE(),NEWID());");
        using(var db=new DatabaseModel(connection)) {
            var rows=db.InvoiceCatalogViews.Where(i=>i.InvoiceId==original||i.InvoiceId==credit||i.InvoiceId==replacement).ToArray();
            Check(rows.Single(i=>i.InvoiceId==credit).TransactionSign==-1,"batch credit reverses the original catalog sign without changing its invoice type");
            Check(rows.All(i=>!i.IsCanceled&&!i.IsReplaced),"correction preserves visibility of all three documents");
            var totals=new InvoiceCatalogTotals(rows,types);
            Check(totals.Get(4,"TotalAmount")==124&&totals.Get(1,"TotalAmount")==0&&totals.Get(0,"TotalAmount")==124,
                "batch pair offsets in credit sales and leaves the original cash total intact");
        }
    }

    private static void CancellationPairTotals()
    {
        var rows = new[]
        {
            new InvoiceCatalogView
            {
                InvoiceId = Guid.NewGuid(), InvoiceTypeId = type, PaymentType = 0,
                TotalAmount = 31.83m, TransactionSign = 1, Canceled = 1, Replaced = 1
            },
            new InvoiceCatalogView
            {
                InvoiceId = Guid.NewGuid(), InvoiceTypeId = type, PaymentType = 0,
                TotalAmount = 31.83m, TransactionSign = 1, Cancelation = 1
            },
            new InvoiceCatalogView
            {
                InvoiceId = Guid.NewGuid(), InvoiceTypeId = type, PaymentType = 0,
                TotalAmount = 25.67m, TransactionSign = 1
            },
            new InvoiceCatalogView
            {
                InvoiceId = Guid.NewGuid(), InvoiceTypeId = type, PaymentType = 0,
                TotalAmount = 206.05m, TransactionSign = 1
            }
        };
        var totals = new InvoiceCatalogTotals(rows, types);
        Check(totals.Get(4, "TotalAmount") == 231.72m &&
              totals.Get(1, "TotalAmount") == 231.72m,
            "catalog totals omit both sides of a cancellation pair");
    }

    private static IQueryable<InvoiceCatalogView> LegacyFilter(IQueryable<InvoiceCatalogView> q, InvoiceCatalogFilter filter)
    {
        var from = filter.From; var to = filter.To; var search = filter.Search; var typeId = filter.InvoiceTypeId;
        q = q.Where(i => i.TransactionDate.Date >= from.Date && i.TransactionDate.Date <= to.Date);
        if (typeId != Guid.Empty) q = q.Where(i => i.InvoiceTypeId == typeId);
        if (!string.IsNullOrEmpty(search))
        {
            if (search.Contains(","))
            {
                var numbers = search.Split(',');
                q = q.Where(i => numbers.Contains(i.Number.ToString()));
            }
            else q = q.Where(i => i.Number.ToString().Contains(search) || i.TraderName.Contains(search) || i.PlateNumber.Contains(search) || i.SupplyNumber.Contains(search));
        }
        return q;
    }

    private static void SnapshotAndTotals()
    {
        var filter = new InvoiceCatalogFilter(new DateTime(2026, 10, 17), new DateTime(2026, 10, 19), Guid.Empty, "");
        var localCopyParent = (Guid)Scalar("SELECT InvoiceId FROM dbo.Invoice WHERE Number=2");
        var localCopyChild = (Guid)Scalar("SELECT InvoiceId FROM dbo.Invoice WHERE Number=3");
        var markCancelledInvoice = (Guid)Scalar("SELECT InvoiceId FROM dbo.Invoice WHERE Number=4");
        var releasedOriginal = (Guid)Scalar("SELECT InvoiceId FROM dbo.Invoice WHERE Number=7");
        var cancelledCredit = (Guid)Scalar("SELECT InvoiceId FROM dbo.Invoice WHERE Number=8");
        var activeOriginal = (Guid)Scalar("SELECT InvoiceId FROM dbo.Invoice WHERE Number=11");
        var activeCredit = (Guid)Scalar("SELECT InvoiceId FROM dbo.Invoice WHERE Number=12");
        var failedInvoice = (Guid)Scalar("SELECT InvoiceId FROM dbo.Invoice WHERE Number=14");
        var resolvedInvoice = (Guid)Scalar("SELECT InvoiceId FROM dbo.Invoice WHERE Number=15");
        var transmittedWithoutMark = (Guid)Scalar("SELECT InvoiceId FROM dbo.Invoice WHERE Number=16");
        var transmittedWithMark = (Guid)Scalar("SELECT InvoiceId FROM dbo.Invoice WHERE Number=17");
        var currentSignWithoutSignature = (Guid)Scalar("SELECT InvoiceId FROM dbo.Invoice WHERE Number=18");
        var untouchedDraft = (Guid)Scalar("SELECT InvoiceId FROM dbo.Invoice WHERE Number=19");
        var currentTransmissionWithoutMark = (Guid)Scalar("SELECT InvoiceId FROM dbo.Invoice WHERE Number=22");
        var historicalInvoice = (Guid)Scalar("SELECT InvoiceId FROM dbo.Invoice WHERE Number=25");
        var frozenLocalInvoice = (Guid)Scalar("SELECT InvoiceId FROM dbo.Invoice WHERE Number=28");
        var currentTransmissionWithMark = (Guid)Scalar("SELECT InvoiceId FROM dbo.Invoice WHERE Number=31");
        Execute(connection, "INSERT dbo.InvoiceRelation(ParentInvoiceId,ChildInvoiceId,RelationType) VALUES('" +
            localCopyParent + "','" + localCopyChild + "',2); INSERT dbo.MyDataInvoice(MyDataInvoiceId,InvoiceId,CanceledByMark) VALUES(NEWID(),'" +
            markCancelledInvoice + "',900004); INSERT dbo.InvoiceRelation(ParentInvoiceId,ChildInvoiceId,RelationType) VALUES('" +
            releasedOriginal + "','" + cancelledCredit + "',0),('" + activeOriginal + "','" + activeCredit + "',0); " +
            "INSERT dbo.MyDataInvoice(MyDataInvoiceId,InvoiceId,CanceledByMark) VALUES(NEWID(),'" + cancelledCredit + "',900008)");
        Execute(connection,@"INSERT dbo.FiscalConfiguration(ConfigurationJson,CreatedUtc,CreatedBy)
VALUES(N'{""Enabled"":true,""FirstInvoiceDate"":""2026-10-18T00:00:00""}',SYSUTCDATETIME(),N'test');
INSERT dbo.FiscalInvoiceTypePolicy(InvoiceTypeId,TransmissionMode,SignWithFiscalDevice,Revision,UpdatedUtc)
VALUES('00000000-0000-0000-0000-000000000001',2,0,1,SYSUTCDATETIME()),
      ('00000000-0000-0000-0000-000000000002',0,1,1,SYSUTCDATETIME()),
      ('00000000-0000-0000-0000-000000000003',0,0,1,SYSUTCDATETIME());
UPDATE dbo.Invoice SET InvoiceSignature=N'SIG' WHERE InvoiceId='"+resolvedInvoice+@"';
UPDATE dbo.Invoice SET Number=0,IsPrinted=0 WHERE InvoiceId='"+untouchedDraft+@"';
UPDATE dbo.Invoice SET TransactionDate='20261017 12:00' WHERE InvoiceId='"+historicalInvoice+@"';
INSERT dbo.MyDataInvoice(MyDataInvoiceId,InvoiceId,Mark) VALUES(NEWID(),'"+transmittedWithMark+@"',900017),(NEWID(),'"+currentTransmissionWithMark+@"',900031);
INSERT dbo.FiscalInvoiceWorkflow(InvoiceId,Payload,PayloadSha256,CreatedUtc,FencingToken)
VALUES('"+failedInvoice+@"',CONVERT(varbinary(max),'{""Provider"":null,""Signing"":{""Scope"":{""SignerKey"":""Samtec""}},""LocalOnly"":false}'),REPLICATE('0',64),SYSUTCDATETIME(),1),
      ('"+resolvedInvoice+@"',CONVERT(varbinary(max),'{""Provider"":null,""Signing"":{""Scope"":{""SignerKey"":""Samtec""}},""LocalOnly"":false}'),REPLICATE('0',64),SYSUTCDATETIME(),1),
      ('"+transmittedWithoutMark+@"',CONVERT(varbinary(max),'{""Provider"":{""ProviderKey"":""myDATA""},""Signing"":null,""LocalOnly"":false}'),REPLICATE('0',64),SYSUTCDATETIME(),1),
      ('"+transmittedWithMark+@"',CONVERT(varbinary(max),'{""Provider"":{""ProviderKey"":""iTimologia""},""Signing"":null,""LocalOnly"":false}'),REPLICATE('0',64),SYSUTCDATETIME(),1),
      ('"+frozenLocalInvoice+@"',CONVERT(varbinary(max),'{""Provider"":null,""Signing"":null,""LocalOnly"":true}'),REPLICATE('0',64),SYSUTCDATETIME(),1);
INSERT dbo.FiscalDocumentLease(DocumentKind,DocumentId,FencingToken)
VALUES(1,'"+failedInvoice+@"',1),(1,'"+resolvedInvoice+@"',1);
DECLARE @failed uniqueidentifier=NEWID(),@old uniqueidentifier=NEWID();
INSERT dbo.FiscalAttempt(AttemptId,DocumentKind,DocumentId,Family,Operation,Scope,PayloadFormat,RequestPayload,RequestSha256,CreatedUtc,CurrentRevision,BlocksNewAttempt)
VALUES(@failed,1,'"+failedInvoice+@"','Signing','Sign','{}','test/v1',0x01,REPLICATE('0',64),DATEADD(minute,-10,SYSUTCDATETIME()),0,1),
(@old,1,'"+resolvedInvoice+@"','Signing','Sign','{}','test/v1',0x01,REPLICATE('0',64),DATEADD(minute,-10,SYSUTCDATETIME()),0,0);
INSERT dbo.FiscalAttemptRevision(AttemptId,Revision,State,RecordedUtc,SnapshotJson,FencingToken)
VALUES(@failed,0,'Unknown',DATEADD(minute,-10,SYSUTCDATETIME()),'{}',1),
(@old,0,'Rejected',DATEADD(minute,-10,SYSUTCDATETIME()),'{}',1);");
        var snapshot = InvoiceCatalogData.Read(connection, filter, admin, types);
        Check(snapshot.Invoices.Length == 2405 && snapshot.IsAdmin, "real OpenAccess loads every row and admin state");
        Check(snapshot.Invoices.Single(i => i.InvoiceId == localCopyParent).IsReplaced,
            "local transformation provenance marks the source invoice as transformed");
        Check(snapshot.Invoices.Single(i => i.InvoiceId == markCancelledInvoice).IsCanceled,
            "accepted MARK cancellation marks the source invoice as cancelled in the catalog");
        Check(!snapshot.Invoices.Single(i => i.InvoiceId == releasedOriginal).IsCanceled,
            "cancelled credit MARK releases the original invoice for another credit");
        Check(snapshot.Invoices.Single(i => i.InvoiceId == cancelledCredit).IsCanceled,
            "the successfully MARK-cancelled credit remains shown as cancelled");
        Check(snapshot.Invoices.Single(i => i.InvoiceId == activeOriginal).IsCanceled,
            "an active credit relation keeps the original invoice cancelled");
        Check(snapshot.FiscalFailures.ContainsKey(failedInvoice) &&
              snapshot.FiscalFailures[failedInvoice].Contains("Λείπει υπογραφή") &&
              snapshot.FiscalFailures[failedInvoice].Contains("άγνωστη έκβαση"),
            "sign-only invoice without a signature is exposed with its attempt diagnostic");
        Check(!snapshot.FiscalFailures.ContainsKey(resolvedInvoice),
            "stored signature clears the indicator even when the latest attempt says rejected");
        Check(snapshot.FiscalFailures.ContainsKey(transmittedWithoutMark) &&
              snapshot.FiscalFailures[transmittedWithoutMark].Contains("Λείπει MARK"),
            "frozen transmission without a MARK is exposed to the catalog");
        Check(!snapshot.FiscalFailures.ContainsKey(transmittedWithMark),
            "stored MARK completes a frozen provider transmission");
        Check(snapshot.FiscalFailures.ContainsKey(currentSignWithoutSignature) &&
              snapshot.FiscalFailures[currentSignWithoutSignature].Contains("Λείπει υπογραφή"),
            "current sign-only policy is used after cutover when no workflow was frozen");
        Check(snapshot.FiscalFailures.ContainsKey(currentTransmissionWithoutMark) &&
              snapshot.FiscalFailures[currentTransmissionWithoutMark].Contains("Λείπει MARK"),
            "current transmission policy is used after cutover when no workflow was frozen");
        Check(!snapshot.FiscalFailures.ContainsKey(currentTransmissionWithMark),
            "stored MARK completes a current-policy transmission");
        Check(!snapshot.FiscalFailures.ContainsKey(untouchedDraft),
            "untouched draft is not reported as missing fiscal evidence");
        Check(!snapshot.FiscalFailures.ContainsKey(historicalInvoice),
            "current settings do not flag an invoice before the fiscal cutover");
        Check(!snapshot.FiscalFailures.ContainsKey(frozenLocalInvoice),
            "frozen local-only route takes precedence over later type settings");
        Check(snapshot.Invoices.Where(i => i.TraderId.HasValue).Select(i => i.TraderId.Value).Distinct().Count() > 2000, "VAT lookup crosses multiple parameter-safe batches");
        var vat = new Dictionary<Guid, string>();
        using (var c = new SqlConnection(connection))
        using (var cmd = new SqlCommand("SELECT TraderId, TaxRegistrationNumber FROM dbo.Trader", c))
        {
            c.Open(); using (var reader = cmd.ExecuteReader())
                while (reader.Read()) vat.Add(reader.GetGuid(0), reader.IsDBNull(1) ? null : reader.GetString(1));
        }
        Check(snapshot.Invoices.All(i => i.VATNumber == (i.TraderId.HasValue && vat.ContainsKey(i.TraderId.Value) ? vat[i.TraderId.Value] : null)), "VAT values match including missing customer, null VAT and anonymous invoices");
        // Independent oracle: the previous event handler's full-list calculation.
        int[] payments = { 1, 0, 2, 3 };
        for (int row = 0; row < 5; row++)
        foreach (string column in new[] { "TotalAmount", "RestAmount" })
        {
            decimal expected = 0;
            foreach (var invoice in snapshot.Invoices)
            {
                if (invoice.IsCanceled || invoice.IsCanceling || invoice.IsReplaced) continue;
                if (row < 4 && invoice.PaymentType != payments[row]) continue;
                int transactionType = types.First(t => t.Key == invoice.InvoiceTypeId).Value;
                if (column == "TotalAmount" && transactionType == 1) continue;
                if (column == "RestAmount" && transactionType == 0) continue;
                expected += invoice.TransactionSign * invoice.TotalAmount;
            }
            Check(expected == snapshot.Totals.Get(row, column), "legacy total row " + row + " / " + column);
        }
        var empty = InvoiceCatalogData.Read(connection, new InvoiceCatalogFilter(filter.From, filter.To, Guid.NewGuid(), ""), Guid.NewGuid(), types);
        Check(empty.Invoices.Length == 0 && empty.Totals.Get(4, "TotalAmount") == 0 && !empty.IsAdmin, "empty results clear totals and missing user is not admin");
        var nonAdmin = InvoiceCatalogData.Read(connection, new InvoiceCatalogFilter(filter.From, filter.To, Guid.NewGuid(), ""), Guid.Parse("00000000-0000-0000-0000-000000000011"), types);
        Check(!nonAdmin.IsAdmin, "non-admin remains non-admin");
    }

    private static async Task AsyncLoads()
    {
        var loader = new InvoiceCatalogLoadCoordinator();
        var started = new TaskCompletionSource<bool>();
        using (var release = new ManualResetEventSlim())
        {
            var first = loader.LoadAsync(() => { started.SetResult(true); release.Wait(); return new InvoiceCatalogSnapshot(); });
            await started.Task;
            Check(!first.IsCompleted, "loading does not block caller");
            bool skippedRan = false;
            var skipped = loader.LoadAsync(() => { skippedRan = true; return new InvoiceCatalogSnapshot(); });
            var wanted = new InvoiceCatalogSnapshot();
            var latest = loader.LoadAsync(() => wanted);
            release.Set();
            Check(await first == null && await skipped == null && !skippedRan && await latest == wanted, "older results discarded, queued obsolete load skipped, latest result retained");
        }
        started = new TaskCompletionSource<bool>();
        using (var release = new ManualResetEventSlim())
        {
            var closed = loader.LoadAsync(() => { started.SetResult(true); release.Wait(); throw new InvalidOperationException("obsolete error"); });
            await started.Task; loader.Invalidate(); release.Set();
            Check(await closed == null, "disposal invalidates pending result/error");
        }
        bool surfaced = false;
        try { await loader.LoadAsync(() => { throw new InvalidOperationException("current error"); }); }
        catch (InvalidOperationException) { surfaced = true; }
        Check(surfaced && await loader.LoadAsync(() => new InvoiceCatalogSnapshot()) != null, "current failure surfaces and following refresh succeeds");
    }

    private static void Benchmark()
    {
        foreach (string view in new[] { "LegacyInvoiceCatalogView", "InvoiceCatalogView" })
        {
            long elapsed = 0; int rows = 0;
            for (int run = 0; run < 3; run++)
            {
                var watch = Stopwatch.StartNew();
                using (var c = new SqlConnection(connection))
                using (var cmd = new SqlCommand("SELECT * FROM dbo." + view + " WHERE TransactionDate >= '20261018' AND TransactionDate < '20261019' ORDER BY TransactionDate DESC", c))
                {
                    c.Open(); rows = 0; using (var r = cmd.ExecuteReader()) while (r.Read()) rows++;
                }
                watch.Stop(); if (run > 0) elapsed += watch.ElapsedMilliseconds;
            }
            Console.WriteLine("SYNTHETIC SQL timing " + view + ": " + rows + " rows, warm mean " + elapsed / 2 + " ms (not production timing).");
        }
    }

    private static void Check(bool ok, string name) { if (!ok) throw new Exception("FAIL " + name); passed++; Console.WriteLine("PASS " + name); }
    private static object Scalar(string sql)
    {
        using (var c = new SqlConnection(connection)) using (var cmd = new SqlCommand(sql, c))
        { c.Open(); cmd.CommandTimeout = 120; return cmd.ExecuteScalar(); }
    }
    private static void Execute(string cs, string sql)
    {
        using (var c = new SqlConnection(cs)) using (var cmd = new SqlCommand(sql, c))
        { c.Open(); cmd.CommandTimeout = 120; cmd.ExecuteNonQuery(); }
    }
    private static void Script(string script)
    {
        foreach (string batch in Regex.Split(script, @"^GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
            if (!string.IsNullOrWhiteSpace(batch)) Execute(connection, batch);
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ASFuelControl.Windows.ViewModels
{
    // Capture on the UI thread: changing the controls must not change an in-flight query.
    internal sealed class InvoiceCatalogFilter
    {
        public readonly DateTime From, To;
        public readonly Guid InvoiceTypeId;
        public readonly string Search;

        public InvoiceCatalogFilter(DateTime from, DateTime to, Guid invoiceTypeId, string search)
        {
            From = from.Date;
            To = to.Date;
            InvoiceTypeId = invoiceTypeId;
            Search = search;
        }

        public IQueryable<Data.InvoiceCatalogView> Apply(IQueryable<Data.InvoiceCatalogView> query)
        {
            var from = From;
            query = query.Where(i => i.TransactionDate >= from);
            if (To < DateTime.MaxValue.Date)
            {
                var until = To.AddDays(1);
                query = query.Where(i => i.TransactionDate < until);
            }
            if (InvoiceTypeId != Guid.Empty)
            {
                var type = InvoiceTypeId;
                query = query.Where(i => i.InvoiceTypeId == type);
            }
            // Preserve the existing string matching, including comma lists, leading
            // zeros, whitespace and substring searches. Do not parse numbers here.
            var search = Search;
            if (!string.IsNullOrEmpty(search))
            {
                if (search.Contains(","))
                {
                    var numbers = search.Split(',');
                    query = query.Where(i => numbers.Contains(i.Number.ToString()));
                }
                else
                    query = query.Where(i => i.Number.ToString().Contains(search) ||
                        i.TraderName.Contains(search) || i.PlateNumber.Contains(search) ||
                        i.SupplyNumber.Contains(search));
            }
            return query.OrderByDescending(i => i.TransactionDate);
        }
    }

    internal sealed class InvoiceCatalogTotals
    {
        private readonly decimal[,] amounts = new decimal[5, 2];

        public InvoiceCatalogTotals(IEnumerable<Data.InvoiceCatalogView> invoices, IDictionary<Guid, int> transactionTypes)
        {
            foreach (var invoice in invoices)
            {
                // Catalog summaries represent the currently active documents.
                // A canceled source is omitted, therefore its linked cancellation
                // document must be omitted as well; otherwise the cancellation is
                // added once even though the source it reverses is no longer part
                // of the summary.
                if (invoice.IsCanceled || invoice.IsCanceling || invoice.IsReplaced) continue;
                int transactionType;
                if (!transactionTypes.TryGetValue(invoice.InvoiceTypeId, out transactionType))
                    throw new InvalidOperationException("Missing invoice type for catalog totals: " + invoice.InvoiceTypeId);
                int paymentRow;
                switch ((Common.Enumerators.PaymentTypeEnum)invoice.PaymentType)
                {
                    case Common.Enumerators.PaymentTypeEnum.Cash: paymentRow = 0; break;
                    case Common.Enumerators.PaymentTypeEnum.Credit: paymentRow = 1; break;
                    case Common.Enumerators.PaymentTypeEnum.CreditCard: paymentRow = 2; break;
                    case Common.Enumerators.PaymentTypeEnum.FleetCard: paymentRow = 3; break;
                    default: paymentRow = -1; break;
                }
                // Both columns historically total signed TotalAmount, split by
                // TransactionType. RestAmount here is NOT CreditAmount - DebitAmount.
                decimal amount = invoice.TransactionSign * invoice.TotalAmount;
                if (transactionType != 1)
                {
                    amounts[4, 0] += amount;
                    if (paymentRow >= 0) amounts[paymentRow, 0] += amount;
                }
                if (transactionType != 0)
                {
                    amounts[4, 1] += amount;
                    if (paymentRow >= 0) amounts[paymentRow, 1] += amount;
                }
            }
        }

        public decimal Get(int row, string column)
        {
            return amounts[row, column == "TotalAmount" ? 0 : 1];
        }
    }

    internal sealed class InvoiceCatalogSnapshot
    {
        public Data.InvoiceCatalogView[] Invoices;
        public InvoiceCatalogTotals Totals;
        public bool IsAdmin;
        public IDictionary<Guid, string> FiscalFailures = new Dictionary<Guid, string>();
    }

    internal static class InvoiceCatalogData
    {
        public static InvoiceCatalogSnapshot Read(string connection, InvoiceCatalogFilter filter,
            Guid userId, IDictionary<Guid, int> transactionTypes)
        {
            using (var db = new Data.DatabaseModel(connection))
            {
                var invoices = filter.Apply(db.InvoiceCatalogViews).ToArray();
                var traderIds = invoices.Where(i => i.TraderId.HasValue)
                    .Select(i => i.TraderId.Value).Distinct().ToArray();
                var vatNumbers = new Dictionary<Guid, string>();
                // Stay below SQL Server's parameter limit even for a long date range.
                // Project only the two needed fields, not complete Trader entities.
                for (int offset = 0; offset < traderIds.Length; offset += 1000)
                {
                    var batch = traderIds.Skip(offset).Take(1000).ToArray();
                    var traders = db.Traders.Where(t => batch.Contains(t.TraderId))
                        .Select(t => new { t.TraderId, t.TaxRegistrationNumber }).ToArray();
                    foreach (var trader in traders) vatNumbers[trader.TraderId] = trader.TaxRegistrationNumber;
                }
                var transformedCounts = new Dictionary<Guid, int>();
                var activeCancellationCounts = new Dictionary<Guid, int>();
                var invoiceIds = invoices.Select(i => i.InvoiceId).Distinct().ToArray();
                var markCancelled = new HashSet<Guid>();
                // InvoiceCatalogView historically counts only fiscal Replace relations.
                // LocalCopy is deliberately excluded from provider/myDATA correlation,
                // but it still means that the source invoice has been transformed and
                // must therefore be shown as such in the local catalog.
                for (int offset = 0; offset < invoiceIds.Length; offset += 1000)
                {
                    var batch = invoiceIds.Skip(offset).Take(1000).ToArray();
                    var transformedParents = db.InvoiceRelations.Where(relation =>
                            batch.Contains(relation.ParentInvoiceId) &&
                            (relation.RelationType == (int)Common.Enumerators.InvoiceRelationTypeEnum.Replace ||
                             relation.RelationType == (int)Common.Enumerators.InvoiceRelationTypeEnum.LocalCopy))
                        .Select(relation => relation.ParentInvoiceId).ToArray();
                    foreach (var group in transformedParents.GroupBy(id => id))
                        transformedCounts[group.Key] = group.Count();

                    var cancellationRelations = db.InvoiceRelations.Where(relation =>
                            batch.Contains(relation.ParentInvoiceId) &&
                            relation.RelationType == (int)Common.Enumerators.InvoiceRelationTypeEnum.Cancel)
                        .Select(relation => new {
                            relation.ParentInvoiceId,
                            relation.ChildInvoiceId
                        }).ToArray();
                    var cancelledChildren = new HashSet<Guid>();
                    var childIds = cancellationRelations.Select(relation => relation.ChildInvoiceId)
                        .Distinct().ToArray();
                    for (int childOffset = 0; childOffset < childIds.Length; childOffset += 1000)
                    {
                        var childBatch = childIds.Skip(childOffset).Take(1000).ToArray();
                        var cancelledChildIds = db.MyDataInvoices.Where(item =>
                                childBatch.Contains(item.InvoiceId) &&
                                item.CanceledByMark.HasValue && item.CanceledByMark.Value > 0)
                            .Select(item => item.InvoiceId).ToArray();
                        foreach (var id in cancelledChildIds)
                            cancelledChildren.Add(id);
                    }
                    foreach (var group in cancellationRelations.GroupBy(relation =>
                        relation.ParentInvoiceId))
                    {
                        // Preserve every relation for audit, but a credit note whose
                        // own MARK has been successfully cancelled no longer keeps
                        // the original invoice cancelled or unavailable.
                        activeCancellationCounts[group.Key] = group.Count(relation =>
                            !cancelledChildren.Contains(relation.ChildInvoiceId));
                    }

                    var cancelledIds = db.MyDataInvoices.Where(item =>
                            batch.Contains(item.InvoiceId) &&
                            item.CanceledByMark.HasValue && item.CanceledByMark.Value > 0)
                        .Select(item => item.InvoiceId).ToArray();
                    foreach (var id in cancelledIds) markCancelled.Add(id);
                }
                foreach (var invoice in invoices)
                {
                    string vat;
                    if (invoice.TraderId.HasValue && vatNumbers.TryGetValue(invoice.TraderId.Value, out vat))
                        invoice.VATNumber = vat;
                    int transformedCount;
                    if (transformedCounts.TryGetValue(invoice.InvoiceId, out transformedCount))
                    {
                        invoice.Replaced = transformedCount;
                        invoice.ReplaceInvoicesCount = transformedCount;
                    }
                    int activeCancellationCount;
                    invoice.Canceled = activeCancellationCounts.TryGetValue(
                        invoice.InvoiceId, out activeCancellationCount)
                        ? activeCancellationCount
                        : 0;
                    if (markCancelled.Contains(invoice.InvoiceId))
                        invoice.Canceled = Math.Max(invoice.Canceled, 1);
                }
                return new InvoiceCatalogSnapshot
                {
                    Invoices = invoices,
                    Totals = new InvoiceCatalogTotals(invoices, transactionTypes),
                    IsAdmin = db.ApplicationUsers.Any(u => u.ApplicationUserId == userId && u.UserLevel == 0),
                    FiscalFailures = ReadFiscalFailures(connection,invoiceIds)
                };
            }
        }

        private enum FiscalEvidenceRequirement
        {
            None,
            Signature,
            Mark
        }

        private sealed class FiscalAttemptSummary
        {
            internal readonly HashSet<Guid> Started = new HashSet<Guid>();
            internal readonly Dictionary<Guid,List<string>> Problems =
                new Dictionary<Guid,List<string>>();
        }

        private static IDictionary<Guid,string> ReadFiscalFailures(string connection,
            Guid[] invoiceIds)
        {
            if (invoiceIds == null || invoiceIds.Length == 0) return new Dictionary<Guid,string>();
            var attempts = ReadFiscalAttempts(connection,invoiceIds);
            var cutover = ReadFiscalCutover(connection);
            var values = new Dictionary<Guid,string>();
            try
            {
                using (var sql = new SqlConnection(connection))
                {
                    sql.Open();
                    for (int offset = 0; offset < invoiceIds.Length; offset += 900)
                    {
                        var batch = invoiceIds.Skip(offset).Take(900).ToArray();
                        using (var command = sql.CreateCommand())
                        {
                            var names = new string[batch.Length];
                            for (int i = 0; i < batch.Length; i++)
                            {
                                names[i] = "@id" + i;
                                command.Parameters.Add(names[i],SqlDbType.UniqueIdentifier).Value = batch[i];
                            }
                            command.CommandText = @"
SELECT i.InvoiceId,i.TransactionDate,i.Number,ISNULL(i.IsPrinted,0),i.InvoiceSignature,
       w.Payload,p.TransmissionMode,p.SignWithFiscalDevice,
       (SELECT MAX(m.Mark) FROM dbo.MyDataInvoice m WHERE m.InvoiceId=i.InvoiceId) AS Mark
FROM dbo.Invoice i
LEFT JOIN dbo.FiscalInvoiceWorkflow w ON w.InvoiceId=i.InvoiceId
LEFT JOIN dbo.FiscalInvoiceTypePolicy p ON p.InvoiceTypeId=i.InvoiceTypeId
WHERE i.InvoiceId IN (" + string.Join(",",names) + @");";
                            using (var reader = command.ExecuteReader())
                                while (reader.Read())
                                {
                                    var id = reader.GetGuid(0);
                                    var workflow = reader.IsDBNull(5) ? null : (byte[])reader.GetValue(5);
                                    FiscalEvidenceRequirement requirement;
                                    bool frozen = TryReadFrozenRequirement(workflow,out requirement);
                                    bool started = frozen || attempts.Started.Contains(id) ||
                                        reader.GetInt32(2) != 0 || reader.GetBoolean(3);
                                    if (!started) continue;

                                    if (!frozen)
                                    {
                                        // Current settings are only a fallback for an issuance
                                        // without a frozen workflow. The cutover prevents a later
                                        // policy change from flagging historical invoices.
                                        if (!cutover.HasValue || reader.GetDateTime(1) < cutover.Value ||
                                            reader.IsDBNull(6) || reader.IsDBNull(7))
                                            continue;
                                        requirement = CurrentRequirement(reader.GetInt32(6),reader.GetBoolean(7));
                                    }

                                    string problem = null;
                                    if (requirement == FiscalEvidenceRequirement.Mark &&
                                        (reader.IsDBNull(8) || reader.GetInt64(8) <= 0))
                                        problem = "Λείπει MARK διαβίβασης.";
                                    else if (requirement == FiscalEvidenceRequirement.Signature &&
                                        (reader.IsDBNull(4) || string.IsNullOrWhiteSpace(reader.GetString(4))))
                                        problem = "Λείπει υπογραφή φορολογικού μηχανισμού.";
                                    if (problem == null) continue;

                                    List<string> diagnostics;
                                    if (attempts.Problems.TryGetValue(id,out diagnostics) && diagnostics.Count > 0)
                                        problem += " " + string.Join(" · ",diagnostics);
                                    values[id] = problem;
                                }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                // During an older installation's startup the catalog may be
                // opened before the fiscal journal migration has run.
                if (ex.Number != 208 && ex.Number != 207) throw;
            }
            return values;
        }

        private static FiscalAttemptSummary ReadFiscalAttempts(string connection,Guid[] invoiceIds)
        {
            var result = new FiscalAttemptSummary();
            try
            {
                using (var sql = new SqlConnection(connection))
                {
                    sql.Open();
                    DateTime stale = DateTime.UtcNow.AddMinutes(-5);
                    for (int offset = 0; offset < invoiceIds.Length; offset += 900)
                    {
                        var batch = invoiceIds.Skip(offset).Take(900).ToArray();
                        using (var command = sql.CreateCommand())
                        {
                            var names = new string[batch.Length];
                            for (int i = 0; i < batch.Length; i++)
                            {
                                names[i] = "@id" + i;
                                command.Parameters.Add(names[i],SqlDbType.UniqueIdentifier).Value = batch[i];
                            }
                            command.CommandText = @"
WITH ranked AS (
    SELECT a.DocumentId,a.Family,r.State,r.RecordedUtc,
        ROW_NUMBER() OVER(PARTITION BY a.DocumentId,a.Family ORDER BY a.AttemptSequence DESC) AS RowNum
    FROM dbo.FiscalAttempt a
    JOIN dbo.FiscalAttemptRevision r ON r.AttemptId=a.AttemptId AND r.Revision=a.CurrentRevision
    WHERE a.DocumentKind=1 AND a.DocumentId IN (" + string.Join(",",names) + @")
)
SELECT DocumentId,Family,State,RecordedUtc FROM ranked WHERE RowNum=1;";
                            using (var reader = command.ExecuteReader())
                                while (reader.Read())
                                {
                                    var id = reader.GetGuid(0);
                                    var state = reader.GetString(2);
                                    result.Started.Add(id);
                                    bool problem = IsFailureState(state) ||
                                        IsActiveState(state) && reader.GetDateTime(3) < stale;
                                    if (!problem) continue;
                                    List<string> messages;
                                    if (!result.Problems.TryGetValue(id,out messages))
                                        result.Problems[id] = messages = new List<string>();
                                    messages.Add(FailureLabel(reader.GetString(1),state));
                                }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number != 208 && ex.Number != 207) throw;
            }
            return result;
        }

        private static DateTime? ReadFiscalCutover(string connection)
        {
            try
            {
                using (var sql = new SqlConnection(connection))
                using (var command = new SqlCommand(
                    "SELECT TOP (1) ConfigurationJson FROM dbo.FiscalConfiguration ORDER BY Revision DESC",sql))
                {
                    sql.Open();
                    var json = command.ExecuteScalar() as string;
                    if (string.IsNullOrWhiteSpace(json)) return null;
                    DateTime value;
                    var token = JObject.Parse(json)["FirstInvoiceDate"];
                    return token != null && DateTime.TryParse(token.Value<string>(),
                        CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out value)
                        ? value.Date
                        : (DateTime?)null;
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number != 208 && ex.Number != 207) throw;
                return null;
            }
        }

        private static bool TryReadFrozenRequirement(byte[] payload,
            out FiscalEvidenceRequirement requirement)
        {
            requirement = FiscalEvidenceRequirement.None;
            if (payload == null || payload.Length == 0) return false;
            try
            {
                var snapshot = JObject.Parse(Encoding.UTF8.GetString(payload));
                if (snapshot["Provider"] != null && snapshot["Provider"].Type != Newtonsoft.Json.Linq.JTokenType.Null)
                    requirement = FiscalEvidenceRequirement.Mark;
                else if (snapshot["Signing"] != null && snapshot["Signing"].Type != Newtonsoft.Json.Linq.JTokenType.Null)
                    requirement = FiscalEvidenceRequirement.Signature;
                else if ((bool?)snapshot["LocalOnly"] != true)
                    return false;
                return true;
            }
            catch (Newtonsoft.Json.JsonException)
            {
                // Let the current type policy decide when an old/corrupt snapshot
                // cannot be interpreted. Attempt diagnostics remain available.
                return false;
            }
        }

        private static FiscalEvidenceRequirement CurrentRequirement(int transmissionMode,
            bool signWithFiscalDevice)
        {
            if (transmissionMode != 0) return FiscalEvidenceRequirement.Mark;
            return signWithFiscalDevice ? FiscalEvidenceRequirement.Signature :
                FiscalEvidenceRequirement.None;
        }

        private static bool IsFailureState(string state)
        {
            return state == "Rejected" || state == "Unknown" ||
                state == "FailedBeforeDispatch" || state == "QueueFailed" ||
                state == "Declined" || state == "Failed" ||
                state == "RetryAuthorized";
        }

        private static bool IsActiveState(string state)
        {
            return state == "Signing" || state == "Sending" ||
                state == "Processing" || state == "Dispatching";
        }

        private static string FailureLabel(string family, string state)
        {
            string operation;
            switch (family)
            {
                case "Signing": operation = "Σήμανση"; break;
                case "Submission": operation = "Διαβίβαση"; break;
                case "ProviderSession": operation = "Πάροχος"; break;
                case "Payment": operation = "Πληρωμή"; break;
                case "Print": operation = "Εκτύπωση"; break;
                case "Cancellation": operation = "Ακύρωση"; break;
                default: operation = family; break;
            }
            string outcome;
            switch (state)
            {
                case "Rejected": outcome = "απορρίφθηκε"; break;
                case "Unknown": outcome = "άγνωστη έκβαση"; break;
                case "FailedBeforeDispatch": outcome = "τοπική αποτυχία"; break;
                case "QueueFailed": outcome = "αποτυχία ουράς"; break;
                case "Declined": outcome = "δεν εγκρίθηκε"; break;
                case "RetryAuthorized": outcome = "αναμένει νέα προσπάθεια"; break;
                case "Signing": case "Sending": case "Processing": case "Dispatching":
                    outcome = "διακόπηκε"; break;
                default: outcome = "απέτυχε"; break;
            }
            return operation + ": " + outcome;
        }
    }

    // One database load at a time. Queued obsolete searches are skipped and an
    // older result (or error) can never replace the latest search's result.
    internal sealed class InvoiceCatalogLoadCoordinator
    {
        private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
        private int version;

        public void Invalidate() { Interlocked.Increment(ref version); }

        public async Task<InvoiceCatalogSnapshot> LoadAsync(Func<InvoiceCatalogSnapshot> read)
        {
            int request = Interlocked.Increment(ref version);
            await gate.WaitAsync();
            try
            {
                if (request != Volatile.Read(ref version)) return null;
                var result = await Task.Run(read);
                return request == Volatile.Read(ref version) ? result : null;
            }
            catch
            {
                if (request != Volatile.Read(ref version)) return null;
                throw;
            }
            finally { gate.Release(); }
        }
    }
}

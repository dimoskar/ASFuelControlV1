using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Exedron.Invoicing.Core;
using Newtonsoft.Json;

namespace ASFuelControl.Invoicing.Persistence
{
    // Host infrastructure. No application connection string is discovered implicitly.
    public sealed class SqlFiscalDatabase
    {
        private readonly string connectionString;
        private readonly int lockTimeoutMilliseconds;
        private readonly ThreadLocal<Dictionary<string, InvoiceScope>> invoiceScopes = new ThreadLocal<Dictionary<string, InvoiceScope>>(() => new Dictionary<string, InvoiceScope>());
        public SqlFiscalDatabase(string connectionString, int lockTimeoutMilliseconds = 0)
        {
            if (lockTimeoutMilliseconds < 0 || lockTimeoutMilliseconds > 60000) throw new ArgumentOutOfRangeException(nameof(lockTimeoutMilliseconds));
            var builder = new SqlConnectionStringBuilder(connectionString);
            if (string.IsNullOrWhiteSpace(builder.InitialCatalog)) throw new ArgumentException("An explicit database is required.");
            // Session app locks must never outlive a logical connection via pooling/reconnect.
            builder.Pooling = false;
            builder.ConnectRetryCount = 0;
            this.connectionString = builder.ConnectionString;
            this.lockTimeoutMilliseconds = lockTimeoutMilliseconds;
        }
        public void InstallSchema()
        {
            foreach (var resource in new[] { "FiscalSchema.sql", "FiscalWorkflowSchema.sql", "FiscalConfigurationSchema.sql" })
            using (var stream = typeof(SqlFiscalDatabase).Assembly.GetManifestResourceStream(resource))
            using (var reader = new StreamReader(stream))
            using (var connection = Open())
            using (var command = new SqlCommand(reader.ReadToEnd(), connection))
            {
                command.CommandTimeout = 60;
                command.ExecuteNonQuery();
            }
        }
        internal SqlConnection Open()
        {
            var connection = new SqlConnection(connectionString);
            try
            {
                connection.Open();
                using (var command = new SqlCommand(SqlStatements.SessionOptions, connection)) command.ExecuteNonQuery();
                return connection;
            }
            catch { connection.Dispose(); throw; }
        }
        public IFiscalDocumentLease Acquire(FiscalDocumentReference document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var connection = Open();
            try
            {
                string resource = "ASFuelControl.FiscalDocument:" + (int)document.Kind + ":" + document.DocumentId.ToString("N");
                using (var command = Command(connection, null,
                    SqlStatements.Acquire,
                    "@resource", resource, "@timeout", lockTimeoutMilliseconds))
                {
                    command.CommandTimeout = 65;
                    if (Convert.ToInt32(command.ExecuteScalar()) < 0) throw new InvalidOperationException("This fiscal document is owned by another worker.");
                }
                long token;
                using (var transaction = connection.BeginTransaction())
                using (var command = Command(connection, transaction,
                    SqlStatements.AdvanceFence,
                    "@kind", (int)document.Kind, "@id", document.DocumentId))
                {
                    token = Convert.ToInt64(command.ExecuteScalar());
                    transaction.Commit();
                }
                return new Lease(this, connection, document, token, resource);
            }
            catch { connection.Dispose(); throw; }
        }
        // Synchronous, thread-affine workflow scope. Hold this across payment/signature/issuance.
        // Nested provider stores share it; another thread/process still contends in SQL.
        public IFiscalDocumentLease EnterInvoice(Guid invoiceId)
        { return EnterDocument(new FiscalDocumentReference(FiscalDocumentKind.Invoice, invoiceId)); }
        public IFiscalDocumentLease EnterDocument(FiscalDocumentReference document)
        {
            string key = ((int)document.Kind).ToString() + ":" + document.DocumentId.ToString("N");
            InvoiceScope scope;
            if (!invoiceScopes.Value.TryGetValue(key, out scope))
            {
                scope = new InvoiceScope { Lease = Acquire(document) };
                invoiceScopes.Value.Add(key, scope);
            }
            scope.Count++;
            int threadId = Thread.CurrentThread.ManagedThreadId;
            return new ScopeExit(scope.Lease, () => {
                if (Thread.CurrentThread.ManagedThreadId != threadId) throw new InvalidOperationException("Dispose invoice scope on its owning thread.");
                if (--scope.Count == 0) { invoiceScopes.Value.Remove(key); scope.Lease.Dispose(); }
            });
        }
        internal IFiscalDocumentLease InvoiceLease(Guid invoiceId)
        {
            InvoiceScope scope;
            if (!invoiceScopes.Value.TryGetValue("1:" + invoiceId.ToString("N"), out scope))
                throw new InvalidOperationException("Hold EnterInvoice/Acquire across the full fiscal operation before writing.");
            return scope.Lease;
        }
        private sealed class InvoiceScope { internal IFiscalDocumentLease Lease; internal int Count; }
        private sealed class ScopeExit : IFiscalDocumentLease
        {
            private Action exit;
            internal readonly IFiscalDocumentLease Lease;
            internal bool Disposed { get { return exit == null; } }
            internal ScopeExit(IFiscalDocumentLease lease, Action exit) { Lease = lease; this.exit = exit; }
            public FiscalDocumentReference Document { get { return Lease.Document; } }
            public long FencingToken { get { return Lease.FencingToken; } }
            public void Dispose() { if (exit != null) { exit(); exit = null; } }
        }
        internal T Locked<T>(IFiscalDocumentLease supplied, Func<Lease, SqlTransaction, T> action)
        {
            var nested = supplied as ScopeExit;
            if (nested != null && nested.Disposed) throw new InvalidOperationException("The invoice scope is disposed.");
            var lease = (nested == null ? supplied : nested.Lease) as Lease;
            if (lease == null || !ReferenceEquals(lease.Database, this)) throw new InvalidOperationException("A lease owned by this store is required.");
            lock (lease.Sync)
            {
                if (lease.Disposed || lease.Connection.State != ConnectionState.Open) throw new InvalidOperationException("The fiscal lease has been lost or disposed.");
                using (var tx = lease.Connection.BeginTransaction())
                {
                    using (var command = Command(lease.Connection, tx,
                        SqlStatements.AssertLease,
                        "@kind", (int)lease.Document.Kind, "@id", lease.Document.DocumentId, "@resource", lease.Resource))
                    {
                        var value = command.ExecuteScalar();
                        if (value == null || Convert.ToInt64(value) != lease.FencingToken) throw new InvalidOperationException("Stale fiscal lease.");
                    }
                    var result = action(lease, tx);
                    tx.Commit();
                    return result;
                }
            }
        }
        internal IReadOnlyList<JournalEntry> Load(IFiscalDocumentLease lease, string family, Guid? attemptId = null, bool history = false)
        {
            return Locked(lease, (held, tx) => Load(held, tx, family, attemptId, history));
        }
        internal IReadOnlyList<JournalEntry> Load(Lease lease, SqlTransaction tx, string family, Guid? attemptId = null, bool history = false)
        {
            var result = new List<JournalEntry>();
            using (var command = Command(lease.Connection, tx,
                SqlStatements.ReadAttempts,
                "@kind", (int)lease.Document.Kind, "@id", lease.Document.DocumentId, "@family", family, "@attempt", attemptId, "@history", history))
            using (var reader = command.ExecuteReader())
                while (reader.Read())
                {
                    var entry = new JournalEntry { Id = reader.GetGuid(0), Family = reader.GetString(1), Operation = reader.GetString(2),
                        Scope = reader.GetString(3), Format = reader.GetString(4), Payload = (byte[])reader[5], Hash = reader.GetString(6),
                        CreatedUtc = Utc(reader.GetDateTime(7)), Revision = reader.GetInt64(8), State = reader.GetString(9),
                        UpdatedUtc = Utc(reader.GetDateTime(10)), Snapshot = reader.GetString(11), OriginalId = reader.IsDBNull(12) ? (Guid?)null : reader.GetGuid(12) };
                    if (Hash(entry.Payload) != entry.Hash) throw new InvalidDataException("Fiscal request fingerprint mismatch.");
                    result.Add(entry);
                }
            return result;
        }
        internal void Write(IFiscalDocumentLease lease, JournalEntry entry, long? expectedRevision, bool blocking,
            Func<JournalEntry, bool> transition)
        {
            Locked(lease, (held, tx) => { Write(held, tx, entry, expectedRevision, blocking, transition); return true; });
        }
        // Allows an audited replacement and its predecessor revision to commit atomically.
        internal void Write(Lease held, SqlTransaction tx, JournalEntry entry, long? expectedRevision, bool blocking,
            Func<JournalEntry, bool> transition)
        {
            var old = Load(held, tx, entry.Family, entry.Id).SingleOrDefault();
            if (entry.Id == Guid.Empty || entry.Payload == null || entry.Payload.Length == 0 || string.IsNullOrWhiteSpace(entry.Scope) ||
                string.IsNullOrWhiteSpace(entry.Format) || string.IsNullOrWhiteSpace(entry.Snapshot) ||
                entry.CreatedUtc.Kind != DateTimeKind.Utc || entry.UpdatedUtc.Kind != DateTimeKind.Utc || entry.UpdatedUtc < entry.CreatedUtc)
                throw new ArgumentException("Invalid fiscal journal entry.");
            if (expectedRevision.HasValue)
            {
                if (old == null || old.Revision != expectedRevision.Value || entry.Revision != old.Revision + 1 ||
                    old.Operation != entry.Operation || old.Scope != entry.Scope || old.Format != entry.Format ||
                    !old.Payload.SequenceEqual(entry.Payload) || old.CreatedUtc != entry.CreatedUtc || old.OriginalId != entry.OriginalId ||
                    entry.UpdatedUtc < old.UpdatedUtc || !transition(old))
                    throw new InvalidOperationException("Stale revision, changed request, or forbidden fiscal transition.");
                using (var command = Command(held.Connection, tx,
                    SqlStatements.UpdateProjection,
                    "@revision", entry.Revision, "@blocking", blocking, "@attempt", entry.Id, "@expected", expectedRevision.Value))
                    if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException("Fiscal revision conflict.");
            }
            else
            {
                if (old != null || entry.Revision != 0) throw new InvalidOperationException("Duplicate fiscal attempt.");
                using (var command = Command(held.Connection, tx,
                    SqlStatements.InsertAttempt,
                    "@attempt", entry.Id, "@kind", (int)held.Document.Kind, "@document", held.Document.DocumentId,
                    "@family", entry.Family, "@operation", entry.Operation, "@scope", entry.Scope, "@format", entry.Format,
                    "@payload", entry.Payload, "@hash", Hash(entry.Payload), "@created", entry.CreatedUtc, "@blocking", blocking, "@original", entry.OriginalId))
                    command.ExecuteNonQuery();
            }
            using (var command = Command(held.Connection, tx,
                SqlStatements.InsertRevision,
                "@id", entry.Id, "@revision", entry.Revision, "@state", entry.State, "@updated", entry.UpdatedUtc,
                "@snapshot", entry.Snapshot, "@token", held.FencingToken)) command.ExecuteNonQuery();
        }
        internal static SqlCommand Command(SqlConnection connection, SqlTransaction transaction, string sql, params object[] parameters)
        {
            var command = new SqlCommand(sql, connection, transaction);
            for (int i = 0; i < parameters.Length; i += 2)
            {
                var value = parameters[i + 1];
                var p = command.Parameters.AddWithValue((string)parameters[i], value ?? DBNull.Value);
                if (value is DateTime) p.SqlDbType = SqlDbType.DateTime2;
                if (value is string) { p.SqlDbType = SqlDbType.NVarChar; p.Size = -1; }
                if (value is byte[]) { p.SqlDbType = SqlDbType.VarBinary; p.Size = -1; }
                if (value == null && ((string)parameters[i] == "@attempt" || (string)parameters[i] == "@original" || (string)parameters[i] == "@invoice")) p.SqlDbType = SqlDbType.UniqueIdentifier;
            }
            return command;
        }
        internal static string Json(object value) { return JsonConvert.SerializeObject(value); }
        internal static T Read<T>(string value) { return JsonConvert.DeserializeObject<T>(value); }
        internal static DateTime Utc(DateTime value) { return DateTime.SpecifyKind(value, DateTimeKind.Utc); }
        internal static string Hash(byte[] bytes)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
        internal sealed class Lease : IFiscalDocumentLease
        {
            internal readonly object Sync = new object();
            internal readonly SqlFiscalDatabase Database;
            internal readonly SqlConnection Connection;
            internal readonly string Resource;
            internal bool Disposed;
            internal Lease(SqlFiscalDatabase database, SqlConnection connection, FiscalDocumentReference document, long token, string resource)
            { Database = database; Connection = connection; Document = document; FencingToken = token; Resource = resource; }
            public FiscalDocumentReference Document { get; }
            public long FencingToken { get; }
            public void Dispose() { lock (Sync) { if (!Disposed) { Disposed = true; Connection.Dispose(); } } }
        }
        internal sealed class JournalEntry
        {
            internal Guid Id;
            internal string Family, Operation, Scope, Format, Hash, State, Snapshot;
            internal byte[] Payload;
            internal DateTime CreatedUtc, UpdatedUtc;
            internal long Revision;
            internal Guid? OriginalId;
        }
    }
}

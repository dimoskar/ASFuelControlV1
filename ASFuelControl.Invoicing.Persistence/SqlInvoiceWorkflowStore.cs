using System;
using System.IO;
using System.Text;
using Exedron.Invoicing.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ASFuelControl.Invoicing.Persistence
{
    public sealed class SqlInvoiceWorkflowStore
    {
        private readonly SqlFiscalDatabase database;
        public SqlInvoiceWorkflowStore(SqlFiscalDatabase database) { this.database = database; }
        public byte[] Load(IFiscalDocumentLease lease)
        {
            return database.Locked(lease, (held, tx) => {
                using (var command = SqlFiscalDatabase.Command(held.Connection, tx,
                    lease.Document.Kind == FiscalDocumentKind.Invoice ? SqlStatements.ReadWorkflow : SqlStatements.ReadDocumentWorkflow,
                    "@id", lease.Document.DocumentId, "@kind", (int)lease.Document.Kind))
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) return null;
                    var bytes = (byte[])reader[0];
                    if (SqlFiscalDatabase.Hash(bytes) != reader.GetString(1)) throw new InvalidDataException("Workflow fingerprint mismatch.");
                    return bytes;
                }
            });
        }
        public void Save(IFiscalDocumentLease lease, byte[] snapshot)
        {
            if (snapshot == null || snapshot.Length == 0) throw new ArgumentException("Workflow snapshot required.");
            database.Locked(lease, (held, tx) => {
                using (var command = SqlFiscalDatabase.Command(held.Connection, tx,
                    lease.Document.Kind == FiscalDocumentKind.Invoice ? SqlStatements.InsertWorkflow : SqlStatements.InsertDocumentWorkflow,
                    "@kind", (int)lease.Document.Kind,
                    "@id", lease.Document.DocumentId, "@payload", snapshot, "@hash", SqlFiscalDatabase.Hash(snapshot),
                    "@now", DateTime.UtcNow, "@token", lease.FencingToken)) command.ExecuteNonQuery();
                return true;
            });
        }
        // Authentication can fail before the first attempt is created. Only that
        // unused connection may follow configuration changes; invoice input stays frozen.
        public byte[] RefreshProviderConnection(IFiscalDocumentLease lease, byte[] expectedSnapshot, string providerJson)
        {
            RequireInvoice(lease);
            JObject workflow;
            using (var reader = new JsonTextReader(new StringReader(Encoding.UTF8.GetString(expectedSnapshot)))
                { DateParseHandling = DateParseHandling.None, FloatParseHandling = FloatParseHandling.Decimal })
                workflow = JObject.Load(reader);
            var previous = workflow["Provider"] as JObject;
            var configured = JObject.Parse(providerJson);
            if (previous == null || string.IsNullOrWhiteSpace((string)configured["Account"]) ||
                string.IsNullOrWhiteSpace((string)configured["CredentialProfile"])) throw new InvalidOperationException("A configured provider connection is required.");
            var oldScope = (JObject)previous.DeepClone(); var newScope = (JObject)configured.DeepClone();
            foreach (var field in new[] { "Account", "CredentialProfile", "SoftwareLicence" })
            { oldScope.Remove(field); newScope.Remove(field); }
            if (!JToken.DeepEquals(oldScope,newScope)) throw new InvalidOperationException("Provider, environment, issuer and endpoint must match the saved invoice.");
            if (JToken.DeepEquals(previous,configured)) return expectedSnapshot;
            var payment = workflow["Payment"] as JObject;
            if (payment != null)
            {
                if (!JToken.DeepEquals(payment["Provider"],previous)) throw new InvalidOperationException("The saved POS connection does not match the invoice.");
                payment["Provider"] = configured.DeepClone();
            }
            var changes = workflow["PreviousProviderConnections"] as JArray ?? new JArray();
            changes.Add(new JObject { ["Provider"] = previous.DeepClone(), ["ChangedUtc"] = DateTime.UtcNow });
            workflow["PreviousProviderConnections"] = changes;
            workflow["Provider"] = configured;
            var updated = Encoding.UTF8.GetBytes(workflow.ToString(Formatting.None));
            return database.Locked(lease, (held, tx) => {
                using (var command = SqlFiscalDatabase.Command(held.Connection, tx, SqlStatements.RefreshWorkflowConnection,
                    "@id", lease.Document.DocumentId, "@expected", expectedSnapshot, "@expectedHash", SqlFiscalDatabase.Hash(expectedSnapshot),
                    "@payload", updated, "@hash", SqlFiscalDatabase.Hash(updated), "@token", lease.FencingToken)) command.ExecuteNonQuery();
                return updated;
            });
        }
        public int AllocateNumbers(IFiscalDocumentLease lease, DateTime? firstInvoiceDate = null)
        {
            RequireInvoice(lease);
            return database.Locked(lease, (held, tx) => {
                using (var command = SqlFiscalDatabase.Command(held.Connection, tx, SqlStatements.AllocateInvoiceNumbers,
                    "@id", lease.Document.DocumentId, "@firstDate", firstInvoiceDate ?? new DateTime(1753,1,1)))
                    return Convert.ToInt32(command.ExecuteScalar());
            });
        }
        private static void RequireInvoice(IFiscalDocumentLease lease)
        {
            if (lease == null || lease.Document.Kind != FiscalDocumentKind.Invoice) throw new ArgumentException("Invoice lease required.");
        }
    }
}

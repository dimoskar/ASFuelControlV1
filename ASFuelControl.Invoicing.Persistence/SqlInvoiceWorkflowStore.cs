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
        // A definitive device rejection proves that the saved request was not
        // signed. This is the only point at which recalculated invoice values may
        // replace the workflow snapshot before a new signing attempt is created.
        public byte[] ReplaceAfterRejectedSigning(IFiscalDocumentLease lease,
            byte[] expectedSnapshot, byte[] replacementSnapshot)
        {
            RequireInvoice(lease);
            if (expectedSnapshot == null || expectedSnapshot.Length == 0 ||
                replacementSnapshot == null || replacementSnapshot.Length == 0)
                throw new ArgumentException("Both workflow snapshots are required.");
            return database.Locked(lease,(held,tx) => {
                using (var command = SqlFiscalDatabase.Command(held.Connection,tx,
                    SqlStatements.ReplaceWorkflowAfterRejectedSigning,
                    "@id",lease.Document.DocumentId,"@expected",expectedSnapshot,
                    "@expectedHash",SqlFiscalDatabase.Hash(expectedSnapshot),
                    "@payload",replacementSnapshot,"@hash",SqlFiscalDatabase.Hash(replacementSnapshot),
                    "@token",lease.FencingToken)) command.ExecuteNonQuery();
                return replacementSnapshot;
            });
        }
        public byte[] ReplaceAfterRejectedSubmission(IFiscalDocumentLease lease,
            byte[] expectedSnapshot, byte[] replacementSnapshot)
        {
            RequireInvoice(lease);
            if (expectedSnapshot == null || expectedSnapshot.Length == 0 ||
                replacementSnapshot == null || replacementSnapshot.Length == 0)
            {
                throw new ArgumentException("Both workflow snapshots are required.");
            }

            string expectedHash = SqlFiscalDatabase.Hash(expectedSnapshot);
            string replacementHash = SqlFiscalDatabase.Hash(replacementSnapshot);
            return database.Locked(lease, (held, tx) =>
            {
                using (var command = SqlFiscalDatabase.Command(held.Connection, tx,
                    SqlStatements.ReplaceWorkflowAfterRejectedSubmission,
                    "@id", lease.Document.DocumentId,
                    "@expected", expectedSnapshot,
                    "@expectedHash", expectedHash,
                    "@payload", replacementSnapshot,
                    "@hash", replacementHash,
                    "@token", lease.FencingToken))
                {
                    command.ExecuteNonQuery();
                }

                return replacementSnapshot;
            });
        }
        // A direct-pump invoice-delivery that never completed issuance may be
        // dispatched on a later day. The host invoice row is deliberately not
        // updated: only the fingerprinted workflow payload changes under the
        // database guards in ReplaceWorkflowForDispatchRecovery.
        public byte[] ReplaceDeliveryDispatchForRecovery(IFiscalDocumentLease lease,
            byte[] expectedSnapshot, byte[] replacementSnapshot)
        {
            RequireInvoice(lease);
            if (expectedSnapshot == null || expectedSnapshot.Length == 0 ||
                replacementSnapshot == null || replacementSnapshot.Length == 0)
                throw new ArgumentException("Both workflow snapshots are required.");
            return database.Locked(lease,(held,tx) => {
                using (var command = SqlFiscalDatabase.Command(held.Connection,tx,
                    SqlStatements.ReplaceWorkflowForDispatchRecovery,
                    "@id",lease.Document.DocumentId,"@expected",expectedSnapshot,
                    "@expectedHash",SqlFiscalDatabase.Hash(expectedSnapshot),
                    "@payload",replacementSnapshot,"@hash",SqlFiscalDatabase.Hash(replacementSnapshot),
                    "@token",held.FencingToken)) command.ExecuteNonQuery();
                return replacementSnapshot;
            });
        }
        public byte[] ChangePaymentMethod(IFiscalDocumentLease lease, byte[] expectedSnapshot,
            byte[] replacementSnapshot, int previousPaymentType, int paymentType)
        {
            RequireInvoice(lease);
            if (previousPaymentType == paymentType)
                return expectedSnapshot;
            if (paymentType < 0 || paymentType > 4)
                throw new ArgumentOutOfRangeException(nameof(paymentType));
            if ((expectedSnapshot == null) != (replacementSnapshot == null))
                throw new ArgumentException("Both workflow snapshots must either be supplied or omitted.");
            return database.Locked(lease,(held,tx) => {
                if (expectedSnapshot == null)
                {
                    using (var command = SqlFiscalDatabase.Command(held.Connection,tx,
                        SqlStatements.ChangePaymentWithoutWorkflow,
                        "@id",lease.Document.DocumentId,"@oldPayment",previousPaymentType,
                        "@newPayment",paymentType)) command.ExecuteNonQuery();
                    return null;
                }
                if (expectedSnapshot.Length == 0 || replacementSnapshot.Length == 0)
                    throw new ArgumentException("Workflow snapshots cannot be empty.");
                using (var command = SqlFiscalDatabase.Command(held.Connection,tx,
                    SqlStatements.ReplaceWorkflowForPaymentChange,
                    "@id",lease.Document.DocumentId,"@oldPayment",previousPaymentType,
                    "@newPayment",paymentType,"@expected",expectedSnapshot,
                    "@expectedHash",SqlFiscalDatabase.Hash(expectedSnapshot),
                    "@payload",replacementSnapshot,"@hash",SqlFiscalDatabase.Hash(replacementSnapshot),
                    "@now",DateTime.UtcNow,"@token",held.FencingToken)) command.ExecuteNonQuery();
                return replacementSnapshot;
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
        public int AllocateIssueDateReissueIdentity(IFiscalDocumentLease lease,
            int previousNumber, string previousSeries, DateTime previousIssueDate,
            DateTime issueDate)
        {
            RequireInvoice(lease);
            if (previousNumber <= 0) throw new ArgumentOutOfRangeException(nameof(previousNumber));
            if (issueDate.Kind == DateTimeKind.Utc)
                throw new ArgumentException("The issue date must be local fiscal time.",nameof(issueDate));
            return database.Locked(lease,(held,tx) => {
                using (var command = SqlFiscalDatabase.Command(held.Connection,tx,
                    SqlStatements.AllocateIssueDateReissueIdentity,
                    "@id",lease.Document.DocumentId,"@oldNumber",previousNumber,
                    "@oldSeries",previousSeries ?? string.Empty,"@oldDate",previousIssueDate,
                    "@newDate",issueDate))
                    return Convert.ToInt32(command.ExecuteScalar());
            });
        }
        public byte[] ReplaceAfterIssueDateReissue(IFiscalDocumentLease lease,
            byte[] expectedSnapshot, byte[] replacementSnapshot)
        {
            RequireInvoice(lease);
            if (expectedSnapshot == null || expectedSnapshot.Length == 0 ||
                replacementSnapshot == null || replacementSnapshot.Length == 0)
                throw new ArgumentException("Both workflow snapshots are required.");
            return database.Locked(lease,(held,tx) => {
                using (var command = SqlFiscalDatabase.Command(held.Connection,tx,
                    SqlStatements.ReplaceWorkflowForIssueDateReissue,
                    "@id",lease.Document.DocumentId,"@expected",expectedSnapshot,
                    "@expectedHash",SqlFiscalDatabase.Hash(expectedSnapshot),
                    "@payload",replacementSnapshot,"@hash",SqlFiscalDatabase.Hash(replacementSnapshot),
                    "@token",held.FencingToken)) command.ExecuteNonQuery();
                return replacementSnapshot;
            });
        }
        private static void RequireInvoice(IFiscalDocumentLease lease)
        {
            if (lease == null || lease.Document.Kind != FiscalDocumentKind.Invoice) throw new ArgumentException("Invoice lease required.");
        }
    }
}

using System;
using System.Collections.Generic;
using Exedron.Invoicing.Core;
using Newtonsoft.Json.Linq;

namespace ASFuelControl.Invoicing.Persistence
{
    // Ownership starts with either a frozen workflow or any journal family. A
    // pre-workflow attempt is evidence to reconcile, never permission for legacy send.
    public sealed class SqlFiscalOwnershipStore
    {
        private readonly SqlFiscalDatabase database;
        public SqlFiscalOwnershipStore(SqlFiscalDatabase database) { this.database = database; }
        internal const string ReadOwnership = @"SELECT CASE WHEN
            EXISTS(SELECT 1 FROM dbo.FiscalAttempt WHERE DocumentKind=@kind AND DocumentId=@id)
            OR (@kind=1 AND EXISTS(SELECT 1 FROM dbo.FiscalInvoiceWorkflow WHERE InvoiceId=@id))
            OR EXISTS(SELECT 1 FROM dbo.FiscalDocumentWorkflow WHERE DocumentKind=@kind AND DocumentId=@id)
            THEN 1 ELSE 0 END";
        internal const string ReadProtectedKinds = @"SELECT DocumentKind FROM dbo.FiscalAttempt
            UNION SELECT 1 FROM dbo.FiscalInvoiceWorkflow
            UNION SELECT DocumentKind FROM dbo.FiscalDocumentWorkflow";
        internal const string ReadActivations = "SELECT ConfigurationJson FROM dbo.FiscalConfiguration ORDER BY Revision";

        public bool Owns(IFiscalDocumentLease lease)
        {
            return database.Locked(lease,(held,tx) => {
                using (var command = SqlFiscalDatabase.Command(held.Connection,tx,ReadOwnership,
                    "@kind",(int)held.Document.Kind,"@id",held.Document.DocumentId))
                    return Convert.ToInt32(command.ExecuteScalar()) != 0;
            });
        }

        // The caller holds this lease through its database commit. Taking only a
        // momentary ownership check would race the background issuance worker.
        public IDisposable AcquireUnissuedInvoice(Guid id)
        {
            var lease = database.EnterInvoice(id);
            try
            {
                if (Owns(lease)) throw new InvalidOperationException("Το παραστατικό έχει κοινό ιστορικό έκδοσης. Δεν επιτρέπεται αλλαγή ή διαγραφή. Χρησιμοποιήστε τον έλεγχο ιστορικού.");
                return lease;
            }
            catch { lease.Dispose(); throw; }
        }

        // Disabling activation pauses NEW work; it does not reactivate legacy
        // transports. Old configuration revisions and journals form a durable latch.
        public HashSet<FiscalDocumentKind> ProtectedKinds()
        {
            var result = new HashSet<FiscalDocumentKind>();
            using (var connection = database.Open())
            {
                using (var command = SqlFiscalDatabase.Command(connection,null,ReadProtectedKinds))
                using (var reader = command.ExecuteReader())
                    while (reader.Read()) result.Add((FiscalDocumentKind)reader.GetInt32(0));
                using (var command = SqlFiscalDatabase.Command(connection,null,ReadActivations))
                using (var reader = command.ExecuteReader())
                    while (reader.Read())
                    {
                        var config = JObject.Parse(reader.GetString(0));
                        if ((bool?)config["Enabled"] != true) continue;
                        result.Add(FiscalDocumentKind.Invoice);
                        var documents = config["Documents"] as JObject;
                        if (documents != null)
                            foreach (var property in documents.Properties())
                            {
                                FiscalDocumentKind kind;
                                if (Enum.TryParse(property.Name,out kind)) result.Add(kind);
                            }
                    }
            }
            return result;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Exedron.Invoicing.Core;
using Newtonsoft.Json.Linq;

namespace ASFuelControl.Invoicing.Persistence
{
    public sealed class InvoiceTypeCorrectionRecord
    {
        public Guid OriginalInvoiceId, BatchId, ReplacementInvoiceId, CreatedBy;
        public Guid? CreditInvoiceId;
        public long? CancellationMark;
        public string PlanJson, State, Message;
        public DateTime CreatedUtc;
    }

    public sealed class SqlInvoiceTypeCorrectionStore
    {
        private readonly SqlFiscalDatabase db;
        private readonly SqlHeatingCorrectionStore host;
        public SqlInvoiceTypeCorrectionStore(SqlFiscalDatabase database) { db = database; host = new SqlHeatingCorrectionStore(database); }

        public IDisposable EnterBatch()
        {
            var connection = db.Open();
            try
            {
                using (var command = SqlFiscalDatabase.Command(connection, null, SqlStatements.Acquire,
                    "@resource", "ASFuelControl.InvoiceTypeCorrection.Batch", "@timeout", 0))
                    if (Convert.ToInt32(command.ExecuteScalar()) < 0)
                        throw new InvalidOperationException("Εκτελείται ήδη παρτίδα διόρθωσης τύπου σε άλλο παράθυρο ή υπολογιστή.");
                return connection;
            }
            catch { connection.Dispose(); throw; }
        }

        public InvoiceTypeCorrectionRecord[] Load()
        {
            using (var connection = db.Open())
            using (var command = SqlFiscalDatabase.Command(connection, null,
                "SELECT * FROM dbo.FiscalInvoiceTypeCorrection ORDER BY CreatedUtc,OriginalInvoiceId"))
            using (var reader = command.ExecuteReader())
            {
                var rows = new List<InvoiceTypeCorrectionRecord>();
                while (reader.Read())
                {
                    string json = (string)reader["PlanJson"];
                    if (SqlFiscalDatabase.Hash(Encoding.UTF8.GetBytes(json)) != (string)reader["PlanSha256"])
                        throw new InvalidOperationException("Το αποθηκευμένο σχέδιο διόρθωσης τύπου έχει αλλοιωθεί.");
                    rows.Add(new InvoiceTypeCorrectionRecord {
                        OriginalInvoiceId = (Guid)reader["OriginalInvoiceId"], BatchId = (Guid)reader["BatchId"],
                        CreditInvoiceId = reader["CreditInvoiceId"] == DBNull.Value ? (Guid?)null : (Guid)reader["CreditInvoiceId"],
                        ReplacementInvoiceId = (Guid)reader["ReplacementInvoiceId"], CreatedBy = (Guid)reader["CreatedBy"],
                        CancellationMark = reader["CancellationMark"] == DBNull.Value ? (long?)null : Convert.ToInt64(reader["CancellationMark"]),
                        PlanJson = json, State = (string)reader["State"], Message = (string)reader["Message"],
                        CreatedUtc = (DateTime)reader["CreatedUtc"]
                    });
                }
                return rows.ToArray();
            }
        }

        public bool IsChild(Guid id, bool incompleteOnly = false)
        {
            using (var connection = db.Open())
            using (var command = SqlFiscalDatabase.Command(connection, null, @"IF OBJECT_ID(N'dbo.FiscalInvoiceTypeCorrection',N'U') IS NULL SELECT 0;
ELSE SELECT COUNT(*) FROM dbo.FiscalInvoiceTypeCorrection WHERE (CreditInvoiceId=@id OR ReplacementInvoiceId=@id) AND (@pending=0 OR State<>'Completed')",
                "@id", id, "@pending", incompleteOnly))
                return Convert.ToInt32(command.ExecuteScalar()) != 0;
        }

        public void Reserve(IFiscalDocumentLease lease, InvoiceTypeCorrectionRecord row)
        {
            if (row.OriginalInvoiceId != lease.Document.DocumentId) throw new ArgumentException("Correction lease mismatch.");
            db.Locked(lease, (held, transaction) => {
                using (var command = SqlFiscalDatabase.Command(held.Connection, transaction, @"INSERT dbo.FiscalInvoiceTypeCorrection
(OriginalInvoiceId,BatchId,CreditInvoiceId,ReplacementInvoiceId,PlanJson,PlanSha256,State,Message,CreatedUtc,UpdatedUtc,CreatedBy)
VALUES(@id,@batch,@credit,@replacement,@plan,@hash,'Prepared',N'Αποθηκεύτηκε η παρτίδα.',@now,@now,@user)",
                    "@id", row.OriginalInvoiceId, "@batch", row.BatchId, "@credit", (object)row.CreditInvoiceId ?? DBNull.Value,
                    "@replacement", row.ReplacementInvoiceId, "@plan", row.PlanJson,
                    "@hash", SqlFiscalDatabase.Hash(Encoding.UTF8.GetBytes(row.PlanJson)), "@now", DateTime.UtcNow, "@user", row.CreatedBy))
                    command.ExecuteNonQuery();
                return true;
            });
        }

        public void Progress(IFiscalDocumentLease lease, string state, string message, long? cancellationMark = null)
        {
            db.Locked(lease, (held, transaction) => {
                using (var command = SqlFiscalDatabase.Command(held.Connection, transaction, @"UPDATE dbo.FiscalInvoiceTypeCorrection
SET State=@state,Message=@message,CancellationMark=COALESCE(@mark,CancellationMark),UpdatedUtc=@now
WHERE OriginalInvoiceId=@id AND State<>'Completed'", "@state", state, "@message", message ?? "",
                    "@mark", (object)cancellationMark ?? DBNull.Value, "@now", DateTime.UtcNow, "@id", lease.Document.DocumentId))
                    command.ExecuteNonQuery();
                return true;
            });
        }

        public JObject ReadHost(Guid id) => host.ReadHost(id);
        public void CreateChild(IFiscalDocumentLease lease, JObject frozenHost, Guid typeId, string series,
            Guid userId, string note, DateTime issuedAt) => host.CreateChild(lease, frozenHost, typeId, series, userId, note, issuedAt);
    }
}

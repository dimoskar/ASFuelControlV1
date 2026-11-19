using System;
using System.Collections.Generic;
using System.Text;
using Exedron.Invoicing.Core;
using Newtonsoft.Json.Linq;

namespace ASFuelControl.Invoicing.Persistence
{
    public sealed class MyDieselInvoiceCorrectionRecord
    {
        public Guid OriginalInvoiceId { get; set; }
        public Guid BatchId { get; set; }
        public Guid CouponCaseId { get; set; }
        public Guid CreditInvoiceId { get; set; }
        public Guid ReplacementInvoiceId { get; set; }
        public Guid CreatedBy { get; set; }
        public string PlanJson { get; set; }
        public string State { get; set; }
        public string Message { get; set; }
        public DateTime CreatedUtc { get; set; }
    }

    public sealed class SqlMyDieselInvoiceCorrectionStore
    {
        private readonly SqlFiscalDatabase database;
        private readonly SqlHeatingCorrectionStore hostStore;

        public SqlMyDieselInvoiceCorrectionStore(SqlFiscalDatabase value)
        {
            database = value;
            hostStore = new SqlHeatingCorrectionStore(value);
        }

        public IDisposable EnterBatch()
        {
            var connection = database.Open();
            try
            {
                using (var command = SqlFiscalDatabase.Command(connection, null,
                    SqlStatements.Acquire, "@resource",
                    "ASFuelControl.MyDieselInvoiceCorrection.Batch", "@timeout", 0))
                {
                    object lockResult = command.ExecuteScalar();
                    if (Convert.ToInt32(lockResult) < 0)
                    {
                        throw new InvalidOperationException(
                            "Εκτελείται ήδη παρτίδα διόρθωσης myDiesel σε άλλο παράθυρο ή υπολογιστή.");
                    }
                }
                return connection;
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        public MyDieselInvoiceCorrectionRecord[] Load()
        {
            using (var connection = database.Open())
            using (var command = SqlFiscalDatabase.Command(connection, null,
                "SELECT * FROM dbo.FiscalMyDieselInvoiceCorrection ORDER BY CreatedUtc,OriginalInvoiceId"))
            using (var reader = command.ExecuteReader())
            {
                var rows = new List<MyDieselInvoiceCorrectionRecord>();
                while (reader.Read())
                {
                    string planJson = (string)reader["PlanJson"];
                    byte[] planBytes = Encoding.UTF8.GetBytes(planJson);
                    string planHash = SqlFiscalDatabase.Hash(planBytes);
                    if (planHash != (string)reader["PlanSha256"])
                    {
                        throw new InvalidOperationException(
                            "Το αποθηκευμένο σχέδιο διόρθωσης myDiesel έχει αλλοιωθεί.");
                    }
                    rows.Add(new MyDieselInvoiceCorrectionRecord
                    {
                        OriginalInvoiceId = (Guid)reader["OriginalInvoiceId"],
                        BatchId = (Guid)reader["BatchId"],
                        CouponCaseId = (Guid)reader["CouponCaseId"],
                        CreditInvoiceId = (Guid)reader["CreditInvoiceId"],
                        ReplacementInvoiceId = (Guid)reader["ReplacementInvoiceId"],
                        CreatedBy = (Guid)reader["CreatedBy"],
                        PlanJson = planJson,
                        State = (string)reader["State"],
                        Message = (string)reader["Message"],
                        CreatedUtc = (DateTime)reader["CreatedUtc"]
                    });
                }
                return rows.ToArray();
            }
        }

        public bool IsChild(Guid invoiceId, bool incompleteOnly = false)
        {
            const string sql = @"IF OBJECT_ID(N'dbo.FiscalMyDieselInvoiceCorrection',N'U') IS NULL SELECT 0;
ELSE SELECT COUNT(*) FROM dbo.FiscalMyDieselInvoiceCorrection
WHERE (CreditInvoiceId=@id OR ReplacementInvoiceId=@id)
AND (@pending=0 OR State<>'Completed')";
            using (var connection = database.Open())
            using (var command = SqlFiscalDatabase.Command(connection, null, sql,
                "@id", invoiceId, "@pending", incompleteOnly))
            {
                object count = command.ExecuteScalar();
                return Convert.ToInt32(count) != 0;
            }
        }

        public JObject ReadHost(Guid invoiceId)
        {
            JObject host = hostStore.ReadHost(invoiceId);
            return host;
        }

        public void ReserveAndCreateChildren(IFiscalDocumentLease lease,
            MyDieselInvoiceCorrectionRecord row, JObject creditHost,
            JObject replacementHost,
            Guid creditTypeId, string creditSeries, string creditNote,
            Guid replacementTypeId, string replacementSeries,
            string replacementNote, DateTime issuedAt)
        {
            if (row.OriginalInvoiceId != lease.Document.DocumentId)
            {
                throw new ArgumentException("Correction lease mismatch.");
            }

            database.Locked(lease, (held, transaction) =>
            {
                byte[] planBytes = Encoding.UTF8.GetBytes(row.PlanJson);
                string planHash = SqlFiscalDatabase.Hash(planBytes);
                DateTime now = DateTime.UtcNow;
                const string insert = @"INSERT dbo.FiscalMyDieselInvoiceCorrection
(OriginalInvoiceId,BatchId,CouponCaseId,CreditInvoiceId,ReplacementInvoiceId,PlanJson,PlanSha256,State,Message,CreatedUtc,UpdatedUtc,CreatedBy)
VALUES(@id,@batch,@coupon,@credit,@replacement,@plan,@hash,'Prepared',N'Αποθηκεύτηκε η παρτίδα.',@now,@now,@user)";
                using (var command = SqlFiscalDatabase.Command(held.Connection,
                    transaction, insert, "@id", row.OriginalInvoiceId,
                    "@batch", row.BatchId, "@coupon", row.CouponCaseId,
                    "@credit", row.CreditInvoiceId, "@replacement",
                    row.ReplacementInvoiceId, "@plan", row.PlanJson,
                    "@hash", planHash, "@now", now, "@user", row.CreatedBy))
                {
                    command.ExecuteNonQuery();
                }
                return true;
            });

            using (IFiscalDocumentLease creditLease =
                database.EnterInvoice(row.CreditInvoiceId))
            {
                hostStore.CreateChild(creditLease, creditHost, creditTypeId,
                    creditSeries, row.CreatedBy, creditNote, issuedAt, 0);
            }
            using (IFiscalDocumentLease replacementLease =
                database.EnterInvoice(row.ReplacementInvoiceId))
            {
                hostStore.CreateChild(replacementLease, replacementHost,
                    replacementTypeId, replacementSeries, row.CreatedBy,
                    replacementNote, issuedAt, 0);
            }
        }

        public void Progress(IFiscalDocumentLease lease, string state,
            string message)
        {
            const string update = @"UPDATE dbo.FiscalMyDieselInvoiceCorrection
SET State=@state,Message=@message,UpdatedUtc=@now
WHERE OriginalInvoiceId=@id AND State<>'Completed'";
            database.Locked(lease, (held, transaction) =>
            {
                using (var command = SqlFiscalDatabase.Command(held.Connection,
                    transaction, update, "@state", state, "@message",
                    message ?? string.Empty, "@now", DateTime.UtcNow,
                    "@id", lease.Document.DocumentId))
                {
                    command.ExecuteNonQuery();
                }
                return true;
            });
        }
    }
}

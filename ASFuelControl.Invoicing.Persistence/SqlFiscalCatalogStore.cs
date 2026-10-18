using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace ASFuelControl.Invoicing.Persistence
{
    public sealed class FiscalCatalogRow
    {
        public Guid InvoiceId { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public int Number { get; set; }
        public string Series { get; set; }
        public string Trader { get; set; }
        public string Route { get; set; }
        public string Signing { get; set; }
        public string Payment { get; set; }
        public string Submission { get; set; }
        public string Printing { get; set; }
        public bool Owned { get; set; }
        public long? Mark { get; set; }
        public Guid? LegacyId { get; set; }
        public int LegacyStatus { get; set; }
        public string LegacyErrors { get; set; }
    }
    public sealed class SqlFiscalCatalogStore
    {
        private readonly SqlFiscalDatabase database;
        public SqlFiscalCatalogStore(SqlFiscalDatabase database) { this.database = database; }
        // One bounded, parameterized SQL-100 query also includes unsigned/local and
        // interrupted invoices which have never acquired a legacy MyDataInvoice row.
        internal const string Query = @"SELECT TOP (1000) i.InvoiceId,i.TransactionDate,t.Description,i.Number,i.Series,b.Name,
            w.Payload,w.PayloadSha256,m.Mark,m.MyDataInvoiceId,ISNULL(m.Status,-1),m.Errors,
            (SELECT TOP (1) r.State FROM dbo.FiscalAttempt a JOIN dbo.FiscalAttemptRevision r ON r.AttemptId=a.AttemptId AND r.Revision=a.CurrentRevision WHERE a.DocumentKind=1 AND a.DocumentId=i.InvoiceId AND a.Family='Signing' ORDER BY a.AttemptSequence DESC),
            (SELECT TOP (1) r.State FROM dbo.FiscalAttempt a JOIN dbo.FiscalAttemptRevision r ON r.AttemptId=a.AttemptId AND r.Revision=a.CurrentRevision WHERE a.DocumentKind=1 AND a.DocumentId=i.InvoiceId AND a.Family='Payment' ORDER BY a.AttemptSequence DESC),
            (SELECT TOP (1) r.State FROM dbo.FiscalAttempt a JOIN dbo.FiscalAttemptRevision r ON r.AttemptId=a.AttemptId AND r.Revision=a.CurrentRevision WHERE a.DocumentKind=1 AND a.DocumentId=i.InvoiceId AND a.Family='Submission' ORDER BY a.AttemptSequence DESC),
            (SELECT TOP (1) r.State FROM dbo.FiscalAttempt a JOIN dbo.FiscalAttemptRevision r ON r.AttemptId=a.AttemptId AND r.Revision=a.CurrentRevision WHERE a.DocumentKind=1 AND a.DocumentId=i.InvoiceId AND a.Family='Print' ORDER BY a.AttemptSequence DESC),
            CASE WHEN EXISTS(SELECT 1 FROM dbo.FiscalAttempt a WHERE a.DocumentKind=1 AND a.DocumentId=i.InvoiceId) THEN 1 ELSE 0 END
            FROM dbo.Invoice i JOIN dbo.InvoiceType t ON t.InvoiceTypeId=i.InvoiceTypeId
            LEFT JOIN dbo.Trader b ON b.TraderId=i.TraderId
            LEFT JOIN dbo.FiscalInvoiceWorkflow w ON w.InvoiceId=i.InvoiceId
            OUTER APPLY (SELECT TOP (1) Mark,MyDataInvoiceId,Status,Errors FROM dbo.MyDataInvoice md WHERE md.InvoiceId=i.InvoiceId ORDER BY md.DateTimeSent DESC,md.MyDataInvoiceId) m
            WHERE i.TransactionDate>=@from AND i.TransactionDate<@until AND
            (t.Description LIKE @filter ESCAPE '~' OR b.Name LIKE @filter ESCAPE '~' OR i.VehiclePlateNumber LIKE @filter ESCAPE '~')
            ORDER BY i.TransactionDate DESC,i.InvoiceId";
        public IList<FiscalCatalogRow> Load(DateTime from,DateTime until,string filter)
        {
            var result = new List<FiscalCatalogRow>();
            string pattern = "%" + (filter ?? "").Replace("~","~~").Replace("%","~%").Replace("_","~_").Replace("[","~[") + "%";
            using (var connection = database.Open())
            using (var command = SqlFiscalDatabase.Command(connection,null,Query,"@from",from,"@until",until,"@filter",pattern))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    Func<int,string> text = n => reader.IsDBNull(n) ? null : reader.GetString(n);
                    JObject snapshot = null;
                    if (!reader.IsDBNull(6))
                    {
                        byte[] bytes = (byte[])reader[6];
                        if (SqlFiscalDatabase.Hash(bytes) != reader.GetString(7)) throw new InvalidOperationException("Workflow fingerprint mismatch.");
                        snapshot = JObject.Parse(Encoding.UTF8.GetString(bytes));
                    }
                    var provider = snapshot?["Provider"] as JObject;
                    bool local = (bool?)snapshot?["LocalOnly"] == true;
                    bool owned = snapshot != null || reader.GetInt32(16) != 0;
                    result.Add(new FiscalCatalogRow { InvoiceId = reader.GetGuid(0), Date = reader.GetDateTime(1), Description = text(2), Number = reader.GetInt32(3), Series = text(4), Trader = text(5),
                        Owned = owned, Route = snapshot == null ? owned ? "Κοινό ιστορικό / έλεγχος" : "Παλαιό / χωρίς έκδοση" : local ? "Τοπικό" : provider == null ? "Samtec" : (string)provider["ProviderKey"],
                        Mark = reader.IsDBNull(8) ? (long?)null : reader.GetInt64(8), LegacyId = reader.IsDBNull(9) ? (Guid?)null : reader.GetGuid(9), LegacyStatus = reader.GetInt32(10), LegacyErrors = text(11),
                        Signing = text(12) ?? (snapshot == null ? null : snapshot["Signing"] is JObject ? "Δεν ξεκίνησε" : provider != null && (string)provider["ProviderKey"] != "myDATA" ? "Πάροχος" : "Δεν απαιτείται"),
                        Payment = text(13) ?? (snapshot == null ? null : snapshot["Payment"] is JObject ? "Δεν ξεκίνησε" : "Δεν απαιτείται"),
                        Submission = text(14) ?? (snapshot != null && provider == null ? "Χωρίς διαβίβαση" : null), Printing = text(15) });
                }
            }
            return result;
        }
    }
}

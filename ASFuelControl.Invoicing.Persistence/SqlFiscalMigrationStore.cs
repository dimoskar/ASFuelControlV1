using System;
using System.Collections.Generic;

namespace ASFuelControl.Invoicing.Persistence
{
    public sealed class FiscalMigrationRow
    {
        public Guid InvoiceId { get; set; }
        public DateTime Date { get; set; }
        public int Number { get; set; }
        public string Series { get; set; }
        public bool Workflow { get; set; }
        public int Attempts { get; set; }
        public int LegacyRows { get; set; }
        public long? Mark { get; set; }
        public bool CancellationEvidence { get; set; }
        public bool SignatureEvidence { get; set; }
        public bool QrEvidence { get; set; }
        public bool PrintedFlag { get; set; }
        public bool ZeroAaPayload { get; set; }
        public string Classification => Workflow ? "Shared workflow" : Attempts > 0 ? "Shared attempts: reconciliation required" :
            CancellationEvidence ? "Legacy cancellation evidence" : Mark > 0 ? "Legacy MARK evidence" :
            LegacyRows > 0 || SignatureEvidence || QrEvidence || PrintedFlag ? "Legacy outcome needs reconciliation" : "No recorded fiscal evidence";
    }
    public sealed class FiscalMigrationPage
    {
        public IList<FiscalMigrationRow> Rows { get; internal set; }
        public bool HasMore { get; internal set; }
    }
    // Preview only: never imports, cancels, deletes, changes status or guesses an
    // environment/provider from today's configuration. No raw payloads/secrets.
    public sealed class SqlFiscalMigrationStore
    {
        private readonly SqlFiscalDatabase database;
        public SqlFiscalMigrationStore(SqlFiscalDatabase database) { this.database = database; }
        internal const string Query = @"SELECT TOP (@take) i.InvoiceId,i.TransactionDate,i.Number,i.Series,
            CASE WHEN w.InvoiceId IS NULL THEN 0 ELSE 1 END,
            (SELECT COUNT(*) FROM dbo.FiscalAttempt a WHERE a.DocumentKind=1 AND a.DocumentId=i.InvoiceId),
            m.EvidenceRows,m.Mark,m.Cancelled,m.ZeroAa,
            CASE WHEN ISNULL(i.InvoiceSignature,'')<>'' THEN 1 ELSE 0 END,
            CASE WHEN ISNULL(i.QRCodeData,'')<>'' THEN 1 ELSE 0 END,ISNULL(i.IsPrinted,0)
            FROM dbo.Invoice i LEFT JOIN dbo.FiscalInvoiceWorkflow w ON w.InvoiceId=i.InvoiceId
            OUTER APPLY (SELECT COUNT(*) AS EvidenceRows,MAX(md.Mark) AS Mark,
                MAX(CASE WHEN md.CanceledByMark>0 OR md.CancelationMark>0 THEN 1 ELSE 0 END) AS Cancelled,
                MAX(CASE WHEN CAST(md.Data AS nvarchar(max)) LIKE '%<aa>0</aa>%' THEN 1 ELSE 0 END) AS ZeroAa
                FROM dbo.MyDataInvoice md WHERE md.InvoiceId=i.InvoiceId) m
            WHERE i.TransactionDate>=@from AND i.TransactionDate<@until
                AND (@first=1 OR i.TransactionDate<@before OR (i.TransactionDate=@before AND i.InvoiceId<@invoice))
            ORDER BY i.TransactionDate DESC,i.InvoiceId DESC";

        public FiscalMigrationPage Load(DateTime from,DateTime until,DateTime? before = null,Guid? beforeId = null,int pageSize = 200)
        {
            if (from < new DateTime(1753,1,1) || until <= from || pageSize < 1 || pageSize > 1000 || before.HasValue != beforeId.HasValue)
                throw new ArgumentException("Valid dates and a complete page cursor are required (maximum 1000 rows).");
            var rows = new List<FiscalMigrationRow>();
            using (var connection = database.Open())
            using (var command = SqlFiscalDatabase.Command(connection,null,Query,"@take",pageSize+1,
                "@from",from,"@until",until,"@first",!before.HasValue,"@before",before ?? until,"@invoice",beforeId ?? Guid.Empty))
            using (var reader = command.ExecuteReader())
                while (reader.Read()) rows.Add(new FiscalMigrationRow {
                    InvoiceId = reader.GetGuid(0),Date = reader.GetDateTime(1),Number = reader.GetInt32(2),Series = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Workflow = reader.GetInt32(4) == 1,Attempts = reader.GetInt32(5),LegacyRows = reader.GetInt32(6),Mark = reader.IsDBNull(7) ? (long?)null : reader.GetInt64(7),
                    CancellationEvidence = !reader.IsDBNull(8) && reader.GetInt32(8) == 1,ZeroAaPayload = !reader.IsDBNull(9) && reader.GetInt32(9) == 1,
                    SignatureEvidence = reader.GetInt32(10) == 1,QrEvidence = reader.GetInt32(11) == 1,PrintedFlag = reader.GetBoolean(12) });
            bool more = rows.Count > pageSize;
            if (more) rows.RemoveAt(rows.Count-1);
            return new FiscalMigrationPage { Rows = rows,HasMore = more };
        }
    }
}

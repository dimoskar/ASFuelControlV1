using System;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;

namespace ASFuelControl.Invoicing.Persistence
{
    public sealed class InvoiceReportDefinitionRecord
    {
        public Guid ReportDefinitionId { get; internal set; }
        public string ReportKey { get; internal set; }
        public string DefinitionFormat { get; internal set; }
        public byte[] DefinitionPayload { get; internal set; }
        public string DefinitionSha256 { get; internal set; }
        public DateTime CreatedUtc { get; internal set; }
    }

    public sealed class InvoicePrintDataRecord
    {
        public Guid InvoicePrintDataId { get; internal set; }
        public Guid InvoiceId { get; internal set; }
        public Guid ReportDefinitionId { get; internal set; }
        public string DataFormat { get; internal set; }
        public byte[] DataPayload { get; internal set; }
        public string DataSha256 { get; internal set; }
        public DateTime CreatedUtc { get; internal set; }
    }

    public sealed class SqlInvoicePrintArchiveStore
    {
        private readonly SqlFiscalDatabase database;

        public SqlInvoicePrintArchiveStore(SqlFiscalDatabase database)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public InvoiceReportDefinitionRecord EnsureDefinition(string reportKey,
            string definitionFormat, byte[] definitionPayload)
        {
            if (string.IsNullOrWhiteSpace(reportKey))
            {
                throw new ArgumentException("A report key is required.", nameof(reportKey));
            }
            if (string.IsNullOrWhiteSpace(definitionFormat))
            {
                throw new ArgumentException("A report definition format is required.", nameof(definitionFormat));
            }
            ValidatePayload(definitionPayload, nameof(definitionPayload));

            string definitionSha256 = ComputeSha256(definitionPayload);
            using (SqlConnection connection = database.Open())
            using (SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable))
            {
                InvoiceReportDefinitionRecord existing = ReadDefinition(connection,
                    transaction, reportKey, definitionSha256);
                if (existing != null)
                {
                    transaction.Commit();
                    return existing;
                }

                var record = new InvoiceReportDefinitionRecord
                {
                    ReportDefinitionId = Guid.NewGuid(),
                    ReportKey = reportKey,
                    DefinitionFormat = definitionFormat,
                    DefinitionPayload = (byte[])definitionPayload.Clone(),
                    DefinitionSha256 = definitionSha256,
                    CreatedUtc = DateTime.UtcNow
                };
                const string insert = @"INSERT dbo.InvoiceReportDefinition
(ReportDefinitionId,ReportKey,DefinitionFormat,DefinitionPayload,DefinitionSha256,CreatedUtc)
VALUES(@id,@key,@format,@payload,@sha,@created);";
                using (var command = new SqlCommand(insert, connection, transaction))
                {
                    command.Parameters.AddWithValue("@id", record.ReportDefinitionId);
                    command.Parameters.AddWithValue("@key", record.ReportKey);
                    command.Parameters.AddWithValue("@format", record.DefinitionFormat);
                    command.Parameters.Add("@payload", SqlDbType.VarBinary, -1).Value = record.DefinitionPayload;
                    command.Parameters.AddWithValue("@sha", record.DefinitionSha256);
                    command.Parameters.AddWithValue("@created", record.CreatedUtc);
                    command.ExecuteNonQuery();
                }
                transaction.Commit();
                return record;
            }
        }

        public InvoicePrintDataRecord GetOrCreatePrintData(Guid invoiceId,
            Guid reportDefinitionId, string dataFormat, byte[] dataPayload)
        {
            if (invoiceId == Guid.Empty)
            {
                throw new ArgumentException("An invoice id is required.", nameof(invoiceId));
            }
            if (reportDefinitionId == Guid.Empty)
            {
                throw new ArgumentException("A report definition id is required.", nameof(reportDefinitionId));
            }
            if (string.IsNullOrWhiteSpace(dataFormat))
            {
                throw new ArgumentException("A print data format is required.", nameof(dataFormat));
            }
            ValidatePayload(dataPayload, nameof(dataPayload));

            using (SqlConnection connection = database.Open())
            using (SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable))
            {
                InvoicePrintDataRecord existing = ReadPrintDataForInvoice(connection,
                    transaction, invoiceId);
                if (existing != null)
                {
                    transaction.Commit();
                    return existing;
                }

                var record = new InvoicePrintDataRecord
                {
                    InvoicePrintDataId = Guid.NewGuid(),
                    InvoiceId = invoiceId,
                    ReportDefinitionId = reportDefinitionId,
                    DataFormat = dataFormat,
                    DataPayload = (byte[])dataPayload.Clone(),
                    DataSha256 = ComputeSha256(dataPayload),
                    CreatedUtc = DateTime.UtcNow
                };
                const string insert = @"INSERT dbo.InvoicePrintData
(InvoicePrintDataId,InvoiceId,ReportDefinitionId,DataFormat,DataPayload,DataSha256,CreatedUtc)
VALUES(@id,@invoice,@definition,@format,@payload,@sha,@created);";
                using (var command = new SqlCommand(insert, connection, transaction))
                {
                    command.Parameters.AddWithValue("@id", record.InvoicePrintDataId);
                    command.Parameters.AddWithValue("@invoice", record.InvoiceId);
                    command.Parameters.AddWithValue("@definition", record.ReportDefinitionId);
                    command.Parameters.AddWithValue("@format", record.DataFormat);
                    command.Parameters.Add("@payload", SqlDbType.VarBinary, -1).Value = record.DataPayload;
                    command.Parameters.AddWithValue("@sha", record.DataSha256);
                    command.Parameters.AddWithValue("@created", record.CreatedUtc);
                    command.ExecuteNonQuery();
                }
                transaction.Commit();
                return record;
            }
        }

        public InvoiceReportDefinitionRecord LoadDefinition(Guid reportDefinitionId)
        {
            using (SqlConnection connection = database.Open())
            {
                const string select = @"SELECT ReportDefinitionId,ReportKey,DefinitionFormat,
DefinitionPayload,DefinitionSha256,CreatedUtc
FROM dbo.InvoiceReportDefinition WHERE ReportDefinitionId=@id;";
                using (var command = new SqlCommand(select, connection))
                {
                    command.Parameters.AddWithValue("@id", reportDefinitionId);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        return reader.Read() ? RestoreDefinition(reader) : null;
                    }
                }
            }
        }

        public InvoicePrintDataRecord LoadPrintData(Guid invoicePrintDataId)
        {
            using (SqlConnection connection = database.Open())
            {
                const string select = @"SELECT InvoicePrintDataId,InvoiceId,ReportDefinitionId,
DataFormat,DataPayload,DataSha256,CreatedUtc
FROM dbo.InvoicePrintData WHERE InvoicePrintDataId=@id;";
                using (var command = new SqlCommand(select, connection))
                {
                    command.Parameters.AddWithValue("@id", invoicePrintDataId);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        return reader.Read() ? RestorePrintData(reader) : null;
                    }
                }
            }
        }

        private static InvoiceReportDefinitionRecord ReadDefinition(SqlConnection connection,
            SqlTransaction transaction, string reportKey, string definitionSha256)
        {
            const string select = @"SELECT ReportDefinitionId,ReportKey,DefinitionFormat,
DefinitionPayload,DefinitionSha256,CreatedUtc
FROM dbo.InvoiceReportDefinition WITH (UPDLOCK,HOLDLOCK)
WHERE ReportKey=@key AND DefinitionSha256=@sha;";
            using (var command = new SqlCommand(select, connection, transaction))
            {
                command.Parameters.AddWithValue("@key", reportKey);
                command.Parameters.AddWithValue("@sha", definitionSha256);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    return reader.Read() ? RestoreDefinition(reader) : null;
                }
            }
        }

        private static InvoicePrintDataRecord ReadPrintDataForInvoice(SqlConnection connection,
            SqlTransaction transaction, Guid invoiceId)
        {
            const string select = @"SELECT InvoicePrintDataId,InvoiceId,ReportDefinitionId,
DataFormat,DataPayload,DataSha256,CreatedUtc
FROM dbo.InvoicePrintData WITH (UPDLOCK,HOLDLOCK) WHERE InvoiceId=@invoice;";
            using (var command = new SqlCommand(select, connection, transaction))
            {
                command.Parameters.AddWithValue("@invoice", invoiceId);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    return reader.Read() ? RestorePrintData(reader) : null;
                }
            }
        }

        private static InvoiceReportDefinitionRecord RestoreDefinition(SqlDataReader reader)
        {
            return new InvoiceReportDefinitionRecord
            {
                ReportDefinitionId = reader.GetGuid(0),
                ReportKey = reader.GetString(1),
                DefinitionFormat = reader.GetString(2),
                DefinitionPayload = (byte[])reader.GetValue(3),
                DefinitionSha256 = reader.GetString(4),
                CreatedUtc = reader.GetDateTime(5)
            };
        }

        private static InvoicePrintDataRecord RestorePrintData(SqlDataReader reader)
        {
            return new InvoicePrintDataRecord
            {
                InvoicePrintDataId = reader.GetGuid(0),
                InvoiceId = reader.GetGuid(1),
                ReportDefinitionId = reader.GetGuid(2),
                DataFormat = reader.GetString(3),
                DataPayload = (byte[])reader.GetValue(4),
                DataSha256 = reader.GetString(5),
                CreatedUtc = reader.GetDateTime(6)
            };
        }

        private static void ValidatePayload(byte[] payload, string parameterName)
        {
            if (payload == null || payload.Length == 0)
            {
                throw new ArgumentException("A non-empty payload is required.", parameterName);
            }
        }

        private static string ComputeSha256(byte[] payload)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(payload);
                return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            }
        }
    }
}

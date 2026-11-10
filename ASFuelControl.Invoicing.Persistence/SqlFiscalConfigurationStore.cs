using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Exedron.Invoicing.Core;

namespace ASFuelControl.Invoicing.Persistence
{
    public sealed class FiscalConfigurationRecord
    {
        public long Revision { get; set; }
        public string Json { get; set; }
    }

    public sealed class SqlFiscalConfigurationStore
    {
        private readonly SqlFiscalDatabase database;
        public SqlFiscalConfigurationStore(SqlFiscalDatabase database) { this.database = database; }

        internal const string ReadConfiguration = "SELECT TOP (1) Revision,ConfigurationJson FROM dbo.FiscalConfiguration ORDER BY Revision DESC";
        internal const string LockConfiguration = "SELECT ISNULL(MAX(Revision),0) FROM dbo.FiscalConfiguration WITH (UPDLOCK,HOLDLOCK)";
        internal const string InsertConfiguration = "INSERT dbo.FiscalConfiguration(ConfigurationJson,CreatedUtc,CreatedBy) VALUES(@json,@now,@user); SELECT CAST(SCOPE_IDENTITY() AS bigint)";
        internal const string ReadPolicies = "SELECT InvoiceTypeId,TransmissionMode,SignWithFiscalDevice,Revision FROM dbo.FiscalInvoiceTypePolicy";
        internal const string LockPolicy = "SELECT Revision FROM dbo.FiscalInvoiceTypePolicy WITH (UPDLOCK,HOLDLOCK) WHERE InvoiceTypeId=@id";
        internal const string InsertPolicy = "INSERT dbo.FiscalInvoiceTypePolicy(InvoiceTypeId,TransmissionMode,SignWithFiscalDevice,Revision,UpdatedUtc) VALUES(@id,@mode,@sign,1,@now)";
        internal const string UpdatePolicy = "UPDATE dbo.FiscalInvoiceTypePolicy SET TransmissionMode=@mode,SignWithFiscalDevice=@sign,Revision=Revision+1,UpdatedUtc=@now WHERE InvoiceTypeId=@id";
        internal const string LegacyInCutover = @"SELECT COUNT(*) FROM dbo.Invoice i
            WHERE i.TransactionDate>=@from AND NOT EXISTS(SELECT 1 FROM dbo.FiscalInvoiceWorkflow w WHERE w.InvoiceId=i.InvoiceId)
            AND NOT EXISTS(SELECT 1 FROM dbo.FiscalAttempt a WHERE a.DocumentKind=1 AND a.DocumentId=i.InvoiceId)
            AND (i.IsPrinted=1 OR ISNULL(i.InvoiceSignature,'')<>'' OR ISNULL(i.QRCodeData,'')<>''
                OR EXISTS(SELECT 1 FROM dbo.MyDataInvoice m WHERE m.InvoiceId=i.InvoiceId))";

        public int CountLegacyInCutover(DateTime from)
        {
            using (var connection = database.Open())
            using (var command = SqlFiscalDatabase.Command(connection,null,LegacyInCutover,"@from",from))
                return Convert.ToInt32(command.ExecuteScalar());
        }

        public FiscalConfigurationRecord Load()
        {
            using (var connection = database.Open())
            using (var command = SqlFiscalDatabase.Command(connection,null,ReadConfiguration))
            using (var reader = command.ExecuteReader())
                return reader.Read() ? new FiscalConfigurationRecord { Revision = reader.GetInt64(0), Json = reader.GetString(1) } : null;
        }
        public long Save(string json, long expectedRevision, string user)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Configuration required.");
            using (var connection = database.Open())
            using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
            {
                using (var command = SqlFiscalDatabase.Command(connection,transaction,LockConfiguration))
                    if (Convert.ToInt64(command.ExecuteScalar()) != expectedRevision) throw new InvalidOperationException("Settings changed in another window. Reopen the form.");
                long revision;
                using (var command = SqlFiscalDatabase.Command(connection,transaction,InsertConfiguration,"@json",json,"@now",DateTime.UtcNow,"@user",user ?? ""))
                    revision = Convert.ToInt64(command.ExecuteScalar());
                transaction.Commit();
                return revision;
            }
        }
        public Dictionary<Guid,InvoiceTypePolicy> LoadPolicies()
        {
            var result = new Dictionary<Guid,InvoiceTypePolicy>();
            using (var connection = database.Open())
            using (var command = SqlFiscalDatabase.Command(connection,null,ReadPolicies))
            using (var reader = command.ExecuteReader())
                while (reader.Read()) result.Add(reader.GetGuid(0),new InvoiceTypePolicy { InvoiceTypeId = reader.GetGuid(0), Transmission = (InvoiceTransmissionMode)reader.GetInt32(1), SignWithFiscalDevice = reader.GetBoolean(2), Revision = reader.GetInt64(3) });
            return result;
        }
        public void SavePolicies(IEnumerable<InvoiceTypePolicy> policies)
        {
            InvoiceTypePolicy[] rows = PreparePolicies(policies);
            using (var connection = database.Open())
            using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
            {
                WritePolicies(connection, transaction, rows);
                transaction.Commit();
            }
            AcceptStagedPolicies(rows);
        }

        public void StagePolicies(DbConnection contextConnection,
            IEnumerable<InvoiceTypePolicy> policies)
        {
            if (contextConnection == null)
            {
                throw new ArgumentNullException("contextConnection");
            }

            InvoiceTypePolicy[] rows = PreparePolicies(policies);
            WritePolicies(contextConnection, null, rows);
        }

        public void AcceptStagedPolicies(
            IEnumerable<InvoiceTypePolicy> policies)
        {
            if (policies == null)
            {
                throw new ArgumentNullException("policies");
            }

            foreach (InvoiceTypePolicy policy in policies)
            {
                policy.Revision++;
            }
        }

        private static InvoiceTypePolicy[] PreparePolicies(
            IEnumerable<InvoiceTypePolicy> policies)
        {
            if (policies == null)
            {
                throw new ArgumentNullException("policies");
            }

            InvoiceTypePolicy[] rows = policies
                .OrderBy(policy => policy.InvoiceTypeId)
                .ToArray();
            foreach (InvoiceTypePolicy row in rows)
            {
                row.Validate();
            }

            return rows;
        }

        private static void WritePolicies(DbConnection connection,
            DbTransaction transaction, IEnumerable<InvoiceTypePolicy> rows)
        {
            foreach (InvoiceTypePolicy row in rows)
            {
                using (DbCommand lockCommand = CreateCommand(connection,
                    transaction, LockPolicy, "@id", row.InvoiceTypeId))
                {
                    object value = lockCommand.ExecuteScalar();
                    long storedRevision = value == null || value == DBNull.Value
                        ? 0
                        : Convert.ToInt64(value);
                    if (storedRevision != row.Revision)
                    {
                        throw new InvalidOperationException(
                            "Invoice type policy changed in another window. Reopen the form.");
                    }
                }

                string statement = row.Revision == 0
                    ? InsertPolicy
                    : UpdatePolicy;
                DateTime updatedUtc = DateTime.UtcNow;
                using (DbCommand writeCommand = CreateCommand(connection,
                    transaction, statement, "@id", row.InvoiceTypeId,
                    "@mode", (int)row.Transmission, "@sign",
                    row.SignWithFiscalDevice, "@now", updatedUtc))
                {
                    int affectedRows = writeCommand.ExecuteNonQuery();
                    if (affectedRows != 1)
                    {
                        throw new InvalidOperationException(
                            "Invoice type policy could not be saved.");
                    }
                }
            }
        }

        private static DbCommand CreateCommand(DbConnection connection,
            DbTransaction transaction, string sql, params object[] parameters)
        {
            DbCommand command = connection.CreateCommand();
            command.CommandText = sql;
            if (transaction != null)
            {
                command.Transaction = transaction;
            }

            for (int index = 0; index < parameters.Length; index += 2)
            {
                string parameterName = (string)parameters[index];
                object parameterValue = parameters[index + 1];
                DbParameter parameter = command.CreateParameter();
                parameter.ParameterName = parameterName;
                parameter.Value = parameterValue ?? DBNull.Value;
                if (parameterValue is Guid)
                {
                    parameter.DbType = DbType.Guid;
                }
                else if (parameterValue is int)
                {
                    parameter.DbType = DbType.Int32;
                }
                else if (parameterValue is bool)
                {
                    parameter.DbType = DbType.Boolean;
                }
                else if (parameterValue is DateTime)
                {
                    parameter.DbType = DbType.DateTime2;
                }
                command.Parameters.Add(parameter);
            }

            return command;
        }
    }
}

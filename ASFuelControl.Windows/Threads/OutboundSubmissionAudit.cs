using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ASFuelControl.Windows.Threads
{
    internal sealed class OutboundSubmissionAttempt
    {
        public Guid SubmissionId { get; set; }
        public Guid AttemptId { get; set; }
        public int AttemptNumber { get; set; }
        public DateTime StartedUtc { get; set; }
        public string CorrelationId { get; set; }
    }

    internal sealed class OutboundSubmissionAuditEntry
    {
        public Guid SubmissionId { get; set; }
        public string EntityType { get; set; }
        public Guid EntityId { get; set; }
        public string Destination { get; set; }
        public string Status { get; set; }
        public int AttemptCount { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime? LastAttemptUtc { get; set; }
        public DateTime? NextAttemptUtc { get; set; }
        public DateTime? CompletedUtc { get; set; }
        public string LastErrorCategory { get; set; }
        public string LastErrorSummary { get; set; }
    }

    internal sealed class OutboundSubmissionAttemptEntry
    {
        public Guid AttemptId { get; set; }
        public int AttemptNumber { get; set; }
        public DateTime StartedUtc { get; set; }
        public DateTime? FinishedUtc { get; set; }
        public string Outcome { get; set; }
        public string RequestContentType { get; set; }
        public string RequestPayload { get; set; }
        public string RequestHash { get; set; }
        public int? ResponseStatusCode { get; set; }
        public string ResponseContentType { get; set; }
        public string ResponsePayload { get; set; }
        public string ErrorType { get; set; }
        public string ErrorMessage { get; set; }
        public string StackTrace { get; set; }
        public long? DurationMilliseconds { get; set; }
        public string ApplicationVersion { get; set; }
        public string MachineName { get; set; }
        public string CorrelationId { get; set; }
        public string InitiatedBy { get; set; }
    }

    /// <summary>
    /// Durable, append-only audit trail for outbound submissions. The request is
    /// persisted before the remote call so that a process failure leaves an
    /// auditable attempt with an Unknown outcome instead of silently resending it.
    /// </summary>
    internal sealed class OutboundSubmissionAuditStore
    {
        private const int CommandTimeoutSeconds = 60;
        private static readonly object SchemaSync = new object();
        private static readonly Regex XmlSecret = new Regex(
            "(<(?:eToken|Password|SubscriptionKey|ApiKey|Authorization)>)[\\s\\S]*?(</(?:eToken|Password|SubscriptionKey|ApiKey|Authorization)>)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex JsonSecret = new Regex(
            "(\"(?:password|subscriptionKey|apiKey|authorization|accessToken|clientSecret)\"\\s*:\\s*\")[^\"]*(\")",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static bool schemaReady;

        private readonly string connectionString;

        public OutboundSubmissionAuditStore(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("A database connection string is required.", "connectionString");
            this.connectionString = connectionString;
        }

        public OutboundSubmissionAttempt StartAttempt(
            string entityType,
            Guid entityId,
            string destination,
            string requestContentType,
            string requestPayload,
            string initiatedBy)
        {
            EnsureSchema();
            DateTime now = DateTime.UtcNow;
            Guid attemptId = Guid.NewGuid();
            string correlationId = Guid.NewGuid().ToString("N");
            string requestHash = ComputeHash(requestPayload);
            string safePayload = RedactSecrets(requestPayload);

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    Guid submissionId;
                    int attemptNumber;
                    using (var command = CreateCommand(connection, transaction, @"
SELECT OutboundSubmissionId, AttemptCount
FROM dbo.OutboundSubmission WITH (UPDLOCK, HOLDLOCK)
WHERE EntityType = @EntityType AND EntityId = @EntityId AND Destination = @Destination;"))
                    {
                        Add(command, "@EntityType", SqlDbType.NVarChar, 64, entityType);
                        Add(command, "@EntityId", SqlDbType.UniqueIdentifier, entityId);
                        Add(command, "@Destination", SqlDbType.NVarChar, 128, destination);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                submissionId = reader.GetGuid(0);
                                attemptNumber = reader.GetInt32(1) + 1;
                            }
                            else
                            {
                                submissionId = Guid.NewGuid();
                                attemptNumber = 1;
                            }
                        }
                    }

                    using (var command = CreateCommand(connection, transaction, @"
IF EXISTS (SELECT 1 FROM dbo.OutboundSubmission WHERE OutboundSubmissionId = @SubmissionId)
BEGIN
    UPDATE dbo.OutboundSubmission
    SET Status = N'Sending', AttemptCount = @AttemptNumber, LastAttemptUtc = @Now,
        NextAttemptUtc = NULL, CompletedUtc = NULL, LastErrorCategory = NULL,
        LastErrorSummary = NULL
    WHERE OutboundSubmissionId = @SubmissionId;
END
ELSE
BEGIN
    INSERT dbo.OutboundSubmission
        (OutboundSubmissionId, EntityType, EntityId, Destination, Status,
         AttemptCount, CreatedUtc, LastAttemptUtc)
    VALUES
        (@SubmissionId, @EntityType, @EntityId, @Destination, N'Sending',
         @AttemptNumber, @Now, @Now);
END"))
                    {
                        Add(command, "@SubmissionId", SqlDbType.UniqueIdentifier, submissionId);
                        Add(command, "@EntityType", SqlDbType.NVarChar, 64, entityType);
                        Add(command, "@EntityId", SqlDbType.UniqueIdentifier, entityId);
                        Add(command, "@Destination", SqlDbType.NVarChar, 128, destination);
                        Add(command, "@AttemptNumber", SqlDbType.Int, attemptNumber);
                        Add(command, "@Now", SqlDbType.DateTime2, now);
                        command.ExecuteNonQuery();
                    }

                    using (var command = CreateCommand(connection, transaction, @"
INSERT dbo.OutboundSubmissionAttempt
    (AttemptId, OutboundSubmissionId, AttemptNumber, StartedUtc, Outcome,
     RequestContentType, RequestPayload, RequestHash, ApplicationVersion,
     MachineName, CorrelationId, InitiatedBy)
VALUES
    (@AttemptId, @SubmissionId, @AttemptNumber, @Now, N'Sending',
     @RequestContentType, @RequestPayload, @RequestHash, @ApplicationVersion,
     @MachineName, @CorrelationId, @InitiatedBy);"))
                    {
                        Add(command, "@AttemptId", SqlDbType.UniqueIdentifier, attemptId);
                        Add(command, "@SubmissionId", SqlDbType.UniqueIdentifier, submissionId);
                        Add(command, "@AttemptNumber", SqlDbType.Int, attemptNumber);
                        Add(command, "@Now", SqlDbType.DateTime2, now);
                        Add(command, "@RequestContentType", SqlDbType.NVarChar, 128, requestContentType);
                        Add(command, "@RequestPayload", SqlDbType.NVarChar, -1, safePayload);
                        Add(command, "@RequestHash", SqlDbType.Char, 64, requestHash);
                        Add(command, "@ApplicationVersion", SqlDbType.NVarChar, 64, GetApplicationVersion());
                        Add(command, "@MachineName", SqlDbType.NVarChar, 128, Environment.MachineName);
                        Add(command, "@CorrelationId", SqlDbType.NVarChar, 64, correlationId);
                        Add(command, "@InitiatedBy", SqlDbType.NVarChar, 128, initiatedBy);
                        command.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return new OutboundSubmissionAttempt
                    {
                        SubmissionId = submissionId,
                        AttemptId = attemptId,
                        AttemptNumber = attemptNumber,
                        StartedUtc = now,
                        CorrelationId = correlationId
                    };
                }
            }
        }

        public void Complete(
            OutboundSubmissionAttempt attempt,
            int? responseStatusCode,
            string responseContentType,
            string responsePayload)
        {
            FinishAttempt(attempt, "Accepted", "Completed", responseStatusCode,
                responseContentType, responsePayload, null, null, null, null, null);
        }

        public void Fail(
            OutboundSubmissionAttempt attempt,
            string outcome,
            int? responseStatusCode,
            string responseContentType,
            string responsePayload,
            string errorCategory,
            Exception error,
            DateTime? nextAttemptUtc)
        {
            string errorType = error == null ? null : error.GetType().FullName;
            string errorMessage = error == null ? null : error.Message;
            string stackTrace = error == null ? null : error.ToString();
            string status = nextAttemptUtc.HasValue ? "RetryScheduled" :
                (string.Equals(outcome, "Unknown", StringComparison.OrdinalIgnoreCase) ? "Unknown" : "Failed");
            FinishAttempt(attempt, outcome, status, responseStatusCode, responseContentType,
                responsePayload, errorCategory, errorType, errorMessage, stackTrace, nextAttemptUtc);
        }

        public IList<OutboundSubmissionAuditEntry> LoadSubmissions(
            DateTime fromUtc,
            DateTime toUtc,
            string status,
            int maximumRows)
        {
            EnsureSchema();
            var result = new List<OutboundSubmissionAuditEntry>();
            using (var connection = new SqlConnection(connectionString))
            using (var command = CreateCommand(connection, null, @"
SELECT TOP (@MaximumRows)
    OutboundSubmissionId, EntityType, EntityId, Destination, Status, AttemptCount,
    CreatedUtc, LastAttemptUtc, NextAttemptUtc, CompletedUtc,
    LastErrorCategory, LastErrorSummary
FROM dbo.OutboundSubmission
WHERE CreatedUtc >= @FromUtc AND CreatedUtc < @ToUtc
  AND (@Status IS NULL OR Status = @Status)
ORDER BY COALESCE(LastAttemptUtc, CreatedUtc) DESC;"))
            {
                Add(command, "@MaximumRows", SqlDbType.Int, Math.Max(1, Math.Min(maximumRows, 5000)));
                Add(command, "@FromUtc", SqlDbType.DateTime2, fromUtc);
                Add(command, "@ToUtc", SqlDbType.DateTime2, toUtc);
                Add(command, "@Status", SqlDbType.NVarChar, 32, string.IsNullOrWhiteSpace(status) ? null : status);
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new OutboundSubmissionAuditEntry
                        {
                            SubmissionId = reader.GetGuid(0),
                            EntityType = reader.GetString(1),
                            EntityId = reader.GetGuid(2),
                            Destination = reader.GetString(3),
                            Status = reader.GetString(4),
                            AttemptCount = reader.GetInt32(5),
                            CreatedUtc = reader.GetDateTime(6),
                            LastAttemptUtc = GetNullableDateTime(reader, 7),
                            NextAttemptUtc = GetNullableDateTime(reader, 8),
                            CompletedUtc = GetNullableDateTime(reader, 9),
                            LastErrorCategory = GetNullableString(reader, 10),
                            LastErrorSummary = GetNullableString(reader, 11)
                        });
                    }
                }
            }
            return result;
        }

        public IList<OutboundSubmissionAttemptEntry> LoadAttempts(Guid submissionId)
        {
            EnsureSchema();
            var result = new List<OutboundSubmissionAttemptEntry>();
            using (var connection = new SqlConnection(connectionString))
            using (var command = CreateCommand(connection, null, @"
SELECT AttemptId, AttemptNumber, StartedUtc, FinishedUtc, Outcome,
       RequestContentType, RequestPayload, RequestHash, ResponseStatusCode,
       ResponseContentType, ResponsePayload, ErrorType, ErrorMessage, StackTrace,
       DurationMilliseconds, ApplicationVersion, MachineName, CorrelationId, InitiatedBy
FROM dbo.OutboundSubmissionAttempt
WHERE OutboundSubmissionId = @SubmissionId
ORDER BY AttemptNumber DESC;"))
            {
                Add(command, "@SubmissionId", SqlDbType.UniqueIdentifier, submissionId);
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new OutboundSubmissionAttemptEntry
                        {
                            AttemptId = reader.GetGuid(0),
                            AttemptNumber = reader.GetInt32(1),
                            StartedUtc = reader.GetDateTime(2),
                            FinishedUtc = GetNullableDateTime(reader, 3),
                            Outcome = reader.GetString(4),
                            RequestContentType = GetNullableString(reader, 5),
                            RequestPayload = GetNullableString(reader, 6),
                            RequestHash = GetNullableString(reader, 7),
                            ResponseStatusCode = reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8),
                            ResponseContentType = GetNullableString(reader, 9),
                            ResponsePayload = GetNullableString(reader, 10),
                            ErrorType = GetNullableString(reader, 11),
                            ErrorMessage = GetNullableString(reader, 12),
                            StackTrace = GetNullableString(reader, 13),
                            DurationMilliseconds = reader.IsDBNull(14) ? (long?)null : reader.GetInt64(14),
                            ApplicationVersion = GetNullableString(reader, 15),
                            MachineName = GetNullableString(reader, 16),
                            CorrelationId = GetNullableString(reader, 17),
                            InitiatedBy = GetNullableString(reader, 18)
                        });
                    }
                }
            }
            return result;
        }

        internal static string RedactSecrets(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;
            string redacted = XmlSecret.Replace(value, "$1[REDACTED]$2");
            return JsonSecret.Replace(redacted, "$1[REDACTED]$2");
        }

        internal static string ComputeHash(string value)
        {
            if (value == null)
                return null;
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
                var result = new StringBuilder(hash.Length * 2);
                foreach (byte item in hash)
                    result.Append(item.ToString("x2"));
                return result.ToString();
            }
        }

        private void FinishAttempt(
            OutboundSubmissionAttempt attempt,
            string outcome,
            string status,
            int? responseStatusCode,
            string responseContentType,
            string responsePayload,
            string errorCategory,
            string errorType,
            string errorMessage,
            string stackTrace,
            DateTime? nextAttemptUtc)
        {
            if (attempt == null)
                throw new ArgumentNullException("attempt");
            EnsureSchema();
            DateTime now = DateTime.UtcNow;
            long duration = Math.Max(0, (long)(now - attempt.StartedUtc).TotalMilliseconds);
            string safeResponse = RedactSecrets(responsePayload);
            string safeError = Truncate(errorMessage, 2000);

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    using (var command = CreateCommand(connection, transaction, @"
UPDATE dbo.OutboundSubmissionAttempt
SET FinishedUtc = @Now, Outcome = @Outcome, ResponseStatusCode = @ResponseStatusCode,
    ResponseContentType = @ResponseContentType, ResponsePayload = @ResponsePayload,
    ErrorType = @ErrorType, ErrorMessage = @ErrorMessage, StackTrace = @StackTrace,
    DurationMilliseconds = @Duration
WHERE AttemptId = @AttemptId AND Outcome = N'Sending';"))
                    {
                        Add(command, "@Now", SqlDbType.DateTime2, now);
                        Add(command, "@Outcome", SqlDbType.NVarChar, 32, outcome);
                        Add(command, "@ResponseStatusCode", SqlDbType.Int, responseStatusCode);
                        Add(command, "@ResponseContentType", SqlDbType.NVarChar, 128, responseContentType);
                        Add(command, "@ResponsePayload", SqlDbType.NVarChar, -1, safeResponse);
                        Add(command, "@ErrorType", SqlDbType.NVarChar, 512, errorType);
                        Add(command, "@ErrorMessage", SqlDbType.NVarChar, 2000, safeError);
                        Add(command, "@StackTrace", SqlDbType.NVarChar, -1, RedactSecrets(stackTrace));
                        Add(command, "@Duration", SqlDbType.BigInt, duration);
                        Add(command, "@AttemptId", SqlDbType.UniqueIdentifier, attempt.AttemptId);
                        command.ExecuteNonQuery();
                    }

                    using (var command = CreateCommand(connection, transaction, @"
UPDATE dbo.OutboundSubmission
SET Status = @Status, NextAttemptUtc = @NextAttemptUtc,
    CompletedUtc = CASE WHEN @Status = N'Completed' THEN @Now ELSE NULL END,
    LastErrorCategory = @ErrorCategory, LastErrorSummary = @ErrorMessage
WHERE OutboundSubmissionId = @SubmissionId;"))
                    {
                        Add(command, "@Status", SqlDbType.NVarChar, 32, status);
                        Add(command, "@NextAttemptUtc", SqlDbType.DateTime2, nextAttemptUtc);
                        Add(command, "@Now", SqlDbType.DateTime2, now);
                        Add(command, "@ErrorCategory", SqlDbType.NVarChar, 64, errorCategory);
                        Add(command, "@ErrorMessage", SqlDbType.NVarChar, 2000, safeError);
                        Add(command, "@SubmissionId", SqlDbType.UniqueIdentifier, attempt.SubmissionId);
                        command.ExecuteNonQuery();
                    }
                    transaction.Commit();
                }
            }
        }

        private void EnsureSchema()
        {
            if (schemaReady)
                return;
            lock (SchemaSync)
            {
                if (schemaReady)
                    return;
                using (var connection = new SqlConnection(connectionString))
                using (var command = CreateCommand(connection, null, SchemaSql))
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                schemaReady = true;
            }
        }

        private static SqlCommand CreateCommand(SqlConnection connection, SqlTransaction transaction, string sql)
        {
            return new SqlCommand(sql, connection, transaction) { CommandTimeout = CommandTimeoutSeconds };
        }

        private static void Add(SqlCommand command, string name, SqlDbType type, object value)
        {
            var parameter = command.Parameters.Add(name, type);
            parameter.Value = value ?? DBNull.Value;
        }

        private static void Add(SqlCommand command, string name, SqlDbType type, int size, object value)
        {
            var parameter = command.Parameters.Add(name, type, size);
            parameter.Value = value ?? DBNull.Value;
        }

        private static DateTime? GetNullableDateTime(IDataRecord reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? (DateTime?)null : reader.GetDateTime(ordinal);
        }

        private static string GetNullableString(IDataRecord reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }

        private static string GetApplicationVersion()
        {
            Assembly assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            Version version = assembly.GetName().Version;
            return version == null ? null : version.ToString();
        }

        private static string Truncate(string value, int length)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= length)
                return value;
            return value.Substring(0, length);
        }

        private const string SchemaSql = @"
IF OBJECT_ID(N'dbo.OutboundSubmission', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OutboundSubmission
    (
        OutboundSubmissionId uniqueidentifier NOT NULL,
        EntityType nvarchar(64) NOT NULL,
        EntityId uniqueidentifier NOT NULL,
        Destination nvarchar(128) NOT NULL,
        Status nvarchar(32) NOT NULL,
        AttemptCount int NOT NULL CONSTRAINT DF_OutboundSubmission_AttemptCount DEFAULT (0),
        CreatedUtc datetime2(3) NOT NULL,
        LastAttemptUtc datetime2(3) NULL,
        NextAttemptUtc datetime2(3) NULL,
        CompletedUtc datetime2(3) NULL,
        LastErrorCategory nvarchar(64) NULL,
        LastErrorSummary nvarchar(2000) NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT PK_OutboundSubmission PRIMARY KEY (OutboundSubmissionId),
        CONSTRAINT UQ_OutboundSubmission_Entity UNIQUE (EntityType, EntityId, Destination)
    );
    CREATE INDEX IX_OutboundSubmission_StatusNext
        ON dbo.OutboundSubmission(Status, NextAttemptUtc, CreatedUtc);
END;

IF OBJECT_ID(N'dbo.OutboundSubmissionAttempt', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OutboundSubmissionAttempt
    (
        AttemptId uniqueidentifier NOT NULL,
        OutboundSubmissionId uniqueidentifier NOT NULL,
        AttemptNumber int NOT NULL,
        StartedUtc datetime2(3) NOT NULL,
        FinishedUtc datetime2(3) NULL,
        Outcome nvarchar(32) NOT NULL,
        RequestContentType nvarchar(128) NULL,
        RequestPayload nvarchar(max) NULL,
        RequestHash char(64) NULL,
        ResponseStatusCode int NULL,
        ResponseContentType nvarchar(128) NULL,
        ResponsePayload nvarchar(max) NULL,
        ErrorType nvarchar(512) NULL,
        ErrorMessage nvarchar(2000) NULL,
        StackTrace nvarchar(max) NULL,
        DurationMilliseconds bigint NULL,
        ApplicationVersion nvarchar(64) NULL,
        MachineName nvarchar(128) NULL,
        CorrelationId nvarchar(64) NULL,
        InitiatedBy nvarchar(128) NULL,
        CONSTRAINT PK_OutboundSubmissionAttempt PRIMARY KEY (AttemptId),
        CONSTRAINT FK_OutboundSubmissionAttempt_Submission FOREIGN KEY (OutboundSubmissionId)
            REFERENCES dbo.OutboundSubmission(OutboundSubmissionId),
        CONSTRAINT UQ_OutboundSubmissionAttempt_Number UNIQUE (OutboundSubmissionId, AttemptNumber)
    );
    CREATE INDEX IX_OutboundSubmissionAttempt_SubmissionStarted
        ON dbo.OutboundSubmissionAttempt(OutboundSubmissionId, StartedUtc DESC);
END;

UPDATE attempt
SET FinishedUtc = SYSUTCDATETIME(), Outcome = N'Unknown',
    ErrorType = N'ProcessInterrupted',
    ErrorMessage = N'The application stopped before the remote outcome was recorded.'
FROM dbo.OutboundSubmissionAttempt attempt
WHERE attempt.Outcome = N'Sending'
  AND attempt.StartedUtc < DATEADD(minute, -5, SYSUTCDATETIME());

UPDATE submission
SET Status = N'Unknown', NextAttemptUtc = NULL,
    LastErrorCategory = N'AmbiguousOutcome',
    LastErrorSummary = N'The application stopped before the remote outcome was recorded.'
FROM dbo.OutboundSubmission submission
WHERE submission.Status = N'Sending'
  AND EXISTS
  (
      SELECT 1 FROM dbo.OutboundSubmissionAttempt attempt
      WHERE attempt.OutboundSubmissionId = submission.OutboundSubmissionId
        AND attempt.Outcome = N'Unknown'
  );";
    }
}

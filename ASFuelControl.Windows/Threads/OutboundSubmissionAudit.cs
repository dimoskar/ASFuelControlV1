using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        public bool IsPinned { get; set; }
        public DateTime? PinnedUtc { get; set; }
        public string PinnedBy { get; set; }
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
        public Guid? ArchiveId { get; set; }
        public DateTime? ArchivedUtc { get; set; }
        public DateTime? PayloadPurgedUtc { get; set; }
        public string ArchiveFilePath { get; set; }
        public string ArchiveSha256 { get; set; }
    }

    internal sealed class OutboundAuditRetentionPolicy
    {
        private const string Prefix = "OutboundAudit.";

        public bool Enabled { get; set; }
        public string ArchiveDirectory { get; set; }
        public int CompletedPayloadDays { get; set; }
        public int FailedPayloadDays { get; set; }
        public int UnknownPayloadDays { get; set; }
        public int MetadataDays { get; set; }
        public int ArchiveFileDays { get; set; }
        public int BatchSize { get; set; }
        public int RunHour { get; set; }
        public int MinimumFreeSpaceMb { get; set; }

        public static OutboundAuditRetentionPolicy Load()
        {
            var options = Data.Implementation.OptionHandler.Instance;
            return new OutboundAuditRetentionPolicy
            {
                Enabled = options.GetBoolOption(Prefix + "Enabled", true),
                ArchiveDirectory = options.GetOption(Prefix + "ArchiveDirectory", DefaultArchiveDirectory()),
                CompletedPayloadDays = ClampDays(options.GetIntOption(Prefix + "CompletedPayloadDays", 30), 1),
                FailedPayloadDays = ClampDays(options.GetIntOption(Prefix + "FailedPayloadDays", 180), 1),
                UnknownPayloadDays = ClampDays(options.GetIntOption(Prefix + "UnknownPayloadDays", 365), 1),
                MetadataDays = ClampDays(options.GetIntOption(Prefix + "MetadataDays", 0), 0),
                ArchiveFileDays = ClampDays(options.GetIntOption(Prefix + "ArchiveFileDays", 0), 0),
                BatchSize = Math.Max(10, Math.Min(options.GetIntOption(Prefix + "BatchSize", 100), 500)),
                RunHour = Math.Max(0, Math.Min(options.GetIntOption(Prefix + "RunHour", 3), 23)),
                MinimumFreeSpaceMb = Math.Max(100, options.GetIntOption(Prefix + "MinimumFreeSpaceMb", 1024))
            };
        }

        public void Save()
        {
            var options = Data.Implementation.OptionHandler.Instance;
            options.SetOption(Prefix + "Enabled", this.Enabled);
            options.SetOption(Prefix + "ArchiveDirectory", string.IsNullOrWhiteSpace(this.ArchiveDirectory) ? DefaultArchiveDirectory() : this.ArchiveDirectory.Trim());
            options.SetOption(Prefix + "CompletedPayloadDays", ClampDays(this.CompletedPayloadDays, 1));
            options.SetOption(Prefix + "FailedPayloadDays", ClampDays(this.FailedPayloadDays, 1));
            options.SetOption(Prefix + "UnknownPayloadDays", ClampDays(this.UnknownPayloadDays, 1));
            options.SetOption(Prefix + "MetadataDays", ClampDays(this.MetadataDays, 0));
            options.SetOption(Prefix + "ArchiveFileDays", ClampDays(this.ArchiveFileDays, 0));
            options.SetOption(Prefix + "BatchSize", Math.Max(10, Math.Min(this.BatchSize, 500)));
            options.SetOption(Prefix + "RunHour", Math.Max(0, Math.Min(this.RunHour, 23)));
            options.SetOption(Prefix + "MinimumFreeSpaceMb", Math.Max(100, this.MinimumFreeSpaceMb));
        }

        public static string DefaultArchiveDirectory()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "ASFuelControl",
                "OutboundAuditArchive");
        }

        private static int ClampDays(int value, int minimum)
        {
            return Math.Max(minimum, Math.Min(value, 36500));
        }
    }

    internal sealed class OutboundAuditStorageStatistics
    {
        public long AttemptCount { get; set; }
        public long PurgedAttemptCount { get; set; }
        public long DatabasePayloadBytes { get; set; }
        public long VerifiedArchiveCount { get; set; }
        public long ArchiveBytes { get; set; }
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
    LastErrorCategory, LastErrorSummary, IsPinned, PinnedUtc, PinnedBy
FROM dbo.OutboundSubmission
WHERE CreatedUtc >= @FromUtc AND CreatedUtc < @ToUtc
  AND (@Status IS NULL OR Status = @Status OR (@Status = N'Problems' AND Status <> N'Completed'))
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
                            LastErrorSummary = GetNullableString(reader, 11),
                            IsPinned = reader.GetBoolean(12),
                            PinnedUtc = GetNullableDateTime(reader, 13),
                            PinnedBy = GetNullableString(reader, 14)
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
       RequestContentType, CAST(NULL AS nvarchar(max)) AS RequestPayload,
       RequestHash, ResponseStatusCode,
       ResponseContentType, CAST(NULL AS nvarchar(max)) AS ResponsePayload,
       ErrorType, ErrorMessage, CAST(NULL AS nvarchar(max)) AS StackTrace,
       DurationMilliseconds, ApplicationVersion, MachineName, CorrelationId, InitiatedBy,
       attempt.ArchiveId, attempt.ArchivedUtc, attempt.PayloadPurgedUtc,
       archive.FilePath, archive.Sha256
FROM dbo.OutboundSubmissionAttempt attempt
LEFT JOIN dbo.OutboundAuditArchive archive ON archive.ArchiveId = attempt.ArchiveId
WHERE attempt.OutboundSubmissionId = @SubmissionId
ORDER BY AttemptNumber DESC;"))
            {
                Add(command, "@SubmissionId", SqlDbType.UniqueIdentifier, submissionId);
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                        result.Add(ReadAttempt(reader));
                }
            }
            return result;
        }

        public OutboundSubmissionAttemptEntry LoadAttempt(Guid attemptId)
        {
            EnsureSchema();
            using (var connection = new SqlConnection(connectionString))
            using (var command = CreateCommand(connection, null, @"
SELECT AttemptId, AttemptNumber, StartedUtc, FinishedUtc, Outcome,
       RequestContentType, RequestPayload, RequestHash, ResponseStatusCode,
       ResponseContentType, ResponsePayload, ErrorType, ErrorMessage, StackTrace,
       DurationMilliseconds, ApplicationVersion, MachineName, CorrelationId, InitiatedBy,
       attempt.ArchiveId, attempt.ArchivedUtc, attempt.PayloadPurgedUtc,
       archive.FilePath, archive.Sha256
FROM dbo.OutboundSubmissionAttempt attempt
LEFT JOIN dbo.OutboundAuditArchive archive ON archive.ArchiveId = attempt.ArchiveId
WHERE attempt.AttemptId = @AttemptId;"))
            {
                Add(command, "@AttemptId", SqlDbType.UniqueIdentifier, attemptId);
                connection.Open();
                using (var reader = command.ExecuteReader(CommandBehavior.SingleRow))
                {
                    OutboundSubmissionAttemptEntry attempt = reader.Read() ? ReadAttempt(reader) : null;
                    return attempt == null ? null : RestoreArchivedPayload(attempt);
                }
            }
        }

        public OutboundSubmissionAuditEntry LoadSubmission(Guid submissionId)
        {
            EnsureSchema();
            using (var connection = new SqlConnection(connectionString))
            using (var command = CreateCommand(connection, null, @"
SELECT OutboundSubmissionId, EntityType, EntityId, Destination, Status, AttemptCount,
       CreatedUtc, LastAttemptUtc, NextAttemptUtc, CompletedUtc,
       LastErrorCategory, LastErrorSummary, IsPinned, PinnedUtc, PinnedBy
FROM dbo.OutboundSubmission
WHERE OutboundSubmissionId = @SubmissionId;"))
            {
                Add(command, "@SubmissionId", SqlDbType.UniqueIdentifier, submissionId);
                connection.Open();
                using (var reader = command.ExecuteReader(CommandBehavior.SingleRow))
                {
                    if (!reader.Read())
                        return null;
                    return new OutboundSubmissionAuditEntry
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
                        LastErrorSummary = GetNullableString(reader, 11),
                        IsPinned = reader.GetBoolean(12),
                        PinnedUtc = GetNullableDateTime(reader, 13),
                        PinnedBy = GetNullableString(reader, 14)
                    };
                }
            }
        }

        public OutboundAuditStorageStatistics GetStorageStatistics()
        {
            EnsureSchema();
            var result = new OutboundAuditStorageStatistics();
            using (var connection = new SqlConnection(connectionString))
            using (var command = CreateCommand(connection, null, @"
SELECT COUNT_BIG(*) AS AttemptCount,
       COALESCE(SUM(CAST(CASE WHEN PayloadPurgedUtc IS NULL THEN 0 ELSE 1 END AS bigint)), 0) AS PurgedCount,
       COALESCE(SUM(CAST(COALESCE(DATALENGTH(RequestPayload), 0) AS bigint) +
                    CAST(COALESCE(DATALENGTH(ResponsePayload), 0) AS bigint) +
                    CAST(COALESCE(DATALENGTH(StackTrace), 0) AS bigint)), 0) AS PayloadBytes
FROM dbo.OutboundSubmissionAttempt;

SELECT COUNT_BIG(*) AS ArchiveCount,
       COALESCE(SUM(CompressedBytes), 0) AS ArchiveBytes
FROM dbo.OutboundAuditArchive
WHERE Status = N'Verified';"))
            {
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        result.AttemptCount = reader.GetInt64(0);
                        result.PurgedAttemptCount = reader.GetInt64(1);
                        result.DatabasePayloadBytes = reader.GetInt64(2);
                    }
                    if (reader.NextResult() && reader.Read())
                    {
                        result.VerifiedArchiveCount = reader.GetInt64(0);
                        result.ArchiveBytes = reader.GetInt64(1);
                    }
                }
            }
            return result;
        }

        public void ExportSubmission(Guid submissionId, string targetPath)
        {
            if (string.IsNullOrWhiteSpace(targetPath))
                throw new ArgumentException("An export path is required.", "targetPath");
            OutboundSubmissionAuditEntry submission = LoadSubmission(submissionId);
            if (submission == null)
                throw new InvalidOperationException("The selected outbound submission no longer exists.");
            IList<OutboundSubmissionAttemptEntry> attempts = LoadAttempts(submissionId);
            string directory = Path.GetDirectoryName(Path.GetFullPath(targetPath));
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
            using (var stream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 65536))
            using (var text = new StreamWriter(stream, new UTF8Encoding(true), 65536))
            using (var writer = new JsonTextWriter(text) { Formatting = Formatting.Indented })
            {
                var serializer = JsonSerializer.CreateDefault();
                writer.WriteStartObject();
                WriteJson(writer, "ExportVersion", 1);
                WriteJson(writer, "ExportedUtc", DateTime.UtcNow);
                writer.WritePropertyName("Submission");
                serializer.Serialize(writer, submission);
                writer.WritePropertyName("Attempts");
                writer.WriteStartArray();
                foreach (OutboundSubmissionAttemptEntry summary in attempts.OrderBy(item => item.AttemptNumber))
                    serializer.Serialize(writer, LoadAttempt(summary.AttemptId));
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
        }

        private static void WriteJson(JsonTextWriter writer, string name, object value)
        {
            writer.WritePropertyName(name);
            writer.WriteValue(value);
        }

        private static OutboundSubmissionAttemptEntry RestoreArchivedPayload(OutboundSubmissionAttemptEntry attempt)
        {
            if (!attempt.PayloadPurgedUtc.HasValue)
                return attempt;
            if (string.IsNullOrWhiteSpace(attempt.ArchiveFilePath))
                throw new InvalidDataException("The archived audit payload has no archive file reference.");
            if (!File.Exists(attempt.ArchiveFilePath))
                throw new FileNotFoundException("The archived audit payload file was not found.", attempt.ArchiveFilePath);
            if (!string.IsNullOrWhiteSpace(attempt.ArchiveSha256))
            {
                string actualHash = OutboundAuditArchiveService.ComputeFileHash(attempt.ArchiveFilePath);
                if (!string.Equals(actualHash, attempt.ArchiveSha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("The archived audit payload checksum is invalid.");
            }
            using (var file = new FileStream(attempt.ArchiveFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.SequentialScan))
            using (var gzip = new GZipStream(file, CompressionMode.Decompress, false))
            using (var reader = new StreamReader(gzip, Encoding.UTF8, true, 65536))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                    JObject item = JObject.Parse(line);
                    Guid id;
                    if (!Guid.TryParse((string)item["AttemptId"], out id) || id != attempt.AttemptId)
                        continue;
                    attempt.RequestContentType = (string)item["RequestContentType"];
                    attempt.RequestPayload = (string)item["RequestPayload"];
                    attempt.RequestHash = (string)item["RequestHash"];
                    attempt.ResponseContentType = (string)item["ResponseContentType"];
                    attempt.ResponsePayload = (string)item["ResponsePayload"];
                    attempt.ErrorType = (string)item["ErrorType"];
                    attempt.ErrorMessage = (string)item["ErrorMessage"];
                    attempt.StackTrace = (string)item["StackTrace"];
                    return attempt;
                }
            }
            throw new InvalidDataException("The selected audit attempt was not found in its verified archive.");
        }

        public void SetPinned(Guid submissionId, bool pinned, string initiatedBy)
        {
            EnsureSchema();
            using (var connection = new SqlConnection(connectionString))
            using (var command = CreateCommand(connection, null, @"
UPDATE dbo.OutboundSubmission
SET IsPinned = @Pinned,
    PinnedUtc = CASE WHEN @Pinned = 1 THEN SYSUTCDATETIME() ELSE NULL END,
    PinnedBy = CASE WHEN @Pinned = 1 THEN @PinnedBy ELSE NULL END
WHERE OutboundSubmissionId = @SubmissionId;"))
            {
                Add(command, "@Pinned", SqlDbType.Bit, pinned);
                Add(command, "@PinnedBy", SqlDbType.NVarChar, 128, initiatedBy);
                Add(command, "@SubmissionId", SqlDbType.UniqueIdentifier, submissionId);
                connection.Open();
                command.ExecuteNonQuery();
            }
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

        internal void EnsureSchema()
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

        private static OutboundSubmissionAttemptEntry ReadAttempt(IDataRecord reader)
        {
            return new OutboundSubmissionAttemptEntry
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
                InitiatedBy = GetNullableString(reader, 18),
                ArchiveId = reader.IsDBNull(19) ? (Guid?)null : reader.GetGuid(19),
                ArchivedUtc = GetNullableDateTime(reader, 20),
                PayloadPurgedUtc = GetNullableDateTime(reader, 21),
                ArchiveFilePath = GetNullableString(reader, 22),
                ArchiveSha256 = GetNullableString(reader, 23)
            };
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
        IsPinned bit NOT NULL CONSTRAINT DF_OutboundSubmission_IsPinned DEFAULT (0),
        PinnedUtc datetime2(3) NULL,
        PinnedBy nvarchar(128) NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT PK_OutboundSubmission PRIMARY KEY (OutboundSubmissionId),
        CONSTRAINT UQ_OutboundSubmission_Entity UNIQUE (EntityType, EntityId, Destination)
    );
    CREATE INDEX IX_OutboundSubmission_StatusNext
        ON dbo.OutboundSubmission(Status, NextAttemptUtc, CreatedUtc);
END;

IF OBJECT_ID(N'dbo.OutboundAuditArchive', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OutboundAuditArchive
    (
        ArchiveId uniqueidentifier NOT NULL,
        CreatedUtc datetime2(3) NOT NULL,
        CompletedUtc datetime2(3) NULL,
        Status nvarchar(32) NOT NULL,
        FilePath nvarchar(1024) NOT NULL,
        FromUtc datetime2(3) NULL,
        ToUtc datetime2(3) NULL,
        RecordCount int NULL,
        CompressedBytes bigint NULL,
        Sha256 char(64) NULL,
        ErrorMessage nvarchar(2000) NULL,
        MachineName nvarchar(128) NULL,
        CONSTRAINT PK_OutboundAuditArchive PRIMARY KEY (ArchiveId)
    );
    CREATE INDEX IX_OutboundAuditArchive_StatusCreated
        ON dbo.OutboundAuditArchive(Status, CreatedUtc);
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
        ArchiveId uniqueidentifier NULL,
        ArchivedUtc datetime2(3) NULL,
        PayloadPurgedUtc datetime2(3) NULL,
        CONSTRAINT PK_OutboundSubmissionAttempt PRIMARY KEY (AttemptId),
        CONSTRAINT FK_OutboundSubmissionAttempt_Submission FOREIGN KEY (OutboundSubmissionId)
            REFERENCES dbo.OutboundSubmission(OutboundSubmissionId),
        CONSTRAINT FK_OutboundSubmissionAttempt_Archive FOREIGN KEY (ArchiveId)
            REFERENCES dbo.OutboundAuditArchive(ArchiveId),
        CONSTRAINT UQ_OutboundSubmissionAttempt_Number UNIQUE (OutboundSubmissionId, AttemptNumber)
    );
    CREATE INDEX IX_OutboundSubmissionAttempt_SubmissionStarted
        ON dbo.OutboundSubmissionAttempt(OutboundSubmissionId, StartedUtc DESC);
END;

IF COL_LENGTH(N'dbo.OutboundSubmission', N'IsPinned') IS NULL
    ALTER TABLE dbo.OutboundSubmission ADD IsPinned bit NOT NULL
        CONSTRAINT DF_OutboundSubmission_IsPinned DEFAULT (0);
IF COL_LENGTH(N'dbo.OutboundSubmission', N'PinnedUtc') IS NULL
    ALTER TABLE dbo.OutboundSubmission ADD PinnedUtc datetime2(3) NULL;
IF COL_LENGTH(N'dbo.OutboundSubmission', N'PinnedBy') IS NULL
    ALTER TABLE dbo.OutboundSubmission ADD PinnedBy nvarchar(128) NULL;

IF COL_LENGTH(N'dbo.OutboundSubmissionAttempt', N'ArchiveId') IS NULL
    ALTER TABLE dbo.OutboundSubmissionAttempt ADD ArchiveId uniqueidentifier NULL;
IF COL_LENGTH(N'dbo.OutboundSubmissionAttempt', N'ArchivedUtc') IS NULL
    ALTER TABLE dbo.OutboundSubmissionAttempt ADD ArchivedUtc datetime2(3) NULL;
IF COL_LENGTH(N'dbo.OutboundSubmissionAttempt', N'PayloadPurgedUtc') IS NULL
    ALTER TABLE dbo.OutboundSubmissionAttempt ADD PayloadPurgedUtc datetime2(3) NULL;

IF NOT EXISTS
(
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_OutboundSubmissionAttempt_Archive'
      AND parent_object_id = OBJECT_ID(N'dbo.OutboundSubmissionAttempt')
)
    ALTER TABLE dbo.OutboundSubmissionAttempt WITH CHECK
        ADD CONSTRAINT FK_OutboundSubmissionAttempt_Archive FOREIGN KEY (ArchiveId)
        REFERENCES dbo.OutboundAuditArchive(ArchiveId);

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_OutboundSubmissionAttempt_ArchiveEligibility'
      AND object_id = OBJECT_ID(N'dbo.OutboundSubmissionAttempt')
)
    CREATE INDEX IX_OutboundSubmissionAttempt_ArchiveEligibility
        ON dbo.OutboundSubmissionAttempt(PayloadPurgedUtc, StartedUtc, Outcome)
        INCLUDE (OutboundSubmissionId, ArchiveId);

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

    internal sealed class OutboundAuditCleanupResult
    {
        public int ArchivedAttempts { get; set; }
        public int DeletedMetadataRows { get; set; }
        public int DeletedArchiveFiles { get; set; }
        public long CompressedBytes { get; set; }
        public string ArchiveFile { get; set; }
        public bool Skipped { get; set; }
        public string Message { get; set; }
    }

    internal sealed class OutboundAuditArchiveService
    {
        private const int CommandTimeoutSeconds = 120;
        private const string LockName = "ASFuelControl.OutboundAuditArchive";
        private readonly string connectionString;

        public OutboundAuditArchiveService(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public OutboundAuditCleanupResult Run(OutboundAuditRetentionPolicy policy, Func<bool> cancelRequested)
        {
            if (policy == null)
                throw new ArgumentNullException("policy");
            if (!policy.Enabled)
                return new OutboundAuditCleanupResult { Skipped = true, Message = "Η αυτόματη αρχειοθέτηση είναι απενεργοποιημένη." };

            new OutboundSubmissionAuditStore(connectionString).EnsureSchema();
            string root = Path.GetFullPath(string.IsNullOrWhiteSpace(policy.ArchiveDirectory)
                ? OutboundAuditRetentionPolicy.DefaultArchiveDirectory()
                : policy.ArchiveDirectory);
            Directory.CreateDirectory(root);
            EnsureFreeSpace(root, policy.MinimumFreeSpaceMb);
            DeleteStaleTemporaryFiles(root);

            using (var lockConnection = new SqlConnection(connectionString))
            {
                lockConnection.Open();
                if (!AcquireApplicationLock(lockConnection))
                    return new OutboundAuditCleanupResult { Skipped = true, Message = "Εκτελείται ήδη άλλη εργασία αρχειοθέτησης." };
                try
                {
                    var result = ArchiveBatch(root, policy, cancelRequested ?? delegate { return false; });
                    if (cancelRequested != null && cancelRequested())
                        return result;
                    result.DeletedMetadataRows = DeleteExpiredMetadata(policy);
                    result.DeletedArchiveFiles = DeleteExpiredArchiveFiles(root, policy);
                    if (string.IsNullOrWhiteSpace(result.Message))
                        result.Message = result.ArchivedAttempts == 0
                            ? "Δεν βρέθηκαν ληγμένα payload για αρχειοθέτηση."
                            : "Η αρχειοθέτηση και η επαλήθευση ολοκληρώθηκαν.";
                    return result;
                }
                finally
                {
                    ReleaseApplicationLock(lockConnection);
                }
            }
        }

        private OutboundAuditCleanupResult ArchiveBatch(string root, OutboundAuditRetentionPolicy policy, Func<bool> cancelRequested)
        {
            var result = new OutboundAuditCleanupResult();
            Guid archiveId = Guid.NewGuid();
            DateTime now = DateTime.UtcNow;
            string directory = Path.Combine(root, SafePathPart(Environment.MachineName), now.ToString("yyyy"), now.ToString("MM"));
            Directory.CreateDirectory(directory);
            string baseName = "outbound-audit-" + now.ToString("yyyyMMdd-HHmmss") + "-" + archiveId.ToString("N") + ".jsonl.gz";
            string finalPath = Path.Combine(directory, baseName);
            string temporaryPath = finalPath + ".tmp";
            ArchiveWriteResult written = null;
            try
            {
                written = WriteEligibleAttempts(temporaryPath, archiveId, policy, cancelRequested);
                if (written.AttemptIds.Count == 0)
                {
                    TryDelete(temporaryPath);
                    return result;
                }
                string hash = ComputeFileHash(temporaryPath);
                VerifyArchive(temporaryPath, written.AttemptIds, hash);
                long compressedBytes = new FileInfo(temporaryPath).Length;
                InsertPreparedArchive(archiveId, finalPath, written, compressedBytes, hash);
                File.Move(temporaryPath, finalPath);
                CommitVerifiedArchive(archiveId, finalPath, written.AttemptIds, compressedBytes, hash);
                result.ArchivedAttempts = written.AttemptIds.Count;
                result.CompressedBytes = compressedBytes;
                result.ArchiveFile = finalPath;
                return result;
            }
            catch (OperationCanceledException)
            {
                TryDelete(temporaryPath);
                result.Skipped = true;
                result.Message = "Η αρχειοθέτηση διακόπηκε με ασφάλεια πριν διαγραφούν payload από τη βάση.";
                return result;
            }
            catch (Exception ex)
            {
                TryDelete(temporaryPath);
                MarkArchiveFailed(archiveId, finalPath, ex.Message);
                throw;
            }
        }

        private ArchiveWriteResult WriteEligibleAttempts(
            string path,
            Guid archiveId,
            OutboundAuditRetentionPolicy policy,
            Func<bool> cancelRequested)
        {
            var result = new ArchiveWriteResult();
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(@"
SELECT TOP (@BatchSize)
    attempt.AttemptId, attempt.OutboundSubmissionId, attempt.AttemptNumber,
    attempt.StartedUtc, attempt.FinishedUtc, attempt.Outcome,
    attempt.RequestContentType, attempt.RequestPayload, attempt.RequestHash,
    attempt.ResponseStatusCode, attempt.ResponseContentType, attempt.ResponsePayload,
    attempt.ErrorType, attempt.ErrorMessage, attempt.StackTrace,
    attempt.DurationMilliseconds, attempt.ApplicationVersion, attempt.MachineName,
    attempt.CorrelationId, attempt.InitiatedBy,
    submission.EntityType, submission.EntityId, submission.Destination, submission.Status
FROM dbo.OutboundSubmissionAttempt attempt
JOIN dbo.OutboundSubmission submission
  ON submission.OutboundSubmissionId = attempt.OutboundSubmissionId
WHERE submission.IsPinned = 0
  AND attempt.ArchiveId IS NULL
  AND attempt.PayloadPurgedUtc IS NULL
  AND attempt.Outcome <> N'Sending'
  AND (attempt.RequestPayload IS NOT NULL OR attempt.ResponsePayload IS NOT NULL OR attempt.StackTrace IS NOT NULL)
  AND
  (
      (submission.Status = N'Completed' AND attempt.StartedUtc < @CompletedCutoff)
      OR (submission.Status = N'Failed' AND attempt.StartedUtc < @FailedCutoff)
      OR (submission.Status = N'Unknown' AND attempt.StartedUtc < @UnknownCutoff)
  )
ORDER BY attempt.StartedUtc, attempt.AttemptId;", connection))
            {
                command.CommandTimeout = CommandTimeoutSeconds;
                command.Parameters.Add("@BatchSize", SqlDbType.Int).Value = policy.BatchSize;
                command.Parameters.Add("@CompletedCutoff", SqlDbType.DateTime2).Value = DateTime.UtcNow.AddDays(-policy.CompletedPayloadDays);
                command.Parameters.Add("@FailedCutoff", SqlDbType.DateTime2).Value = DateTime.UtcNow.AddDays(-policy.FailedPayloadDays);
                command.Parameters.Add("@UnknownCutoff", SqlDbType.DateTime2).Value = DateTime.UtcNow.AddDays(-policy.UnknownPayloadDays);
                connection.Open();
                using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, FileOptions.SequentialScan))
                using (var gzip = new GZipStream(file, CompressionLevel.Optimal, false))
                using (var text = new StreamWriter(gzip, new UTF8Encoding(false), 65536))
                using (var json = new JsonTextWriter(text) { Formatting = Formatting.None, CloseOutput = false })
                using (var reader = command.ExecuteReader(CommandBehavior.SequentialAccess))
                {
                    while (reader.Read())
                    {
                        if (cancelRequested())
                            throw new OperationCanceledException();
                        Guid attemptId = reader.GetGuid(0);
                        DateTime startedUtc = reader.GetDateTime(3);
                        WriteArchiveRecord(json, archiveId, reader);
                        json.Flush();
                        text.WriteLine();
                        result.AttemptIds.Add(attemptId);
                        if (!result.FromUtc.HasValue || startedUtc < result.FromUtc.Value)
                            result.FromUtc = startedUtc;
                        if (!result.ToUtc.HasValue || startedUtc > result.ToUtc.Value)
                            result.ToUtc = startedUtc;
                    }
                }
            }
            return result;
        }

        private static void WriteArchiveRecord(JsonTextWriter writer, Guid archiveId, IDataRecord reader)
        {
            writer.WriteStartObject();
            Write(writer, "SchemaVersion", 1);
            Write(writer, "ArchiveId", archiveId.ToString("D"));
            Write(writer, "AttemptId", reader.GetGuid(0).ToString("D"));
            Write(writer, "SubmissionId", reader.GetGuid(1).ToString("D"));
            Write(writer, "AttemptNumber", reader.GetInt32(2));
            Write(writer, "StartedUtc", reader.GetDateTime(3));
            WriteNullable(writer, "FinishedUtc", reader, 4);
            Write(writer, "Outcome", reader.GetString(5));
            WriteNullable(writer, "RequestContentType", reader, 6);
            WriteNullable(writer, "RequestPayload", reader, 7);
            WriteNullable(writer, "RequestHash", reader, 8);
            WriteNullable(writer, "ResponseStatusCode", reader, 9);
            WriteNullable(writer, "ResponseContentType", reader, 10);
            WriteNullable(writer, "ResponsePayload", reader, 11);
            WriteNullable(writer, "ErrorType", reader, 12);
            WriteNullable(writer, "ErrorMessage", reader, 13);
            WriteNullable(writer, "StackTrace", reader, 14);
            WriteNullable(writer, "DurationMilliseconds", reader, 15);
            WriteNullable(writer, "ApplicationVersion", reader, 16);
            WriteNullable(writer, "MachineName", reader, 17);
            WriteNullable(writer, "CorrelationId", reader, 18);
            WriteNullable(writer, "InitiatedBy", reader, 19);
            Write(writer, "EntityType", reader.GetString(20));
            Write(writer, "EntityId", reader.GetGuid(21).ToString("D"));
            Write(writer, "Destination", reader.GetString(22));
            Write(writer, "SubmissionStatus", reader.GetString(23));
            writer.WriteEndObject();
        }

        private static void Write(JsonTextWriter writer, string name, object value)
        {
            writer.WritePropertyName(name);
            writer.WriteValue(value);
        }

        private static void WriteNullable(JsonTextWriter writer, string name, IDataRecord reader, int ordinal)
        {
            writer.WritePropertyName(name);
            if (reader.IsDBNull(ordinal))
                writer.WriteNull();
            else
                writer.WriteValue(reader.GetValue(ordinal));
        }

        private void InsertPreparedArchive(
            Guid archiveId,
            string path,
            ArchiveWriteResult written,
            long compressedBytes,
            string hash)
        {
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(@"
INSERT dbo.OutboundAuditArchive
    (ArchiveId, CreatedUtc, Status, FilePath, FromUtc, ToUtc, RecordCount,
     CompressedBytes, Sha256, MachineName)
VALUES
    (@ArchiveId, SYSUTCDATETIME(), N'Prepared', @FilePath, @FromUtc, @ToUtc,
     @RecordCount, @CompressedBytes, @Sha256, @MachineName);", connection))
            {
                command.CommandTimeout = CommandTimeoutSeconds;
                command.Parameters.Add("@ArchiveId", SqlDbType.UniqueIdentifier).Value = archiveId;
                command.Parameters.Add("@FilePath", SqlDbType.NVarChar, 1024).Value = path;
                command.Parameters.Add("@FromUtc", SqlDbType.DateTime2).Value = (object)written.FromUtc ?? DBNull.Value;
                command.Parameters.Add("@ToUtc", SqlDbType.DateTime2).Value = (object)written.ToUtc ?? DBNull.Value;
                command.Parameters.Add("@RecordCount", SqlDbType.Int).Value = written.AttemptIds.Count;
                command.Parameters.Add("@CompressedBytes", SqlDbType.BigInt).Value = compressedBytes;
                command.Parameters.Add("@Sha256", SqlDbType.Char, 64).Value = hash;
                command.Parameters.Add("@MachineName", SqlDbType.NVarChar, 128).Value = Environment.MachineName;
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        private void CommitVerifiedArchive(
            Guid archiveId,
            string path,
            IList<Guid> attemptIds,
            long compressedBytes,
            string hash)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    foreach (Guid attemptId in attemptIds)
                    {
                        using (var command = new SqlCommand(@"
UPDATE dbo.OutboundSubmissionAttempt
SET ArchiveId = @ArchiveId, ArchivedUtc = SYSUTCDATETIME(),
    RequestPayload = NULL, ResponsePayload = NULL, StackTrace = NULL,
    PayloadPurgedUtc = SYSUTCDATETIME()
WHERE AttemptId = @AttemptId AND ArchiveId IS NULL AND PayloadPurgedUtc IS NULL;", connection, transaction))
                        {
                            command.CommandTimeout = CommandTimeoutSeconds;
                            command.Parameters.Add("@ArchiveId", SqlDbType.UniqueIdentifier).Value = archiveId;
                            command.Parameters.Add("@AttemptId", SqlDbType.UniqueIdentifier).Value = attemptId;
                            if (command.ExecuteNonQuery() != 1)
                                throw new InvalidOperationException("An audit attempt changed while it was being archived: " + attemptId);
                        }
                    }
                    using (var command = new SqlCommand(@"
UPDATE dbo.OutboundAuditArchive
SET Status = N'Verified', CompletedUtc = SYSUTCDATETIME(), FilePath = @FilePath,
    RecordCount = @RecordCount, CompressedBytes = @CompressedBytes, Sha256 = @Sha256,
    ErrorMessage = NULL
WHERE ArchiveId = @ArchiveId;", connection, transaction))
                    {
                        command.CommandTimeout = CommandTimeoutSeconds;
                        command.Parameters.Add("@ArchiveId", SqlDbType.UniqueIdentifier).Value = archiveId;
                        command.Parameters.Add("@FilePath", SqlDbType.NVarChar, 1024).Value = path;
                        command.Parameters.Add("@RecordCount", SqlDbType.Int).Value = attemptIds.Count;
                        command.Parameters.Add("@CompressedBytes", SqlDbType.BigInt).Value = compressedBytes;
                        command.Parameters.Add("@Sha256", SqlDbType.Char, 64).Value = hash;
                        if (command.ExecuteNonQuery() != 1)
                            throw new InvalidOperationException("The prepared audit archive was not found: " + archiveId);
                    }
                    transaction.Commit();
                }
            }
        }

        private int DeleteExpiredMetadata(OutboundAuditRetentionPolicy policy)
        {
            if (policy.MetadataDays <= 0)
                return 0;
            int deleted = 0;
            DateTime cutoff = DateTime.UtcNow.AddDays(-policy.MetadataDays);
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    using (var command = new SqlCommand(@"
DELETE TOP (@BatchSize) attempt
FROM dbo.OutboundSubmissionAttempt attempt
JOIN dbo.OutboundSubmission submission
  ON submission.OutboundSubmissionId = attempt.OutboundSubmissionId
WHERE submission.IsPinned = 0
  AND attempt.PayloadPurgedUtc IS NOT NULL
  AND attempt.PayloadPurgedUtc < @Cutoff
  AND attempt.Outcome <> N'Sending';", connection, transaction))
                    {
                        command.CommandTimeout = CommandTimeoutSeconds;
                        command.Parameters.Add("@BatchSize", SqlDbType.Int).Value = policy.BatchSize;
                        command.Parameters.Add("@Cutoff", SqlDbType.DateTime2).Value = cutoff;
                        deleted += command.ExecuteNonQuery();
                    }
                    using (var command = new SqlCommand(@"
DELETE TOP (@BatchSize) submission
FROM dbo.OutboundSubmission submission
WHERE submission.IsPinned = 0
  AND COALESCE(submission.CompletedUtc, submission.LastAttemptUtc, submission.CreatedUtc) < @Cutoff
  AND NOT EXISTS
      (SELECT 1 FROM dbo.OutboundSubmissionAttempt attempt
       WHERE attempt.OutboundSubmissionId = submission.OutboundSubmissionId);", connection, transaction))
                    {
                        command.CommandTimeout = CommandTimeoutSeconds;
                        command.Parameters.Add("@BatchSize", SqlDbType.Int).Value = policy.BatchSize;
                        command.Parameters.Add("@Cutoff", SqlDbType.DateTime2).Value = cutoff;
                        deleted += command.ExecuteNonQuery();
                    }
                    transaction.Commit();
                }
            }
            return deleted;
        }

        private int DeleteExpiredArchiveFiles(string root, OutboundAuditRetentionPolicy policy)
        {
            if (policy.ArchiveFileDays <= 0)
                return 0;
            var archives = new List<KeyValuePair<Guid, string>>();
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(@"
SELECT TOP (20) archive.ArchiveId, archive.FilePath
FROM dbo.OutboundAuditArchive archive
WHERE archive.Status = N'Verified'
  AND archive.CompletedUtc < @Cutoff
  AND NOT EXISTS
      (SELECT 1 FROM dbo.OutboundSubmissionAttempt attempt
       WHERE attempt.ArchiveId = archive.ArchiveId)
ORDER BY archive.CompletedUtc;", connection))
            {
                command.Parameters.Add("@Cutoff", SqlDbType.DateTime2).Value = DateTime.UtcNow.AddDays(-policy.ArchiveFileDays);
                connection.Open();
                using (var reader = command.ExecuteReader())
                    while (reader.Read())
                        archives.Add(new KeyValuePair<Guid, string>(reader.GetGuid(0), reader.GetString(1)));
            }

            int deleted = 0;
            foreach (var archive in archives)
            {
                string path = Path.GetFullPath(archive.Value);
                if (!IsWithinRoot(root, path))
                    continue;
                if (File.Exists(path))
                    File.Delete(path);
                using (var connection = new SqlConnection(connectionString))
                using (var command = new SqlCommand(@"
UPDATE dbo.OutboundAuditArchive
SET Status = N'Deleted', CompressedBytes = 0,
    ErrorMessage = N'Archive expired according to the configured retention policy.'
WHERE ArchiveId = @ArchiveId AND Status = N'Verified';", connection))
                {
                    command.Parameters.Add("@ArchiveId", SqlDbType.UniqueIdentifier).Value = archive.Key;
                    connection.Open();
                    deleted += command.ExecuteNonQuery();
                }
            }
            return deleted;
        }

        private void MarkArchiveFailed(Guid archiveId, string path, string error)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                using (var command = new SqlCommand(@"
UPDATE dbo.OutboundAuditArchive
SET Status = N'Failed', CompletedUtc = SYSUTCDATETIME(), ErrorMessage = @Error
WHERE ArchiveId = @ArchiveId;
IF @@ROWCOUNT = 0
    INSERT dbo.OutboundAuditArchive
        (ArchiveId, CreatedUtc, CompletedUtc, Status, FilePath, ErrorMessage, MachineName)
    VALUES
        (@ArchiveId, SYSUTCDATETIME(), SYSUTCDATETIME(), N'Failed', @FilePath, @Error, @MachineName);", connection))
                {
                    command.Parameters.Add("@ArchiveId", SqlDbType.UniqueIdentifier).Value = archiveId;
                    command.Parameters.Add("@FilePath", SqlDbType.NVarChar, 1024).Value = path;
                    command.Parameters.Add("@Error", SqlDbType.NVarChar, 2000).Value = Truncate(error, 2000);
                    command.Parameters.Add("@MachineName", SqlDbType.NVarChar, 128).Value = Environment.MachineName;
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
            catch
            {
            }
        }

        private static void VerifyArchive(string path, IList<Guid> expectedIds, string expectedHash)
        {
            string actualHash = ComputeFileHash(path);
            if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The audit archive checksum did not match after writing.");
            var found = new HashSet<Guid>();
            using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.SequentialScan))
            using (var gzip = new GZipStream(file, CompressionMode.Decompress, false))
            using (var reader = new StreamReader(gzip, Encoding.UTF8, true, 65536))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                    JObject item = JObject.Parse(line);
                    Guid id;
                    if (!Guid.TryParse((string)item["AttemptId"], out id) || !found.Add(id))
                        throw new InvalidDataException("The audit archive contains an invalid or duplicate attempt identifier.");
                }
            }
            if (found.Count != expectedIds.Count || expectedIds.Any(id => !found.Contains(id)))
                throw new InvalidDataException("The verified audit archive does not contain every selected attempt.");
        }

        internal static string ComputeFileHash(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.SequentialScan))
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(stream);
                var value = new StringBuilder(hash.Length * 2);
                foreach (byte item in hash)
                    value.Append(item.ToString("x2"));
                return value.ToString();
            }
        }

        private static void EnsureFreeSpace(string path, int minimumFreeSpaceMb)
        {
            string root = Path.GetPathRoot(path);
            if (string.IsNullOrWhiteSpace(root) || root.Length < 2 || root[1] != ':')
                return;
            var drive = new DriveInfo(root);
            long required = (long)minimumFreeSpaceMb * 1024L * 1024L;
            if (drive.AvailableFreeSpace < required)
                throw new IOException("Insufficient free space for the audit archive. Required free space: " + minimumFreeSpaceMb + " MB.");
        }

        private static bool AcquireApplicationLock(SqlConnection connection)
        {
            using (var command = new SqlCommand(@"
DECLARE @result int;
EXEC @result = sys.sp_getapplock
    @Resource = @Resource, @LockMode = N'Exclusive', @LockOwner = N'Session', @LockTimeout = 0;
SELECT @result;", connection))
            {
                command.Parameters.Add("@Resource", SqlDbType.NVarChar, 255).Value = LockName;
                return Convert.ToInt32(command.ExecuteScalar()) >= 0;
            }
        }

        private static void ReleaseApplicationLock(SqlConnection connection)
        {
            try
            {
                using (var command = new SqlCommand("EXEC sys.sp_releaseapplock @Resource=@Resource, @LockOwner=N'Session';", connection))
                {
                    command.Parameters.Add("@Resource", SqlDbType.NVarChar, 255).Value = LockName;
                    command.ExecuteNonQuery();
                }
            }
            catch
            {
            }
        }

        private static void DeleteStaleTemporaryFiles(string root)
        {
            foreach (string path in Directory.GetFiles(root, "*.jsonl.gz.tmp", SearchOption.AllDirectories))
            {
                try
                {
                    if (File.GetLastWriteTimeUtc(path) < DateTime.UtcNow.AddDays(-1))
                        File.Delete(path);
                }
                catch
                {
                }
            }
        }

        private static bool IsWithinRoot(string root, string path)
        {
            string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string fullPath = Path.GetFullPath(path);
            return fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
        }

        private static string SafePathPart(string value)
        {
            string result = string.IsNullOrWhiteSpace(value) ? "unknown" : value;
            foreach (char character in Path.GetInvalidFileNameChars())
                result = result.Replace(character, '_');
            return result;
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
            }
        }

        private static string Truncate(string value, int maximum)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maximum)
                return value;
            return value.Substring(0, maximum);
        }

        private sealed class ArchiveWriteResult
        {
            public readonly List<Guid> AttemptIds = new List<Guid>();
            public DateTime? FromUtc { get; set; }
            public DateTime? ToUtc { get; set; }
        }
    }

    internal sealed class OutboundAuditArchiveWorker : IDisposable
    {
        private readonly string connectionString;
        private readonly ManualResetEvent stopSignal = new ManualResetEvent(false);
        private Thread worker;
        private DateTime? lastRunDate;

        public OutboundAuditArchiveWorker(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public bool IsAlive
        {
            get { return worker != null && worker.IsAlive; }
        }

        public void Start()
        {
            if (IsAlive)
                return;
            stopSignal.Reset();
            worker = new Thread(RunLoop)
            {
                IsBackground = true,
                Name = "OutboundAuditArchiveWorker",
                Priority = ThreadPriority.BelowNormal
            };
            worker.Start();
        }

        public bool Stop()
        {
            stopSignal.Set();
            Thread current = worker;
            if (current == null || current == Thread.CurrentThread)
                return true;
            bool stopped = current.Join(TimeSpan.FromSeconds(30));
            if (stopped)
                worker = null;
            return stopped;
        }

        public static OutboundAuditCleanupResult RunNow(string connectionString)
        {
            return new OutboundAuditArchiveService(connectionString).Run(
                OutboundAuditRetentionPolicy.Load(),
                delegate { return false; });
        }

        private void RunLoop()
        {
            while (!stopSignal.WaitOne(0))
            {
                try
                {
                    OutboundAuditRetentionPolicy policy = OutboundAuditRetentionPolicy.Load();
                    DateTime now = DateTime.Now;
                    if (policy.Enabled && now.Hour >= policy.RunHour &&
                        (!lastRunDate.HasValue || lastRunDate.Value.Date != now.Date))
                    {
                        OutboundAuditCleanupResult result = new OutboundAuditArchiveService(connectionString).Run(
                            policy,
                            delegate { return stopSignal.WaitOne(0); });
                        lastRunDate = now.Date;
                        ASFuelControl.Logging.Logger.Instance.LogToFile(
                            "OutboundAuditArchive",
                            result.Message + " Archived=" + result.ArchivedAttempts +
                            ", MetadataDeleted=" + result.DeletedMetadataRows +
                            ", FilesDeleted=" + result.DeletedArchiveFiles);
                    }
                }
                catch (Exception ex)
                {
                    ASFuelControl.Logging.Logger.Instance.LogToFile("OutboundAuditArchive", ex);
                }
                stopSignal.WaitOne(TimeSpan.FromMinutes(15));
            }
        }

        public void Dispose()
        {
            if (Stop())
                stopSignal.Dispose();
        }
    }
}

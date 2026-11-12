/*
    Prunes runaway direct-myDATA recovery evidence for one invoice.

    This script is intentionally conservative:
      - preview mode is the default;
      - it never deletes FiscalAttempt, FiscalDocumentLease,
        FiscalInvoiceWorkflow, MyDataInvoice, invoice, signing, payment,
        printing, cancellation, MARK, UID, signature, or QR records;
      - it preserves every non-Unknown submission revision;
      - it preserves the first Unknown revision, the current revision, and
        the latest configured number of Unknown revisions;
      - it preserves the first and latest configured number of myDATA
        RequestTransmittedDocs HTTP exchanges;
      - it never deletes the original SendInvoices HTTP exchange.

    SQL Server 2008 R2 compatible.

    IMPORTANT:
      Stop the ASFuelControl/PrintAgent process that is retrying the invoice
      before applying the cleanup. Cleaning the rows does not fix the retry
      loop; deploy the automatic-recovery limit first.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @InvoiceId uniqueidentifier;
DECLARE @ApplyChanges bit;
DECLARE @KeepLatestUnknownRevisions int;
DECLARE @KeepLatestRecoveryHttpExchanges int;

SET @InvoiceId = NULL; -- Example: '11111111-2222-3333-4444-555555555555'
SET @ApplyChanges = 0; -- 0 = preview and rollback, 1 = delete and commit
SET @KeepLatestUnknownRevisions = 10;
SET @KeepLatestRecoveryHttpExchanges = 10;

IF @InvoiceId IS NULL
BEGIN
    RAISERROR('Set @InvoiceId before running this script.', 16, 1);
    RETURN;
END;

IF @KeepLatestUnknownRevisions < 1
BEGIN
    RAISERROR('@KeepLatestUnknownRevisions must be at least 1.', 16, 1);
    RETURN;
END;

IF @KeepLatestRecoveryHttpExchanges < 1
BEGIN
    RAISERROR('@KeepLatestRecoveryHttpExchanges must be at least 1.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'dbo.FiscalDocumentLease', N'U') IS NULL OR
   OBJECT_ID(N'dbo.FiscalAttempt', N'U') IS NULL OR
   OBJECT_ID(N'dbo.FiscalAttemptRevision', N'U') IS NULL
BEGIN
    RAISERROR('The fiscal journal tables do not exist.', 16, 1);
    RETURN;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.FiscalDocumentLease
    WHERE DocumentKind = 1
      AND DocumentId = @InvoiceId
)
BEGIN
    RAISERROR('No fiscal document lease exists for the supplied InvoiceId.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'tempdb..#RevisionCandidates', N'U') IS NOT NULL
BEGIN
    DROP TABLE #RevisionCandidates;
END;

IF OBJECT_ID(N'tempdb..#HttpCandidates', N'U') IS NOT NULL
BEGIN
    DROP TABLE #HttpCandidates;
END;

CREATE TABLE #RevisionCandidates
(
    AttemptId uniqueidentifier NOT NULL,
    Revision bigint NOT NULL,
    RecordedUtc datetime2(7) NOT NULL,
    SnapshotBytes bigint NOT NULL,
    PRIMARY KEY (AttemptId, Revision)
);

CREATE TABLE #HttpCandidates
(
    ExchangeId uniqueidentifier NOT NULL PRIMARY KEY,
    StartedUtc datetime2(7) NOT NULL,
    EvidenceBytes bigint NOT NULL
);

DECLARE @LockedFencingToken bigint;
DECLARE @RevisionCandidateCount int;
DECLARE @HttpCandidateCount int;
DECLARE @RevisionBytes bigint;
DECLARE @HttpBytes bigint;
DECLARE @DeletedRevisionCount int;
DECLARE @DeletedHttpCount int;
DECLARE @ErrorMessage nvarchar(2048);
DECLARE @ErrorSeverity int;
DECLARE @ErrorState int;

SET @DeletedRevisionCount = 0;
SET @DeletedHttpCount = 0;

BEGIN TRY
    BEGIN TRANSACTION;

    /*
        The application takes this document row while changing journal state.
        Holding it prevents a cooperating process from advancing the invoice
        during candidate selection and deletion.
    */
    SELECT
        @LockedFencingToken = FencingToken
    FROM dbo.FiscalDocumentLease WITH (UPDLOCK, HOLDLOCK)
    WHERE DocumentKind = 1
      AND DocumentId = @InvoiceId;

    ;WITH RankedUnknownRevisions AS
    (
        SELECT
            attempt.AttemptId,
            revision.Revision,
            revision.RecordedUtc,
            DATALENGTH(revision.SnapshotJson) AS SnapshotBytes,
            ROW_NUMBER() OVER
            (
                PARTITION BY attempt.AttemptId
                ORDER BY revision.Revision
            ) AS OldestRank,
            ROW_NUMBER() OVER
            (
                PARTITION BY attempt.AttemptId
                ORDER BY revision.Revision DESC
            ) AS LatestRank,
            attempt.CurrentRevision
        FROM dbo.FiscalAttempt AS attempt
        INNER JOIN dbo.FiscalAttemptRevision AS revision
            ON revision.AttemptId = attempt.AttemptId
        WHERE attempt.DocumentKind = 1
          AND attempt.DocumentId = @InvoiceId
          AND attempt.Family = N'Submission'
          AND revision.State = N'Unknown'
    )
    INSERT #RevisionCandidates
    (
        AttemptId,
        Revision,
        RecordedUtc,
        SnapshotBytes
    )
    SELECT
        ranked.AttemptId,
        ranked.Revision,
        ranked.RecordedUtc,
        ranked.SnapshotBytes
    FROM RankedUnknownRevisions AS ranked
    WHERE ranked.OldestRank > 1
      AND ranked.LatestRank > @KeepLatestUnknownRevisions
      AND ranked.Revision <> ranked.CurrentRevision;

    IF OBJECT_ID(N'dbo.FiscalHttpExchange', N'U') IS NOT NULL AND
       COL_LENGTH(N'dbo.FiscalHttpExchange', N'DocumentKind') IS NOT NULL AND
       COL_LENGTH(N'dbo.FiscalHttpExchange', N'DocumentId') IS NOT NULL
    BEGIN
        ;WITH RankedRecoveryExchanges AS
        (
            SELECT
                exchangeRow.ExchangeId,
                exchangeRow.StartedUtc,
                CONVERT(bigint, ISNULL(DATALENGTH(exchangeRow.RequestJson), 0)) +
                    CONVERT(bigint, ISNULL(DATALENGTH(exchangeRow.ResponseJson), 0)) AS EvidenceBytes,
                ROW_NUMBER() OVER
                (
                    ORDER BY exchangeRow.StartedUtc, exchangeRow.ExchangeId
                ) AS OldestRank,
                ROW_NUMBER() OVER
                (
                    ORDER BY exchangeRow.StartedUtc DESC, exchangeRow.ExchangeId DESC
                ) AS LatestRank
            FROM dbo.FiscalHttpExchange AS exchangeRow
            WHERE
                (
                    exchangeRow.InvoiceId = @InvoiceId
                    OR
                    (
                        exchangeRow.DocumentKind = 1
                        AND exchangeRow.DocumentId = @InvoiceId
                    )
                )
                AND exchangeRow.RequestJson LIKE N'%"Integration":"myDATA"%'
                AND exchangeRow.RequestJson LIKE N'%"Method":"GET"%'
                AND exchangeRow.RequestJson LIKE N'%RequestTransmittedDocs%'
        )
        INSERT #HttpCandidates
        (
            ExchangeId,
            StartedUtc,
            EvidenceBytes
        )
        SELECT
            ranked.ExchangeId,
            ranked.StartedUtc,
            ranked.EvidenceBytes
        FROM RankedRecoveryExchanges AS ranked
        WHERE ranked.OldestRank > 1
          AND ranked.LatestRank > @KeepLatestRecoveryHttpExchanges;
    END;

    SELECT
        @RevisionCandidateCount = COUNT(*),
        @RevisionBytes = ISNULL(SUM(SnapshotBytes), 0)
    FROM #RevisionCandidates;

    SELECT
        @HttpCandidateCount = COUNT(*),
        @HttpBytes = ISNULL(SUM(EvidenceBytes), 0)
    FROM #HttpCandidates;

    SELECT
        N'CleanupSummary' AS Section,
        @InvoiceId AS InvoiceId,
        @ApplyChanges AS ApplyChanges,
        @LockedFencingToken AS FencingToken,
        @RevisionCandidateCount AS RevisionRowsSelected,
        @RevisionBytes AS RevisionEvidenceBytesSelected,
        @HttpCandidateCount AS HttpRowsSelected,
        @HttpBytes AS HttpEvidenceBytesSelected,
        @KeepLatestUnknownRevisions AS LatestUnknownRevisionsRetained,
        @KeepLatestRecoveryHttpExchanges AS LatestRecoveryHttpExchangesRetained;

    SELECT TOP (25)
        N'RevisionCandidatesSample' AS Section,
        candidate.AttemptId,
        candidate.Revision,
        candidate.RecordedUtc,
        candidate.SnapshotBytes
    FROM #RevisionCandidates AS candidate
    ORDER BY candidate.RecordedUtc, candidate.Revision;

    SELECT TOP (25)
        N'HttpCandidatesSample' AS Section,
        candidate.ExchangeId,
        candidate.StartedUtc,
        candidate.EvidenceBytes
    FROM #HttpCandidates AS candidate
    ORDER BY candidate.StartedUtc, candidate.ExchangeId;

    IF @ApplyChanges = 1
    BEGIN
        DELETE revision
        FROM dbo.FiscalAttemptRevision AS revision
        INNER JOIN #RevisionCandidates AS candidate
            ON candidate.AttemptId = revision.AttemptId
           AND candidate.Revision = revision.Revision
        INNER JOIN dbo.FiscalAttempt AS attempt
            ON attempt.AttemptId = revision.AttemptId
        WHERE attempt.DocumentKind = 1
          AND attempt.DocumentId = @InvoiceId
          AND attempt.Family = N'Submission'
          AND revision.State = N'Unknown'
          AND revision.Revision <> attempt.CurrentRevision;

        SET @DeletedRevisionCount = @@ROWCOUNT;

        DELETE exchangeRow
        FROM dbo.FiscalHttpExchange AS exchangeRow
        INNER JOIN #HttpCandidates AS candidate
            ON candidate.ExchangeId = exchangeRow.ExchangeId;

        SET @DeletedHttpCount = @@ROWCOUNT;

        COMMIT TRANSACTION;
    END;
    ELSE
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    SELECT
        N'CleanupResult' AS Section,
        CASE WHEN @ApplyChanges = 1 THEN N'COMMITTED' ELSE N'PREVIEW_ONLY_ROLLED_BACK' END AS Result,
        @DeletedRevisionCount AS RevisionRowsDeleted,
        @DeletedHttpCount AS HttpRowsDeleted,
        CASE
            WHEN @ApplyChanges = 1 THEN
                N'The selected redundant evidence was deleted. Core fiscal and invoice records were retained.'
            ELSE
                N'No rows were changed. Review the preview, stop the retrying application, then set @ApplyChanges = 1.'
        END AS Message;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    SELECT
        @ErrorMessage = ERROR_MESSAGE(),
        @ErrorSeverity = ERROR_SEVERITY(),
        @ErrorState = ERROR_STATE();

    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;


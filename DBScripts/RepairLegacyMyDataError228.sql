/*
    Repairs one historical direct-myDATA error 228 attempt that an older build
    saved as Rejected instead of ReconciliationRequired.

    This script does NOT mark the invoice as accepted and does NOT write a MARK
    to dbo.Invoice or dbo.MyDataInvoice. It appends an immutable fiscal-journal
    revision. The application must subsequently run "Ανάκτηση διαβίβασης" and
    verify the invoice through AADE before accepting/closing it locally.

    SQL Server 2008 R2 compatible.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @InvoiceId uniqueidentifier;
DECLARE @ExpectedAttemptId uniqueidentifier;
DECLARE @ExpectedUid varchar(40);
DECLARE @ExpectedMark varchar(20);

-- Values from the retained AADE error 228 response:
SET @InvoiceId = '0DB0D978-E660-45E2-BE9B-68C127382C8E';
SET @ExpectedAttemptId = '4858F9F4-188E-40AB-A023-DCB9C346DC38';
SET @ExpectedUid = 'EDA85E68381A6A0A5F699FD03A9C114FFD6446AE';
SET @ExpectedMark = '400015956296676';

IF DB_NAME() = N'master'
BEGIN
    RAISERROR(N'Επιλέξτε πρώτα τη βάση ASFuelControl της εγκατάστασης.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'dbo.Invoice', N'U') IS NULL OR
   OBJECT_ID(N'dbo.MyDataInvoice', N'U') IS NULL OR
   OBJECT_ID(N'dbo.FiscalDocumentLease', N'U') IS NULL OR
   OBJECT_ID(N'dbo.FiscalAttempt', N'U') IS NULL OR
   OBJECT_ID(N'dbo.FiscalAttemptRevision', N'U') IS NULL
BEGIN
    RAISERROR(N'Δεν βρέθηκε το απαιτούμενο φορολογικό ιστορικό.', 16, 1);
    RETURN;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Invoice
    WHERE InvoiceId = @InvoiceId
)
BEGIN
    RAISERROR(N'Δεν βρέθηκε το παραστατικό.', 16, 1);
    RETURN;
END;

IF EXISTS
(
    SELECT 1
    FROM dbo.MyDataInvoice
    WHERE InvoiceId = @InvoiceId
      AND ISNULL(Mark, 0) > 0
)
BEGIN
    RAISERROR(N'Υπάρχει ήδη αποθηκευμένο MARK. Δεν απαιτείται αυτή η επισκευή.', 16, 1);
    RETURN;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @lockResult int;
    DECLARE @resource nvarchar(255);
    SET @resource = N'ASFuelControl.FiscalDocument:1:' +
        REPLACE(CONVERT(nvarchar(36), @InvoiceId), N'-', N'');

    EXEC @lockResult = sys.sp_getapplock
        @Resource = @resource,
        @LockMode = 'Exclusive',
        @LockOwner = 'Transaction',
        @LockTimeout = 15000;

    IF @lockResult < 0
    BEGIN
        RAISERROR(N'Το παραστατικό επεξεργάζεται ήδη. Κλείστε την οθόνη του και δοκιμάστε ξανά.', 16, 1);
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.FiscalDocumentLease WITH (UPDLOCK, HOLDLOCK)
        WHERE DocumentKind = 1
          AND DocumentId = @InvoiceId
    )
    BEGIN
        INSERT dbo.FiscalDocumentLease
            (DocumentKind, DocumentId, FencingToken)
        VALUES
            (1, @InvoiceId, 1);
    END
    ELSE
    BEGIN
        UPDATE dbo.FiscalDocumentLease
        SET FencingToken = FencingToken + 1
        WHERE DocumentKind = 1
          AND DocumentId = @InvoiceId;
    END;

    DECLARE @fencingToken bigint;
    SELECT @fencingToken = FencingToken
    FROM dbo.FiscalDocumentLease
    WHERE DocumentKind = 1
      AND DocumentId = @InvoiceId;

    DECLARE @AttemptId uniqueidentifier;
    DECLARE @CurrentRevision bigint;
    DECLARE @CurrentState nvarchar(32);
    DECLARE @Scope nvarchar(1024);
    DECLARE @Snapshot nvarchar(max);

    SELECT TOP (1)
        @AttemptId = attempt.AttemptId,
        @CurrentRevision = attempt.CurrentRevision,
        @CurrentState = revision.State,
        @Scope = attempt.Scope,
        @Snapshot = revision.SnapshotJson
    FROM dbo.FiscalAttempt AS attempt WITH (UPDLOCK, HOLDLOCK)
    INNER JOIN dbo.FiscalAttemptRevision AS revision WITH (UPDLOCK, HOLDLOCK)
        ON revision.AttemptId = attempt.AttemptId
       AND revision.Revision = attempt.CurrentRevision
    WHERE attempt.DocumentKind = 1
      AND attempt.DocumentId = @InvoiceId
      AND attempt.Family = 'Submission'
    ORDER BY attempt.AttemptSequence DESC;

    IF @AttemptId IS NULL
    BEGIN
        RAISERROR(N'Δεν βρέθηκε προσπάθεια διαβίβασης.', 16, 1);
    END;

    IF @AttemptId <> @ExpectedAttemptId
    BEGIN
        RAISERROR(N'Η τελευταία προσπάθεια δεν είναι η αναμενόμενη. Δεν έγινε αλλαγή.', 16, 1);
    END;

    IF @CurrentState = N'ReconciliationRequired'
    BEGIN
        COMMIT TRANSACTION;
        SELECT
            N'Δεν απαιτήθηκε αλλαγή' AS Result,
            @InvoiceId AS InvoiceId,
            @AttemptId AS AttemptId,
            @CurrentRevision AS CurrentRevision,
            @CurrentState AS CurrentState;
        RETURN;
    END;

    IF @CurrentState <> N'Rejected'
    BEGIN
        RAISERROR(N'Η προσπάθεια δεν βρίσκεται σε κατάσταση Rejected. Δεν έγινε αλλαγή.', 16, 1);
    END;

    IF @Scope NOT LIKE N'myDATA|%'
    BEGIN
        RAISERROR(N'Η προσπάθεια δεν αφορά απευθείας διαβίβαση myDATA.', 16, 1);
    END;

    IF CHARINDEX(N'<code>228</code>', @Snapshot) = 0 OR
       CHARINDEX(CONVERT(nvarchar(40), @ExpectedUid), @Snapshot) = 0 OR
       CHARINDEX(CONVERT(nvarchar(20), @ExpectedMark), @Snapshot) = 0
    BEGIN
        RAISERROR(N'Η αποθηκευμένη απόκριση δεν περιέχει τα αναμενόμενα στοιχεία σφάλματος 228, UID και MARK.', 16, 1);
    END;

    IF CHARINDEX(N'"State":"Rejected"', @Snapshot) = 0 OR
       CHARINDEX(N'"State":"Rejected"', @Snapshot,
           CHARINDEX(N'"State":"Rejected"', @Snapshot) + 1) > 0
    BEGIN
        RAISERROR(N'Η αποθηκευμένη κατάσταση JSON δεν είναι η αναμενόμενη.', 16, 1);
    END;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.FiscalAttempt
        WHERE DocumentKind = 1
          AND DocumentId = @InvoiceId
          AND Family = 'Submission'
          AND AttemptId <> @AttemptId
          AND BlocksNewAttempt = 1
    )
    BEGIN
        RAISERROR(N'Υπάρχει άλλη ενεργή προσπάθεια διαβίβασης. Δεν έγινε αλλαγή.', 16, 1);
    END;

    DECLARE @NewSnapshot nvarchar(max);
    SET @NewSnapshot = REPLACE(@Snapshot,
        N'"State":"Rejected"',
        N'"State":"ReconciliationRequired"');

    IF CHARINDEX(N'"ReconciliationUid":', @NewSnapshot) = 0
    BEGIN
        DECLARE @uidInsertPosition int;
        SET @uidInsertPosition = CHARINDEX(N'"RecoveryResponsesXml":', @NewSnapshot);
        IF @uidInsertPosition = 0
        BEGIN
            RAISERROR(N'Δεν βρέθηκε ασφαλές σημείο εισαγωγής του UID στο JSON.', 16, 1);
        END;
        SET @NewSnapshot = STUFF(@NewSnapshot, @uidInsertPosition, 0,
            N'"ReconciliationUid":"' + CONVERT(nvarchar(40), @ExpectedUid) + N'",');
    END
    ELSE
    BEGIN
        SET @NewSnapshot = REPLACE(@NewSnapshot,
            N'"ReconciliationUid":null',
            N'"ReconciliationUid":"' + CONVERT(nvarchar(40), @ExpectedUid) + N'"');
    END;

    IF CHARINDEX(N'"ReconciliationMark":', @NewSnapshot) = 0
    BEGIN
        DECLARE @markInsertPosition int;
        SET @markInsertPosition = CHARINDEX(N'"RecoveryResponsesXml":', @NewSnapshot);
        IF @markInsertPosition = 0
        BEGIN
            RAISERROR(N'Δεν βρέθηκε ασφαλές σημείο εισαγωγής του MARK στο JSON.', 16, 1);
        END;
        SET @NewSnapshot = STUFF(@NewSnapshot, @markInsertPosition, 0,
            N'"ReconciliationMark":"' + CONVERT(nvarchar(20), @ExpectedMark) + N'",');
    END
    ELSE
    BEGIN
        SET @NewSnapshot = REPLACE(@NewSnapshot,
            N'"ReconciliationMark":null',
            N'"ReconciliationMark":"' + CONVERT(nvarchar(20), @ExpectedMark) + N'"');
    END;

    DECLARE @NewRevision bigint;
    SET @NewRevision = @CurrentRevision + 1;

    INSERT dbo.FiscalAttemptRevision
        (AttemptId, Revision, State, RecordedUtc, SnapshotJson, FencingToken)
    VALUES
        (@AttemptId, @NewRevision, N'ReconciliationRequired',
         GETUTCDATE(), @NewSnapshot, @fencingToken);

    UPDATE dbo.FiscalAttempt
    SET CurrentRevision = @NewRevision,
        BlocksNewAttempt = 1
    WHERE AttemptId = @AttemptId
      AND CurrentRevision = @CurrentRevision;

    IF @@ROWCOUNT <> 1
    BEGIN
        RAISERROR(N'Το ιστορικό άλλαξε ταυτόχρονα. Δεν έγινε αλλαγή.', 16, 1);
    END;

    COMMIT TRANSACTION;

    SELECT
        N'Έτοιμο για ασφαλή ανάκτηση από ΑΑΔΕ' AS Result,
        @InvoiceId AS InvoiceId,
        @AttemptId AS AttemptId,
        @CurrentRevision AS PreviousRevision,
        @NewRevision AS CurrentRevision,
        N'ReconciliationRequired' AS CurrentState,
        @ExpectedUid AS ExpectedUid,
        @ExpectedMark AS ExpectedMark;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;
    DECLARE @errorMessage nvarchar(4000);
    SET @errorMessage = ERROR_MESSAGE();
    RAISERROR(N'Η επισκευή ακυρώθηκε: %s', 16, 1, @errorMessage);
END CATCH;


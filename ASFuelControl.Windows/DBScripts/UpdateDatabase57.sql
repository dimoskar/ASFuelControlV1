-- SQL Server 2008 R2 (10.50), compatibility level 100.
-- Invoicing journal v1. One batch: the legacy updater opens a connection per batch.
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @lockResult int;
    EXEC @lockResult = sys.sp_getapplock @Resource = N'ASFuelControl.FiscalSchema',
        @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 15000;
    IF @lockResult < 0 RAISERROR('Cannot acquire fiscal schema lock.', 16, 1);

    IF OBJECT_ID(N'dbo.FiscalSchemaVersion', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.FiscalSchemaVersion (Version int NOT NULL PRIMARY KEY, InstalledUtc datetime2(7) NOT NULL);
        CREATE TABLE dbo.FiscalDocumentLease (
            DocumentKind int NOT NULL CHECK (DocumentKind BETWEEN 1 AND 5),
            DocumentId uniqueidentifier NOT NULL,
            FencingToken bigint NOT NULL CHECK (FencingToken > 0),
            CONSTRAINT PK_FiscalDocumentLease PRIMARY KEY (DocumentKind, DocumentId)
        );
        CREATE TABLE dbo.FiscalAttempt (
            AttemptId uniqueidentifier NOT NULL CONSTRAINT PK_FiscalAttempt PRIMARY KEY,
            AttemptSequence bigint IDENTITY(1,1) NOT NULL UNIQUE,
            DocumentKind int NOT NULL,
            DocumentId uniqueidentifier NOT NULL,
            Family varchar(24) NOT NULL,
            Operation varchar(24) NOT NULL,
            Scope nvarchar(1024) NOT NULL,
            PayloadFormat nvarchar(128) NOT NULL,
            RequestPayload varbinary(max) NOT NULL,
            RequestSha256 char(64) NOT NULL,
            CreatedUtc datetime2(7) NOT NULL,
            CurrentRevision bigint NOT NULL CHECK (CurrentRevision >= 0),
            BlocksNewAttempt bit NOT NULL,
            OriginalAttemptId uniqueidentifier NULL,
            CONSTRAINT FK_FiscalAttempt_Document FOREIGN KEY (DocumentKind, DocumentId)
                REFERENCES dbo.FiscalDocumentLease (DocumentKind, DocumentId),
            CONSTRAINT FK_FiscalAttempt_Original FOREIGN KEY (OriginalAttemptId) REFERENCES dbo.FiscalAttempt (AttemptId),
            CONSTRAINT CK_FiscalAttempt_Family CHECK (Family IN ('Signing','Submission','ProviderSession','Payment','Print','Cancellation'))
        );
        CREATE INDEX IX_FiscalAttempt_Document ON dbo.FiscalAttempt(DocumentKind, DocumentId, Family, AttemptSequence);
        CREATE UNIQUE INDEX UX_FiscalAttempt_Blocking ON dbo.FiscalAttempt(DocumentKind, DocumentId, Family, Operation)
            WHERE BlocksNewAttempt = 1;
        CREATE TABLE dbo.FiscalAttemptRevision (
            AttemptId uniqueidentifier NOT NULL,
            Revision bigint NOT NULL CHECK (Revision >= 0),
            State nvarchar(32) NOT NULL,
            RecordedUtc datetime2(7) NOT NULL,
            SnapshotJson nvarchar(max) NOT NULL,
            FencingToken bigint NOT NULL,
            CONSTRAINT PK_FiscalAttemptRevision PRIMARY KEY (AttemptId, Revision),
            CONSTRAINT FK_FiscalAttemptRevision_Attempt FOREIGN KEY (AttemptId) REFERENCES dbo.FiscalAttempt(AttemptId)
        );
        -- Bodies are supplied by the adapters' existing sanitized logging boundary.
        CREATE TABLE dbo.FiscalHttpExchange (
            ExchangeId uniqueidentifier NOT NULL PRIMARY KEY,
            InvoiceId uniqueidentifier NULL,
            StartedUtc datetime2(7) NOT NULL,
            RequestJson nvarchar(max) NOT NULL,
            CompletedUtc datetime2(7) NULL,
            ResponseJson nvarchar(max) NULL
        );
        CREATE INDEX IX_FiscalHttpExchange_Invoice ON dbo.FiscalHttpExchange(InvoiceId, StartedUtc);
        CREATE TABLE dbo.FiscalPaymentEvent (
            EventId uniqueidentifier NOT NULL PRIMARY KEY,
            TransactionUid nvarchar(128) NOT NULL,
            RecordedUtc datetime2(7) NOT NULL,
            EventJson nvarchar(max) NOT NULL
        );
        CREATE INDEX IX_FiscalPaymentEvent_Transaction ON dbo.FiscalPaymentEvent(TransactionUid, RecordedUtc);
        INSERT dbo.FiscalSchemaVersion(Version, InstalledUtc) VALUES(1, SYSUTCDATETIME());
    END;
    -- Never silently accept a partial or incompatible installation.
    IF NOT EXISTS (SELECT 1 FROM dbo.FiscalSchemaVersion WHERE Version = 1)
        OR (SELECT COUNT(*) FROM dbo.FiscalSchemaVersion) <> 1
        OR OBJECT_ID(N'dbo.FiscalDocumentLease', N'U') IS NULL
        OR OBJECT_ID(N'dbo.FiscalAttempt', N'U') IS NULL
        OR OBJECT_ID(N'dbo.FiscalAttemptRevision', N'U') IS NULL
        OR OBJECT_ID(N'dbo.FiscalHttpExchange', N'U') IS NULL
        OR OBJECT_ID(N'dbo.FiscalPaymentEvent', N'U') IS NULL
        OR COL_LENGTH(N'dbo.FiscalAttempt', N'AttemptSequence') IS NULL
        OR NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.FiscalAttempt') AND name=N'UX_FiscalAttempt_Blocking' AND is_disabled=0)
        RAISERROR('Fiscal journal schema is incomplete or incompatible.', 16, 1);
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    DECLARE @message nvarchar(2048) = ERROR_MESSAGE();
    RAISERROR('%s', 16, 1, @message);
END CATCH;

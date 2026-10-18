-- SQL Server 2008 R2. Immutable host input captured before any external fiscal call.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.FiscalInvoiceWorkflow', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FiscalInvoiceWorkflow
    (
        InvoiceId uniqueidentifier NOT NULL PRIMARY KEY,
        Payload varbinary(max) NOT NULL,
        PayloadSha256 varchar(64) NOT NULL,
        CreatedUtc datetime2(3) NOT NULL,
        FencingToken bigint NOT NULL
    );
END;
IF OBJECT_ID(N'dbo.FiscalDocumentWorkflow', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FiscalDocumentWorkflow
    (
        DocumentKind int NOT NULL,
        DocumentId uniqueidentifier NOT NULL,
        Payload varbinary(max) NOT NULL,
        PayloadSha256 varchar(64) NOT NULL,
        CreatedUtc datetime2(3) NOT NULL,
        FencingToken bigint NOT NULL,
        CONSTRAINT PK_FiscalDocumentWorkflow PRIMARY KEY(DocumentKind,DocumentId),
        CONSTRAINT CK_FiscalDocumentWorkflow_Kind CHECK(DocumentKind IN (2,3))
    );
END;
COMMIT TRANSACTION;

-- Resumable myDiesel invoice-data correction batch. SQL Server 2008 R2 compatible.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.FiscalMyDieselInvoiceCorrection', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FiscalMyDieselInvoiceCorrection
    (
        OriginalInvoiceId uniqueidentifier NOT NULL PRIMARY KEY,
        BatchId uniqueidentifier NOT NULL,
        CouponCaseId uniqueidentifier NOT NULL UNIQUE,
        CreditInvoiceId uniqueidentifier NOT NULL UNIQUE,
        ReplacementInvoiceId uniqueidentifier NOT NULL UNIQUE,
        PlanJson nvarchar(max) NOT NULL,
        PlanSha256 char(64) NOT NULL,
        State varchar(32) NOT NULL,
        Message nvarchar(2000) NOT NULL,
        CreatedUtc datetime2(3) NOT NULL,
        UpdatedUtc datetime2(3) NOT NULL,
        CreatedBy uniqueidentifier NOT NULL,
        CONSTRAINT CK_FiscalMyDieselInvoiceCorrection_Ids CHECK
        (
            OriginalInvoiceId<>CreditInvoiceId AND
            OriginalInvoiceId<>ReplacementInvoiceId AND
            CreditInvoiceId<>ReplacementInvoiceId
        )
    );
    CREATE INDEX IX_FiscalMyDieselInvoiceCorrection_Batch
        ON dbo.FiscalMyDieselInvoiceCorrection(BatchId,CreatedUtc);
END;
COMMIT TRANSACTION;

SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.FiscalInvoiceTypeCorrection',N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FiscalInvoiceTypeCorrection
    (
        OriginalInvoiceId uniqueidentifier NOT NULL PRIMARY KEY,
        BatchId uniqueidentifier NOT NULL,
        CreditInvoiceId uniqueidentifier NULL,
        ReplacementInvoiceId uniqueidentifier NOT NULL UNIQUE,
        PlanJson nvarchar(max) NOT NULL,
        PlanSha256 char(64) NOT NULL,
        State varchar(32) NOT NULL,
        Message nvarchar(2000) NOT NULL,
        CancellationMark bigint NULL,
        CreatedUtc datetime2(3) NOT NULL,
        UpdatedUtc datetime2(3) NOT NULL,
        CreatedBy uniqueidentifier NOT NULL,
        CONSTRAINT CK_FiscalInvoiceTypeCorrection_Ids CHECK
        (OriginalInvoiceId<>ReplacementInvoiceId AND
         (CreditInvoiceId IS NULL OR CreditInvoiceId<>OriginalInvoiceId AND CreditInvoiceId<>ReplacementInvoiceId)),
        CONSTRAINT CK_FiscalInvoiceTypeCorrection_Mark CHECK (CancellationMark IS NULL OR CancellationMark>0)
    );
    CREATE UNIQUE INDEX UX_FiscalInvoiceTypeCorrection_Credit ON dbo.FiscalInvoiceTypeCorrection(CreditInvoiceId) WHERE CreditInvoiceId IS NOT NULL;
    CREATE INDEX IX_FiscalInvoiceTypeCorrection_Batch ON dbo.FiscalInvoiceTypeCorrection(BatchId,CreatedUtc);
END;
COMMIT TRANSACTION;

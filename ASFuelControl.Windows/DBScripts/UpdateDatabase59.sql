-- SQL Server 2008 R2. Do not infer signing/issuance policy from legacy flags.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.FiscalConfiguration', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FiscalConfiguration
    (
        Revision bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ConfigurationJson nvarchar(max) NOT NULL,
        CreatedUtc datetime2(3) NOT NULL,
        CreatedBy nvarchar(200) NOT NULL
    );
END;
IF OBJECT_ID(N'dbo.FiscalInvoiceTypePolicy', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FiscalInvoiceTypePolicy
    (
        InvoiceTypeId uniqueidentifier NOT NULL PRIMARY KEY,
        TransmissionMode int NOT NULL,
        SignWithFiscalDevice bit NOT NULL,
        Revision bigint NOT NULL,
        UpdatedUtc datetime2(3) NOT NULL,
        CONSTRAINT CK_FiscalInvoiceTypePolicy_Mode CHECK(TransmissionMode IN (0,1,2)),
        CONSTRAINT CK_FiscalInvoiceTypePolicy_Combination CHECK(TransmissionMode <> 1 OR SignWithFiscalDevice = 0)
    );
END;
COMMIT TRANSACTION;

SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.InvoiceReportDefinition', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InvoiceReportDefinition
    (
        ReportDefinitionId uniqueidentifier NOT NULL CONSTRAINT PK_InvoiceReportDefinition PRIMARY KEY,
        ReportKey nvarchar(64) NOT NULL,
        DefinitionFormat varchar(64) NOT NULL,
        DefinitionPayload varbinary(max) NOT NULL,
        DefinitionSha256 char(64) NOT NULL,
        CreatedUtc datetime2(3) NOT NULL,
        CONSTRAINT CK_InvoiceReportDefinition_Payload CHECK (DATALENGTH(DefinitionPayload) > 0)
    );
    CREATE UNIQUE INDEX UX_InvoiceReportDefinition_Version
        ON dbo.InvoiceReportDefinition(ReportKey, DefinitionSha256);
END;

IF OBJECT_ID(N'dbo.InvoicePrintData', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InvoicePrintData
    (
        InvoicePrintDataId uniqueidentifier NOT NULL CONSTRAINT PK_InvoicePrintData PRIMARY KEY,
        InvoiceId uniqueidentifier NOT NULL,
        ReportDefinitionId uniqueidentifier NOT NULL,
        DataFormat varchar(64) NOT NULL,
        DataPayload varbinary(max) NOT NULL,
        DataSha256 char(64) NOT NULL,
        CreatedUtc datetime2(3) NOT NULL,
        CONSTRAINT FK_InvoicePrintData_Definition FOREIGN KEY (ReportDefinitionId)
            REFERENCES dbo.InvoiceReportDefinition(ReportDefinitionId),
        CONSTRAINT CK_InvoicePrintData_Payload CHECK (DATALENGTH(DataPayload) > 0)
    );
    CREATE UNIQUE INDEX UX_InvoicePrintData_Invoice ON dbo.InvoicePrintData(InvoiceId);
END;

COMMIT TRANSACTION;

USE [ASFuelControlDB_Mavroudis];
GO

SET NOCOUNT ON;

/*
    Leave both values NULL to view the latest stored invoice print.
    Set either identifier to select a specific record.
*/
DECLARE @InvoiceId uniqueidentifier;
DECLARE @InvoicePrintDataId uniqueidentifier;
SET @InvoiceId = NULL;
SET @InvoicePrintDataId = NULL;

IF OBJECT_ID(N'dbo.InvoicePrintData', N'U') IS NULL
BEGIN
    RAISERROR(
        'dbo.InvoicePrintData does not exist. Install database update 67.',
        16,
        1);
    RETURN;
END;

DECLARE @SelectedPrintDataId uniqueidentifier;
DECLARE @SelectedInvoiceId uniqueidentifier;
DECLARE @SelectedReportDefinitionId uniqueidentifier;
DECLARE @DataFormat varchar(64);
DECLARE @DataSha256 char(64);
DECLARE @CreatedUtc datetime2(3);
DECLARE @DataPayload varbinary(max);

SELECT TOP (1)
    @SelectedPrintDataId = printData.InvoicePrintDataId,
    @SelectedInvoiceId = printData.InvoiceId,
    @SelectedReportDefinitionId = printData.ReportDefinitionId,
    @DataFormat = printData.DataFormat,
    @DataSha256 = printData.DataSha256,
    @CreatedUtc = printData.CreatedUtc,
    @DataPayload = printData.DataPayload
FROM dbo.InvoicePrintData AS printData
WHERE (@InvoiceId IS NULL OR printData.InvoiceId = @InvoiceId)
  AND (@InvoicePrintDataId IS NULL OR
       printData.InvoicePrintDataId = @InvoicePrintDataId)
ORDER BY printData.CreatedUtc DESC;

IF @SelectedPrintDataId IS NULL
BEGIN
    RAISERROR(
        'No stored invoice print data matched the supplied identifiers.',
        16,
        1);
    RETURN;
END;

/*
    The payload is stored as UTF-8. This conversion preserves Greek text on
    SQL Server 2019 and newer without using any SQL JSON functionality.
*/
DECLARE @PrintDataJson nvarchar(max);
SET @PrintDataJson = CONVERT(
    nvarchar(max),
    CONVERT(varchar(max), @DataPayload)
        COLLATE Latin1_General_100_CI_AS_SC_UTF8);

/* Stored record, size and integrity information. */
SELECT
    @SelectedPrintDataId AS InvoicePrintDataId,
    @SelectedInvoiceId AS InvoiceId,
    @SelectedReportDefinitionId AS ReportDefinitionId,
    definition.ReportKey,
    @DataFormat AS DataFormat,
    @CreatedUtc AS PrintDataCreatedUtc,
    DATALENGTH(@DataPayload) AS DataBytes,
    @DataSha256 AS StoredDataSha256,
    CONVERT(varchar(64), HASHBYTES('SHA2_256', @DataPayload), 2)
        AS CalculatedDataSha256,
    CASE
        WHEN @DataSha256 = CONVERT(
            varchar(64), HASHBYTES('SHA2_256', @DataPayload), 2)
        THEN CAST(1 AS bit)
        ELSE CAST(0 AS bit)
    END AS DataHashIsValid,
    definition.DefinitionFormat,
    DATALENGTH(definition.DefinitionPayload) AS DefinitionBytes,
    definition.CreatedUtc AS DefinitionCreatedUtc
FROM dbo.InvoiceReportDefinition AS definition
WHERE definition.ReportDefinitionId = @SelectedReportDefinitionId;

/* Complete frozen company, trader, invoice, line and QR data. */
SELECT @PrintDataJson AS PrintDataJson;

/* Exact Telerik report definition used by the invoice. */
SELECT
    definition.ReportDefinitionId,
    definition.ReportKey,
    definition.DefinitionFormat,
    CONVERT(
        nvarchar(max),
        CONVERT(varchar(max), definition.DefinitionPayload)
            COLLATE Latin1_General_100_CI_AS_SC_UTF8) AS ReportDefinitionXml
FROM dbo.InvoiceReportDefinition AS definition
WHERE definition.ReportDefinitionId = @SelectedReportDefinitionId;

/* Recent stored prints, useful for finding an InvoiceId. */
SELECT TOP (50)
    printData.InvoicePrintDataId,
    printData.InvoiceId,
    definition.ReportKey,
    printData.DataFormat,
    DATALENGTH(printData.DataPayload) AS DataBytes,
    printData.CreatedUtc
FROM dbo.InvoicePrintData AS printData
INNER JOIN dbo.InvoiceReportDefinition AS definition
    ON definition.ReportDefinitionId = printData.ReportDefinitionId
ORDER BY printData.CreatedUtc DESC;

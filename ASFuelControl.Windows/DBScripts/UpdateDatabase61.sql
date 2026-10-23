-- One-off heating allowance correction journal. SQL Server 2008 R2 compatible.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.FiscalHeatingCorrection', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FiscalHeatingCorrection
    (
        OriginalInvoiceId uniqueidentifier NOT NULL PRIMARY KEY,
        BatchId uniqueidentifier NOT NULL,
        CreditInvoiceId uniqueidentifier NOT NULL UNIQUE,
        ReplacementInvoiceId uniqueidentifier NOT NULL UNIQUE,
        PlanJson nvarchar(max) NOT NULL,
        PlanSha256 char(64) NOT NULL,
        State nvarchar(40) NOT NULL,
        Message nvarchar(max) NOT NULL,
        CreatedUtc datetime2(3) NOT NULL,
        UpdatedUtc datetime2(3) NOT NULL,
        CreatedBy uniqueidentifier NOT NULL,
        CONSTRAINT CK_FiscalHeatingCorrection_Ids CHECK
          (OriginalInvoiceId<>CreditInvoiceId AND OriginalInvoiceId<>ReplacementInvoiceId AND CreditInvoiceId<>ReplacementInvoiceId)
    );
    CREATE INDEX IX_FiscalHeatingCorrection_Batch ON dbo.FiscalHeatingCorrection(BatchId,CreatedUtc);
END;

-- Keep existing catalog semantics; the batch credit reverses the original sale's sign.
-- Conditional for the standalone invoicing library, whose host may have no catalog view.
IF OBJECT_ID(N'dbo.InvoiceCatalogView',N'V') IS NOT NULL
    EXEC(N'ALTER VIEW dbo.InvoiceCatalogView
AS
SELECT a.InvoiceId, a.InvoiceTypeId, a.TransactionDate,
    ISNULL(a.DiscountAmount, 0) AS DiscountAmount,
    ISNULL(a.NettoAmount, 0) AS NettoAmount, a.Number,
    ISNULL(a.PaymentType, 1) AS PaymentType,
    ISNULL(a.TotalAmount, 0) AS TotalAmount, a.TraderId, c.Name AS TraderName,
    a.VehicleId, d.PlateNumber, ISNULL(c.VatExemption, 0) AS VatExemption,
    ISNULL(a.VatAmount, 0) AS VatAmount, b.Abbreviation AS InvoiceTypeDesc,
    ISNULL(b.IsLaserPrint, 0) AS IsLaserPrint, ISNULL(a.IsPrinted, 0) AS IsPrinted,
    CASE WHEN heating.CreditInvoiceId IS NOT NULL THEN -ISNULL(originalType.TransactionSign, 1) ELSE ISNULL(b.TransactionSign, 1) END AS TransactionSign,
    ISNULL(lines.InvoiceLineCount, 0) AS InvoiceLineCount,
    ISNULL(parent.Canceled, 0) AS Canceled,
    ISNULL(child.Cancelation, 0) AS Cancelation,
    ISNULL(parent.Replaced, 0) AS ReplaceInvoicesCount,
    ISNULL(parent.Replaced, 0) AS Replaced,
    ISNULL(fin.DebitAmount, 0) AS DebitAmount,
    ISNULL(fin.CreditAmount, 0) AS CreditAmount,
    -- Keep the legacy XML escaping, leading space and nvarchar(1000) truncation.
    -- Inline the expression instead of calling a scalar UDF for every invoice.
    CAST(ISNULL(STUFF((SELECT DISTINCT '', '' + f.Name
        FROM dbo.InvoiceLine s INNER JOIN dbo.FuelType f ON s.FuelTypeId = f.FuelTypeId
        WHERE s.InvoiceId = a.InvoiceId FOR XML PATH('''')), 1, 1, ''''), '''') AS nvarchar(1000)) AS FuelTypes,
    lines.VolumeSum, a.SupplyNumber, a.VehicleOdometer, a.DeliveryAddress, a.Series
FROM dbo.Invoice a
LEFT JOIN dbo.InvoiceType b ON a.InvoiceTypeId = b.InvoiceTypeId
LEFT JOIN dbo.FiscalHeatingCorrection heating ON heating.CreditInvoiceId = a.InvoiceId
LEFT JOIN dbo.Invoice original ON original.InvoiceId = heating.OriginalInvoiceId
LEFT JOIN dbo.InvoiceType originalType ON originalType.InvoiceTypeId = original.InvoiceTypeId
LEFT JOIN dbo.Trader c ON a.TraderId = c.TraderId
LEFT JOIN dbo.Vehicle d ON a.VehicleId = d.VehicleId
OUTER APPLY
(
    SELECT COUNT(*) AS InvoiceLineCount, SUM(il.Volume) AS VolumeSum
    FROM dbo.InvoiceLine il WHERE il.InvoiceId = a.InvoiceId
) lines
OUTER APPLY
(
    SELECT COUNT(CASE WHEN r.RelationType = 0 THEN 1 END) AS Canceled,
           COUNT(CASE WHEN r.RelationType = 1 THEN 1 END) AS Replaced
    FROM dbo.InvoiceRelation r WHERE r.ParentInvoiceId = a.InvoiceId AND r.RelationType IN (0, 1)
) parent
OUTER APPLY
(
    SELECT COUNT(*) AS Cancelation
    FROM dbo.InvoiceRelation r WHERE r.ChildInvoiceId = a.InvoiceId AND r.RelationType = 0
) child
OUTER APPLY
(
    SELECT SUM(t.DebitAmount) AS DebitAmount, SUM(t.CreditAmount) AS CreditAmount
    FROM dbo.FinTransaction t WHERE t.InvoiceId = a.InvoiceId
) fin;');
COMMIT TRANSACTION;

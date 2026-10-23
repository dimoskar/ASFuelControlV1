-- Invoice catalog: SQL Server 2008 R2 compatible, safe to re-run.
-- Reuse existing unfiltered indexes with the same leading keys (regardless of name).
IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes i
    JOIN sys.index_columns k ON k.object_id = i.object_id AND k.index_id = i.index_id AND k.key_ordinal = 1
    JOIN sys.columns c ON c.object_id = k.object_id AND c.column_id = k.column_id
    WHERE i.object_id = OBJECT_ID(N'dbo.Invoice') AND c.name = N'TransactionDate'
      AND i.type IN (1, 2) AND i.is_disabled = 0 AND i.is_hypothetical = 0 AND i.has_filter = 0
)
    CREATE NONCLUSTERED INDEX IX_InvoiceCatalog_Date ON dbo.Invoice(TransactionDate) INCLUDE (InvoiceId, InvoiceTypeId);

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes i
    JOIN sys.index_columns k ON k.object_id = i.object_id AND k.index_id = i.index_id AND k.key_ordinal = 1
    JOIN sys.columns c ON c.object_id = k.object_id AND c.column_id = k.column_id
    WHERE i.object_id = OBJECT_ID(N'dbo.InvoiceLine') AND c.name = N'InvoiceId'
      AND i.type IN (1, 2) AND i.is_disabled = 0 AND i.is_hypothetical = 0 AND i.has_filter = 0
)
    CREATE NONCLUSTERED INDEX IX_InvoiceCatalog_Lines ON dbo.InvoiceLine(InvoiceId) INCLUDE (Volume, FuelTypeId);

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes i
    JOIN sys.index_columns k ON k.object_id = i.object_id AND k.index_id = i.index_id AND k.key_ordinal = 1
    JOIN sys.columns c ON c.object_id = k.object_id AND c.column_id = k.column_id
    JOIN sys.index_columns k2 ON k2.object_id = i.object_id AND k2.index_id = i.index_id AND k2.key_ordinal = 2
    JOIN sys.columns c2 ON c2.object_id = k2.object_id AND c2.column_id = k2.column_id
    WHERE i.object_id = OBJECT_ID(N'dbo.InvoiceRelation') AND c.name = N'ParentInvoiceId' AND c2.name = N'RelationType'
      AND i.type IN (1, 2) AND i.is_disabled = 0 AND i.is_hypothetical = 0 AND i.has_filter = 0
)
    CREATE NONCLUSTERED INDEX IX_InvoiceCatalog_ParentRelation ON dbo.InvoiceRelation(ParentInvoiceId, RelationType);

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes i
    JOIN sys.index_columns k ON k.object_id = i.object_id AND k.index_id = i.index_id AND k.key_ordinal = 1
    JOIN sys.columns c ON c.object_id = k.object_id AND c.column_id = k.column_id
    JOIN sys.index_columns k2 ON k2.object_id = i.object_id AND k2.index_id = i.index_id AND k2.key_ordinal = 2
    JOIN sys.columns c2 ON c2.object_id = k2.object_id AND c2.column_id = k2.column_id
    WHERE i.object_id = OBJECT_ID(N'dbo.InvoiceRelation') AND c.name = N'ChildInvoiceId' AND c2.name = N'RelationType'
      AND i.type IN (1, 2) AND i.is_disabled = 0 AND i.is_hypothetical = 0 AND i.has_filter = 0
)
    CREATE NONCLUSTERED INDEX IX_InvoiceCatalog_ChildRelation ON dbo.InvoiceRelation(ChildInvoiceId, RelationType);

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes i
    JOIN sys.index_columns k ON k.object_id = i.object_id AND k.index_id = i.index_id AND k.key_ordinal = 1
    JOIN sys.columns c ON c.object_id = k.object_id AND c.column_id = k.column_id
    WHERE i.object_id = OBJECT_ID(N'dbo.FinTransaction') AND c.name = N'InvoiceId'
      AND i.type IN (1, 2) AND i.is_disabled = 0 AND i.is_hypothetical = 0 AND i.has_filter = 0
)
    CREATE NONCLUSTERED INDEX IX_InvoiceCatalog_Financial ON dbo.FinTransaction(InvoiceId) INCLUDE (DebitAmount, CreditAmount);
GO
ALTER VIEW dbo.InvoiceCatalogView
AS
SELECT a.InvoiceId, a.InvoiceTypeId, a.TransactionDate,
    ISNULL(a.DiscountAmount, 0) AS DiscountAmount,
    ISNULL(a.NettoAmount, 0) AS NettoAmount, a.Number,
    ISNULL(a.PaymentType, 1) AS PaymentType,
    ISNULL(a.TotalAmount, 0) AS TotalAmount, a.TraderId, c.Name AS TraderName,
    a.VehicleId, d.PlateNumber, ISNULL(c.VatExemption, 0) AS VatExemption,
    ISNULL(a.VatAmount, 0) AS VatAmount, b.Abbreviation AS InvoiceTypeDesc,
    ISNULL(b.IsLaserPrint, 0) AS IsLaserPrint, ISNULL(a.IsPrinted, 0) AS IsPrinted,
    ISNULL(b.TransactionSign, 1) AS TransactionSign,
    ISNULL(lines.InvoiceLineCount, 0) AS InvoiceLineCount,
    ISNULL(parent.Canceled, 0) AS Canceled,
    ISNULL(child.Cancelation, 0) AS Cancelation,
    ISNULL(parent.Replaced, 0) AS ReplaceInvoicesCount,
    ISNULL(parent.Replaced, 0) AS Replaced,
    ISNULL(fin.DebitAmount, 0) AS DebitAmount,
    ISNULL(fin.CreditAmount, 0) AS CreditAmount,
    -- Keep the legacy XML escaping, leading space and nvarchar(1000) truncation.
    -- Inline the expression instead of calling a scalar UDF for every invoice.
    CAST(ISNULL(STUFF((SELECT DISTINCT ', ' + f.Name
        FROM dbo.InvoiceLine s INNER JOIN dbo.FuelType f ON s.FuelTypeId = f.FuelTypeId
        WHERE s.InvoiceId = a.InvoiceId FOR XML PATH('')), 1, 1, ''), '') AS nvarchar(1000)) AS FuelTypes,
    lines.VolumeSum, a.SupplyNumber, a.VehicleOdometer, a.DeliveryAddress, a.Series
FROM dbo.Invoice a
LEFT JOIN dbo.InvoiceType b ON a.InvoiceTypeId = b.InvoiceTypeId
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
) fin;
GO

CREATE VIEW [dbo].[TankLevelStartView]
AS
SELECT     [Level], TansDate, TankId, NEWID() AS ViewId
FROM         (SELECT     dbo.TankSale.EndLevel AS [Level], dbo.SalesTransaction.TransactionTimeStamp AS TansDate, dbo.TankSale.TankId
                       FROM          dbo.TankSale INNER JOIN
                                              dbo.SalesTransaction ON dbo.TankSale.SalesTransactionId = dbo.SalesTransaction.SalesTransactionId
                       UNION
                       SELECT     LevelEnd AS [Level], TransactionTimeEnd AS TansDate, TankId
                       FROM         dbo.TankFilling) AS derivedtbl_1

GO

CREATE VIEW [dbo].[TankLevelEndView]
AS
SELECT     [Level], TansDate, TankId, NEWID() AS ViewId
FROM         (SELECT     dbo.TankSale.EndLevel AS [Level], dbo.SalesTransaction.TransactionTimeStamp AS TansDate, dbo.TankSale.TankId
                       FROM          dbo.TankSale INNER JOIN
                                              dbo.SalesTransaction ON dbo.TankSale.SalesTransactionId = dbo.SalesTransaction.SalesTransactionId
                       UNION
                       SELECT     LevelStart, TransactionTime, TankId
                       FROM         dbo.TankFilling) AS A

GO

CREATE VIEW [dbo].[TankFillingInvoiceView]
AS
SELECT     dbo.InvoiceType.TransactionType, dbo.InvoiceType.InvoiceTypeId, dbo.TankFilling.Volume, dbo.TankFilling.VolumeNormalized, dbo.TankFilling.VolumeReal, 
                      dbo.TankFilling.VolumeRealNormalized, dbo.TankFilling.LevelStart, dbo.TankFilling.LevelEnd, dbo.TankFilling.TankTemperatureStart, 
                      dbo.TankFilling.TankTemperatureEnd, dbo.TankFilling.TransactionTime, dbo.TankFilling.TransactionTimeEnd, dbo.TankFilling.TankId, NEWID() AS ViewId, 
                      dbo.InvoiceLine.Volume AS InvoiceVolume, dbo.InvoiceLine.VolumeNormalized AS InvoiceVolumeNormalized
FROM         dbo.TankFilling INNER JOIN
                      dbo.InvoiceLine ON dbo.TankFilling.TankFillingId = dbo.InvoiceLine.TankFillingId INNER JOIN
                      dbo.Invoice ON dbo.InvoiceLine.InvoiceId = dbo.Invoice.InvoiceId INNER JOIN
                      dbo.InvoiceType ON dbo.Invoice.InvoiceTypeId = dbo.InvoiceType.InvoiceTypeId

GO

ALTER TABLE dbo.SystemEvent ADD
	PrintedDate datetime NULL,
	DocumentSign nvarchar(100) NULL
GO

update dbo.SystemEvent set PrintedDate = ResolvedDate
GO

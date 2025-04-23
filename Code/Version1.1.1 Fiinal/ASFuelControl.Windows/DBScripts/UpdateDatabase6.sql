ALTER TABLE dbo.InvoiceType ADD
	IsInternal bit NULL
GO

CREATE NONCLUSTERED INDEX [idx_invoiceline_salestransaction]
ON [dbo].[InvoiceLine] ([SaleTransactionId])
GO

CREATE NONCLUSTERED INDEX [idx_salestransaction_endlevel]
ON [dbo].[TankSale] ([TankId])
INCLUDE ([SalesTransactionId],[EndLevel])
GO

CREATE NONCLUSTERED INDEX [idx_titrimetry_height_volume]
ON [dbo].[TitrimetryLevel] ([TitrimetryId])
INCLUDE ([TitrimetryLevelId],[Height],[Volume])
GO




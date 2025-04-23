INSERT [dbo].[FuelType] ([FuelTypeId], [Name], [Code], [Color], [ThermalCoeficient], [EnumeratorValue], [BaseDensity]) VALUES (N'2d8b4d44-8a70-444e-b33b-092a89e9d34a', N'ΝΕΡΟ', N'WAT', 0, CAST(1.00000000 AS Decimal(18, 9)), 99, CAST(1000.000 AS Decimal(18, 3)))
GO

CREATE NONCLUSTERED INDEX [dbo.TankSale_SalesTransactionId]
ON [dbo].[TankSale] ([SalesTransactionId])
GO

CREATE NONCLUSTERED INDEX [dbo.SalesTransaction_TransactionTimeStamp]
ON [dbo].[SalesTransaction] ([TransactionTimeStamp])
GO

UPDATE [dbo].[InvoiceType] set [IsLaserPrint] = 0 where InvoiceTypeId in ('43745033-f668-4910-9c10-2a4ddf8a8e43')
GO

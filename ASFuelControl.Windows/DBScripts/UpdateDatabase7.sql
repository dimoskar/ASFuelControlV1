INSERT [dbo].[FuelType] ([FuelTypeId], [Name], [Code], [Color], [ThermalCoeficient], [EnumeratorValue], [BaseDensity]) VALUES (N'53d9c499-22eb-44b8-9126-7bfd497b0980', N'ΜΙΓΜΑ', N'MIX', 0, CAST(0.001008000 AS Decimal(18, 9)), 99, CAST(800.000 AS Decimal(18, 3)))
GO

ALTER TABLE dbo.InvoiceType ADD
	InternalDeliveryDescription nvarchar(200) NULL
GO
ALTER TABLE dbo.InvoiceType SET (LOCK_ESCALATION = TABLE)
GO

UPDATE dbo.InvoiceType set InternalDeliveryDescription = 'Παραλαβή Λιτρομέτρησης' where InvoiceTypeId = '3f54a35b-01f6-43b8-a17e-2d4927e319b8'
GO
UPDATE dbo.InvoiceType set InternalDeliveryDescription = 'Παραλαβή Καυσίμου' where InvoiceTypeId = '25330fa4-dc1d-428b-bd8f-34d7cf00dda4'
GO
UPDATE dbo.InvoiceType set InternalDeliveryDescription = 'Εξαγωγή Καυσίμου για Εξυδάτωση' where InvoiceTypeId = 'e75c52e3-9207-4be3-96d0-56eb98d7b782'
GO
UPDATE dbo.InvoiceType set InternalDeliveryDescription = 'Εξαγωγή Καυσίμου για άλλους λόγους' where InvoiceTypeId = '76f18cac-1f11-4b10-860f-72c6b1dac7ff'
GO
UPDATE dbo.InvoiceType set InternalDeliveryDescription = 'Επιστροφή Καυσίμου' where InvoiceTypeId = '718c7428-1b23-4c60-9635-81dc2fdcd4f4'
GO
UPDATE dbo.InvoiceType set InternalDeliveryDescription = 'Εξαγωγή Καυσίμου για Διύληση' where InvoiceTypeId = '58d7de2e-2648-4b34-acd4-bf4c0590caa3'
GO
UPDATE dbo.InvoiceType set InternalDeliveryDescription = 'Εξαγωγή Καυσίμου' where InvoiceTypeId = 'fbe44bd8-d3b3-41b7-b454-daa3dcc9bb77'
GO






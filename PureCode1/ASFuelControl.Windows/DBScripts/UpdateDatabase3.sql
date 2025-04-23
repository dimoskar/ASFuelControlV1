CREATE VIEW [dbo].[InvoicePrintView]
AS
SELECT     (SELECT     OptionValue
                       FROM          dbo.[Option]
                       WHERE      (OptionKey = 'CompanyName')) AS CompanyName,
                          (SELECT     OptionValue
                            FROM          dbo.[Option] AS Option_7
                            WHERE      (OptionKey = 'CompanyOccupation')) AS CompanyOccupation,
                          (SELECT     OptionValue
                            FROM          dbo.[Option] AS Option_6
                            WHERE      (OptionKey = 'CompanyTIN')) AS CompanyTIN,
                          (SELECT     OptionValue
                            FROM          dbo.[Option] AS Option_5
                            WHERE      (OptionKey = 'CompanyTaxOffice')) AS CompanyTaxOffice,
                          (SELECT     OptionValue
                            FROM          dbo.[Option] AS Option_4
                            WHERE      (OptionKey = 'CompanyCity')) AS CompanyCity,
                          (SELECT     OptionValue
                            FROM          dbo.[Option] AS Option_3
                            WHERE      (OptionKey = 'CompanyAddress')) AS CompanyAddress,
                          (SELECT     OptionValue
                            FROM          dbo.[Option] AS Option_2
                            WHERE      (OptionKey = 'CompanyPhone')) AS CompanyPhone,
                          (SELECT     OptionValue
                            FROM          dbo.[Option] AS Option_1
                            WHERE      (OptionKey = 'CompanyFax')) AS CompanyFax, ISNULL(dbo.Trader.Name, '') AS TraderName, ISNULL(dbo.Trader.TaxRegistrationNumber, '') 
                      AS TraderTin, ISNULL(dbo.Vehicle.PlateNumber, '') AS VehicleNumber, dbo.Invoice.Number, dbo.Invoice.TransactionDate, dbo.Invoice.NettoAmount, 
                      dbo.Invoice.VatAmount, dbo.Invoice.TotalAmount, dbo.Invoice.Series, dbo.Invoice.InvoiceId, dbo.InvoiceType.Description, dbo.InvoiceLine.Volume, 
                      dbo.InvoiceLine.VolumeNormalized, dbo.InvoiceLine.Temperature, dbo.InvoiceLine.UnitPrice, dbo.FuelType.Name, dbo.FuelType.Code, dbo.InvoiceLine.InvoiceLineId, 
                      dbo.Dispenser.OfficialPumpNumber, dbo.Nozzle.OfficialNozzleNumber, dbo.Invoice.InvoiceSignature
FROM         dbo.Invoice INNER JOIN
                      dbo.InvoiceType ON dbo.Invoice.InvoiceTypeId = dbo.InvoiceType.InvoiceTypeId INNER JOIN
                      dbo.InvoiceLine ON dbo.Invoice.InvoiceId = dbo.InvoiceLine.InvoiceId INNER JOIN
                      dbo.FuelType ON dbo.InvoiceLine.FuelTypeId = dbo.FuelType.FuelTypeId INNER JOIN
                      dbo.SalesTransaction ON dbo.InvoiceLine.SaleTransactionId = dbo.SalesTransaction.SalesTransactionId INNER JOIN
                      dbo.Nozzle ON dbo.SalesTransaction.NozzleId = dbo.Nozzle.NozzleId INNER JOIN
                      dbo.Dispenser ON dbo.Nozzle.DispenserId = dbo.Dispenser.DispenserId LEFT OUTER JOIN
                      dbo.Vehicle ON dbo.Invoice.VehicleId = dbo.Vehicle.VehicleId LEFT OUTER JOIN
                      dbo.Trader ON dbo.Invoice.TraderId = dbo.Trader.TraderId

GO

INSERT [dbo].[Option] ([OptionId], [OptionKey], [OptionValue], [Description]) VALUES (N'F07040C7-0AD6-43E6-B895-2DCD4B847825', N'DayCloseHour', N'1:00:00:00', NULL)
GO

INSERT [dbo].[Option] ([OptionId], [OptionKey], [OptionValue], [Description]) VALUES (N'A320EAE5-C569-4190-8D4F-83A5E0B0ACA0', N'VATValue', N'23,00', NULL)
GO

INSERT [dbo].[Option] ([OptionId], [OptionKey], [OptionValue], [Description]) VALUES (N'B3E3FAB8-6E07-4453-923C-F0F231C138B6', N'WindowsInvoicePrint', N'False', NULL)
GO

INSERT [dbo].[Option] ([OptionId], [OptionKey], [OptionValue], [Description]) VALUES (N'FD757A59-D6B2-4FC9-98F6-84A6C2E995B7', N'PrintInvoiceBarcode', N'False', NULL)
GO

INSERT [dbo].[Option] ([OptionId], [OptionKey], [OptionValue], [Description]) VALUES (N'AF15BCEE-FE35-4294-B5C4-8501C60FC1B9', N'BarCodePattern', N'[FuelType.EnumeratorValue:2][UnitPrice:4][Volume:6][TotalPrice:6][Invoice.Number]', NULL)
GO

ALTER TABLE dbo.Balance ADD
	PrintDate datetime NULL,
	DocumentSign nvarchar(100) NULL
GO

ALTER TABLE dbo.Titrimetry ADD
	PrintDate datetime NULL,
	DocumentSign nvarchar(100) NULL
GO

update dbo.Balance set PrintDate = GetDate()
GO


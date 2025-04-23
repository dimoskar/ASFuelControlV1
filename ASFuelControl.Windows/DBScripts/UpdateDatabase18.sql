CREATE TABLE [dbo].[DeviceSetting]
(
	[DeviceSettingId] [uniqueidentifier] NOT NULL,
	[DeviceId] [uniqueidentifier] NOT NULL,
	[DeviceType] [varchar](150) NOT NULL,
	[SettingKey] [nvarchar](250) NOT NULL,
	[SettingValue] [ntext] NOT NULL,
	[Description] [nvarchar](500) NOT NULL,
	[IsSerialNumber] [bit] NOT NULL,
	CONSTRAINT [PK_DeviceSetting] PRIMARY KEY CLUSTERED 
	(
		[DeviceSettingId] ASC
	)
)
GO

CREATE TABLE [dbo].[FinancialTransaction]
(
	[FinancialTransactionId] [uniqueidentifier] NOT NULL,
	[InvoiceId] [uniqueidentifier] NOT NULL,
	[TransactionDate] [datetime] NOT NULL,
	[Amount] [decimal](19, 2) NOT NULL,
	[TransactionTYpe] [int] NOT NULL,
	[Notes] [ntext] NULL,
	CONSTRAINT [PK_FinancialTransaction] PRIMARY KEY CLUSTERED 
	(
		[FinancialTransactionId] ASC
	)
)
GO

ALTER TABLE dbo.Invoice ADD
	Notes nvarchar(1000) NULL
GO

ALTER TABLE dbo.InvoiceType ADD
	NeedsVehicle bit NULL,
	IsCancelation bit NULL,
	HasFinancialTransactions bit NULL,
	IsLaserPrint bit NULL
GO

ALTER TABLE dbo.Titrimetry ADD
	UncertaintyLevel decimal(18, 2) NULL
GO

ALTER TABLE dbo.TitrimetryLevel ADD
	UncertaintyVolume decimal(18, 2) NULL,
	UncertaintyPercent decimal(18, 3) NULL
GO

ALTER TABLE dbo.Trader ADD
	PaymentType int NULL,
	VatExemptionReason nvarchar(500) NULL
GO

ALTER TABLE dbo.Invoice ADD
	PaymentType int NULL
GO

UPDATE dbo.InvoiceType set NeedsVehicle = 0, IsCancelation = 0
GO

UPDATE dbo.InvoiceType set NeedsVehicle = 1 WHERE Printable = 1 AND ISNULL(IsInternal, 0) = 0 AND TransactionType = 0 AND InvoiceTypeId in ('89499fe5-f0fd-4de6-b239-c74df1b3fe41')
GO

Update dbo.Titrimetry set UncertaintyLevel = 0
GO

Update dbo.TitrimetryLevel set UncertaintyVolume = 0, UncertaintyPercent = 0
GO

INSERT [dbo].[InvoiceType] ([InvoiceTypeId], [Description], [Abbreviation], [LastNumber], [TransactionType], [Printable], [OfficialEnumerator], [Printer], [InvoiceFormId], [IsInternal], [InternalDeliveryDescription], [NeedsVehicle], [IsCancelation], [HasFinancialTransactions]) 
VALUES (N'43745033-f668-4910-9c10-2a4ddf8a8e43', N'Ακυρωτικό', N'ΑΚ', 0, 0, 1, 0, N'C:\ASFuelControl\Sign', NULL, 0, '', 0, 1, 1)
GO

UPDATE [dbo].[InvoiceType] set [HasFinancialTransactions] = 0
GO

UPDATE [dbo].[InvoiceType] set [HasFinancialTransactions] = 1 where InvoiceTypeId in ('394612c6-073c-4820-aa60-828aaecefcb7',
																					  '89499fe5-f0fd-4de6-b239-c74df1b3fe41',
																					  '43745033-f668-4910-9c10-2a4ddf8a8e43')
GO

Insert INTO [dbo].[FinancialTransaction] SELECT NewId(), InvoiceId, TransactionDate, TotalAmount, 1, '' from dbo.Invoice, dbo.InvoiceType Where Invoice.InvoiceTypeId = InvoiceType.InvoiceTypeId and InvoiceType.HasFinancialTransactions = 1
GO

Insert INTO [dbo].[FinancialTransaction] SELECT NewId(), InvoiceId, TransactionDate, TotalAmount, -1, '' from dbo.Invoice, dbo.InvoiceType Where Invoice.InvoiceTypeId = InvoiceType.InvoiceTypeId and InvoiceType.HasFinancialTransactions = 1
GO

UPDATE dbo.Trader set PaymentType = 1
GO

UPDATE dbo.Invoice set PaymentType = 1
GO

IF OBJECT_ID('dbo.InvoiceGroupView') IS NOT NULL
	DROP VIEW dbo.InvoiceGroupView
GO

CREATE VIEW dbo.InvoiceGroupView
AS
SELECT     TOP (100) PERCENT NEWID() AS ViewId, MIN(dbo.Invoice.Number) AS MinNumber, MAX(dbo.Invoice.Number) AS MaxNumber, CONVERT(nvarchar(10), 
					  dbo.Invoice.TransactionDate, 103) AS DateString, CONVERT(nvarchar(10), dbo.Invoice.TransactionDate, 112) AS DateString2, SUM(dbo.Invoice.NettoAmount) 
					  AS NettoAmount, SUM(dbo.Invoice.VatAmount) AS VatAmount, SUM(dbo.Invoice.TotalAmount) AS TotalAmount, dbo.InvoiceType.TransactionType, 
					  dbo.InvoiceType.Description, CONVERT(DateTime, CONVERT(nvarchar(10), dbo.Invoice.TransactionDate, 103), 103) AS TransactionDate
FROM         dbo.Invoice INNER JOIN
					  dbo.InvoiceType ON dbo.Invoice.InvoiceTypeId = dbo.InvoiceType.InvoiceTypeId
WHERE     (dbo.InvoiceType.TransactionType = 0)
GROUP BY CONVERT(nvarchar(10), dbo.Invoice.TransactionDate, 112), CONVERT(nvarchar(10), dbo.Invoice.TransactionDate, 103), dbo.InvoiceType.TransactionType, 
					  dbo.InvoiceType.Description
ORDER BY DateString2
GO

UPDATE [dbo].[InvoiceType] set [IsLaserPrint] = 0 where InvoiceTypeId in ('3f54a35b-01f6-43b8-a17e-2d4927e319b8', 
																		  '394612c6-073c-4820-aa60-828aaecefcb7')
GO

UPDATE [dbo].[InvoiceType] set [IsLaserPrint] = 1 where InvoiceTypeId not in ('3f54a35b-01f6-43b8-a17e-2d4927e319b8', 
																		  '394612c6-073c-4820-aa60-828aaecefcb7')
GO

CREATE NONCLUSTERED INDEX [dbo.Invoice_trnasdate_type_idx]
ON [dbo].[Invoice] ([TransactionDate])
INCLUDE ([InvoiceId],[InvoiceTypeId])
GO

CREATE NONCLUSTERED INDEX [dbo.SaleTrans_SentDateTime_idx]
ON [dbo].[SalesTransaction] ([SentDateTime])
GO

CREATE NONCLUSTERED INDEX [dbo.tankSale_all_idx]
ON [dbo].[TankSale] ([TankId])
INCLUDE ([TankSaleId],[SalesTransactionId],[StartVolume],[EndVolume],[StartVolumeNormalized],[EndVolumeNormalized],[StartTemperature],[EndTemperature],[CRC],[StartLevel],[EndLevel],[FuelDensity])
GO

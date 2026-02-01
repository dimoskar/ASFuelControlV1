ALTER TABLE dbo.MyDataInvoice ADD
	InvoiceUrl nvarchar(500) NULL,
	QrCodeUrl nvarchar(500) NULL,
	VerificationHash nvarchar(50) NULL,
	ProviderUrl nvarchar(255) NULL
GO

ALTER TABLE dbo.Trader ADD
	Branch int NOT NULL default 0
GO


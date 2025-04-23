ALTER TABLE dbo.Invoice ALTER COLUMN InvoiceSignature nvarchar(1500)
GO
ALTER TABLE dbo.Invoice ADD
	QRCodeData nvarchar(1000) NULL
GO

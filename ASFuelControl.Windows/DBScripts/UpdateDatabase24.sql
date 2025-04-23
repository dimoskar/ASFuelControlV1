ALTER TABLE dbo.Invoice ADD
	IsEuromat bit NULL
GO

ALTER TABLE dbo.InvoiceType ADD
	TransactionSign int NULL
GO

Update  dbo.InvoiceType set TransactionSign = 1
GO

ALTER TABLE dbo.Invoice ADD
	DiscountAmount decimal(18, 2) NULL
GO

update dbo.Invoice set DiscountAmount = 0
GO

ALTER TABLE dbo.SalesTransaction ADD
	DiscountPercentage decimal(10, 3) NULL
GO

update dbo.SalesTransaction set DiscountPercentage = 0
GO



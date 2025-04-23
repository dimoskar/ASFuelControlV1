ALTER TABLE dbo.Invoice ADD
	LastRestAmount decimal(18, 2) NULL
GO

ALTER TABLE dbo.Trader ADD
	PrintDebtOnInvoice bit NULL
GO

UPDATE InvoiceType set AdminView = 1 where InvoiceTypeId in
(
'5fd053a5-84b1-4564-b445-6b0b23a359f1',
'78271fe2-3b97-4870-9d96-6ecbbd4b9ce0',
'04c39f93-e648-4be0-8d06-a8e34f48839a',
'04c3f1d4-437b-4874-be3c-c1238615a4a6',
'c7d3d685-e4cf-4a98-a757-d4c12da411d3'
)
GO

ALTER TABLE dbo.InvoiceTypeTransform ADD
	CreationInvoiceTypeId uniqueidentifier NULL,
	CreationType int NULL,
	CreationNotesAddition nvarchar(500) NULL
GO

ALTER TABLE dbo.Invoice ADD
	AllowEdit bit NULL
GO

ALTER TABLE dbo.InvoiceTypeTransform ADD CONSTRAINT
	FK_InvoiceTypeTransform_InvoiceType FOREIGN KEY
	(
	CreationInvoiceTypeId
	) REFERENCES dbo.InvoiceType
	(
	InvoiceTypeId
	) ON UPDATE  NO ACTION ON DELETE  NO ACTION 
GO

Update InvoiceTypeTransform set CreationInvoiceTypeId = 'a9bf8c4d-b056-4220-b53f-ad2bbfcdee1d', CreationType = 1 
Where ParentInvoiceTypeId = '394612c6-073c-4820-aa60-828aaecefcb7' and ChildInvoiceTypeId = '10efcdf0-4838-4b88-96fc-2fb8faf28849'
GO


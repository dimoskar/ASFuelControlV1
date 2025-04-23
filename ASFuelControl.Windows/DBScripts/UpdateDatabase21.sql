update dbo.InvoiceType Set IsInternal = 0 where InvoiceTypeId ='25330fa4-dc1d-428b-bd8f-34d7cf00dda4'
GO
update dbo.InvoiceType Set IsInternal = 0 where InvoiceTypeId ='43745033-f668-4910-9c10-2a4ddf8a8e43'
GO
update dbo.InvoiceType Set IsInternal = 0 where InvoiceTypeId ='394612c6-073c-4820-aa60-828aaecefcb7'
GO
update dbo.InvoiceType Set IsInternal = 0 where InvoiceTypeId ='89499fe5-f0fd-4de6-b239-c74df1b3fe41'
GO
update dbo.InvoiceType Set IsInternal = 1 where InvoiceTypeId ='fbe44bd8-d3b3-41b7-b454-daa3dcc9bb77'
GO
update dbo.InvoiceType Set IsInternal = 1 where InvoiceTypeId ='58d7de2e-2648-4b34-acd4-bf4c0590caa3'
GO
update dbo.InvoiceType Set IsInternal = 1 where InvoiceTypeId ='3f54a35b-01f6-43b8-a17e-2d4927e319b8'
GO
update dbo.InvoiceType Set IsInternal = 1 where InvoiceTypeId ='e75c52e3-9207-4be3-96d0-56eb98d7b782'
GO
update dbo.InvoiceType Set IsInternal = 1 where InvoiceTypeId ='76f18cac-1f11-4b10-860f-72c6b1dac7ff'
GO
update dbo.InvoiceType Set IsInternal = 1 where InvoiceTypeId ='718c7428-1b23-4c60-9635-81dc2fdcd4f4'
GO

ALTER VIEW [dbo].[InvoiceGroupView]
AS
SELECT	NEWID() AS ViewId, 
		MIN(dbo.Invoice.Number) AS MinNumber, 
		MAX(dbo.Invoice.Number) AS MaxNumber, 
		CONVERT(nvarchar(10), dbo.Invoice.TransactionDate, 103) AS DateString, 
		CONVERT(nvarchar(10), dbo.Invoice.TransactionDate, 112) AS DateString2, 
		SUM(dbo.Invoice.NettoAmount) AS NettoAmount, 
		SUM(dbo.Invoice.VatAmount) AS VatAmount, 
		SUM(dbo.Invoice.TotalAmount) AS TotalAmount, 
		dbo.InvoiceType.TransactionType, 
		dbo.InvoiceType.Description, 
		CONVERT(DateTime, CONVERT(nvarchar(10), dbo.Invoice.TransactionDate, 103), 103) AS TransactionDate, 
		dbo.InvoiceType.IsInternal
FROM	dbo.Invoice INNER JOIN
			dbo.InvoiceType ON dbo.Invoice.InvoiceTypeId = dbo.InvoiceType.InvoiceTypeId
WHERE     (dbo.InvoiceType.TransactionType = 0)
GROUP BY	CONVERT(nvarchar(10), dbo.Invoice.TransactionDate, 112), 
			CONVERT(nvarchar(10), dbo.Invoice.TransactionDate, 103), 
			dbo.InvoiceType.TransactionType, 
			dbo.InvoiceType.Description, 
			dbo.InvoiceType.IsInternal
GO

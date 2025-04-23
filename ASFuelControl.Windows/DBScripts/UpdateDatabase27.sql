ALTER TABLE dbo.InvoiceType ADD
	ShowFinancialData bit NULL
GO

UPDATE dbo.InvoiceType set ShowFinancialData = 0
GO


UPDATE dbo.InvoiceType set ShowFinancialData = 1 WHERE InvoiceTypeId = '43745033-f668-4910-9c10-2a4ddf8a8e43'
GO

UPDATE dbo.InvoiceType set ShowFinancialData = 1 WHERE InvoiceTypeId = '89499fe5-f0fd-4de6-b239-c74df1b3fe41'
GO

UPDATE dbo.InvoiceType set ShowFinancialData = 1 WHERE InvoiceTypeId = '394612c6-073c-4820-aa60-828aaecefcb7'
GO

UPDATE dbo.InvoiceType set OfficialEnumerator=215 WHERE InvoiceTypeId='43745033-f668-4910-9c10-2a4ddf8a8e43'
GO

UPDATE dbo.InvoiceType set OfficialEnumerator=160 WHERE InvoiceTypeId='3f54a35b-01f6-43b8-a17e-2d4927e319b8'
GO

UPDATE dbo.InvoiceType set OfficialEnumerator=221 WHERE InvoiceTypeId='25330fa4-dc1d-428b-bd8f-34d7cf00dda4'
GO

UPDATE dbo.InvoiceType set OfficialEnumerator=501 WHERE InvoiceTypeId='e75c52e3-9207-4be3-96d0-56eb98d7b782'
GO

UPDATE dbo.InvoiceType set OfficialEnumerator=501 WHERE InvoiceTypeId='76f18cac-1f11-4b10-860f-72c6b1dac7ff'
GO

UPDATE dbo.InvoiceType set OfficialEnumerator=158 WHERE InvoiceTypeId='0a4d983e-107e-4a07-9a8b-79e1ddfa4185'
GO

UPDATE dbo.InvoiceType set OfficialEnumerator=501 WHERE InvoiceTypeId='718c7428-1b23-4c60-9635-81dc2fdcd4f4'
GO

UPDATE dbo.InvoiceType set OfficialEnumerator=173 WHERE InvoiceTypeId='394612c6-073c-4820-aa60-828aaecefcb7'
GO

UPDATE dbo.InvoiceType set OfficialEnumerator=501 WHERE InvoiceTypeId='58d7de2e-2648-4b34-acd4-bf4c0590caa3'
GO

UPDATE dbo.InvoiceType set OfficialEnumerator=221 WHERE InvoiceTypeId='89499fe5-f0fd-4de6-b239-c74df1b3fe41'
GO

UPDATE dbo.InvoiceType set OfficialEnumerator=158 WHERE InvoiceTypeId='fbe44bd8-d3b3-41b7-b454-daa3dcc9bb77'
GO

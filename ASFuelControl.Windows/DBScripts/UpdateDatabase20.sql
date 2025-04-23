CREATE PROCEDURE dbo.ClearLog
AS
BEGIN
	delete ChangeLog Where DateDiff(minute, DateTimeStamp, GETDATE()) > 14400
END
GO

ALTER TABLE dbo.Trader ADD
	Occupation nvarchar(150) NULL
GO

UPDATE dbo.InvoiceType set [Abbreviation] = 'ΤΔΑ' WHERE InvoiceTypeId = '89499fe5-f0fd-4de6-b239-c74df1b3fe41'
GO

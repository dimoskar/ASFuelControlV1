ALTER TABLE dbo.CommunicationController ADD
	EuromatEnabled bit NULL
GO

UPDATE dbo.CommunicationController set EuromatEnabled = 0 where EuromatEnabled is null
GO

ALTER TABLE dbo.CommunicationController ADD
	EuromatIp nvarchar(20) NULL,
	EuromatPort int NULL
GO

CREATE FUNCTION dbo.GetFuelTypeSums
(
	@Year int,
	@Month int,
	@Day int,
	@FuelTypeId uniqueidentifier,
	@InvoiceTypeId uniqueidentifier
)
RETURNS decimal(18,2)
AS
BEGIN
	declare @ret decimal(18,2)

	SELECT	@ret = SUM(InvoiceLine.TotalPrice - InvoiceLine.VatAmount)
	FROM	InvoiceLine INNER JOIN
				Invoice AS Inv ON InvoiceLine.InvoiceId = Inv.InvoiceId
	Where	DATEPART(year, Inv.TransactionDate) = @Year AND
			DATEPART(month, Inv.TransactionDate) = @Month AND
			DATEPART(day, Inv.TransactionDate) = @Day AND
			Inv.InvoiceTypeId = @InvoiceTypeId and 
			FuelTypeId = @FuelTypeId
	return IsNull(@ret, 0)
END
GO

ALTER TABLE dbo.Invoice ADD
	IsPrinted bit NULL,
	Series nvarchar(10) NULL
GO

update Invoice Set IsPrinted = 1
GO

ALTER TABLE dbo.TankFilling ADD
	SentDateTime datetime NULL,
	ResponseCode nvarchar(500) NULL
GO

ALTER TABLE dbo.SalesTransaction ADD
	SentDateTime datetime NULL,
	ResponseCode nvarchar(500) NULL
GO

ALTER TABLE dbo.Balance ADD
	SentDateTime datetime NULL,
	ResponseCode nvarchar(500) NULL
GO

ALTER TABLE dbo.FuelTypePrice ADD
	SentDateTime datetime NULL,
	ResponseCode nvarchar(500) NULL
GO

ALTER TABLE dbo.SystemEvent ADD
	AlarmType int NULL
GO

ALTER TABLE dbo.Dispenser ADD
	DecimalPlaces int NULL,
	UnitPriceDecimalPlaces int NULL
GO

ALTER TABLE dbo.Tank ADD
	OffestWater decimal(19,2) NULL,
	IsVirtual bit NULL
GO

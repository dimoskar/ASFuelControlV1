ALTER TABLE dbo.Dispenser ADD
	VolumeDecimalPlaces int NULL
GO

ALTER TABLE dbo.Nozzle ADD
	NozzleIndex int NULL
GO

Update dbo.Dispenser set VolumeDecimalPlaces = DecimalPlaces
GO

Update dbo.Nozzle set NozzleIndex = OrderId
GO


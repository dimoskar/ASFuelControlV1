ALTER TABLE dbo.FuelType ADD
	ExcludeFromBalance bit NULL
GO

UPDATE dbo.FuelType SET ExcludeFromBalance = 0
GO

UPDATE dbo.FuelType SET ExcludeFromBalance = 1 WHERE EnumeratorValue = 40 OR Code = 'LPG'
GO

UPDATE dbo.FuelType SET BaseDensity = 560, ThermalCoeficient = 0.002 WHERE EnumeratorValue = 40 OR Code = 'LPG'
GO

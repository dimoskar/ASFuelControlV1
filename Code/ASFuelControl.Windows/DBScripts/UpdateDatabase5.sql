ALTER TABLE dbo.TankFilling ADD
	SignSignature nvarchar(255) NULL
GO

update  dbo.TankFilling set SignSignature = 'NOT Signed'
GO


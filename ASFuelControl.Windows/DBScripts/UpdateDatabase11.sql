ALTER TABLE dbo.Tank ADD
	AlarmThreshold int NULL
GO

update dbo.Tank set AlarmThreshold = 4 where AlarmThreshold is null
GO

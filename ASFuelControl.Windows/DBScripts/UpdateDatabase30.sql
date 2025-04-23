ALTER TABLE dbo.TankCheck ADD
	SentDatetime datetime NULL
GO


ALTER TABLE dbo.FleetManagmentCotroller ADD
	ControlerType int NULL,
	DeviceIp nvarchar(50) NULL,
	DevicePort int NULL,
	DeviceIndex int NULL
GO

ALTER VIEW dbo.TankLevelEndView
AS  
SELECT        TankLevel as [Level], CheckDate as TansDate, TankId, TankCheckId AS ViewId
FROM            TankCheck
GO

ALTER VIEW dbo.TankLevelStartView
AS  
SELECT        TankLevel as [Level], CheckDate as TansDate, TankId, TankCheckId AS ViewId
FROM            TankCheck
GO

update dbo.TankCheck set SentDatetime = GetDate() Where SentDatetime is null
GO

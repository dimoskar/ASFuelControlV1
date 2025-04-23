ALTER VIEW [dbo].[SalesView]
AS
SELECT     NEWID() AS ViewId, dbo.SalesTransaction.Volume, dbo.SalesTransaction.VolumeNormalized, dbo.SalesTransaction.UnitPrice, dbo.SalesTransaction.TotalPrice, 
                      dbo.SalesTransaction.TotalizerStart, dbo.SalesTransaction.TotalizerEnd, dbo.FuelType.Name, dbo.Nozzle.OfficialNozzleNumber, dbo.Dispenser.OfficialPumpNumber, 
                      dbo.SalesTransaction.TransactionTimeStamp, dbo.ApplicationUser.UserName
FROM         dbo.SalesTransaction INNER JOIN
                      dbo.Nozzle ON dbo.SalesTransaction.NozzleId = dbo.Nozzle.NozzleId INNER JOIN
                      dbo.Dispenser ON dbo.Nozzle.DispenserId = dbo.Dispenser.DispenserId INNER JOIN
                      dbo.FuelType ON dbo.Nozzle.FuelTypeId = dbo.FuelType.FuelTypeId LEFT OUTER JOIN
                      dbo.ApplicationUser ON dbo.SalesTransaction.ApplicationUserId = dbo.ApplicationUser.ApplicationUserId

GO


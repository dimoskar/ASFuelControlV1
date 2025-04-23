CREATE VIEW [dbo].[TankSaleView]
AS
SELECT     NEWID() AS ViewId, dbo.TankSale.TankSaleId, dbo.TankSale.SalesTransactionId, dbo.TankSale.TankId, dbo.TankSale.StartVolume, dbo.TankSale.EndVolume, 
                      dbo.TankSale.StartVolumeNormalized, dbo.TankSale.EndVolumeNormalized, dbo.TankSale.StartTemperature, dbo.TankSale.EndTemperature, dbo.TankSale.CRC, 
                      dbo.TankSale.StartLevel, dbo.TankSale.EndLevel, dbo.TankSale.FuelDensity, dbo.SalesTransaction.NozzleId, dbo.SalesTransaction.TransactionTimeStamp, 
                      dbo.SalesTransaction.Volume, dbo.SalesTransaction.VolumeNormalized, dbo.SalesTransaction.UnitPrice, dbo.SalesTransaction.TotalPrice
FROM         dbo.SalesTransaction INNER JOIN
                      dbo.TankSale ON dbo.SalesTransaction.SalesTransactionId = dbo.TankSale.SalesTransactionId

GO

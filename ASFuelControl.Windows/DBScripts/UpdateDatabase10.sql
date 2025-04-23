ALTER VIEW [dbo].[TankLevelStartView]
AS
SELECT     [Level], TansDate, TankId, NEWID() AS ViewId
FROM         (SELECT     dbo.TankSale.EndLevel AS [Level], dbo.SalesTransaction.TransactionTimeStamp AS TansDate, dbo.TankSale.TankId
                       FROM          dbo.TankSale INNER JOIN
                                              dbo.SalesTransaction ON dbo.TankSale.SalesTransactionId = dbo.SalesTransaction.SalesTransactionId
                       UNION
                       SELECT     LevelStart AS [Level], TransactionTime AS TansDate, TankId
                       FROM         dbo.TankFilling) AS A

GO

ALTER VIEW [dbo].[TankLevelEndView]
AS
SELECT     [Level], TansDate, TankId, NEWID() AS ViewId
FROM         (SELECT     dbo.TankSale.EndLevel AS [Level], dbo.SalesTransaction.TransactionTimeStamp AS TansDate, dbo.TankSale.TankId
                       FROM          dbo.TankSale INNER JOIN
                                              dbo.SalesTransaction ON dbo.TankSale.SalesTransactionId = dbo.SalesTransaction.SalesTransactionId
                       UNION
                       SELECT     LevelEnd, TransactionTimeEnd, TankId
                       FROM         dbo.TankFilling) AS A

GO


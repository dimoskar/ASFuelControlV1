CREATE VIEW [dbo].[SaleDataView]
AS
SELECT     NEWID() AS ViewId, dbo.SalesTransaction.TotalizerStart, dbo.SalesTransaction.TotalizerEnd, dbo.SalesTransaction.Volume, dbo.SalesTransaction.UnitPrice, 
                      dbo.SalesTransaction.TotalPrice, dbo.SalesTransaction.TransactionTimeStamp, dbo.TankSale.StartLevel, dbo.TankSale.EndLevel, dbo.TankSale.StartVolume, 
                      dbo.TankSale.EndVolume, dbo.Invoice.Number, dbo.Invoice.NettoAmount, dbo.Invoice.VatAmount, dbo.Invoice.TotalAmount, 
                      dbo.InvoiceLine.Volume AS InvoiceLineVolume, dbo.InvoiceLine.InvoiceLineId
FROM         dbo.SalesTransaction INNER JOIN
                      dbo.TankSale ON dbo.SalesTransaction.SalesTransactionId = dbo.TankSale.SalesTransactionId INNER JOIN
                      dbo.InvoiceLine ON dbo.SalesTransaction.SalesTransactionId = dbo.InvoiceLine.SaleTransactionId INNER JOIN
                      dbo.Invoice ON dbo.InvoiceLine.InvoiceId = dbo.Invoice.InvoiceId

GO


ALTER VIEW [dbo].[InvoiceCatalogView]
AS
SELECT	a.InvoiceId, 
		a.InvoiceTypeId, 
		a.TransactionDate, 
		ISNULL(a.DiscountAmount, 0) AS DiscountAmount, 
		ISNULL(a.NettoAmount, 0) AS NettoAmount, 
		a.Number, 
		ISNULL(a.PaymentType, 1) AS PaymentType, 
		ISNULL(a.TotalAmount, 0) AS TotalAmount, 
		a.TraderId, 
		c.Name AS TraderName, 
		a.VehicleId, 
		d.PlateNumber, 
		ISNULL(c.VatExemption, 0) AS VatExemption, 
		ISNULL(a.VatAmount, 0) AS VatAmount, 
		b.Abbreviation AS InvoiceTypeDesc, 
		ISNULL(b.IsLaserPrint, 0) AS IsLaserPrint, 
		ISNULL(a.IsPrinted, 0) AS IsPrinted, 
		ISNULL(b.TransactionSign, 1) AS TransactionSign, 
		ISNULL((SELECT        COUNT(*) AS Expr1
				FROM            dbo.InvoiceLine AS il
				WHERE        (a.InvoiceId = InvoiceId)), 0) AS InvoiceLineCount, 
		ISNULL((SELECT        COUNT(*) AS Expr1
				FROM            dbo.InvoiceRelation AS pr
				WHERE        (ParentInvoiceId = a.InvoiceId) AND (RelationType = 0)), 0) AS Canceled, 
		ISNULL((SELECT        COUNT(*) AS Expr1
				FROM            dbo.InvoiceRelation AS pr
				WHERE        (ChildInvoiceId = a.InvoiceId) AND (RelationType = 0)), 0) AS Cancelation, 
		ISNULL((SELECT        COUNT(*) AS Expr1
				FROM            dbo.InvoiceRelation AS pr
				WHERE        (ParentInvoiceId = a.InvoiceId) AND (RelationType = 1)), 0) AS ReplaceInvoicesCount, 
		ISNULL((SELECT        COUNT(*) AS Expr1
				FROM            dbo.InvoiceRelation AS e
				WHERE        (a.InvoiceId = ParentInvoiceId) AND (RelationType = 1)), 0) AS Replaced, 
		ISNULL((SELECT        SUM(DebitAmount) AS Expr1
				FROM            dbo.FinTransaction AS h
				WHERE        (a.InvoiceId IS NOT NULL) AND (a.InvoiceId = InvoiceId)), 0) AS DebitAmount, 
		ISNULL((SELECT        SUM(CreditAmount) AS Expr1
				FROM            dbo.FinTransaction AS h
				WHERE        (a.InvoiceId IS NOT NULL) AND (a.InvoiceId = InvoiceId)), 0) AS CreditAmount, dbo.GetInvoiceFuelTypes(a.InvoiceId) AS FuelTypes,
							(SELECT        SUM(Volume) AS Expr1
							FROM            dbo.InvoiceLine
							WHERE        (InvoiceId = a.InvoiceId)) AS VolumeSum, 
		a.SupplyNumber, 
		a.VehicleOdometer, 
		a.DeliveryAddress, a.Series
FROM    dbo.Invoice AS a LEFT OUTER JOIN
			dbo.InvoiceType AS b ON a.InvoiceTypeId = b.InvoiceTypeId LEFT OUTER JOIN
			dbo.Trader AS c ON a.TraderId = c.TraderId LEFT OUTER JOIN
			dbo.Vehicle AS d ON a.VehicleId = d.VehicleId
GO

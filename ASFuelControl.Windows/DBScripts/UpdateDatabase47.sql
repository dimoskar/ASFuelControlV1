if COLUMNPROPERTY( OBJECT_ID('dbo.InvoiceLine'),'UnitPriceRetail','AllowsNull') IS NULL
	ALTER TABLE dbo.InvoiceLine
	   ADD
		  UnitPriceRetail DECIMAL(18,5),
		  DiscountAmountRetail DECIMAL(18,2),
		  UnitPriceWhole DECIMAL(18,5),
		  DiscountAmountWhole DECIMAL(18,2),
		  DiscountPercentage DECIMAL(18,5)
GO

if COLUMNPROPERTY( OBJECT_ID('dbo.Invoice'),'DiscountAmountWhole','AllowsNull') is NULL
	ALTER TABLE dbo.Invoice
	ADD
      DiscountAmountWhole DECIMAL(18,2),
      DiscountAmountRetail DECIMAL(18,2)
GO

update InvoiceLine Set 
	UnitPriceRetail = UnitPrice * ((100 + VatPercentage) / 100), 
	UnitPriceWhole = UnitPrice, 
	DiscountAmount = 0, 
	DiscountAmountRetail = 0, 
	DiscountAmountWhole = 0,
	DiscountPercentage = 0
Where InvoiceId in 
(
	SELECT InvoiceId FROM 
	(
		SELECT	Invoice.InvoiceId,	
				Invoice.TotalAmount, 
				Invoice.VatAmount, 
				Invoice.DiscountAmount,
				Sum(InvoiceLine.TotalPrice) LineTotal, 
				Sum(InvoiceLine.VatAmount) as LineVAT,
				Sum(IsNull(InvoiceLine.DiscountAmount, 0)) LineDiscount, 
				Sum(TotalPrice - (Round(UnitPrice * Volume, 2) - IsNull(InvoiceLine.DiscountAmount, 0))) as DiffRetail,
				Sum(TotalPrice - (Round(UnitPrice * VolumeNormalized, 2) - IsNull(InvoiceLine.DiscountAmount, 0))) DiffWhole,
				Invoice.TotalAmount - Sum(InvoiceLine.TotalPrice) as RetailDiff,
				Invoice.TotalAmount - Sum(InvoiceLine.TotalPrice) - Invoice.VatAmount as RetailDiffVAT
		FROM    Invoice INNER JOIN
					InvoiceLine ON Invoice.InvoiceId = InvoiceLine.InvoiceId
		GROUP BY Invoice.InvoiceId, Invoice.TotalAmount, Invoice.VatAmount, Invoice.DiscountAmount
	) AS A
	WHERE A.RetailDiff <> 0 AND VatAmount <> 0 AND LineDiscount = 0
)
GO

update InvoiceLine Set 
	UnitPriceRetail = UnitPrice, 
	UnitPriceWhole = UnitPrice / ((100.0 + VatPercentage) / 100.0), 
	DiscountAmount = 0, 
	DiscountAmountRetail = 0, 
	DiscountAmountWhole = 0,
	DiscountPercentage = 0
Where InvoiceId in 
(
	SELECT InvoiceId FROM 
	(
		SELECT	Invoice.InvoiceId,	
				Invoice.TotalAmount, 
				Invoice.VatAmount, 
				Invoice.DiscountAmount,
				Sum(InvoiceLine.TotalPrice) LineTotal, 
				Sum(InvoiceLine.VatAmount) as LineVAT,
				Sum(IsNull(InvoiceLine.DiscountAmount, 0)) LineDiscount, 
				Sum(TotalPrice - (Round(UnitPrice * Volume, 2) - IsNull(InvoiceLine.DiscountAmount, 0))) as DiffRetail,
				Sum(TotalPrice - (Round(UnitPrice * VolumeNormalized, 2) - IsNull(InvoiceLine.DiscountAmount, 0))) DiffWhole,
				Invoice.TotalAmount - Sum(InvoiceLine.TotalPrice) as RetailDiff,
				Invoice.TotalAmount - Sum(InvoiceLine.TotalPrice) - Invoice.VatAmount as RetailDiffVAT
		FROM    Invoice INNER JOIN
					InvoiceLine ON Invoice.InvoiceId = InvoiceLine.InvoiceId
		GROUP BY Invoice.InvoiceId, Invoice.TotalAmount, Invoice.VatAmount, Invoice.DiscountAmount
	) AS A
	WHERE ABS(A.DiffRetail) <= 0.02 AND VatAmount <> 0  AND LineDiscount = 0
)
GO

update InvoiceLine Set 
	UnitPriceRetail = UnitPrice / ((100.0 + (select case when Invoice.TransactionDate < '2016/06/01' then 23 else 24 end from Invoice Where Invoice.InvoiceId =  InvoiceLine.InvoiceId)) / 100),
	UnitPriceWhole = UnitPrice / ((100.0 + (select case when Invoice.TransactionDate < '2016/06/01' then 23 else 24 end from Invoice Where Invoice.InvoiceId =  InvoiceLine.InvoiceId)) / 100),
	DiscountAmount = 0, 
	DiscountAmountRetail = 0, 
	DiscountAmountWhole = 0,
	DiscountPercentage = 0
Where InvoiceId in 
(
	SELECT InvoiceId FROM 
	(
		SELECT	Invoice.InvoiceId,	
				Invoice.TotalAmount, 
				Invoice.VatAmount, 
				Invoice.DiscountAmount,
				Sum(InvoiceLine.TotalPrice) LineTotal, 
				Sum(InvoiceLine.VatAmount) as LineVAT,
				Sum(IsNull(InvoiceLine.DiscountAmount, 0)) LineDiscount, 
				Sum(TotalPrice - (Round(UnitPrice * Volume, 2) - IsNull(InvoiceLine.DiscountAmount, 0))) as DiffRetail,
				Sum(TotalPrice - (Round(UnitPrice * VolumeNormalized, 2) - IsNull(InvoiceLine.DiscountAmount, 0))) DiffWhole,
				Invoice.TotalAmount - Sum(InvoiceLine.TotalPrice) as RetailDiff,
				Invoice.TotalAmount - Sum(InvoiceLine.TotalPrice) - Invoice.VatAmount as RetailDiffVAT
		FROM    Invoice INNER JOIN
					InvoiceLine ON Invoice.InvoiceId = InvoiceLine.InvoiceId
		GROUP BY Invoice.InvoiceId, Invoice.TotalAmount, Invoice.VatAmount, Invoice.DiscountAmount
	) AS A
	WHERE VatAmount = 0  AND LineDiscount = 0
)
GO

update InvoiceLine Set 
	UnitPriceRetail = UnitPrice * ((100 + VatPercentage) / 100), 
	UnitPriceWhole = UnitPrice, 
	DiscountAmountRetail = DiscountAmount * ((100 + VatPercentage) / 100), 
	DiscountAmountWhole = DiscountAmount
Where InvoiceId in 
(
	SELECT InvoiceId FROM 
	(
		SELECT	Invoice.InvoiceId,	
				Invoice.TotalAmount, 
				Invoice.VatAmount, 
				Invoice.DiscountAmount,
				Sum(InvoiceLine.TotalPrice) LineTotal, 
				Sum(InvoiceLine.VatAmount) as LineVAT,
				Sum(IsNull(InvoiceLine.DiscountAmount, 0)) LineDiscount, 
				Sum(TotalPrice - (Round(UnitPrice * Volume, 2) - IsNull(InvoiceLine.DiscountAmount, 0))) as DiffRetail,
				Sum(TotalPrice - (Round(UnitPrice * VolumeNormalized, 2) - IsNull(InvoiceLine.DiscountAmount, 0))) DiffWhole,
				Invoice.TotalAmount - Sum(InvoiceLine.TotalPrice) as RetailDiff,
				Invoice.TotalAmount - Sum(InvoiceLine.TotalPrice) - Invoice.VatAmount as RetailDiffVAT
		FROM    Invoice INNER JOIN
					InvoiceLine ON Invoice.InvoiceId = InvoiceLine.InvoiceId
		GROUP BY Invoice.InvoiceId, Invoice.TotalAmount, Invoice.VatAmount, Invoice.DiscountAmount
	) AS A
	WHERE A.RetailDiff <> 0 AND VatAmount <> 0  AND LineDiscount <> 0
)
GO

update InvoiceLine Set 
	UnitPriceRetail = UnitPrice, 
	UnitPriceWhole = UnitPrice / ((100 + VatPercentage) / 100), 
	DiscountAmountRetail = DiscountAmount, 
	DiscountAmountWhole = DiscountAmount / ((100 + VatPercentage) / 100)
Where InvoiceId in 
(
	SELECT InvoiceId FROM 
	(
		SELECT	Invoice.InvoiceId,	
				Invoice.TotalAmount, 
				Invoice.VatAmount, 
				Invoice.DiscountAmount,
				Sum(InvoiceLine.TotalPrice) LineTotal, 
				Sum(InvoiceLine.VatAmount) as LineVAT,
				Sum(IsNull(InvoiceLine.DiscountAmount, 0)) LineDiscount, 
				Sum(TotalPrice - (Round(UnitPrice * Volume, 2) - IsNull(InvoiceLine.DiscountAmount, 0))) as DiffRetail,
				Sum(TotalPrice - (Round(UnitPrice * VolumeNormalized, 2) - IsNull(InvoiceLine.DiscountAmount, 0))) DiffWhole,
				Invoice.TotalAmount - Sum(InvoiceLine.TotalPrice) as RetailDiff,
				Invoice.TotalAmount - Sum(InvoiceLine.TotalPrice) - Invoice.VatAmount as RetailDiffVAT
		FROM    Invoice INNER JOIN
					InvoiceLine ON Invoice.InvoiceId = InvoiceLine.InvoiceId
		GROUP BY Invoice.InvoiceId, Invoice.TotalAmount, Invoice.VatAmount, Invoice.DiscountAmount
	) AS A
	WHERE ABS(A.DiffRetail) <= 0.02 AND VatAmount <> 0  AND LineDiscount <> 0
)
GO

update InvoiceLine Set 
	UnitPriceRetail = UnitPrice / ((100.0 + (select case when Invoice.TransactionDate < '2016/06/01' then 23 else 24 end from Invoice Where Invoice.InvoiceId =  InvoiceLine.InvoiceId)) / 100),
	UnitPriceWhole = UnitPrice / ((100.0 + (select case when Invoice.TransactionDate < '2016/06/01' then 23 else 24 end from Invoice Where Invoice.InvoiceId =  InvoiceLine.InvoiceId)) / 100),
	DiscountAmountRetail = DiscountAmount, 
	DiscountAmountWhole = DiscountAmount
Where InvoiceId in 
(
	SELECT InvoiceId FROM 
	(
		SELECT	Invoice.InvoiceId,	
				Invoice.TotalAmount, 
				Invoice.VatAmount, 
				Invoice.DiscountAmount,
				Sum(InvoiceLine.TotalPrice) LineTotal, 
				Sum(InvoiceLine.VatAmount) as LineVAT,
				Sum(IsNull(InvoiceLine.DiscountAmount, 0)) LineDiscount, 
				Sum(TotalPrice - (Round(UnitPrice * Volume, 2) - IsNull(InvoiceLine.DiscountAmount, 0))) as DiffRetail,
				Sum(TotalPrice - (Round(UnitPrice * VolumeNormalized, 2) - IsNull(InvoiceLine.DiscountAmount, 0))) DiffWhole,
				Invoice.TotalAmount - Sum(InvoiceLine.TotalPrice) as RetailDiff,
				Invoice.TotalAmount - Sum(InvoiceLine.TotalPrice) - Invoice.VatAmount as RetailDiffVAT
		FROM    Invoice INNER JOIN
					InvoiceLine ON Invoice.InvoiceId = InvoiceLine.InvoiceId
		GROUP BY Invoice.InvoiceId, Invoice.TotalAmount, Invoice.VatAmount, Invoice.DiscountAmount
	) AS A
	WHERE VatAmount = 0  AND LineDiscount <> 0
)
GO

update InvoiceLine Set 
	UnitPriceRetail = UnitPrice,
	UnitPriceWhole = UnitPrice / ((100 + VatPercentage) / 100), 
	DiscountAmountRetail = DiscountAmount, 
	DiscountAmountWhole = DiscountAmount/ ((100 + VatPercentage) / 100)
Where InvoiceId in 
(
	SELECT InvoiceId FROM 
	(
		SELECT	Invoice.InvoiceId,	
				Invoice.TotalAmount, 
				Invoice.VatAmount, 
				Invoice.DiscountAmount,
				Sum(InvoiceLine.TotalPrice) LineTotal, 
				Sum(InvoiceLine.VatAmount) as LineVAT,
				Sum(IsNull(InvoiceLine.DiscountAmount, 0)) LineDiscount, 
				Sum(TotalPrice - (Round(UnitPrice * Volume, 2) - IsNull(InvoiceLine.DiscountAmount, 0))) as DiffRetail,
				Sum(TotalPrice - (Round(UnitPrice * VolumeNormalized, 2) - IsNull(InvoiceLine.DiscountAmount, 0))) DiffWhole,
				Invoice.TotalAmount - Sum(InvoiceLine.TotalPrice) as RetailDiff,
				Invoice.TotalAmount - Sum(InvoiceLine.TotalPrice) - Invoice.VatAmount as RetailDiffVAT
		FROM    Invoice INNER JOIN
					InvoiceLine ON Invoice.InvoiceId = InvoiceLine.InvoiceId
		GROUP BY Invoice.InvoiceId, Invoice.TotalAmount, Invoice.VatAmount, Invoice.DiscountAmount, InvoiceTypeId
	) AS A
	Where NOT(A.RetailDiff <> 0 AND VatAmount <> 0 AND LineDiscount = 0) AND NOT(ABS(A.DiffRetail) <= 0.02 AND VatAmount <> 0  AND LineDiscount = 0) AND NOT(VatAmount = 0  AND LineDiscount = 0) 
		AND NOT (A.RetailDiff <> 0 AND VatAmount <> 0  AND LineDiscount <> 0) AND NOT (ABS(A.DiffRetail) <= 0.02 AND VatAmount <> 0  AND LineDiscount <> 0) AND NOT (VatAmount = 0  AND LineDiscount <> 0)
		AND ABS(A.RetailDiff) <= 0.01
)
GO

update InvoiceLine Set 
	UnitPriceRetail = UnitPrice * ((100 + VatPercentage) / 100),
	UnitPriceWhole = UnitPrice, 
	DiscountAmountRetail = DiscountAmount * ((100 + VatPercentage) / 100), 
	DiscountAmountWhole = DiscountAmount
Where InvoiceId in 
(
	SELECT InvoiceId FROM 
	(
		SELECT	Invoice.InvoiceId,	
				Invoice.TotalAmount, 
				Invoice.VatAmount, 
				Invoice.DiscountAmount,
				Sum(InvoiceLine.TotalPrice) LineTotal, 
				Sum(InvoiceLine.VatAmount) as LineVAT,
				Sum(IsNull(InvoiceLine.DiscountAmount, 0)) LineDiscount, 
				Sum(TotalPrice - (Round(UnitPrice * Volume, 2) - IsNull(InvoiceLine.DiscountAmount, 0))) as DiffRetail,
				Sum(TotalPrice - (Round(UnitPrice * VolumeNormalized, 2) - IsNull(InvoiceLine.DiscountAmount, 0))) DiffWhole,
				Invoice.TotalAmount - Sum(InvoiceLine.TotalPrice) as RetailDiff,
				Invoice.TotalAmount - Sum(InvoiceLine.TotalPrice) - Invoice.VatAmount as RetailDiffVAT
		FROM    Invoice INNER JOIN
					InvoiceLine ON Invoice.InvoiceId = InvoiceLine.InvoiceId
		GROUP BY Invoice.InvoiceId, Invoice.TotalAmount, Invoice.VatAmount, Invoice.DiscountAmount, InvoiceTypeId
	) AS A
	Where NOT(A.RetailDiff <> 0 AND VatAmount <> 0 AND LineDiscount = 0) AND NOT(ABS(A.DiffRetail) <= 0.02 AND VatAmount <> 0  AND LineDiscount = 0) AND NOT(VatAmount = 0  AND LineDiscount = 0) 
		AND NOT (A.RetailDiff <> 0 AND VatAmount <> 0  AND LineDiscount <> 0) AND NOT (ABS(A.DiffRetail) <= 0.02 AND VatAmount <> 0  AND LineDiscount <> 0) AND NOT (VatAmount = 0  AND LineDiscount <> 0)
		AND ABS(A.RetailDiff) > 0.01
)
GO

update InvoiceLine Set 
	DiscountPercentage = 100 * DiscountAmountWhole / (UnitPriceWhole * Volume)
Where DiscountAmount > 0 AND InvoiceId in 
(
	SELECT InvoiceId FROM 
	(
		SELECT	Invoice.InvoiceId,	
				Invoice.TotalAmount, 
				Invoice.VatAmount, 
				Invoice.DiscountAmount,
				Sum(InvoiceLine.TotalPrice) LineTotal, 
				Sum(InvoiceLine.VatAmount) as LineVAT,
				Sum(IsNull(InvoiceLine.DiscountAmount, 0)) LineDiscount, 
				Sum(TotalPrice - (Round(UnitPrice * Volume, 2) - IsNull(InvoiceLine.DiscountAmount, 0))) as DiffRetail,
				Sum(TotalPrice - (Round(UnitPrice * VolumeNormalized, 2) - IsNull(InvoiceLine.DiscountAmount, 0))) DiffWhole,
				Invoice.TotalAmount - Sum(InvoiceLine.TotalPrice) as RetailDiff,
				Invoice.TotalAmount - Sum(InvoiceLine.TotalPrice) - Invoice.VatAmount as RetailDiffVAT
		FROM    Invoice INNER JOIN
					InvoiceLine ON Invoice.InvoiceId = InvoiceLine.InvoiceId
		GROUP BY Invoice.InvoiceId, Invoice.TotalAmount, Invoice.VatAmount, Invoice.DiscountAmount, InvoiceTypeId
	) AS A
	Where DiffRetail < 0.02 
) 
GO

update InvoiceLine Set 
	DiscountPercentage = 100 * DiscountAmountWhole / (UnitPriceWhole * VolumeNormalized)
Where DiscountAmount > 0 AND InvoiceId in 
(
	SELECT InvoiceId FROM 
	(
		SELECT	Invoice.InvoiceId,	
				Invoice.TotalAmount, 
				Invoice.VatAmount, 
				Invoice.DiscountAmount,
				Sum(InvoiceLine.TotalPrice) LineTotal, 
				Sum(InvoiceLine.VatAmount) as LineVAT,
				Sum(IsNull(InvoiceLine.DiscountAmount, 0)) LineDiscount, 
				Sum(TotalPrice - (Round(UnitPrice * Volume, 2) - IsNull(InvoiceLine.DiscountAmount, 0))) as DiffRetail,
				Sum(TotalPrice - (Round(UnitPrice * VolumeNormalized, 2) - IsNull(InvoiceLine.DiscountAmount, 0))) DiffWhole,
				Invoice.TotalAmount - Sum(InvoiceLine.TotalPrice) as RetailDiff,
				Invoice.TotalAmount - Sum(InvoiceLine.TotalPrice) - Invoice.VatAmount as RetailDiffVAT
		FROM    Invoice INNER JOIN
					InvoiceLine ON Invoice.InvoiceId = InvoiceLine.InvoiceId
		GROUP BY Invoice.InvoiceId, Invoice.TotalAmount, Invoice.VatAmount, Invoice.DiscountAmount, InvoiceTypeId
	) AS A
	Where DiffRetail > 0.02 
) 
GO

Update InvoiceLine set 
	DiscountAmountWhole = 0
WHERE DiscountAmountWhole is null
GO

Update InvoiceLine set 
	DiscountAmountRetail = 0
WHERE DiscountAmountRetail is null
GO

Update InvoiceLine set 
	DiscountPercentage = 0
WHERE DiscountPercentage is null
GO

Update Invoice set 
	DiscountAmountRetail = IsNull((Select Sum(DiscountAmountRetail) from InvoiceLine Where InvoiceLine.InvoiceId = Invoice.InvoiceId), 0),
	DiscountAmountWhole = IsNull((Select Sum(DiscountAmountWhole) from InvoiceLine Where InvoiceLine.InvoiceId = Invoice.InvoiceId), 0)
GO

Update Invoice set 
	DiscountAmount = 0
WHERE DiscountAmount is null
GO

Update Invoice set 
	DiscountAmountRetail = 0
WHERE DiscountAmountRetail is null
GO

Update Invoice set 
	DiscountAmountWhole = 0
WHERE DiscountAmountWhole is null
GO

if COLUMNPROPERTY( OBJECT_ID('dbo.Invoice'),'DiscountAmount','AllowsNull') = 1
	ALTER TABLE Invoice
		ALTER COLUMN DiscountAmount Decimal(18, 2) NOT NULL
GO

if COLUMNPROPERTY( OBJECT_ID('dbo.Invoice'),'DiscountAmountWhole','AllowsNull') = 1
	ALTER TABLE Invoice
		ALTER COLUMN DiscountAmountWhole Decimal(18, 2) NOT NULL
GO

if COLUMNPROPERTY( OBJECT_ID('dbo.Invoice'),'DiscountAmountRetail','AllowsNull') = 1
	ALTER TABLE Invoice
		ALTER COLUMN DiscountAmountRetail Decimal(18, 2) NOT NULL
GO

if COLUMNPROPERTY( OBJECT_ID('dbo.InvoiceLine'),'DiscountPercentage','AllowsNull') = 1
	ALTER TABLE InvoiceLine
		ALTER COLUMN DiscountPercentage Decimal(18, 5) NOT NULL
GO

if COLUMNPROPERTY( OBJECT_ID('dbo.InvoiceLine'),'UnitPriceRetail','AllowsNull') = 1
	ALTER TABLE InvoiceLine
		ALTER COLUMN UnitPriceRetail Decimal(18, 5) NOT NULL
GO

if COLUMNPROPERTY( OBJECT_ID('dbo.InvoiceLine'),'UnitPriceWhole','AllowsNull') = 1
	ALTER TABLE InvoiceLine
		ALTER COLUMN UnitPriceWhole Decimal(18, 5) NOT NULL
GO

if COLUMNPROPERTY( OBJECT_ID('dbo.InvoiceLine'),'DiscountAmountRetail','AllowsNull') = 1
	ALTER TABLE InvoiceLine
		ALTER COLUMN DiscountAmountRetail Decimal(18, 2) NOT NULL
GO

if COLUMNPROPERTY( OBJECT_ID('dbo.InvoiceLine'),'DiscountAmountWhole','AllowsNull') = 1
	ALTER TABLE InvoiceLine
		ALTER COLUMN DiscountAmountWhole Decimal(18, 2) NOT NULL
GO

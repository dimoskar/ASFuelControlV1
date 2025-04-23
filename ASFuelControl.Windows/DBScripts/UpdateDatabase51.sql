ALTER TABLE dbo.Invoice ADD
	NettoAfterDiscount decimal(18, 2) NOT NULL CONSTRAINT DF_Invoice_NettoAfterDiscount DEFAULT 0
GO

UPDATE Invoice set 
			NettoAmount = IsNull(TotalAmount, 0) - IsNull(VatAmount, 0) + IsNull(DiscountAmountWhole, 0), 
			NettoAfterDiscount = IsNull(TotalAmount, 0) - IsNull(VatAmount, 0), 
			DiscountAmount = IsNull(DiscountAmountWhole, 0)
GO

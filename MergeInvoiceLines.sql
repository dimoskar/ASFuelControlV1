begin
	 declare @InvoiceTypeId uniqueidentifier
	 declare @InvoiceId uniqueidentifier
	 declare @invlineid uniqueidentifier
	 declare @InvoiceDesc nvarchar(20)
	 declare @InvoiceNumber integer
	 declare @InvoiceDate DateTime
	 declare @volume decimal(18,3)
	 declare @volumenorm decimal(18,3)
	 declare @untiprice decimal(18,5)
	 declare @totalprice decimal(18,2)
	 declare @untipriceret decimal(18,5)
	 declare @untipricewh decimal(18,5)
	 declare @temp decimal(18,5)
	 declare @fd decimal(18,3)
	 declare @vat decimal(18,3)
	 
	 declare @disc decimal(18,2)
	 declare @discRet decimal(18,2)
	 declare @discWhole decimal(18,2)
	 declare @discper decimal (18,5)

	set @InvoiceDesc = 'тил'
	set @InvoiceNumber = 309
	set @InvoiceDate = '2024/07/31'

	select @InvoiceTypeId = InvoiceTypeId from InvoiceType where Invalidated = 0 and Abbreviation = @InvoiceDesc;
	select @InvoiceId = InvoiceId from Invoice Where InvoiceTypeId = @InvoiceTypeId and Number = @InvoiceNumber and DateDiff(day, @InvoiceDate, TransactionDate) = 0

	select	@volume = Sum(Volume), 
		@volumenorm = Sum(VolumeNormalized), 
		@untiprice = Sum(TotalPrice) / Sum(Volume), 
		@totalprice = Sum(TotalPrice), 
		@temp = Sum(Temperature * Volume) / Sum(Volume),
		@fd = Sum(FuelDensity * Volume) / Sum(Volume),
		@untipriceret = Sum(TotalPrice) / Sum(Volume), 
		@untipricewh = Round(((Sum(TotalPrice) / Sum(Volume))/1.24),5), 
		@vat = Sum(VatAmount),
		@disc = Sum(DiscountAmount),
		@discRet = Sum(DiscountAmountRetail),
		@discWHole = Sum(DiscountAmountWhole)
	from InvoiceLine Where InvoiceId = @InvoiceId

	select top 1 @invlineid = InvoiceLineId from InvoiceLine where InvoiceId = @InvoiceId;
	update InvoiceLineRelation set ChildRelationId = @invlineid where ChildRelationId in 
	(
		select InvoiceLineId from InvoiceLine where InvoiceId = @InvoiceId
	);
	delete InvoiceLine where InvoiceId = @InvoiceId and InvoiceLineId <> @invlineid;
	update InvoiceLine set 
		Volume = @volume,
		VolumeNormalized = @volumenorm,
		UnitPrice = @untiprice,
		UnitPriceRetail = @untipriceret,
		UnitPriceWhole = @untipricewh,
		TotalPrice = @totalprice,
		VatAmount = @vat,
		Temperature = @temp,
		FuelDensity = @fd,
		SaleTransactionId = null,
		DiscountAmount = @disc,
		DiscountAmountRetail = @discRet,
		DiscountAmountWhole = @discWhole,
		DiscountPercentage = Round(100 * (@discRet / (@untiprice * @volume)), 5)
	where InvoiceLineId = @invlineid
end
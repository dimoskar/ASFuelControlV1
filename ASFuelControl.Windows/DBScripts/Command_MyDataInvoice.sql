SELECT	Invoice.TransactionDate, 
		Invoice.Number, 
		Invoice.Series, 
		Trader.Name, 
		InvoiceType.Description, 
		MyDataInvoice.Mark, 
		MyDataInvoice.InvoiceId, 
		MyDataInvoice.DateTimeSent,
		MyDataInvoice.Status, 
		MyDataInvoice.CanceledByMark, 
		MyDataInvoice.CancelationMark, 
		MyDataInvoice.MyDataInvoiceId, 
		MyDataInvoice.Uid, 
		Invoice.VehiclePlateNumber,
		MyDataInvoice.Errors 
FROM	MyDataInvoice INNER JOIN
			Invoice ON MyDataInvoice.InvoiceId = Invoice.InvoiceId INNER JOIN
			InvoiceType ON Invoice.InvoiceTypeId = InvoiceType.InvoiceTypeId LEFT OUTER JOIN
			Trader ON Invoice.TraderId = Trader.TraderId
Where	DateDiff(day, '[DateFrom]', MyDataInvoice.DateTimeSent) >= 0 AND
		DateDiff(day, MyDataInvoice.DateTimeSent, '[DateTo]') >= 0 AND
		(
			InvoiceType.Description LIKE '%[Filter]%' OR
			Trader.Name LIKE '%[Filter]%' OR
			Invoice.VehiclePlateNumber LIKE '%[Filter]%'
		)
Order By MyDataInvoice.DateTimeSent
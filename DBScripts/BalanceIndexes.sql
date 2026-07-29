CREATE INDEX IX_TankFillings_TankId_TransactionTimeEnd
ON TankFilling (TankId, TransactionTimeEnd);

CREATE INDEX IX_Invoices_TransactionDate_InvoiceTypeId
ON Invoice (TransactionDate, InvoiceTypeId);

CREATE INDEX IX_InvoiceLines_FuelTypeId
ON InvoiceLine (FuelTypeId);

CREATE INDEX IX_InvoiceLines_TankFillingId
ON InvoiceLine (TankFillingId);

CREATE INDEX IX_InvoiceLines_InvoiceId
ON InvoiceLine (InvoiceId);

CREATE INDEX IX_InvoiceTypes_IncludeInBalance_DeliveryType
ON InvoiceType (IncludeInBalance, DeliveryType);

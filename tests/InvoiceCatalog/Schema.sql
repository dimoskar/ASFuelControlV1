-- Synthetic tables only, in a private disposable database.
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
CREATE TABLE dbo.InvoiceType (InvoiceTypeId uniqueidentifier NOT NULL PRIMARY KEY, Abbreviation nvarchar(10), IsLaserPrint bit, TransactionSign int);
CREATE TABLE dbo.Trader (TraderId uniqueidentifier NOT NULL PRIMARY KEY, Name nvarchar(100), VatExemption bit, TaxRegistrationNumber nvarchar(40));
CREATE TABLE dbo.Vehicle (VehicleId uniqueidentifier NOT NULL PRIMARY KEY, PlateNumber nvarchar(20));
CREATE TABLE dbo.FuelType (FuelTypeId uniqueidentifier NOT NULL PRIMARY KEY, Name nvarchar(100));
CREATE TABLE dbo.ApplicationUser (ApplicationUserId uniqueidentifier NOT NULL PRIMARY KEY, UserLevel int, UserName nvarchar(100), Password nvarchar(100), PasswordEncrypted nvarchar(200));
CREATE TABLE dbo.Invoice
(
 InvoiceId uniqueidentifier NOT NULL PRIMARY KEY, InvoiceTypeId uniqueidentifier NOT NULL, TransactionDate datetime NOT NULL,
 DiscountAmount decimal(18,2), NettoAmount decimal(18,2), Number int NOT NULL, PaymentType int, TotalAmount decimal(18,2),
 TraderId uniqueidentifier, VehicleId uniqueidentifier, VatAmount decimal(18,2), IsPrinted bit, SupplyNumber nvarchar(100),
 VehicleOdometer int, DeliveryAddress nvarchar(150), Series nvarchar(10)
);
CREATE TABLE dbo.InvoiceLine (InvoiceLineId uniqueidentifier NOT NULL PRIMARY KEY, InvoiceId uniqueidentifier, FuelTypeId uniqueidentifier, Volume decimal(18,3));
CREATE TABLE dbo.InvoiceRelation (ParentInvoiceId uniqueidentifier, ChildInvoiceId uniqueidentifier, RelationType int);
CREATE TABLE dbo.FinTransaction (InvoiceId uniqueidentifier, DebitAmount decimal(18,2), CreditAmount decimal(18,2));
-- Simulate existing indexes under different names: the migration must reuse these.
CREATE INDEX Existing_Date ON dbo.Invoice(TransactionDate) INCLUDE(InvoiceId, InvoiceTypeId);
CREATE INDEX Existing_Line ON dbo.InvoiceLine(InvoiceId) INCLUDE(FuelTypeId, Volume);

DECLARE @type uniqueidentifier, @credit uniqueidentifier, @other uniqueidentifier, @trader uniqueidentifier, @vehicle uniqueidentifier,
        @fuel uniqueidentifier, @fuel2 uniqueidentifier, @id uniqueidentifier, @previous uniqueidentifier, @n int;
SET @type = '00000000-0000-0000-0000-000000000001';
SET @credit = '00000000-0000-0000-0000-000000000002';
SET @other = '00000000-0000-0000-0000-000000000003';
INSERT dbo.InvoiceType VALUES (@type, N'INV', 1, 1), (@credit, N'CR', 0, -1), (@other, N'OTHER', NULL, NULL);
INSERT dbo.ApplicationUser(ApplicationUserId, UserLevel) VALUES ('00000000-0000-0000-0000-000000000010',0), ('00000000-0000-0000-0000-000000000011',1);
SET @vehicle = NEWID(); INSERT dbo.Vehicle VALUES (@vehicle, N'AB-1234');
SET @fuel = NEWID(); SET @fuel2 = NEWID();
INSERT dbo.FuelType VALUES (@fuel, N'Diesel & <fuel>'), (@fuel2, N'Αμόλυβδη');
SET @n = 0;
WHILE @n < 2405
BEGIN
 SET @id = NEWID(); SET @trader = NEWID();
 INSERT dbo.Trader VALUES (@trader, CASE WHEN @n % 17 = 0 THEN NULL ELSE N'ΠΕΛΑΤΗΣ ' + CAST(@n AS nvarchar(12)) END,
   CASE WHEN @n % 5 = 0 THEN NULL ELSE 0 END, CASE WHEN @n % 7 = 0 THEN NULL ELSE N'VAT-' + CAST(@n AS nvarchar(12)) END);
 INSERT dbo.Invoice VALUES (@id, CASE WHEN @n % 3 = 0 THEN @credit WHEN @n % 3 = 1 THEN @type ELSE @other END,
   CASE WHEN @n = 0 THEN '20261017 23:59:59.997' WHEN @n = 1 THEN '20261019' WHEN @n = 2 THEN '20261018'
        WHEN @n = 3 THEN '20261018 23:59:59.997' ELSE DATEADD(second, @n, CAST('20261018' AS datetime)) END,
   CASE WHEN @n % 7 = 0 THEN NULL ELSE 1.23 END, @n * 1.11, CASE WHEN @n = 6 THEN -12 ELSE @n END,
   CASE WHEN @n % 11 = 0 THEN NULL ELSE @n % 6 END, CASE WHEN @n % 13 = 0 THEN NULL ELSE @n * 1.37 END,
   CASE WHEN @n % 11 = 0 THEN NULL WHEN @n % 19 = 0 THEN NEWID() ELSE @trader END,
   CASE WHEN @n % 4 = 0 THEN NULL ELSE @vehicle END, @n * 0.24, CASE WHEN @n % 5 = 0 THEN NULL ELSE 1 END,
   CASE WHEN @n % 8 = 0 THEN NULL ELSE N'SUP-' + CAST(@n AS nvarchar(12)) END, NULL, NULL, N'A');
 IF @n % 9 <> 0
 BEGIN
   INSERT dbo.InvoiceLine VALUES (NEWID(), @id, @fuel, @n * 0.123), (NEWID(), @id, @fuel, NULL), (NEWID(), @id, @fuel2, 1.234);
 END;
 IF @n % 4 = 0
   INSERT dbo.FinTransaction VALUES (@id, 2.34, NULL), (@id, NULL, 5.67), (@id, -1.11, 0);
 IF @previous IS NOT NULL AND @n % 10 = 0
   INSERT dbo.InvoiceRelation VALUES (@id, @previous, 0), (@id, @previous, 0);
 IF @previous IS NOT NULL AND @n % 13 = 0
   INSERT dbo.InvoiceRelation VALUES (@id, @previous, 1), (@id, @previous, 1), (@id, @previous, 2);
 SET @previous = @id;
 SET @n = @n + 1;
END;
-- Overflow-length fuel list, duplicate names, NULL names, and dangling fuel IDs.
SET @n = 0;
WHILE @n < 25
BEGIN
 SET @fuel = NEWID();
 INSERT dbo.FuelType VALUES (@fuel, REPLICATE(N'Ω', 75) + CAST(@n AS nvarchar(10)));
 INSERT dbo.InvoiceLine VALUES (NEWID(), @id, @fuel, 1);
 SET @n = @n + 1;
END;
INSERT dbo.InvoiceLine VALUES (NEWID(), @id, NEWID(), NULL);
SET @fuel = NEWID(); INSERT dbo.FuelType VALUES (@fuel, NULL);
INSERT dbo.InvoiceLine VALUES (NEWID(), @id, @fuel, NULL);
COMMIT TRANSACTION;

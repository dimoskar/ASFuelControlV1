/*
    Correct the invalid legacy invoice type shown for:
        Trader:  ΣΟΥΛΕΙΜΑΝ ΟΜΕΡ ΣΟΥΛΕΙΜΑΝ
        Date:    2026-11-12
        Amount:  100.00

    The script replaces the retired invoice type
        89499fe5-f0fd-4de6-b239-c74df1b3fe41
        Τιμολόγιο Δελτίο Αποστολής (Πωλήσεων)

    with its current replacement
        c7d3d685-e4cf-4a98-a757-d4c12da411d3
        Τιμολόγιο – Δελτίο Αποστολής Πωλήσεων

    Before running:
      1. Close ASFuelControl on the target PC.
      2. Take a database backup.
      3. In SQL Server Management Studio, select the site's ASFuelControlDB_*
         database in the database selector. Do not run this against master.

    This script is intentionally strict. It rolls back unless it finds exactly
    one trader and exactly one matching pending invoice. It does not alter an
    issued/printed invoice.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @TraderName nvarchar(100);
DECLARE @InvoiceDate datetime;
DECLARE @InvoiceAmount decimal(18, 2);
DECLARE @LegacyInvoiceTypeId uniqueidentifier;
DECLARE @ReplacementInvoiceTypeId uniqueidentifier;
DECLARE @TraderId uniqueidentifier;
DECLARE @TraderCount int;
DECLARE @LegacyPendingInvoiceCount int;
DECLARE @ReplacementPendingInvoiceCount int;
DECLARE @CurrentTraderInvoiceTypeId uniqueidentifier;
DECLARE @ReplacementInvoiceFormId uniqueidentifier;

SET @TraderName = N'ΣΟΥΛΕΙΜΑΝ ΟΜΕΡ ΣΟΥΛΕΙΜΑΝ';
SET @InvoiceDate = CONVERT(datetime, '20261112', 112);
SET @InvoiceAmount = 100.00;
SET @LegacyInvoiceTypeId = '89499fe5-f0fd-4de6-b239-c74df1b3fe41';
SET @ReplacementInvoiceTypeId = 'c7d3d685-e4cf-4a98-a757-d4c12da411d3';

BEGIN TRY
    BEGIN TRANSACTION;

    IF DB_NAME() = N'master'
    BEGIN
        RAISERROR(N'Select the site ASFuelControl database before running this script.', 16, 1);
    END;

    IF OBJECT_ID(N'dbo.Trader', N'U') IS NULL OR
       OBJECT_ID(N'dbo.Invoice', N'U') IS NULL OR
       OBJECT_ID(N'dbo.InvoiceType', N'U') IS NULL
    BEGIN
        RAISERROR(N'The selected database is not an ASFuelControl database.', 16, 1);
    END;

    IF OBJECT_ID(N'dbo.FiscalInvoiceTypePolicy', N'U') IS NULL
    BEGIN
        RAISERROR(N'The fiscal invoice-type policy table is missing. Upgrade the database before applying this correction.', 16, 1);
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.InvoiceType
        WHERE InvoiceTypeId = @LegacyInvoiceTypeId
    )
    BEGIN
        RAISERROR(N'The expected legacy invoice type does not exist in the selected database.', 16, 1);
    END;

    SELECT @ReplacementInvoiceFormId = InvoiceFormId
    FROM dbo.InvoiceType
    WHERE InvoiceTypeId = @ReplacementInvoiceTypeId;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.InvoiceType
        WHERE InvoiceTypeId = @ReplacementInvoiceTypeId
          AND OfficialEnumerator = 222
          AND Printable = 1
    )
    BEGIN
        RAISERROR(N'The expected replacement invoice type is missing or has unexpected settings.', 16, 1);
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.FiscalInvoiceTypePolicy
        WHERE InvoiceTypeId = @ReplacementInvoiceTypeId
    )
    BEGIN
        RAISERROR(N'The replacement invoice type has no saved transmission/signing policy. Configure and save it in ASFuelControl first.', 16, 1);
    END;

    SELECT @TraderCount = COUNT(*)
    FROM dbo.Trader
    WHERE LTRIM(RTRIM(Name)) = @TraderName;

    IF @TraderCount <> 1
    BEGIN
        RAISERROR(N'Expected exactly one matching trader, but found %d. No changes were made.', 16, 1, @TraderCount);
    END;

    SELECT
        @TraderId = TraderId,
        @CurrentTraderInvoiceTypeId = InvoiceTypeId
    FROM dbo.Trader WITH (UPDLOCK, HOLDLOCK)
    WHERE LTRIM(RTRIM(Name)) = @TraderName;

    IF @CurrentTraderInvoiceTypeId IS NULL OR
       @CurrentTraderInvoiceTypeId NOT IN (@LegacyInvoiceTypeId, @ReplacementInvoiceTypeId)
    BEGIN
        RAISERROR(N'The trader default is neither the expected legacy type nor the replacement type. No changes were made.', 16, 1);
    END;

    SELECT @LegacyPendingInvoiceCount = COUNT(*)
    FROM dbo.Invoice WITH (UPDLOCK, HOLDLOCK)
    WHERE TraderId = @TraderId
      AND InvoiceTypeId = @LegacyInvoiceTypeId
      AND Number = 0
      AND ISNULL(IsPrinted, 0) = 0
      AND TransactionDate >= @InvoiceDate
      AND TransactionDate < DATEADD(day, 1, @InvoiceDate)
      AND ABS(ISNULL(TotalAmount, 0) - @InvoiceAmount) < 0.01;

    SELECT @ReplacementPendingInvoiceCount = COUNT(*)
    FROM dbo.Invoice WITH (UPDLOCK, HOLDLOCK)
    WHERE TraderId = @TraderId
      AND InvoiceTypeId = @ReplacementInvoiceTypeId
      AND Number = 0
      AND ISNULL(IsPrinted, 0) = 0
      AND TransactionDate >= @InvoiceDate
      AND TransactionDate < DATEADD(day, 1, @InvoiceDate)
      AND ABS(ISNULL(TotalAmount, 0) - @InvoiceAmount) < 0.01;

    IF @LegacyPendingInvoiceCount = 0 AND
       @ReplacementPendingInvoiceCount = 1 AND
       @CurrentTraderInvoiceTypeId = @ReplacementInvoiceTypeId
    BEGIN
        COMMIT TRANSACTION;

        SELECT
            N'Already corrected; no changes were necessary.' AS Result,
            DB_NAME() AS DatabaseName,
            @TraderId AS TraderId,
            @ReplacementPendingInvoiceCount AS MatchingPendingInvoices;

        RETURN;
    END;

    IF @LegacyPendingInvoiceCount <> 1
    BEGIN
        RAISERROR(N'Expected exactly one matching pending legacy invoice, but found %d. No changes were made.', 16, 1, @LegacyPendingInvoiceCount);
    END;

    UPDATE dbo.Invoice
    SET InvoiceTypeId = @ReplacementInvoiceTypeId,
        InvoiceFormId = @ReplacementInvoiceFormId
    WHERE TraderId = @TraderId
      AND InvoiceTypeId = @LegacyInvoiceTypeId
      AND Number = 0
      AND ISNULL(IsPrinted, 0) = 0
      AND TransactionDate >= @InvoiceDate
      AND TransactionDate < DATEADD(day, 1, @InvoiceDate)
      AND ABS(ISNULL(TotalAmount, 0) - @InvoiceAmount) < 0.01;

    IF @@ROWCOUNT <> 1
    BEGIN
        RAISERROR(N'The pending invoice update did not affect exactly one row. No changes were made.', 16, 1);
    END;

    UPDATE dbo.Trader
    SET InvoiceTypeId = @ReplacementInvoiceTypeId
    WHERE TraderId = @TraderId
      AND InvoiceTypeId IN (@LegacyInvoiceTypeId, @ReplacementInvoiceTypeId);

    IF @@ROWCOUNT <> 1
    BEGIN
        RAISERROR(N'The trader update did not affect exactly one row. No changes were made.', 16, 1);
    END;

    COMMIT TRANSACTION;

    SELECT
        N'Correction applied successfully.' AS Result,
        DB_NAME() AS DatabaseName,
        i.InvoiceId,
        i.TransactionDate,
        i.Number,
        i.TotalAmount,
        t.TraderId,
        t.Name AS TraderName,
        oldType.Description AS PreviousInvoiceType,
        newType.Description AS CurrentInvoiceType,
        p.TransmissionMode,
        p.SignWithFiscalDevice
    FROM dbo.Invoice i
    INNER JOIN dbo.Trader t
        ON t.TraderId = i.TraderId
    INNER JOIN dbo.InvoiceType oldType
        ON oldType.InvoiceTypeId = @LegacyInvoiceTypeId
    INNER JOIN dbo.InvoiceType newType
        ON newType.InvoiceTypeId = i.InvoiceTypeId
    INNER JOIN dbo.FiscalInvoiceTypePolicy p
        ON p.InvoiceTypeId = i.InvoiceTypeId
    WHERE i.TraderId = @TraderId
      AND i.InvoiceTypeId = @ReplacementInvoiceTypeId
      AND i.Number = 0
      AND ISNULL(i.IsPrinted, 0) = 0
      AND i.TransactionDate >= @InvoiceDate
      AND i.TransactionDate < DATEADD(day, 1, @InvoiceDate)
      AND ABS(ISNULL(i.TotalAmount, 0) - @InvoiceAmount) < 0.01;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    DECLARE @ErrorMessage nvarchar(4000);
    DECLARE @ErrorSeverity int;
    DECLARE @ErrorState int;

    SELECT
        @ErrorMessage = ERROR_MESSAGE(),
        @ErrorSeverity = ERROR_SEVERITY(),
        @ErrorState = ERROR_STATE();

    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;

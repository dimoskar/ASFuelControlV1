/*
    Returns myDiesel coupon / myDATA MARK pairs for invoices transmitted
    directly to AADE myDATA, without an invoice provider.

    Compatible with SQL Server 2008 R2. FiscalInvoiceWorkflow.Payload is
    UTF-8 JSON stored as varbinary(max), so the route check intentionally
    uses an ASCII LIKE comparison instead of JSON_VALUE.

    Parameters:
      @DateFrom / @DateToExclusive filter Invoice.TransactionDate.
      Leave either value NULL to keep that side of the range open.
      Set @OnlyWithMyDieselCoupon to 0 to also list direct-myDATA invoices
      which do not have a myDiesel coupon.
*/

SET NOCOUNT ON;

DECLARE @DateFrom datetime;
DECLARE @DateToExclusive datetime;
DECLARE @OnlyWithMyDieselCoupon bit;

SET @DateFrom = NULL;
SET @DateToExclusive = NULL;
SET @OnlyWithMyDieselCoupon = 1;

IF OBJECT_ID(N'dbo.MyDieselCouponCase', N'U') IS NULL
BEGIN
    RAISERROR('Table dbo.MyDieselCouponCase does not exist.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'dbo.FiscalInvoiceWorkflow', N'U') IS NULL
BEGIN
    RAISERROR('Table dbo.FiscalInvoiceWorkflow does not exist.', 16, 1);
    RETURN;
END;

;WITH LatestMyDataInvoice AS
(
    SELECT
        myDataInvoice.InvoiceId,
        myDataInvoice.MyDataInvoiceId,
        myDataInvoice.Mark,
        myDataInvoice.Uid,
        myDataInvoice.Status,
        myDataInvoice.DateTimeSent,
        myDataInvoice.ProviderUrl,
        ROW_NUMBER() OVER
        (
            PARTITION BY myDataInvoice.InvoiceId
            ORDER BY
                CASE WHEN ISNULL(myDataInvoice.Mark, 0) > 0 THEN 0 ELSE 1 END,
                myDataInvoice.DateTimeSent DESC,
                myDataInvoice.MyDataInvoiceId DESC
        ) AS RowNumber
    FROM dbo.MyDataInvoice AS myDataInvoice
),
LatestMyDieselCoupon AS
(
    SELECT
        coupon.InvoiceId,
        coupon.CouponCaseId,
        coupon.CouponCode,
        coupon.InvoiceMark,
        coupon.LocalStage,
        coupon.RemoteStatus,
        coupon.CreatedUtc,
        coupon.UpdatedUtc,
        ROW_NUMBER() OVER
        (
            PARTITION BY coupon.InvoiceId
            ORDER BY coupon.CreatedUtc DESC, coupon.CouponCaseId DESC
        ) AS RowNumber
    FROM dbo.MyDieselCouponCase AS coupon
)
SELECT
    invoice.InvoiceId,
    invoice.TransactionDate,
    invoice.Number,
    invoice.Series,
    invoiceType.Description AS InvoiceType,
    trader.Name AS TraderName,
    trader.TaxRegistrationNumber AS TraderVat,
    coupon.CouponCode AS MyDieselCoupon,
    COALESCE
    (
        NULLIF(coupon.InvoiceMark, ''),
        CONVERT(varchar(20), myDataInvoice.Mark)
    ) AS MyDataMark,
    myDataInvoice.Mark AS MyDataProjectionMark,
    coupon.InvoiceMark AS MyDieselRecordedMark,
    CASE
        WHEN coupon.InvoiceMark IS NULL OR myDataInvoice.Mark IS NULL THEN NULL
        WHEN coupon.InvoiceMark = CONVERT(varchar(20), myDataInvoice.Mark) THEN 1
        ELSE 0
    END AS MarksMatch,
    coupon.LocalStage AS MyDieselLocalStage,
    coupon.RemoteStatus AS MyDieselRemoteStatus,
    myDataInvoice.Uid AS MyDataUid,
    myDataInvoice.DateTimeSent AS MyDataDateTimeSent,
    myDataInvoice.Status AS MyDataStatus,
    CASE
        WHEN workflow.InvoiceId IS NOT NULL THEN 'Workflow: direct myDATA'
        ELSE 'Legacy: inferred direct myDATA'
    END AS RouteEvidence
FROM dbo.Invoice AS invoice
INNER JOIN dbo.InvoiceType AS invoiceType
    ON invoiceType.InvoiceTypeId = invoice.InvoiceTypeId
LEFT JOIN dbo.Trader AS trader
    ON trader.TraderId = invoice.TraderId
LEFT JOIN dbo.FiscalInvoiceWorkflow AS workflow
    ON workflow.InvoiceId = invoice.InvoiceId
LEFT JOIN LatestMyDataInvoice AS myDataInvoice
    ON myDataInvoice.InvoiceId = invoice.InvoiceId
    AND myDataInvoice.RowNumber = 1
LEFT JOIN LatestMyDieselCoupon AS coupon
    ON coupon.InvoiceId = invoice.InvoiceId
    AND coupon.RowNumber = 1
WHERE
    (@DateFrom IS NULL OR invoice.TransactionDate >= @DateFrom)
    AND (@DateToExclusive IS NULL OR invoice.TransactionDate < @DateToExclusive)
    AND (@OnlyWithMyDieselCoupon = 0 OR coupon.CouponCaseId IS NOT NULL)
    AND
    (
        ISNULL(myDataInvoice.Mark, 0) > 0
        OR NULLIF(coupon.InvoiceMark, '') IS NOT NULL
    )
    AND
    (
        (
            workflow.InvoiceId IS NOT NULL
            AND CONVERT(varchar(max), workflow.Payload)
                LIKE '%"Provider":{"ProviderKey":"myDATA"%'
        )
        OR
        (
            workflow.InvoiceId IS NULL
            AND NULLIF(LTRIM(RTRIM(myDataInvoice.ProviderUrl)), '') IS NULL
        )
    )
ORDER BY
    invoice.TransactionDate,
    invoice.Number,
    invoice.InvoiceId;

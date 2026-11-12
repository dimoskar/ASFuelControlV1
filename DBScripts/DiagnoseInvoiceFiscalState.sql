/*
    ASFuelControl invoice transmission/closure diagnostic
    -----------------------------------------------------
    SQL Server 2008 R2 compatible. This script does not modify production data.
    It creates only a session-local temporary table for diagnostic findings.

    Instructions:
      1. Select the site's ASFuelControlDB_* database in SQL Server Management Studio.
      2. Paste the InvoiceId into @InvoiceId below.
      3. Execute the complete script.
      4. Save all result grids (Results -> Save Results As) or copy them with headers.

    The output contains invoice/customer/provider diagnostic evidence. Transfer it
    through an appropriate secure channel.
*/

SET NOCOUNT ON;
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

DECLARE @InvoiceId uniqueidentifier;

-- REQUIRED INPUT:
SET @InvoiceId = NULL; -- Example: '11111111-2222-3333-4444-555555555555'

IF @InvoiceId IS NULL
BEGIN
    RAISERROR(N'Set @InvoiceId before executing the script.', 16, 1);
    RETURN;
END;

IF DB_NAME() = N'master'
BEGIN
    RAISERROR(N'Select the site ASFuelControl database before executing the script.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'dbo.Invoice', N'U') IS NULL OR
   OBJECT_ID(N'dbo.InvoiceType', N'U') IS NULL OR
   OBJECT_ID(N'dbo.InvoiceLine', N'U') IS NULL
BEGIN
    RAISERROR(N'The selected database is not an ASFuelControl database.', 16, 1);
    RETURN;
END;

CREATE TABLE #DiagnosticFindings
(
    Severity int NOT NULL,
    FindingCode varchar(64) NOT NULL,
    Finding nvarchar(2000) NOT NULL,
    Evidence nvarchar(max) NULL
);

SELECT
    N'Environment' AS Section,
    DB_NAME() AS DatabaseName,
    CONVERT(nvarchar(128), SERVERPROPERTY(N'ServerName')) AS ServerName,
    CONVERT(nvarchar(128), SERVERPROPERTY(N'ProductVersion')) AS SqlServerVersion,
    CONVERT(nvarchar(128), SERVERPROPERTY(N'Edition')) AS SqlServerEdition,
    GETDATE() AS DatabaseLocalTime,
    GETUTCDATE() AS DatabaseUtcTime,
    @InvoiceId AS RequestedInvoiceId,
    CASE WHEN EXISTS
    (
        SELECT 1
        FROM dbo.Invoice
        WHERE InvoiceId = @InvoiceId
    ) THEN 1 ELSE 0 END AS InvoiceExists;

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Invoice
    WHERE InvoiceId = @InvoiceId
)
BEGIN
    INSERT #DiagnosticFindings
    (
        Severity,
        FindingCode,
        Finding,
        Evidence
    )
    VALUES
    (
        3,
        'INVOICE_NOT_FOUND',
        N'The InvoiceId does not exist in the selected database.',
        CONVERT(nvarchar(36), @InvoiceId)
    );

    SELECT
        N'Findings' AS Section,
        Severity,
        FindingCode,
        Finding,
        Evidence
    FROM #DiagnosticFindings
    ORDER BY Severity DESC, FindingCode;

    DROP TABLE #DiagnosticFindings;
    RETURN;
END;

INSERT #DiagnosticFindings
(
    Severity,
    FindingCode,
    Finding,
    Evidence
)
SELECT
    3,
    'INVOICE_TYPE_MISSING',
    N'The invoice references an invoice type that does not exist.',
    CONVERT(nvarchar(36), i.InvoiceTypeId)
FROM dbo.Invoice i
LEFT JOIN dbo.InvoiceType invoiceType
    ON invoiceType.InvoiceTypeId = i.InvoiceTypeId
WHERE i.InvoiceId = @InvoiceId
  AND invoiceType.InvoiceTypeId IS NULL;

INSERT #DiagnosticFindings
(
    Severity,
    FindingCode,
    Finding,
    Evidence
)
SELECT
    3,
    'INVOICE_TYPE_INVALIDATED',
    N'The invoice uses an inactive/invalidated invoice type.',
    invoiceType.Description
FROM dbo.Invoice i
INNER JOIN dbo.InvoiceType invoiceType
    ON invoiceType.InvoiceTypeId = i.InvoiceTypeId
WHERE i.InvoiceId = @InvoiceId
  AND invoiceType.Invalidated = 1;

INSERT #DiagnosticFindings
(
    Severity,
    FindingCode,
    Finding,
    Evidence
)
SELECT
    3,
    'NO_INVOICE_LINES',
    N'The invoice has no lines and cannot be issued.',
    NULL
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.InvoiceLine
    WHERE InvoiceId = @InvoiceId
);

INSERT #DiagnosticFindings
(
    Severity,
    FindingCode,
    Finding,
    Evidence
)
SELECT
    2,
    'NOT_NUMBERED',
    N'The invoice has not received a fiscal number.',
    N'Number=' + CONVERT(nvarchar(20), i.Number)
FROM dbo.Invoice i
WHERE i.InvoiceId = @InvoiceId
  AND i.Number <= 0;

INSERT #DiagnosticFindings
(
    Severity,
    FindingCode,
    Finding,
    Evidence
)
SELECT
    2,
    'NOT_CLOSED_OR_PRINTED',
    N'The invoice is not marked as printed/closed locally.',
    N'IsPrinted=' + CONVERT(nvarchar(5), ISNULL(i.IsPrinted, 0))
FROM dbo.Invoice i
WHERE i.InvoiceId = @InvoiceId
  AND ISNULL(i.IsPrinted, 0) = 0;

INSERT #DiagnosticFindings
(
    Severity,
    FindingCode,
    Finding,
    Evidence
)
SELECT
    1,
    'NO_SIGNATURE_OR_QR',
    N'The invoice has neither a stored fiscal signature nor QR data.',
    NULL
FROM dbo.Invoice i
WHERE i.InvoiceId = @InvoiceId
  AND NULLIF(i.InvoiceSignature, N'') IS NULL
  AND NULLIF(i.QRCodeData, N'') IS NULL;

SELECT
    N'Invoice' AS Section,
    i.*
FROM dbo.Invoice i
WHERE i.InvoiceId = @InvoiceId;

SELECT
    N'InvoiceType' AS Section,
    invoiceType.*
FROM dbo.Invoice i
INNER JOIN dbo.InvoiceType invoiceType
    ON invoiceType.InvoiceTypeId = i.InvoiceTypeId
WHERE i.InvoiceId = @InvoiceId;

IF OBJECT_ID(N'dbo.Trader', N'U') IS NOT NULL
BEGIN
    EXEC sys.sp_executesql N'
        SELECT N''Trader'' AS Section, trader.*
        FROM dbo.Invoice invoice
        INNER JOIN dbo.Trader trader
            ON trader.TraderId = invoice.TraderId
        WHERE invoice.InvoiceId = @InvoiceId;',
        N'@InvoiceId uniqueidentifier',
        @InvoiceId;
END;

IF OBJECT_ID(N'dbo.Vehicle', N'U') IS NOT NULL
BEGIN
    EXEC sys.sp_executesql N'
        SELECT N''Vehicle'' AS Section, vehicle.*
        FROM dbo.Invoice invoice
        INNER JOIN dbo.Vehicle vehicle
            ON vehicle.VehicleId = invoice.VehicleId
        WHERE invoice.InvoiceId = @InvoiceId;',
        N'@InvoiceId uniqueidentifier',
        @InvoiceId;
END;

SELECT
    N'InvoiceLines' AS Section,
    invoiceLine.*
FROM dbo.InvoiceLine invoiceLine
WHERE invoiceLine.InvoiceId = @InvoiceId
ORDER BY invoiceLine.InvoiceLineId;

IF OBJECT_ID(N'dbo.SalesTransaction', N'U') IS NOT NULL
BEGIN
    EXEC sys.sp_executesql N'
        SELECT N''SalesTransactions'' AS Section, sale.*
        FROM dbo.SalesTransaction sale
        WHERE EXISTS
        (
            SELECT 1
            FROM dbo.InvoiceLine invoiceLine
            WHERE invoiceLine.InvoiceId = @InvoiceId
              AND invoiceLine.SaleTransactionId = sale.SalesTransactionId
        )
        ORDER BY sale.TransactionTimeStamp, sale.SalesTransactionId;',
        N'@InvoiceId uniqueidentifier',
        @InvoiceId;
END;

IF OBJECT_ID(N'dbo.InvoiceRelation', N'U') IS NOT NULL
BEGIN
    EXEC sys.sp_executesql N'
        SELECT
            N''InvoiceRelations'' AS Section,
            relation.*,
            parentInvoice.Number AS ParentNumber,
            parentInvoice.Series AS ParentSeries,
            parentInvoice.TransactionDate AS ParentDate,
            childInvoice.Number AS ChildNumber,
            childInvoice.Series AS ChildSeries,
            childInvoice.TransactionDate AS ChildDate
        FROM dbo.InvoiceRelation relation
        INNER JOIN dbo.Invoice parentInvoice
            ON parentInvoice.InvoiceId = relation.ParentInvoiceId
        INNER JOIN dbo.Invoice childInvoice
            ON childInvoice.InvoiceId = relation.ChildInvoiceId
        WHERE relation.ParentInvoiceId = @InvoiceId
           OR relation.ChildInvoiceId = @InvoiceId
        ORDER BY relation.RelationType, relation.InvoiceRelationId;',
        N'@InvoiceId uniqueidentifier',
        @InvoiceId;
END;

IF OBJECT_ID(N'dbo.FinTransaction', N'U') IS NOT NULL
BEGIN
    EXEC sys.sp_executesql N'
        SELECT N''FinancialTransactions'' AS Section, transactionRow.*
        FROM dbo.FinTransaction transactionRow
        WHERE transactionRow.InvoiceId = @InvoiceId;',
        N'@InvoiceId uniqueidentifier',
        @InvoiceId;
END;

IF OBJECT_ID(N'dbo.FiscalConfiguration', N'U') IS NULL
BEGIN
    INSERT #DiagnosticFindings
    (
        Severity,
        FindingCode,
        Finding,
        Evidence
    )
    VALUES
    (
        3,
        'FISCAL_CONFIGURATION_SCHEMA_MISSING',
        N'The shared fiscal configuration tables are not installed.',
        NULL
    );
END;
ELSE
BEGIN
    EXEC sys.sp_executesql N'
        SELECT TOP (1)
            N''LatestFiscalConfiguration'' AS Section,
            Revision,
            CreatedUtc,
            CreatedBy,
            DATALENGTH(ConfigurationJson) AS ConfigurationBytes,
            ConfigurationJson
        FROM dbo.FiscalConfiguration
        ORDER BY Revision DESC;';

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.FiscalConfiguration
    )
    BEGIN
        INSERT #DiagnosticFindings
        (
            Severity,
            FindingCode,
            Finding,
            Evidence
        )
        VALUES
        (
            3,
            'FISCAL_CONFIGURATION_MISSING',
            N'No shared fiscal configuration has been saved.',
            NULL
        );
    END;
END;

IF OBJECT_ID(N'dbo.FiscalInvoiceTypePolicy', N'U') IS NULL
BEGIN
    INSERT #DiagnosticFindings
    (
        Severity,
        FindingCode,
        Finding,
        Evidence
    )
    VALUES
    (
        3,
        'INVOICE_TYPE_POLICY_SCHEMA_MISSING',
        N'The fiscal invoice-type policy table is not installed.',
        NULL
    );
END;
ELSE
BEGIN
    EXEC sys.sp_executesql N'
        SELECT
            N''InvoiceTypePolicy'' AS Section,
            policy.*,
            CASE policy.TransmissionMode
                WHEN 0 THEN N''None / local''
                WHEN 1 THEN N''Provider''
                WHEN 2 THEN N''Direct myDATA''
                ELSE N''Unknown''
            END AS TransmissionModeDescription
        FROM dbo.Invoice invoice
        LEFT JOIN dbo.FiscalInvoiceTypePolicy policy
            ON policy.InvoiceTypeId = invoice.InvoiceTypeId
        WHERE invoice.InvoiceId = @InvoiceId;',
        N'@InvoiceId uniqueidentifier',
        @InvoiceId;

    EXEC sys.sp_executesql N'
        INSERT #DiagnosticFindings
        (
            Severity,
            FindingCode,
            Finding,
            Evidence
        )
        SELECT
            3,
            ''INVOICE_TYPE_POLICY_MISSING'',
            N''The invoice type has no saved transmission/signing policy.'',
            invoiceType.Description
        FROM dbo.Invoice invoice
        INNER JOIN dbo.InvoiceType invoiceType
            ON invoiceType.InvoiceTypeId = invoice.InvoiceTypeId
        LEFT JOIN dbo.FiscalInvoiceTypePolicy policy
            ON policy.InvoiceTypeId = invoice.InvoiceTypeId
        WHERE invoice.InvoiceId = @InvoiceId
          AND policy.InvoiceTypeId IS NULL;',
        N'@InvoiceId uniqueidentifier',
        @InvoiceId;
END;

IF OBJECT_ID(N'dbo.FiscalInvoiceWorkflow', N'U') IS NULL
BEGIN
    INSERT #DiagnosticFindings
    (
        Severity,
        FindingCode,
        Finding,
        Evidence
    )
    VALUES
    (
        2,
        'WORKFLOW_SCHEMA_MISSING',
        N'The immutable invoice workflow table is not installed.',
        NULL
    );
END;
ELSE
BEGIN
    EXEC sys.sp_executesql N'
        SELECT
            N''InvoiceWorkflow'' AS Section,
            InvoiceId,
            PayloadSha256,
            CreatedUtc,
            FencingToken,
            DATALENGTH(Payload) AS PayloadBytes,
            Payload,
            CONVERT(varchar(max), Payload) AS PayloadText
        FROM dbo.FiscalInvoiceWorkflow
        WHERE InvoiceId = @InvoiceId;',
        N'@InvoiceId uniqueidentifier',
        @InvoiceId;

    EXEC sys.sp_executesql N'
        INSERT #DiagnosticFindings
        (
            Severity,
            FindingCode,
            Finding,
            Evidence
        )
        SELECT
            2,
            ''WORKFLOW_NOT_CREATED'',
            N''Fiscal processing has not created an immutable workflow snapshot.'',
            NULL
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM dbo.FiscalInvoiceWorkflow
            WHERE InvoiceId = @InvoiceId
        );',
        N'@InvoiceId uniqueidentifier',
        @InvoiceId;
END;

IF OBJECT_ID(N'dbo.FiscalDocumentLease', N'U') IS NOT NULL
BEGIN
    EXEC sys.sp_executesql N'
        SELECT N''FiscalDocumentLease'' AS Section, lease.*
        FROM dbo.FiscalDocumentLease lease
        WHERE lease.DocumentKind = 1
          AND lease.DocumentId = @InvoiceId;',
        N'@InvoiceId uniqueidentifier',
        @InvoiceId;
END;

IF OBJECT_ID(N'dbo.FiscalAttempt', N'U') IS NULL OR
   OBJECT_ID(N'dbo.FiscalAttemptRevision', N'U') IS NULL
BEGIN
    INSERT #DiagnosticFindings
    (
        Severity,
        FindingCode,
        Finding,
        Evidence
    )
    VALUES
    (
        2,
        'FISCAL_ATTEMPT_SCHEMA_MISSING',
        N'The fiscal attempt journal is not installed.',
        NULL
    );
END;
ELSE
BEGIN
    EXEC sys.sp_executesql N'
        SELECT
            N''CurrentFiscalAttempts'' AS Section,
            attempt.AttemptSequence,
            attempt.AttemptId,
            attempt.Family,
            attempt.Operation,
            attempt.Scope,
            attempt.PayloadFormat,
            attempt.RequestSha256,
            attempt.CreatedUtc,
            attempt.CurrentRevision,
            attempt.BlocksNewAttempt,
            attempt.OriginalAttemptId,
            revision.State,
            revision.RecordedUtc,
            revision.FencingToken,
            revision.SnapshotJson,
            DATALENGTH(attempt.RequestPayload) AS RequestPayloadBytes,
            attempt.RequestPayload,
            CONVERT(varchar(max), attempt.RequestPayload) AS RequestPayloadText
        FROM dbo.FiscalAttempt attempt
        INNER JOIN dbo.FiscalAttemptRevision revision
            ON revision.AttemptId = attempt.AttemptId
           AND revision.Revision = attempt.CurrentRevision
        WHERE attempt.DocumentKind = 1
          AND attempt.DocumentId = @InvoiceId
        ORDER BY attempt.AttemptSequence;',
        N'@InvoiceId uniqueidentifier',
        @InvoiceId;

    EXEC sys.sp_executesql N'
        SELECT
            N''FiscalAttemptRevisionHistory'' AS Section,
            attempt.AttemptSequence,
            attempt.AttemptId,
            attempt.Family,
            attempt.Operation,
            attempt.Scope,
            attempt.PayloadFormat,
            attempt.RequestSha256,
            attempt.CreatedUtc,
            attempt.BlocksNewAttempt,
            attempt.OriginalAttemptId,
            revision.Revision,
            revision.State,
            revision.RecordedUtc,
            revision.FencingToken,
            revision.SnapshotJson
        FROM dbo.FiscalAttempt attempt
        INNER JOIN dbo.FiscalAttemptRevision revision
            ON revision.AttemptId = attempt.AttemptId
        WHERE attempt.DocumentKind = 1
          AND attempt.DocumentId = @InvoiceId
        ORDER BY attempt.AttemptSequence, revision.Revision;',
        N'@InvoiceId uniqueidentifier',
        @InvoiceId;

    EXEC sys.sp_executesql N'
        INSERT #DiagnosticFindings
        (
            Severity,
            FindingCode,
            Finding,
            Evidence
        )
        SELECT
            2,
            ''FISCAL_PROCESSING_NOT_STARTED'',
            N''No signing, submission, payment, or printing attempt exists.'',
            NULL
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM dbo.FiscalAttempt
            WHERE DocumentKind = 1
              AND DocumentId = @InvoiceId
        );

        INSERT #DiagnosticFindings
        (
            Severity,
            FindingCode,
            Finding,
            Evidence
        )
        SELECT
            CASE
                WHEN revision.State IN (N''Rejected'', N''Declined'', N''Failed'') THEN 3
                ELSE 2
            END,
            ''CURRENT_'' + UPPER(attempt.Family) + ''_STATE'',
            N''The current '' + attempt.Family + N'' attempt state is '' + revision.State + N''.'',
            revision.SnapshotJson
        FROM dbo.FiscalAttempt attempt
        INNER JOIN dbo.FiscalAttemptRevision revision
            ON revision.AttemptId = attempt.AttemptId
           AND revision.Revision = attempt.CurrentRevision
        WHERE attempt.DocumentKind = 1
          AND attempt.DocumentId = @InvoiceId
          AND revision.State NOT IN
          (
              N''Accepted'', N''Signed'', N''Printed'', N''Completed'',
              N''Succeeded'', N''Cancelled'', N''Expired''
          );',
        N'@InvoiceId uniqueidentifier',
        @InvoiceId;
END;

IF OBJECT_ID(N'dbo.FiscalHttpExchange', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.FiscalHttpExchange', N'DocumentKind') IS NOT NULL AND
       COL_LENGTH(N'dbo.FiscalHttpExchange', N'DocumentId') IS NOT NULL
    BEGIN
        EXEC sys.sp_executesql N'
            SELECT N''FiscalHttpExchanges'' AS Section, exchangeRow.*
            FROM dbo.FiscalHttpExchange exchangeRow
            WHERE exchangeRow.InvoiceId = @InvoiceId
               OR
               (
                   exchangeRow.DocumentKind = 1
                   AND exchangeRow.DocumentId = @InvoiceId
               )
            ORDER BY exchangeRow.StartedUtc, exchangeRow.ExchangeId;',
            N'@InvoiceId uniqueidentifier',
            @InvoiceId;
    END;
    ELSE
    BEGIN
        EXEC sys.sp_executesql N'
            SELECT N''FiscalHttpExchanges'' AS Section, exchangeRow.*
            FROM dbo.FiscalHttpExchange exchangeRow
            WHERE exchangeRow.InvoiceId = @InvoiceId
            ORDER BY exchangeRow.StartedUtc, exchangeRow.ExchangeId;',
            N'@InvoiceId uniqueidentifier',
            @InvoiceId;
    END;
END;

IF OBJECT_ID(N'dbo.FiscalPaymentEvent', N'U') IS NOT NULL AND
   OBJECT_ID(N'dbo.FiscalAttempt', N'U') IS NOT NULL AND
   OBJECT_ID(N'dbo.FiscalAttemptRevision', N'U') IS NOT NULL
BEGIN
    EXEC sys.sp_executesql N'
        SELECT N''FiscalPaymentEvents'' AS Section, paymentEvent.*
        FROM dbo.FiscalPaymentEvent paymentEvent
        WHERE EXISTS
        (
            SELECT 1
            FROM dbo.FiscalAttempt attempt
            INNER JOIN dbo.FiscalAttemptRevision revision
                ON revision.AttemptId = attempt.AttemptId
            WHERE attempt.DocumentKind = 1
              AND attempt.DocumentId = @InvoiceId
              AND attempt.Family = ''Payment''
              AND
              (
                  revision.SnapshotJson LIKE N''%'' + paymentEvent.TransactionUid + N''%''
                  OR CONVERT(varchar(max), attempt.RequestPayload) LIKE
                     ''%'' + CONVERT(varchar(128), paymentEvent.TransactionUid) + ''%''
              )
        )
        ORDER BY paymentEvent.RecordedUtc, paymentEvent.EventId;',
        N'@InvoiceId uniqueidentifier',
        @InvoiceId;
END;

IF OBJECT_ID(N'dbo.MyDataInvoice', N'U') IS NULL
BEGIN
    INSERT #DiagnosticFindings
    (
        Severity,
        FindingCode,
        Finding,
        Evidence
    )
    VALUES
    (
        2,
        'MYDATA_PROJECTION_SCHEMA_MISSING',
        N'The myDATA projection table is not installed.',
        NULL
    );
END;
ELSE
BEGIN
    EXEC sys.sp_executesql N'
        SELECT N''MyDataInvoice'' AS Section, projection.*
        FROM dbo.MyDataInvoice projection
        WHERE projection.InvoiceId = @InvoiceId
        ORDER BY projection.DateTimeSent, projection.MyDataInvoiceId;',
        N'@InvoiceId uniqueidentifier',
        @InvoiceId;

    EXEC sys.sp_executesql N'
        INSERT #DiagnosticFindings
        (
            Severity,
            FindingCode,
            Finding,
            Evidence
        )
        SELECT
            2,
            ''NO_MYDATA_MARK'',
            N''No positive myDATA MARK is stored for the invoice.'',
            NULL
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM dbo.MyDataInvoice
            WHERE InvoiceId = @InvoiceId
              AND ISNULL(Mark, 0) > 0
        );

        INSERT #DiagnosticFindings
        (
            Severity,
            FindingCode,
            Finding,
            Evidence
        )
        SELECT
            3,
            ''MYDATA_ERRORS'',
            N''The myDATA projection contains errors.'',
            CONVERT(nvarchar(max), Errors)
        FROM dbo.MyDataInvoice
        WHERE InvoiceId = @InvoiceId
          AND NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(max), Errors))), N'''') IS NOT NULL;',
        N'@InvoiceId uniqueidentifier',
        @InvoiceId;
END;

IF OBJECT_ID(N'dbo.MyDataInvoiceHistory', N'U') IS NOT NULL
BEGIN
    EXEC sys.sp_executesql N'
        SELECT N''MyDataInvoiceHistory'' AS Section, historyRow.*
        FROM dbo.MyDataInvoiceHistory historyRow
        WHERE historyRow.InvoiceId = @InvoiceId
        ORDER BY historyRow.DateTimeSent, historyRow.MyDataInvoiceId;',
        N'@InvoiceId uniqueidentifier',
        @InvoiceId;
END;

IF OBJECT_ID(N'dbo.MyDieselCouponCase', N'U') IS NOT NULL
BEGIN
    EXEC sys.sp_executesql N'
        SELECT N''MyDieselCouponCases'' AS Section, couponCase.*
        FROM dbo.MyDieselCouponCase couponCase
        WHERE couponCase.InvoiceId = @InvoiceId
        ORDER BY couponCase.CreatedUtc, couponCase.CouponCaseId;

        INSERT #DiagnosticFindings
        (
            Severity,
            FindingCode,
            Finding,
            Evidence
        )
        SELECT
            3,
            ''MYDIESEL_BLOCKS_INVOICE'',
            N''A myDiesel coupon case is blocking invoice completion.'',
            N''Stage='' + LocalStage +
                N''; RemoteStatus='' + ISNULL(RemoteStatus, N''<null>'') +
                N''; Coupon='' + CONVERT(nvarchar(24), CouponCode)
        FROM dbo.MyDieselCouponCase
        WHERE InvoiceId = @InvoiceId
          AND BlocksInvoice = 1;',
        N'@InvoiceId uniqueidentifier',
        @InvoiceId;
END;

IF OBJECT_ID(N'dbo.FiscalInvoiceTypeCorrection', N'U') IS NOT NULL
BEGIN
    EXEC sys.sp_executesql N'
        SELECT N''InvoiceTypeCorrections'' AS Section, correction.*
        FROM dbo.FiscalInvoiceTypeCorrection correction
        WHERE correction.OriginalInvoiceId = @InvoiceId
           OR correction.CreditInvoiceId = @InvoiceId
           OR correction.ReplacementInvoiceId = @InvoiceId
        ORDER BY correction.CreatedUtc;',
        N'@InvoiceId uniqueidentifier',
        @InvoiceId;
END;

IF OBJECT_ID(N'dbo.FiscalHeatingCorrection', N'U') IS NOT NULL
BEGIN
    EXEC sys.sp_executesql N'
        SELECT N''HeatingCorrections'' AS Section, correction.*
        FROM dbo.FiscalHeatingCorrection correction
        WHERE correction.OriginalInvoiceId = @InvoiceId
           OR correction.CreditInvoiceId = @InvoiceId
           OR correction.ReplacementInvoiceId = @InvoiceId
        ORDER BY correction.CreatedUtc;',
        N'@InvoiceId uniqueidentifier',
        @InvoiceId;
END;

IF OBJECT_ID(N'dbo.ChangeLog', N'U') IS NOT NULL
BEGIN
    EXEC sys.sp_executesql N'
        SELECT TOP (200)
            N''InvoiceChangeLog'' AS Section,
            changeRow.*
        FROM dbo.ChangeLog changeRow
        WHERE changeRow.TableName = N''Invoice''
          AND
          (
              changeRow.PrimaryKey = CONVERT(nvarchar(50), @InvoiceId)
              OR CONVERT(nvarchar(max), changeRow.OldValue) LIKE
                 N''%'' + CONVERT(nvarchar(36), @InvoiceId) + N''%''
              OR CONVERT(nvarchar(max), changeRow.NewValue) LIKE
                 N''%'' + CONVERT(nvarchar(36), @InvoiceId) + N''%''
          )
        ORDER BY changeRow.DateTimeStamp, changeRow.ChangeLogId;',
        N'@InvoiceId uniqueidentifier',
        @InvoiceId;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM #DiagnosticFindings
)
BEGIN
    INSERT #DiagnosticFindings
    (
        Severity,
        FindingCode,
        Finding,
        Evidence
    )
    VALUES
    (
        0,
        'NO_OBVIOUS_DATABASE_BLOCKER',
        N'No obvious blocker was detected from the persisted database state. Review the detailed workflow, attempt, HTTP, and application-log evidence.',
        NULL
    );
END;

SELECT
    N'Findings' AS Section,
    CASE Severity
        WHEN 3 THEN N'ERROR'
        WHEN 2 THEN N'WARNING'
        WHEN 1 THEN N'INFORMATION'
        ELSE N'OK'
    END AS SeverityDescription,
    Severity,
    FindingCode,
    Finding,
    Evidence
FROM #DiagnosticFindings
ORDER BY Severity DESC, FindingCode;

DROP TABLE #DiagnosticFindings;

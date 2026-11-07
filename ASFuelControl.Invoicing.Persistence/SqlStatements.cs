namespace ASFuelControl.Invoicing.Persistence
{
    // SQL Server 2008 R2 (10.50), grammar/compatibility level 100.
    // Keep every runtime batch here so the SQL-100 parser tests cover the actual statements.
    internal static class SqlStatements
    {
        internal const string ReadWorkflow = "SELECT Payload,PayloadSha256 FROM dbo.FiscalInvoiceWorkflow WHERE InvoiceId=@id;";
        internal const string InsertWorkflow = "INSERT dbo.FiscalInvoiceWorkflow(InvoiceId,Payload,PayloadSha256,CreatedUtc,FencingToken) VALUES(@id,@payload,@hash,@now,@token);";
        internal const string RefreshWorkflowConnection = @"
IF EXISTS(SELECT 1 FROM dbo.FiscalAttempt WITH (UPDLOCK,HOLDLOCK) WHERE DocumentKind=1 AND DocumentId=@id)
BEGIN RAISERROR('The provider connection is fixed once fiscal processing has started.',16,1); RETURN; END;
UPDATE dbo.FiscalInvoiceWorkflow SET Payload=@payload,PayloadSha256=@hash,FencingToken=@token
WHERE InvoiceId=@id AND PayloadSha256=@expectedHash AND Payload=@expected;
IF @@ROWCOUNT<>1 BEGIN RAISERROR('The saved workflow changed. Reload before continuing.',16,1); RETURN; END;";
        internal const string ReplaceWorkflowAfterRejectedSigning = @"
DECLARE @state nvarchar(32);
SELECT TOP (1) @state=r.State
FROM dbo.FiscalAttempt a
JOIN dbo.FiscalAttemptRevision r ON r.AttemptId=a.AttemptId AND r.Revision=a.CurrentRevision
WHERE a.DocumentKind=1 AND a.DocumentId=@id AND a.Family='Signing'
ORDER BY a.AttemptSequence DESC;
IF ISNULL(@state,'')<>N'Rejected'
BEGIN RAISERROR('Only a definitively rejected signing request can be regenerated.',16,1); RETURN; END;
UPDATE dbo.FiscalInvoiceWorkflow SET Payload=@payload,PayloadSha256=@hash,FencingToken=@token
WHERE InvoiceId=@id AND PayloadSha256=@expectedHash AND Payload=@expected;
IF @@ROWCOUNT<>1 BEGIN RAISERROR('The saved workflow changed. Reload before continuing.',16,1); RETURN; END;";
        internal const string ReplaceWorkflowAfterRejectedSubmission = @"
DECLARE @state nvarchar(32);
SELECT TOP (1) @state=r.State
FROM dbo.FiscalAttempt a
JOIN dbo.FiscalAttemptRevision r ON r.AttemptId=a.AttemptId AND r.Revision=a.CurrentRevision
WHERE a.DocumentKind=1 AND a.DocumentId=@id AND a.Family='Submission'
ORDER BY a.AttemptSequence DESC;
IF ISNULL(@state,'') NOT IN (N'Rejected',N'RetryAuthorized')
BEGIN RAISERROR('Only a rejected provider submission can be regenerated.',16,1); RETURN; END;
UPDATE dbo.FiscalInvoiceWorkflow SET Payload=@payload,PayloadSha256=@hash,FencingToken=@token
WHERE InvoiceId=@id AND PayloadSha256=@expectedHash AND Payload=@expected;
IF @@ROWCOUNT<>1 BEGIN RAISERROR('The saved workflow changed. Reload before continuing.',16,1); RETURN; END;";
        internal const string ReplaceWorkflowForDispatchRecovery = @"
IF NOT EXISTS(
    SELECT 1
    FROM dbo.Invoice i WITH (UPDLOCK,HOLDLOCK)
    JOIN dbo.InvoiceType t ON t.InvoiceTypeId=i.InvoiceTypeId
    WHERE i.InvoiceId=@id AND i.Number>0 AND ISNULL(i.IsPrinted,0)=0
        AND t.OfficialEnumerator=222
        AND EXISTS(SELECT 1 FROM dbo.InvoiceLine l WHERE l.InvoiceId=i.InvoiceId AND l.SaleTransactionId IS NOT NULL))
BEGIN RAISERROR('Only an unprinted numbered direct-pump invoice-delivery can recover its dispatch date.',16,1); RETURN; END;
IF EXISTS(SELECT 1 FROM dbo.MyDataInvoice WHERE InvoiceId=@id AND ISNULL(Mark,0)>0)
BEGIN RAISERROR('An invoice with a MARK cannot change its dispatch date.',16,1); RETURN; END;
IF EXISTS(
    SELECT 1 FROM dbo.FiscalAttempt a
    JOIN dbo.FiscalAttemptRevision r ON r.AttemptId=a.AttemptId AND r.Revision=a.CurrentRevision
    WHERE a.DocumentKind=1 AND a.DocumentId=@id AND (
        (a.Family='Signing' AND r.State NOT IN ('Signed','Rejected','FailedBeforeDispatch')) OR
        (a.Family='Submission' AND r.State<>'Rejected') OR
        a.Family NOT IN ('Signing','Submission')))
BEGIN RAISERROR('A completed or unresolved fiscal operation prevents dispatch-date recovery.',16,1); RETURN; END;
UPDATE dbo.FiscalInvoiceWorkflow SET Payload=@payload,PayloadSha256=@hash,FencingToken=@token
WHERE InvoiceId=@id AND PayloadSha256=@expectedHash AND Payload=@expected;
IF @@ROWCOUNT<>1 BEGIN RAISERROR('The saved workflow changed. Reload before continuing.',16,1); RETURN; END;";
        internal const string ChangePaymentWithoutWorkflow = @"
IF EXISTS(SELECT 1 FROM dbo.FiscalInvoiceWorkflow WITH (UPDLOCK,HOLDLOCK) WHERE InvoiceId=@id)
BEGIN RAISERROR('The invoice workflow was created. Reload before changing payment.',16,1); RETURN; END;
IF EXISTS(SELECT 1 FROM dbo.FiscalAttempt WITH (UPDLOCK,HOLDLOCK) WHERE DocumentKind=1 AND DocumentId=@id)
BEGIN RAISERROR('Fiscal history without a saved workflow requires reconciliation.',16,1); RETURN; END;
IF NOT EXISTS(SELECT 1 FROM dbo.Invoice WITH (UPDLOCK,HOLDLOCK) WHERE InvoiceId=@id AND Number>0
    AND ISNULL(IsPrinted,0)=0 AND NULLIF(InvoiceSignature,'') IS NULL AND NULLIF(QRCodeData,'') IS NULL
    AND ISNULL(PaymentType,1)=@oldPayment)
BEGIN RAISERROR('The numbered invoice is no longer eligible for a payment-method change.',16,1); RETURN; END;
IF EXISTS(SELECT 1 FROM dbo.MyDataInvoice WHERE InvoiceId=@id AND ISNULL(Mark,0)>0)
BEGIN RAISERROR('An invoice with a MARK cannot change payment method.',16,1); RETURN; END;
UPDATE dbo.Invoice SET PaymentType=@newPayment WHERE InvoiceId=@id AND ISNULL(PaymentType,1)=@oldPayment;
IF @@ROWCOUNT<>1 BEGIN RAISERROR('The invoice payment method changed. Reload before continuing.',16,1); RETURN; END;";
        internal const string ReplaceWorkflowForPaymentChange = @"
IF NOT EXISTS(SELECT 1 FROM dbo.Invoice WITH (UPDLOCK,HOLDLOCK) WHERE InvoiceId=@id AND Number>0
    AND ISNULL(IsPrinted,0)=0 AND NULLIF(InvoiceSignature,'') IS NULL AND NULLIF(QRCodeData,'') IS NULL
    AND ISNULL(PaymentType,1)=@oldPayment)
BEGIN RAISERROR('The numbered invoice is no longer eligible for a payment-method change.',16,1); RETURN; END;
IF EXISTS(SELECT 1 FROM dbo.MyDataInvoice WHERE InvoiceId=@id AND ISNULL(Mark,0)>0)
BEGIN RAISERROR('An invoice with a MARK cannot change payment method.',16,1); RETURN; END;
IF EXISTS(
    SELECT 1 FROM dbo.FiscalAttempt a
    JOIN dbo.FiscalAttemptRevision r ON r.AttemptId=a.AttemptId AND r.Revision=a.CurrentRevision
    WHERE a.DocumentKind=1 AND a.DocumentId=@id AND (
        (a.Family='Signing' AND r.State NOT IN ('Prepared','Rejected','FailedBeforeDispatch')) OR
        (a.Family='Submission' AND r.State<>'Rejected') OR
        (a.Family='ProviderSession' AND r.State NOT IN ('Rejected','Cancelled','Expired')) OR
        (a.Family='Payment' AND r.State NOT IN ('Declined','Failed')) OR
        a.Family NOT IN ('Signing','Submission','ProviderSession','Payment')))
BEGIN RAISERROR('A completed or unresolved fiscal operation prevents changing payment method.',16,1); RETURN; END;
INSERT dbo.FiscalAttemptRevision(AttemptId,Revision,State,RecordedUtc,SnapshotJson,FencingToken)
SELECT a.AttemptId,a.CurrentRevision+1,'FailedBeforeDispatch',@now,
    N'{""Diagnostic"":""Superseded by payment-method change before dispatch.""}',@token
FROM dbo.FiscalAttempt a
JOIN dbo.FiscalAttemptRevision r ON r.AttemptId=a.AttemptId AND r.Revision=a.CurrentRevision
WHERE a.DocumentKind=1 AND a.DocumentId=@id AND a.Family='Signing' AND r.State='Prepared';
UPDATE a SET CurrentRevision=a.CurrentRevision+1,BlocksNewAttempt=0
FROM dbo.FiscalAttempt a
JOIN dbo.FiscalAttemptRevision r ON r.AttemptId=a.AttemptId AND r.Revision=a.CurrentRevision
WHERE a.DocumentKind=1 AND a.DocumentId=@id AND a.Family='Signing' AND r.State='Prepared';
UPDATE dbo.FiscalInvoiceWorkflow SET Payload=@payload,PayloadSha256=@hash,FencingToken=@token
WHERE InvoiceId=@id AND PayloadSha256=@expectedHash AND Payload=@expected;
IF @@ROWCOUNT<>1 BEGIN RAISERROR('The saved workflow changed. Reload before continuing.',16,1); RETURN; END;
UPDATE dbo.Invoice SET PaymentType=@newPayment WHERE InvoiceId=@id AND ISNULL(PaymentType,1)=@oldPayment;
IF @@ROWCOUNT<>1 BEGIN RAISERROR('The invoice payment method changed. Reload before continuing.',16,1); RETURN; END;";
        internal const string ReadDocumentWorkflow = "SELECT Payload,PayloadSha256 FROM dbo.FiscalDocumentWorkflow WHERE DocumentKind=@kind AND DocumentId=@id;";
        internal const string InsertDocumentWorkflow = "INSERT dbo.FiscalDocumentWorkflow(DocumentKind,DocumentId,Payload,PayloadSha256,CreatedUtc,FencingToken) VALUES(@kind,@id,@payload,@hash,@now,@token);";
        // Lock the counter before selecting unnumbered rows. Earlier rows of this type
        // are allocated in the same transaction, including a manual caller's backlog.
        internal const string AllocateInvoiceNumbers = @"
DECLARE @type uniqueidentifier, @last int, @series nvarchar(255), @date datetime, @target uniqueidentifier;
SELECT @type=InvoiceTypeId,@date=TransactionDate FROM dbo.Invoice WHERE InvoiceId=@id;
IF @type IS NULL BEGIN RAISERROR('Invoice/type not found.',16,1); RETURN; END;
SELECT @last=LastNumber,@series=ISNULL(DefaultSeries,'') FROM dbo.InvoiceType WITH (UPDLOCK,HOLDLOCK) WHERE InvoiceTypeId=@type;
IF @last IS NULL BEGIN RAISERROR('Invoice counter not found.',16,1); RETURN; END;
-- Number allocation is serialized by the invoice-type counter lock. Validate the
-- rolling edge of the sequence instead of allowing old imported history to block
-- current issuance. Number is the secondary order for invoices on the same date.
IF EXISTS(
    SELECT 1 FROM (
        SELECT TOP (10) Number
        FROM dbo.Invoice
        WHERE InvoiceTypeId=@type AND ISNULL(Series,'')=@series AND Number>0
        ORDER BY TransactionDate DESC,Number DESC,InvoiceId DESC
    ) recent WHERE recent.Number>@last
)
BEGIN RAISERROR('Invoice counter is behind issued history.',16,1); RETURN; END;
DECLARE pending CURSOR LOCAL STATIC READ_ONLY FOR
SELECT InvoiceId FROM dbo.Invoice WITH (UPDLOCK,HOLDLOCK)
WHERE InvoiceTypeId=@type AND Number=0 AND (TransactionDate<@date OR (TransactionDate=@date AND InvoiceId<=@id))
AND TransactionDate>=@firstDate AND ISNULL(IsPrinted,0)=0
AND NULLIF(InvoiceSignature,'') IS NULL AND NULLIF(QRCodeData,'') IS NULL
AND NOT EXISTS(SELECT 1 FROM dbo.MyDataInvoice WHERE MyDataInvoice.InvoiceId=Invoice.InvoiceId)
AND EXISTS(SELECT 1 FROM dbo.InvoiceLine WHERE InvoiceLine.InvoiceId=Invoice.InvoiceId)
ORDER BY TransactionDate,InvoiceId;
OPEN pending; FETCH NEXT FROM pending INTO @target;
WHILE @@FETCH_STATUS=0
BEGIN
    SET @last=@last+1;
    UPDATE dbo.Invoice SET Number=@last,Series=@series WHERE InvoiceId=@target AND Number=0;
    FETCH NEXT FROM pending INTO @target;
END;
CLOSE pending; DEALLOCATE pending;
UPDATE dbo.InvoiceType SET LastNumber=@last WHERE InvoiceTypeId=@type;
SELECT Number FROM dbo.Invoice WHERE InvoiceId=@id;";
        internal const string AllocateIssueDateReissueIdentity = @"
DECLARE @type uniqueidentifier,@last int,@number int,@series nvarchar(255),@date datetime;
SELECT @type=InvoiceTypeId,@number=Number,@series=ISNULL(Series,''),@date=TransactionDate
FROM dbo.Invoice WITH(UPDLOCK,HOLDLOCK) WHERE InvoiceId=@id;
IF @type IS NULL BEGIN RAISERROR('Invoice/type not found.',16,1); RETURN; END;
IF @number<>@oldNumber OR @series<>@oldSeries OR @date<>@oldDate
BEGIN RAISERROR('The invoice identity changed. Reload before reissuing.',16,1); RETURN; END;
IF @number<=0 OR EXISTS(SELECT 1 FROM dbo.Invoice WHERE InvoiceId=@id AND
    (NULLIF(InvoiceSignature,'') IS NOT NULL OR NULLIF(QRCodeData,'') IS NOT NULL)) OR
    EXISTS(SELECT 1 FROM dbo.MyDataInvoice WHERE InvoiceId=@id AND Mark>0)
BEGIN RAISERROR('An issued invoice cannot be renumbered.',16,1); RETURN; END;
SELECT @last=LastNumber FROM dbo.InvoiceType WITH(UPDLOCK,HOLDLOCK) WHERE InvoiceTypeId=@type;
IF @last IS NULL BEGIN RAISERROR('Invoice counter not found.',16,1); RETURN; END;
IF EXISTS(
    SELECT 1 FROM (
        SELECT TOP (10) Number FROM dbo.Invoice
        WHERE InvoiceTypeId=@type AND ISNULL(Series,'')=@series AND Number>0 AND InvoiceId<>@id
        ORDER BY TransactionDate DESC,Number DESC,InvoiceId DESC
    ) recent WHERE recent.Number>@last
)
BEGIN RAISERROR('Invoice counter is behind issued history.',16,1); RETURN; END;
SET @last=@last+1;
-- Some legacy forms set IsPrinted before provider acceptance. The fiscal journal
-- is authoritative here: this guarded path permits only rejected submissions and
-- no print/sign/payment evidence, so the replacement identity is unprinted.
UPDATE dbo.Invoice SET Number=@last,TransactionDate=@newDate,IsPrinted=0 WHERE InvoiceId=@id;
UPDATE dbo.InvoiceType SET LastNumber=@last WHERE InvoiceTypeId=@type;
SELECT @last;";
        internal const string ReplaceWorkflowForIssueDateReissue = @"
IF EXISTS(SELECT 1 FROM dbo.FiscalAttempt a
    JOIN dbo.FiscalAttemptRevision r ON r.AttemptId=a.AttemptId AND r.Revision=a.CurrentRevision
    WHERE a.DocumentKind=1 AND a.DocumentId=@id AND
    (a.Family<>'Submission' OR r.State<>'Rejected'))
BEGIN RAISERROR('Only definitively rejected provider attempts permit issue-date reissuance.',16,1); RETURN; END;
IF NOT EXISTS(SELECT 1 FROM dbo.FiscalAttempt a
    JOIN dbo.FiscalAttemptRevision r ON r.AttemptId=a.AttemptId AND r.Revision=a.CurrentRevision
    WHERE a.DocumentKind=1 AND a.DocumentId=@id AND a.Family='Submission' AND r.State='Rejected')
BEGIN RAISERROR('A rejected provider attempt is required.',16,1); RETURN; END;
UPDATE dbo.FiscalInvoiceWorkflow SET Payload=@payload,PayloadSha256=@hash,FencingToken=@token
WHERE InvoiceId=@id AND PayloadSha256=@expectedHash AND Payload=@expected;
IF @@ROWCOUNT<>1 BEGIN RAISERROR('The saved workflow changed. Reload before continuing.',16,1); RETURN; END;";
        internal const string SessionOptions =
            "SET ANSI_NULLS ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON; SET ARITHABORT ON; " +
            "SET CONCAT_NULL_YIELDS_NULL ON; SET QUOTED_IDENTIFIER ON; SET NUMERIC_ROUNDABORT OFF;";
        internal const string Acquire =
            "DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=@resource, @LockMode='Exclusive', @LockOwner='Session', @LockTimeout=@timeout; SELECT @r;";
        internal const string AdvanceFence =
            "IF NOT EXISTS (SELECT 1 FROM dbo.FiscalDocumentLease WHERE DocumentKind=@kind AND DocumentId=@id) " +
            "INSERT dbo.FiscalDocumentLease(DocumentKind,DocumentId,FencingToken) VALUES(@kind,@id,1); ELSE UPDATE dbo.FiscalDocumentLease SET FencingToken=FencingToken+1 WHERE DocumentKind=@kind AND DocumentId=@id; " +
            "SELECT FencingToken FROM dbo.FiscalDocumentLease WHERE DocumentKind=@kind AND DocumentId=@id;";
        internal const string AssertLease =
            "SELECT FencingToken FROM dbo.FiscalDocumentLease WHERE DocumentKind=@kind AND DocumentId=@id " +
            "AND APPLOCK_MODE('public',@resource,'Session')='Exclusive';";
        internal const string ReadAttempts =
            "SELECT a.AttemptId,a.Family,a.Operation,a.Scope,a.PayloadFormat,a.RequestPayload,a.RequestSha256,a.CreatedUtc,r.Revision,r.State,r.RecordedUtc,r.SnapshotJson,a.OriginalAttemptId " +
            "FROM dbo.FiscalAttempt a JOIN dbo.FiscalAttemptRevision r ON r.AttemptId=a.AttemptId " +
            "WHERE a.DocumentKind=@kind AND a.DocumentId=@id AND a.Family=@family " +
            "AND (@attempt IS NULL OR a.AttemptId=@attempt) AND (@history=1 OR r.Revision=a.CurrentRevision) ORDER BY a.AttemptSequence,r.Revision;";
        internal const string ReadDeferredSamtecInvoiceRetries = @"
SELECT TOP (@maximum) a.DocumentId
FROM dbo.FiscalAttempt a
JOIN dbo.FiscalAttemptRevision r ON r.AttemptId=a.AttemptId AND r.Revision=a.CurrentRevision
WHERE a.DocumentKind=1 AND a.Family='Signing' AND a.Operation='Sign'
  AND a.PayloadFormat='samtec-sign-json/v1' AND a.BlocksNewAttempt=1
  AND r.State='Unknown' AND r.RecordedUtc<=@due
  AND r.SnapshotJson LIKE @diagnostic
ORDER BY r.RecordedUtc,a.AttemptSequence;";
        internal const string CountDeferredSamtecInvoiceRetries = @"
SELECT COUNT(*)
FROM dbo.FiscalAttempt a
JOIN dbo.FiscalAttemptRevision r ON r.AttemptId=a.AttemptId AND r.Revision=a.CurrentRevision
WHERE a.DocumentKind=1 AND a.Family='Signing' AND a.Operation='Sign'
  AND a.PayloadFormat='samtec-sign-json/v1' AND a.BlocksNewAttempt=1
  AND r.State='Unknown' AND r.RecordedUtc<=@due
  AND r.SnapshotJson LIKE @diagnostic;";
        internal const string UpdateProjection =
            "UPDATE dbo.FiscalAttempt SET CurrentRevision=@revision,BlocksNewAttempt=@blocking WHERE AttemptId=@attempt AND CurrentRevision=@expected;";
        internal const string InsertAttempt =
            "IF EXISTS(SELECT 1 FROM dbo.FiscalAttempt WHERE DocumentKind=@kind AND DocumentId=@document AND Family=@family AND Operation=@operation AND BlocksNewAttempt=1) " +
            "RAISERROR('A previous fiscal attempt must be reconciled or is already accepted.',16,1); ELSE " +
            "INSERT dbo.FiscalAttempt(AttemptId,DocumentKind,DocumentId,Family,Operation,Scope,PayloadFormat,RequestPayload,RequestSha256,CreatedUtc,CurrentRevision,BlocksNewAttempt,OriginalAttemptId) " +
            "VALUES(@attempt,@kind,@document,@family,@operation,@scope,@format,@payload,@hash,@created,0,@blocking,@original);";
        internal const string InsertRevision =
            "INSERT dbo.FiscalAttemptRevision(AttemptId,Revision,State,RecordedUtc,SnapshotJson,FencingToken) VALUES(@id,@revision,@state,@updated,@snapshot,@token);";
        internal const string FindPaymentDocument =
            "SELECT DocumentId FROM dbo.FiscalAttempt WHERE AttemptId=@id AND Family='Payment' AND DocumentKind=1;";
        internal const string InsertExchange =
            "INSERT dbo.FiscalHttpExchange(ExchangeId,InvoiceId,DocumentKind,DocumentId,StartedUtc,RequestJson) VALUES(@id,@invoice,@kind,@document,@started,@request);";
        internal const string CompleteExchange =
            "UPDATE dbo.FiscalHttpExchange SET CompletedUtc=@completed,ResponseJson=@response WHERE ExchangeId=@id AND ResponseJson IS NULL AND StartedUtc<=@completed;";
        internal const string InsertPaymentEvent =
            "INSERT dbo.FiscalPaymentEvent(EventId,TransactionUid,RecordedUtc,EventJson) VALUES(@id,@uid,@now,@event);";
        internal const string ReadPaymentEvents =
            "SELECT EventJson FROM dbo.FiscalPaymentEvent WHERE TransactionUid=@uid ORDER BY RecordedUtc,EventId;";
    }
}

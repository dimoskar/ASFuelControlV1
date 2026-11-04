/*
    Removes only incorrect cancellation-relation metadata.

    No Invoice or InvoiceLine is deleted. Both the original invoice and the
    related invoice remain unchanged, including signatures and fiscal history.

    Set @InvoiceId to the InvoiceId of the catalog row whose "Ακυρ." checkbox
    is incorrectly selected. That invoice is ParentInvoiceId in the bad link.

    Run first with @Apply=0. The transaction is rolled back after showing the
    affected invoices and row counts. Set @Apply=1 only after checking preview.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;

DECLARE @InvoiceId uniqueidentifier = '00000000-0000-0000-0000-000000000000'; -- replace with the checked row's InvoiceId
DECLARE @Apply bit = 0; -- 0 = preview/ROLLBACK, 1 = remove relation/COMMIT

IF @InvoiceId='00000000-0000-0000-0000-000000000000'
    THROW 50001, 'Set @InvoiceId before running the script.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM dbo.Invoice WHERE InvoiceId=@InvoiceId)
        THROW 50002, 'The supplied InvoiceId does not exist in dbo.Invoice.', 1;

    DECLARE @BadRelations TABLE
    (
        InvoiceRelationId uniqueidentifier PRIMARY KEY,
        ParentInvoiceId uniqueidentifier NOT NULL,
        ChildInvoiceId uniqueidentifier NOT NULL
    );

    INSERT @BadRelations (InvoiceRelationId,ParentInvoiceId,ChildInvoiceId)
    SELECT InvoiceRelationId,ParentInvoiceId,ChildInvoiceId
    FROM dbo.InvoiceRelation
    WHERE ParentInvoiceId=@InvoiceId
      AND RelationType=0; -- InvoiceRelationTypeEnum.Cancel

    IF NOT EXISTS (SELECT 1 FROM @BadRelations)
        THROW 50003, 'This invoice has no outgoing cancellation relation. Nothing was changed.', 1;

    /* Preview the original and every incorrectly related child invoice. */
    SELECT
        original.InvoiceId AS OriginalInvoiceId,
        originalType.Description AS OriginalInvoiceType,
        original.Series AS OriginalSeries,
        original.Number AS OriginalNumber,
        original.TransactionDate AS OriginalDate,
        bad.InvoiceRelationId,
        related.InvoiceId AS RelatedInvoiceId,
        relatedType.Description AS RelatedInvoiceType,
        related.Series AS RelatedSeries,
        related.Number AS RelatedNumber,
        related.TransactionDate AS RelatedDate,
        related.InvoiceSignature AS RelatedSignature
    FROM @BadRelations AS bad
    INNER JOIN dbo.Invoice AS original ON original.InvoiceId=bad.ParentInvoiceId
    INNER JOIN dbo.InvoiceType AS originalType ON originalType.InvoiceTypeId=original.InvoiceTypeId
    INNER JOIN dbo.Invoice AS related ON related.InvoiceId=bad.ChildInvoiceId
    INNER JOIN dbo.InvoiceType AS relatedType ON relatedType.InvoiceTypeId=related.InvoiceTypeId
    ORDER BY related.TransactionDate,related.Number;

    DECLARE @Changed TABLE (OperationName nvarchar(100) NOT NULL,RowsChanged int NOT NULL);

    /* Remove per-line mappings that belong to the incorrect invoice relation. */
    DELETE lineLink
    FROM dbo.InvoiceLineRelation AS lineLink
    INNER JOIN @BadRelations AS bad ON bad.InvoiceRelationId=lineLink.InvoiceRelationId;
    INSERT @Changed VALUES ('Deleted InvoiceLineRelation rows',@@ROWCOUNT);

    /* Preserve the invoice lines but detach their obsolete relation reference. */
    UPDATE invoiceLine
    SET InvoiceRelationId=NULL
    FROM dbo.InvoiceLine AS invoiceLine
    INNER JOIN @BadRelations AS bad ON bad.InvoiceRelationId=invoiceLine.InvoiceRelationId;
    INSERT @Changed VALUES ('Detached InvoiceLine rows',@@ROWCOUNT);

    /* This is the only invoice-level metadata being removed. */
    DELETE invoiceLink
    FROM dbo.InvoiceRelation AS invoiceLink
    INNER JOIN @BadRelations AS bad ON bad.InvoiceRelationId=invoiceLink.InvoiceRelationId;
    INSERT @Changed VALUES ('Deleted InvoiceRelation rows',@@ROWCOUNT);

    SELECT OperationName,RowsChanged FROM @Changed ORDER BY OperationName;

    IF @Apply=1
    BEGIN
        COMMIT TRANSACTION;
        PRINT 'COMMITTED: only the incorrect cancellation relation metadata was removed. No invoice was deleted.';
    END
    ELSE
    BEGIN
        ROLLBACK TRANSACTION;
        PRINT 'PREVIEW ONLY: all changes were rolled back. Set @Apply=1 after checking the related invoice list.';
    END;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;

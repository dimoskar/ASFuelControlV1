-- myDiesel coupon journal and projection. SQL Server 2008 R2 / compatibility level 100.
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @lockResult int;
    EXEC @lockResult = sys.sp_getapplock @Resource = N'ASFuelControl.MyDieselSchema',
        @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 15000;
    IF @lockResult < 0 RAISERROR('Cannot acquire myDiesel schema lock.',16,1);

    IF OBJECT_ID(N'dbo.FiscalDocumentLease',N'U') IS NULL OR OBJECT_ID(N'dbo.FiscalAttempt',N'U') IS NULL
        OR OBJECT_ID(N'dbo.FiscalHttpExchange',N'U') IS NULL
        RAISERROR('The fiscal journal must be installed before myDiesel.',16,1);

    IF OBJECT_ID(N'dbo.CK_FiscalDocumentLease_DocumentKind',N'C') IS NULL
    BEGIN
        DECLARE @documentKindColumn int, @oldDocumentKindCheck sysname, @dropDocumentKind nvarchar(512);
        SELECT @documentKindColumn=column_id FROM sys.columns
        WHERE object_id=OBJECT_ID(N'dbo.FiscalDocumentLease') AND name=N'DocumentKind';
        SELECT TOP 1 @oldDocumentKindCheck=name FROM sys.check_constraints
        WHERE parent_object_id=OBJECT_ID(N'dbo.FiscalDocumentLease') AND parent_column_id=@documentKindColumn;
        IF @oldDocumentKindCheck IS NOT NULL
        BEGIN
            SET @dropDocumentKind=N'ALTER TABLE dbo.FiscalDocumentLease DROP CONSTRAINT '+QUOTENAME(@oldDocumentKindCheck)+N';';
            EXEC sys.sp_executesql @dropDocumentKind;
        END;
        ALTER TABLE dbo.FiscalDocumentLease WITH CHECK ADD CONSTRAINT CK_FiscalDocumentLease_DocumentKind
            CHECK(DocumentKind BETWEEN 1 AND 6);
    END;

    IF OBJECT_ID(N'dbo.CK_FiscalAttempt_Family',N'C') IS NOT NULL
        ALTER TABLE dbo.FiscalAttempt DROP CONSTRAINT CK_FiscalAttempt_Family;
    ALTER TABLE dbo.FiscalAttempt WITH CHECK ADD CONSTRAINT CK_FiscalAttempt_Family
        CHECK(Family IN ('Signing','Submission','ProviderSession','Payment','Print','Cancellation','MyDiesel'));

    IF COL_LENGTH(N'dbo.FiscalHttpExchange',N'DocumentKind') IS NULL
        EXEC(N'ALTER TABLE dbo.FiscalHttpExchange ADD DocumentKind int NULL;');
    IF COL_LENGTH(N'dbo.FiscalHttpExchange',N'DocumentId') IS NULL
        EXEC(N'ALTER TABLE dbo.FiscalHttpExchange ADD DocumentId uniqueidentifier NULL;');
    IF OBJECT_ID(N'dbo.CK_FiscalHttpExchange_Document',N'C') IS NULL
        EXEC(N'ALTER TABLE dbo.FiscalHttpExchange WITH CHECK ADD CONSTRAINT CK_FiscalHttpExchange_Document
            CHECK((DocumentKind IS NULL AND DocumentId IS NULL) OR (DocumentKind BETWEEN 1 AND 6 AND DocumentId IS NOT NULL));');
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.FiscalHttpExchange') AND name=N'IX_FiscalHttpExchange_Document')
        EXEC(N'CREATE INDEX IX_FiscalHttpExchange_Document ON dbo.FiscalHttpExchange(DocumentKind,DocumentId,StartedUtc);');

    IF OBJECT_ID(N'dbo.MyDieselCouponCase',N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.MyDieselCouponCase
        (
            CouponCaseId uniqueidentifier NOT NULL CONSTRAINT PK_MyDieselCouponCase PRIMARY KEY,
            InvoiceId uniqueidentifier NOT NULL,
            CouponCode varchar(24) NOT NULL,
            BeneficiaryVat char(9) NOT NULL,
            CompanyVat char(9) NOT NULL,
            Environment tinyint NOT NULL,
            CredentialProfileId varchar(128) NOT NULL,
            ReservedBranchNum int NOT NULL,
            CurrentBranchNum int NULL,
            LocalStage varchar(32) NOT NULL,
            RemoteStatus varchar(32) NULL,
            Redeemable bit NULL,
            IssuedLitres decimal(18,2) NULL,
            CouponCreatedUtc datetime2(0) NULL,
            ValidUntilUtc datetime2(0) NULL,
            FuelType varchar(32) NULL,
            FuelCode int NULL,
            LowBalance bit NOT NULL,
            LowBalanceMessageEl nvarchar(1000) NULL,
            InvoiceMark varchar(15) NULL,
            MarkSourceInvoiceId uniqueidentifier NULL,
            BlocksInvoice bit NOT NULL,
            BlocksCouponReuse bit NOT NULL,
            LastCheckedUtc datetime2(7) NULL,
            Revision bigint NOT NULL,
            FencingToken bigint NOT NULL,
            CreatedUtc datetime2(7) NOT NULL,
            UpdatedUtc datetime2(7) NOT NULL,
            CONSTRAINT CK_MyDieselCouponCase_Code CHECK(LEN(CouponCode)=24),
            CONSTRAINT CK_MyDieselCouponCase_BeneficiaryVat CHECK(LEN(BeneficiaryVat)=9 AND BeneficiaryVat NOT LIKE '%[^0-9]%'),
            CONSTRAINT CK_MyDieselCouponCase_CompanyVat CHECK(LEN(CompanyVat)=9 AND CompanyVat NOT LIKE '%[^0-9]%'),
            CONSTRAINT CK_MyDieselCouponCase_Environment CHECK(Environment IN (1,2)),
            CONSTRAINT CK_MyDieselCouponCase_Credential CHECK(LEN(CredentialProfileId)>0),
            CONSTRAINT CK_MyDieselCouponCase_Branches CHECK(ReservedBranchNum>=0 AND (CurrentBranchNum IS NULL OR CurrentBranchNum>=0)),
            CONSTRAINT CK_MyDieselCouponCase_Stage CHECK(LocalStage IN ('Captured','ReserveUnknown','Reserved','Released','InvoiceAccepted','RedeemUnknown','Redeemed','VerificationPending','Final','InterventionRequired')),
            CONSTRAINT CK_MyDieselCouponCase_Litres CHECK(IssuedLitres IS NULL OR IssuedLitres>0),
            CONSTRAINT CK_MyDieselCouponCase_Fuel CHECK(FuelCode IS NULL OR FuelCode>0),
            CONSTRAINT CK_MyDieselCouponCase_Mark CHECK(InvoiceMark IS NULL OR (LEN(InvoiceMark)=15 AND InvoiceMark NOT LIKE '%[^0-9]%')),
            CONSTRAINT CK_MyDieselCouponCase_Revision CHECK(Revision>=0),
            CONSTRAINT CK_MyDieselCouponCase_Fence CHECK(FencingToken>0),
            CONSTRAINT CK_MyDieselCouponCase_Times CHECK(UpdatedUtc>=CreatedUtc)
        );
        CREATE UNIQUE INDEX UX_MyDieselCouponCase_InvoiceBlocking ON dbo.MyDieselCouponCase(InvoiceId) WHERE BlocksInvoice=1;
        CREATE UNIQUE INDEX UX_MyDieselCouponCase_CodeBlocking ON dbo.MyDieselCouponCase(CouponCode) WHERE BlocksCouponReuse=1;
        CREATE INDEX IX_MyDieselCouponCase_Status ON dbo.MyDieselCouponCase(RemoteStatus,LastCheckedUtc);
        CREATE INDEX IX_MyDieselCouponCase_Invoice ON dbo.MyDieselCouponCase(InvoiceId,CreatedUtc);
    END;

    IF OBJECT_ID(N'dbo.MyDieselReferenceCache',N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.MyDieselReferenceCache
        (
            Environment tinyint NOT NULL,
            CompanyVat char(9) NOT NULL,
            CompanyJson nvarchar(max) NULL,
            CompanySha256 char(64) NULL,
            CompanyRefreshedUtc datetime2(7) NULL,
            FuelTypesJson nvarchar(max) NULL,
            FuelTypesSha256 char(64) NULL,
            FuelTypesRefreshedUtc datetime2(7) NULL,
            ContractVersion varchar(32) NOT NULL,
            CONSTRAINT PK_MyDieselReferenceCache PRIMARY KEY(Environment,CompanyVat),
            CONSTRAINT CK_MyDieselReferenceCache_Environment CHECK(Environment IN (1,2)),
            CONSTRAINT CK_MyDieselReferenceCache_CompanyVat CHECK(LEN(CompanyVat)=9 AND CompanyVat NOT LIKE '%[^0-9]%'),
            CONSTRAINT CK_MyDieselReferenceCache_CompanyPair CHECK((CompanyJson IS NULL AND CompanySha256 IS NULL AND CompanyRefreshedUtc IS NULL) OR (CompanyJson IS NOT NULL AND CompanySha256 IS NOT NULL AND CompanyRefreshedUtc IS NOT NULL)),
            CONSTRAINT CK_MyDieselReferenceCache_FuelPair CHECK((FuelTypesJson IS NULL AND FuelTypesSha256 IS NULL AND FuelTypesRefreshedUtc IS NULL) OR (FuelTypesJson IS NOT NULL AND FuelTypesSha256 IS NOT NULL AND FuelTypesRefreshedUtc IS NOT NULL))
        );
    END;

    IF OBJECT_ID(N'dbo.MyDieselCouponCase',N'U') IS NULL OR OBJECT_ID(N'dbo.MyDieselReferenceCache',N'U') IS NULL
        OR COL_LENGTH(N'dbo.FiscalHttpExchange',N'DocumentKind') IS NULL OR COL_LENGTH(N'dbo.FiscalHttpExchange',N'DocumentId') IS NULL
        OR OBJECT_ID(N'dbo.CK_FiscalDocumentLease_DocumentKind',N'C') IS NULL
        OR OBJECT_ID(N'dbo.CK_FiscalAttempt_Family',N'C') IS NULL
        OR NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.MyDieselCouponCase') AND name=N'UX_MyDieselCouponCase_InvoiceBlocking' AND is_disabled=0)
        OR NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.MyDieselCouponCase') AND name=N'UX_MyDieselCouponCase_CodeBlocking' AND is_disabled=0)
        RAISERROR('myDiesel schema is incomplete or incompatible.',16,1);
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    DECLARE @message nvarchar(2048)=ERROR_MESSAGE();
    RAISERROR('%s',16,1,@message);
END CATCH;

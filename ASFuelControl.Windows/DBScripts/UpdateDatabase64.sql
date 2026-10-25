SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.MyDieselCouponCase',N'U') IS NULL
        RAISERROR('myDiesel coupon storage is missing.',16,1);

    IF COL_LENGTH(N'dbo.MyDieselCouponCase',N'ExciseDiscountPerLitre') IS NULL
        ALTER TABLE dbo.MyDieselCouponCase ADD ExciseDiscountPerLitre decimal(18,4) NOT NULL
            CONSTRAINT DF_MyDieselCouponCase_ExciseDiscountPerLitre DEFAULT(0.41) WITH VALUES;

    -- This statement must be compiled after the column has been added. SQL Server
    -- otherwise resolves the CHECK expression while compiling the whole batch and
    -- reports "Invalid column name 'ExciseDiscountPerLitre'" on older databases.
    IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.MyDieselCouponCase') AND name=N'CK_MyDieselCouponCase_ExciseRate')
        EXEC(N'ALTER TABLE dbo.MyDieselCouponCase WITH CHECK ADD CONSTRAINT CK_MyDieselCouponCase_ExciseRate
            CHECK(ExciseDiscountPerLitre>0 AND ExciseDiscountPerLitre<=10);');

    IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.MyDieselCouponCase') AND name=N'CK_MyDieselCouponCase_Stage')
        ALTER TABLE dbo.MyDieselCouponCase DROP CONSTRAINT CK_MyDieselCouponCase_Stage;

    ALTER TABLE dbo.MyDieselCouponCase WITH CHECK ADD CONSTRAINT CK_MyDieselCouponCase_Stage
        CHECK(LocalStage IN ('Captured','ReserveUnknown','Reserved','Released','InvoiceAccepted','RedeemUnknown','Redeemed','VerificationPending','Final','InterventionRequired','Dismissed'));

    IF COL_LENGTH(N'dbo.MyDieselCouponCase',N'ExciseDiscountPerLitre') IS NULL
        RAISERROR('myDiesel excise-discount migration did not complete.',16,1);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    DECLARE @message nvarchar(2048)=ERROR_MESSAGE();
    RAISERROR('%s',16,1,@message);
END CATCH;

SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.MyDieselCouponCase',N'U') IS NULL
        RAISERROR('myDiesel coupon storage is missing.',16,1);

    IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.MyDieselCouponCase') AND name=N'CK_MyDieselCouponCase_Stage')
        ALTER TABLE dbo.MyDieselCouponCase DROP CONSTRAINT CK_MyDieselCouponCase_Stage;

    ALTER TABLE dbo.MyDieselCouponCase WITH CHECK ADD CONSTRAINT CK_MyDieselCouponCase_Stage
        CHECK(LocalStage IN ('Captured','ReserveUnknown','Reserved','Released','InvoiceAccepted','RedeemUnknown','Redeemed','VerificationPending','Final','InterventionRequired','Dismissed','ReassignmentAvailable','ReassignmentPending'));

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    DECLARE @message nvarchar(2048)=ERROR_MESSAGE();
    RAISERROR('%s',16,1,@message);
END CATCH;

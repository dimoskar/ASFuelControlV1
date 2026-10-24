IF OBJECT_ID(N'dbo.MyDieselCouponCase',N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.MyDieselCouponCase',N'ExciseDiscountPerLitre') IS NULL
        ALTER TABLE dbo.MyDieselCouponCase ADD ExciseDiscountPerLitre decimal(18,4) NOT NULL
            CONSTRAINT DF_MyDieselCouponCase_ExciseDiscountPerLitre DEFAULT(0.41) WITH VALUES;

    IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.MyDieselCouponCase') AND name=N'CK_MyDieselCouponCase_ExciseRate')
        ALTER TABLE dbo.MyDieselCouponCase WITH CHECK ADD CONSTRAINT CK_MyDieselCouponCase_ExciseRate
            CHECK(ExciseDiscountPerLitre>0 AND ExciseDiscountPerLitre<=10);

    IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.MyDieselCouponCase') AND name=N'CK_MyDieselCouponCase_Stage')
        ALTER TABLE dbo.MyDieselCouponCase DROP CONSTRAINT CK_MyDieselCouponCase_Stage;

    ALTER TABLE dbo.MyDieselCouponCase WITH CHECK ADD CONSTRAINT CK_MyDieselCouponCase_Stage
        CHECK(LocalStage IN ('Captured','ReserveUnknown','Reserved','Released','InvoiceAccepted','RedeemUnknown','Redeemed','VerificationPending','Final','InterventionRequired','Dismissed'));
END

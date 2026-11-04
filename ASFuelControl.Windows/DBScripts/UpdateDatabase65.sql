SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.MyDieselCouponCase',N'U') IS NULL
        RAISERROR('myDiesel coupon storage is missing.',16,1);

    -- Existing cases predate the live UAT requirement and remain nullable so a
    -- rejected case can still be dismissed. Every newly captured case supplies
    -- the value frozen from the general AMDIKA option.
    IF COL_LENGTH(N'dbo.MyDieselCouponCase',N'ReservedStationAmdika') IS NULL
        ALTER TABLE dbo.MyDieselCouponCase ADD ReservedStationAmdika nvarchar(128) NULL;

    IF COL_LENGTH(N'dbo.MyDieselCouponCase',N'CurrentStationAmdika') IS NULL
        ALTER TABLE dbo.MyDieselCouponCase ADD CurrentStationAmdika nvarchar(128) NULL;

    IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.MyDieselCouponCase') AND name=N'CK_MyDieselCouponCase_Amdika')
        EXEC(N'ALTER TABLE dbo.MyDieselCouponCase WITH CHECK ADD CONSTRAINT CK_MyDieselCouponCase_Amdika
            CHECK((ReservedStationAmdika IS NULL OR LEN(LTRIM(RTRIM(ReservedStationAmdika)))>0)
              AND (CurrentStationAmdika IS NULL OR LEN(LTRIM(RTRIM(CurrentStationAmdika)))>0));');

    IF COL_LENGTH(N'dbo.MyDieselCouponCase',N'ReservedStationAmdika') IS NULL
        OR COL_LENGTH(N'dbo.MyDieselCouponCase',N'CurrentStationAmdika') IS NULL
        RAISERROR('myDiesel AMDIKA migration did not complete.',16,1);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    DECLARE @message nvarchar(2048)=ERROR_MESSAGE();
    RAISERROR('%s',16,1,@message);
END CATCH;

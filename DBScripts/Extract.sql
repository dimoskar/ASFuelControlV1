SELECT        Dispenser.PumpSerialNumber, Nozzle.SerialNumber, SalesTransaction.TotalizerStart, SalesTransaction.TotalizerEnd, FORMAT(SalesTransaction.TransactionTimeStamp, 'dd/MM/yyyy HH:mm:ss'), SalesTransaction.Volume, SalesTransaction.VolumeNormalized, 
                         SalesTransaction.TemperatureStart, SalesTransaction.TemperatureEnd, SalesTransaction.UnitPrice, SalesTransaction.TotalPrice, FORMAT(SentDateTime, 'dd/MM/yyyy HH:mm:ss') 
FROM            SalesTransaction INNER JOIN
                         Nozzle ON SalesTransaction.NozzleId = Nozzle.NozzleId INNER JOIN
                         Dispenser ON Nozzle.DispenserId = Dispenser.DispenserId
WHERE CAST(TransactionTimeStamp AS date) IN
(
    '20250909',
    '20260219',
    '20260228',
    '20260226',
    '20260401'
)
or(DATEDIFF(day, '2026/05/20', TransactionTimeStamp) >=0 and DATEDIFF(day, '2026/05/26', TransactionTimeStamp) <=0)
ORDER BY SalesTransaction.TransactionTimeStamp, SalesTransaction.NozzleId, SalesTransaction.TotalizerStart


SELECT        Tank.TankSerialNumber, Tank.TankNumber, FORMAT(TankFilling.TransactionTime, 'dd/MM/yyyy HH:mm:ss'), FORMAT(TankFilling.TransactionTimeEnd, 'dd/MM/yyyy HH:mm:ss'), TankFilling.LevelStart, TankFilling.LevelEnd, TankFilling.Volume, TankFilling.VolumeNormalized, 
                         TankFilling.VolumeReal, TankFilling.VolumeRealNormalized, TankFilling.TankTemperatureStart, TankFilling.TankTemperatureEnd, TankFilling.FuelDensity, FORMAT(TankFilling.SentDateTime, 'dd/MM/yyyy HH:mm:ss')
FROM            TankFilling INNER JOIN
                         Tank ON TankFilling.TankId = Tank.TankId
WHERE CAST(TransactionTime AS date) IN
(
    '20250909',
    '20260219',
    '20260228',
    '20260226',
    '20260401'
)
or(DATEDIFF(day, '2026/05/20', TransactionTime) >=0 and DATEDIFF(day, '2026/05/26', TransactionTime) <=0)
order by TransactionTime, TankNumber

SELECT Message, FORMAT(EventDate, 'dd/MM/yyyy HH:mm:ss'), FORMAT(SentDate, 'dd/MM/yyyy HH:mm:ss'), ResolveMessage, AlarmType, FORMAT(ResolvedDate, 'dd/MM/yyyy HH:mm:ss')
FROM            SystemEvent
WHERE CAST(EventDate AS date) IN
(
    '20250909',
    '20260219',
    '20260228',
    '20260226',
    '20260401'
)
or(DATEDIFF(day, '2026/05/20', EventDate) >=0 and DATEDIFF(day, '2026/05/26', EventDate) <=0)
order by EventDate

SELECT        FORMAT(SendDate, 'dd/MM/yyyy HH:mm:ss'), FORMAT(LastSent, 'dd/MM/yyyy HH:mm:ss'), Action, SendData, SentStatus
FROM            SendLog
WHERE CAST(SendDate AS date) IN
(
    '20250909',
    '20260219',
    '20260228',
    '20260226',
    '20260401'
)
or(DATEDIFF(day, '2026/05/20', SendDate) >=0 and DATEDIFF(day, '2026/05/26', SendDate) <=0)
order by SendDate

SELECT Tank.TankSerialNumber, Tank.TankNumber, FORMAT(TankCheck.CheckDate, 'dd/MM/yyyy HH:mm:ss'), TankCheck.TankLevel, TankCheck.Temperature, FORMAT(SentDatetime, 'dd/MM/yyyy HH:mm:ss')
FROM            TankCheck INNER JOIN
                         Tank ON TankCheck.TankId = Tank.TankId
WHERE CAST(CheckDate AS date) IN
(
    '20250909',
    '20260219',
    '20260228',
    '20260226',
    '20260401'
)
or(DATEDIFF(day, '2026/05/20', CheckDate) >=0 and DATEDIFF(day, '2026/05/26', CheckDate) <=0)
order by CheckDate, Tank.TankNumber

CREATE OR ALTER FUNCTION dbo.GetVolume
(
    @Level1 decimal(18, 6),
    @Level2 decimal(18, 6),
    @TankId uniqueidentifier
)
RETURNS decimal(18, 3)
AS
BEGIN
    DECLARE @Volume1 decimal(38, 10);
    DECLARE @Volume2 decimal(38, 10);

    IF @Level1 IS NULL OR @Level2 IS NULL OR @TankId IS NULL
        RETURN NULL;

    IF @Level1 = @Level2
        RETURN 0;

    ;WITH LevelsToCalculate AS
    (
        SELECT 1 AS LevelNo, @Level1 AS LevelValue
        UNION ALL
        SELECT 2 AS LevelNo, @Level2 AS LevelValue
    ),
    EstimatedVolumes AS
    (
        SELECT
            l.LevelNo,

            EstimatedVolume =
                CASE
                    -- No calibration rows found
                    WHEN lo.Height IS NULL AND hi.Height IS NULL THEN NULL

                    -- Level is below the lowest titrimetry height
                    WHEN lo.Height IS NULL THEN NULL

                    -- Level is above the highest titrimetry height
                    WHEN hi.Height IS NULL THEN NULL

                    -- Exact height match
                    WHEN lo.Height = hi.Height THEN lo.Volume

                    -- Linear interpolation
                    ELSE
                        lo.Volume +
                        (
                            (l.LevelValue - lo.Height)
                            *
                            (hi.Volume - lo.Volume)
                            /
                            NULLIF(hi.Height - lo.Height, 0)
                        )
                END
        FROM LevelsToCalculate l

        OUTER APPLY
        (
            SELECT TOP (1)
                CAST(tl.Height AS decimal(38, 10)) AS Height,
                CAST(tl.Volume AS decimal(38, 10)) AS Volume
            FROM dbo.TitrimetryLevel tl
            INNER JOIN dbo.Titrimetry tr
                ON tl.TitrimetryId = tr.TitrimetryId
            WHERE
                tr.TankId = @TankId
                AND tl.Height <= l.LevelValue
            ORDER BY
                tl.Height DESC
        ) lo

        OUTER APPLY
        (
            SELECT TOP (1)
                CAST(tl.Height AS decimal(38, 10)) AS Height,
                CAST(tl.Volume AS decimal(38, 10)) AS Volume
            FROM dbo.TitrimetryLevel tl
            INNER JOIN dbo.Titrimetry tr
                ON tl.TitrimetryId = tr.TitrimetryId
            WHERE
                tr.TankId = @TankId
                AND tl.Height >= l.LevelValue
            ORDER BY
                tl.Height ASC
        ) hi
    )
    SELECT
        @Volume1 = MAX(CASE WHEN LevelNo = 1 THEN EstimatedVolume END),
        @Volume2 = MAX(CASE WHEN LevelNo = 2 THEN EstimatedVolume END)
    FROM EstimatedVolumes;

    IF @Volume1 IS NULL OR @Volume2 IS NULL
        RETURN NULL;

    RETURN CAST(@Volume2 - @Volume1 AS decimal(18, 3));
END;
GO

WITH TankRows AS
(
    SELECT
        t.TankSerialNumber,
        t.TankNumber,
        tc.CheckDate,
        tc.TankLevel,
        tc.Temperature,
        tc.SentDatetime,
        tc.TankId,

        LAG(tc.TankLevel) OVER
        (
            PARTITION BY tc.TankId
            ORDER BY tc.CheckDate
        ) AS PreviousTankLevel
    FROM TankCheck tc
    INNER JOIN Tank t
        ON tc.TankId = t.TankId
    WHERE
        CAST(tc.CheckDate AS date) IN
        (
            '20260317',
            '20260403',
            '20260318',
            '20260321',
            '20260401'
        )
        OR
        (
            tc.CheckDate >= '20260331'
            AND tc.CheckDate <  '20260412'
        )
)
SELECT
    TankSerialNumber,
    TankNumber,
    FORMAT(CheckDate, 'dd/MM/yyyy HH:mm:ss') AS CheckDate,
    TankLevel,
    Temperature,
    FORMAT(SentDatetime, 'dd/MM/yyyy HH:mm:ss') AS SentDatetime,

    CASE
        WHEN PreviousTankLevel IS NULL THEN NULL
        ELSE dbo.GetVolume(PreviousTankLevel, TankLevel, TankId)
    END AS DifferenceLiters
FROM TankRows
ORDER BY
    TankNumber, CheckDate;

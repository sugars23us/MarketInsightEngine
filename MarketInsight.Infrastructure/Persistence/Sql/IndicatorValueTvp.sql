-- Idempotent create for the TVP type used to batch indicator upserts.
IF TYPE_ID(N'dbo.IndicatorValueTvp') IS NULL
BEGIN
    EXEC('CREATE TYPE dbo.IndicatorValueTvp AS TABLE
    (
        StockId       int           NOT NULL,
        TimeframeId   smallint      NOT NULL,
        TsUtc         datetime2(0)  NOT NULL,
        MetricCode    nvarchar(32)  NOT NULL,
        Period        smallint      NOT NULL,
        ParamsJson    nvarchar(max) NULL,
        Value         decimal(19,8) NOT NULL
    )');
END
ELSE
BEGIN
    -- NOTE: If schema changes are required, drop & recreate manually:
    --   DROP PROCEDURE IF EXISTS dbo.UpsertIndicatorValues;
    --   DROP TYPE dbo.IndicatorValueTvp;
    --   (then re-run this script and the procedure script)
END

USE [MarketInsight]
GO

CREATE OR ALTER PROCEDURE dbo.UpsertEquityIndicators
    @tvp dbo.EquityIndicatorTvp READONLY
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dbo.EQUITY_INDICATOR AS target
    USING @tvp AS source
        ON target.EquityId     = source.EquityId
       AND target.TimeframeId  = source.TimeframeId
       AND target.TsUtc        = source.TsUtc          -- ← NOW PART OF PK
       AND target.MetricCode   = source.MetricCode
       AND target.Period       = source.Period
    WHEN MATCHED THEN
        UPDATE SET
            Value       = source.Value,
            ParamsJson  = source.ParamsJson
            -- TsUtc is part of PK → cannot change it
            -- TradingDate is computed automatically → no need to touch
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (EquityId, TimeframeId, TsUtc, MetricCode, Period, Value, ParamsJson)
        VALUES (source.EquityId, source.TimeframeId, source.TsUtc, source.MetricCode, source.Period, source.Value, source.ParamsJson);
            -- TradingDate is computed from TsUtc → not in INSERT
END
GO
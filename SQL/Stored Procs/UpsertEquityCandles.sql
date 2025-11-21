USE [MarketInsight]
GO

CREATE OR ALTER PROCEDURE dbo.UpsertEquityCandles
    @tvp dbo.EquityCandleTvp READONLY
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dbo.EQUITY_CANDLE AS target
    USING @tvp AS source
        ON target.EquityId = source.EquityId
       AND target.TimeframeId = source.TimeframeId
       AND target.TsUtc = source.TsUtc
    WHEN MATCHED THEN
        UPDATE SET
            [Open] = source.[Open],
            High = source.High,
            Low = source.Low,
            [Close] = source.[Close],
            Volume = source.Volume,
            Vwap = source.Vwap,
            Ats = source.Ats
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (EquityId, TimeframeId, TsUtc, [Open], High, Low, [Close], Volume, Vwap, Ats)
        VALUES (source.EquityId, source.TimeframeId, source.TsUtc, source.[Open], source.High, source.Low, source.[Close], source.Volume, source.Vwap, source.Ats);
END
GO




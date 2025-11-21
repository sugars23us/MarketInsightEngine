-- 1. Latest 1-minute candle per equity (real-time dashboard)
CREATE OR ALTER VIEW dbo.v_LatestEquityCandle_1m AS
SELECT 
    c.EquityId,
    e.Ticker,
    c.TsUtc,
    c.[Open],
    c.High,
    c.Low,
    c.[Close],
    c.Volume,
    c.Vwap,          -- session VWAP ("a" field)
    c.Ats            -- average trade size ("z" field)
FROM dbo.EQUITY_CANDLE c
INNER JOIN dbo.EQUITY e ON c.EquityId = e.EquityId
WHERE c.TimeframeId = 1
  AND c.TsUtc = (
    SELECT MAX(TsUtc) 
    FROM dbo.EQUITY_CANDLE 
    WHERE EquityId = c.EquityId AND TimeframeId = 1
  );
GO
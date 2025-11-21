-- 4. Top 20 biggest ATS movers today (institutional hunting)
CREATE OR ALTER VIEW dbo.v_TopAtsToday AS
SELECT TOP 20
    e.Ticker,
    c.Ats,
    c.Volume,
    c.Vwap,
    c.[Close],
    c.TsUtc
FROM dbo.EQUITY_CANDLE c
INNER JOIN dbo.EQUITY e ON c.EquityId = e.EquityId
WHERE c.TimeframeId = 1
  AND c.TradingDate = CAST(GETDATE() AS DATE)
  AND c.Ats IS NOT NULL
ORDER BY c.Ats DESC;
GO
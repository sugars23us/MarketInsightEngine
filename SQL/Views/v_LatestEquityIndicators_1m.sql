-- 2. Latest indicators per equity (1-minute timeframe)
CREATE OR ALTER VIEW dbo.v_LatestEquityIndicators_1m AS
SELECT 
    i.EquityId,
    e.Ticker,
    i.MetricCode,
    i.Period,
    i.Value,
    i.TsUtc
FROM dbo.EQUITY_INDICATOR i
INNER JOIN dbo.EQUITY e ON i.EquityId = e.EquityId
WHERE i.TimeframeId = 1
  AND i.TsUtc = (
    SELECT MAX(TsUtc)
    FROM dbo.EQUITY_INDICATOR i2
    WHERE i2.EquityId = i.EquityId
      AND i2.TimeframeId = 1
      AND i2.MetricCode = i.MetricCode
      AND i2.Period = i.Period
  );
GO

-- ATS (Average Trade Size)

DECLARE @ticker nvarchar(16) = 'TSLA';
DECLARE @tf tinyint = 1; -- 1-minute bars

WITH s AS (
  SELECT StockId FROM dbo.STOCK WHERE Ticker = @ticker
),
t AS (
  SELECT b.TsUtc, b.Volume, b.TradeCount
  FROM dbo.BAR b
  JOIN s ON s.StockId = b.StockId
  WHERE b.TimeframeId = @tf
   -- AND b.TradingDate = CONVERT(date, SYSUTCDATETIME())
)
SELECT
  TsUtc,
  Volume,
  TradeCount,
  CASE WHEN TradeCount > 0 THEN 1.0 * Volume / TradeCount ELSE NULL END AS AvgTradeSize
FROM t
ORDER BY TsUtc;








SELECT TOP (1000) [StockId]
      ,[TimeframeId]
      ,[TsUtc]
      ,[Open]
      ,[High]
      ,[Low]
      ,[Close]
      ,[Volume]
      ,[Vwap]
      ,[TradeCount]
      ,[TradingDate]
      ,[InsertedUtc]
  FROM [MarketInsight].[dbo].[BAR]
  where InsertedUtc > '2025-10-29 21:31:01'
  and stockid = 1
  order by InsertedUtc desc 







SELECT TOP (1000)
    StockId,
    TimeframeId,
    TsUtc,
    [Open],
    [High],
    [Low],
    [Close],
    [Volume],
    [Vwap],
    [TradeCount],
    [TradingDate],
    [InsertedUtc],

    -- Average shares per trade
    CASE 
        WHEN TradeCount > 0 THEN CAST(Volume AS decimal(19,4)) / TradeCount
        ELSE NULL
    END AS SharesPerTrade,

    -- Dollar value of average trade (shares per trade × closing price)
    CASE 
        WHEN TradeCount > 0 THEN (CAST(Volume AS decimal(19,4)) / TradeCount) * [Close]
        ELSE NULL
    END AS AvgTradeDollarValue,

    -- Optional: total traded dollar volume for that bar
    CAST(Volume AS decimal(19,4)) * [Close] AS TotalDollarVolume

FROM dbo.BAR
WHERE StockId = 1
ORDER BY TsUtc DESC;

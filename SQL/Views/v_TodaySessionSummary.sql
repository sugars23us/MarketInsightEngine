-- 3. Today's session summary — your main watchlist
CREATE OR ALTER VIEW dbo.v_TodaySessionSummary AS
SELECT 
    e.Ticker,
    c_latest.[Close],
    c_latest.Volume AS VolumeToday,
    c_latest.Vwap,
    c_latest.Ats,
    ROUND(((c_latest.[Close] - c_latest.Vwap) / NULLIF(c_latest.Vwap, 0)) * 100, 2) AS VwapDeviationPct,
    i_sma20.Value AS SMA_20,
    i_sma200.Value AS SMA_200,
    i_rsi.Value AS RSI_14,
    i_ats_z15.Value AS ATS_Z_15,
    i_ats_z60.Value AS ATS_Z_60
FROM dbo.EQUITY e
LEFT JOIN dbo.v_LatestEquityCandle_1m c_latest ON e.EquityId = c_latest.EquityId
LEFT JOIN dbo.v_LatestEquityIndicators_1m i_sma20 
    ON e.EquityId = i_sma20.EquityId AND i_sma20.MetricCode = 'SMA' AND i_sma20.Period = 20
LEFT JOIN dbo.v_LatestEquityIndicators_1m i_sma200 
    ON e.EquityId = i_sma200.EquityId AND i_sma200.MetricCode = 'SMA' AND i_sma200.Period = 200
LEFT JOIN dbo.v_LatestEquityIndicators_1m i_rsi 
    ON e.EquityId = i_rsi.EquityId AND i_rsi.MetricCode = 'RSI' AND i_rsi.Period = 14
LEFT JOIN dbo.v_LatestEquityIndicators_1m i_ats_z15 
    ON e.EquityId = i_ats_z15.EquityId AND i_ats_z15.MetricCode = 'ATS_Z_15' AND i_ats_z15.Period = 15
LEFT JOIN dbo.v_LatestEquityIndicators_1m i_ats_z60 
    ON e.EquityId = i_ats_z60.EquityId AND i_ats_z60.MetricCode = 'ATS_Z_60' AND i_ats_z60.Period = 60
WHERE c_latest.TsUtc >= CAST(GETDATE() AS DATE);
GO
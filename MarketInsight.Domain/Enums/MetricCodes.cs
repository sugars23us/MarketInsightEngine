namespace MarketInsight.Domain.Enums;

/// <summary>
/// Canonical metric code names stored in dbo.INDICATOR_VALUE.MetricCode.
/// Keep these in sync with your calculators and backfills.
/// </summary>
public static class MetricCodes
{
    // ATS family
    public const string ATS = "ATS";         // instantaneous avg trade size (period = 0)
    public const string ATS_MA_15 = "ATS_MA_15";   // rolling mean(ATS, 15)
    public const string ATS_MA_60 = "ATS_MA_60";   // rolling mean(ATS, 60)
    public const string ATS_Z_15 = "ATS_Z_15";    // z-score(ATS, 15)
    public const string ATS_Z_60 = "ATS_Z_60";    // z-score(ATS, 60)

    // Institutional Flow (if used)
    public const string IFI_60 = "IFI_60";      // institutional flow index, 60-bar

    // Flow/exhaustion pack
    public const string R = "R";           // float rotations (cumVol/float), period=0
    public const string RVOL_63 = "RVOL_63";     // relative volume vs 63d ADV, period=63
    public const string TDR_2 = "TDR_2";       // turnover days ratio (2 sessions)
    public const string TDR_5 = "TDR_5";       // turnover days ratio (5 sessions)
    public const string VWAP = "VWAP";        // optional mirror of bar VWAP
    public const string VWAP_DEV = "VWAP_DEV";    // (Close - VWAP)/VWAP
    public const string EFF = "EFF";         // efficiency = ΔP% / rotations
    public const string BACKSIDE = "BACKSIDE";    // 0/1

    // Momentum pack
    public const string RSI_14 = "RSI_14";
    public const string OBV_D = "OBV_D";       // intraday/session OBV
    public const string CVD_1M = "CVD_1M";      // minute-level CVD proxy

    // Add new codes here and keep DB scripts aligned.
}

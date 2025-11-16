
namespace MarketInsight.Shared.Constants
{
    /// <summary>
    /// Central place for metric codes to avoid typos.
    /// Prefer composing names with periods when needed, but the
    /// project currently uses snake-style names to match DB rows.
    /// </summary>
    public static class MetricNames
    {
        // Average Trade Size family
        public const string ATS = "ATS";
        public const string ATS_MA_15 = "ATS_MA_15";
        public const string ATS_MA_60 = "ATS_MA_60";
        public const string ATS_Z_15  = "ATS_Z_15";
        public const string ATS_Z_60  = "ATS_Z_60";

        // Institutional Flow Index (signed)
        public const string IFI_60 = "IFI_60";

        // VWAP (bar-level exists in BAR; keep code for derived uses)
        public const string VWAP = "VWAP";

        // Relative Volume
        public const string RVOL = "RVOL";

        // Float rotations
        public const string FLOAT_ROT = "FLOAT_ROT";

        // Efficiency metrics
        public const string EFF_15 = "EFF_15";
        public const string EFF_60 = "EFF_60";

        /// <summary>Helper to build "NAME_PERIOD" format.</summary>
        public static string WithPeriod(string baseCode, int period)
            => $"{baseCode}_{period}";
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketInsight.Ingestor.Ingestion
{
    /// Per-stock rolling windows used by the ingestion loop.
    /// - Ma15.Mean            -> ATS_MA_15
    /// - Stats15.Mean/Std     -> ATS_Z_15
    /// - Stats60.Mean/Std     -> ATS_MA_60 & ATS_Z_60
    /// - Vol60.Mean/Std       -> Volume Z-60 (for IFI)
    /// - Dev60.Mean/Std       -> VWAP deviation Z-60 (for IFI)
    public sealed class AtsWindows
    {
        public RollingWindow Ma15 { get; } = new(15);  // ATS_MA_15
        public RollingWindow Stats15 { get; } = new(15);  // ATS_Z_15
        public RollingWindow Stats60 { get; } = new(60);  // ATS_MA_60 + ATS_Z_60
        public RollingWindow Vol60 { get; } = new(60);  // Volume Z-60 (IFI)
        public RollingWindow Dev60 { get; } = new(60);  // VWAP deviation Z-60 (IFI)
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketInsight.Ingestor.AppOptions
{
    public sealed class IngestionOptions
    {
        public byte TimeframeIdMinute { get; init; } = 1;
        public int BatchSize { get; init; } = 500;
        public int FlushSeconds { get; init; } = 1;
        public int ReconnectBaseDelayMs { get; init; } = 1000;
        public int ReconnectMaxDelayMs { get; init; } = 30000;
    }
}

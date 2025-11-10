using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketInsight.Ingestor.AppOptions
{
    public sealed class PolygonOptions
    {
        public string WebSocketUrl { get; init; } = "";
        public string ApiKey { get; init; } = "";
        // e.g. "AM.AAPL,AM.MSFT"
        public string Subscribe { get; init; } = "AM.AAPL";
    }
}

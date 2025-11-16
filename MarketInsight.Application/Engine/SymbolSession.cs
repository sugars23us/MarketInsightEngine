
using System;
using System.Collections.Generic;

namespace MarketInsight.Application.Engine
{
    /// <summary>
    /// Per-symbol, per-timeframe, per-session mutable state used by calculators.
    /// </summary>
    public sealed class SymbolSession
    {
        public SymbolSession(int stockId, byte timeframeId, DateOnly sessionDate)
        {
            StockId = stockId;
            TimeframeId = timeframeId;
            SessionDate = sessionDate;
            State = new Dictionary<string, object>();
        }

        public int StockId { get; }
        public byte TimeframeId { get; }
        public DateOnly SessionDate { get; private set; }

        /// <summary>
        /// Arbitrary per-session state used by calculators and orchestrator.
        /// Example keys: "ATS", "FLOW", "MOMENTUM", "META".
        /// </summary>
        public IDictionary<string, object> State { get; }

        public void Reset(DateOnly newSessionDate)
        {
            SessionDate = newSessionDate;
            State.Clear();
        }
    }
}

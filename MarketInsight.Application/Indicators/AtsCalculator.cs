// MarketInsight.Application/Indicators/AtsCalculator.cs
using System;
using MarketInsight.Application.Engine;
using MarketInsight.Application.Interfaces;
using MarketInsight.Shared.Utils;

namespace MarketInsight.Application.Indicators
{
    /// <summary>
    /// Computes ATS moving averages and Z-scores.
    /// ATS is received directly from Polygon's "z" field — no calculation needed.
    /// </summary>
    public sealed class AtsCalculator : IIndicatorCalculator
    {
        private sealed class State
        {
            public RollingWindow Ma15 { get; } = new(15);
            public RollingWindow Stats15 { get; } = new(15);
            public RollingWindow Stats60 { get; } = new(60);
        }

        private static State GetState(SymbolSession session)
        {
            const string key = "ATS";
            if (!session.State.TryGetValue(key, out var obj) || obj is not State state)
            {
                state = new State();
                session.State[key] = state;
            }
            return state;
        }

        public void OnSessionStarted(SymbolSession session)
        {
            session.State["ATS"] = new State();
        }

        public void OnCandle (in EquityCandle candle, SymbolSession session, IndicatorWriter writer)
        {
            // Polygon already gives us exact ATS in the "z" field
            if (!candle.Ats.HasValue || candle.Ats <= 0)
                return;

            var ats = candle.Ats.Value;
            var state = GetState(session);

            double atsDouble = ats;

            state.Ma15.Add(atsDouble);
            state.Stats15.Add(atsDouble);
            state.Stats60.Add(atsDouble);

            // Raw ATS value
            writer.Add(candle, "ATS", 0, (decimal)ats);

            // Moving averages
            if (state.Ma15.Count >= 15)
            {
                var ma15 = (decimal)state.Ma15.Mean;
                writer.Add(candle, "ATS_MA_15", 15, ma15);
            }

            if (state.Stats60.Count >= 60)
            {
                var ma60 = (decimal)state.Stats60.Mean;
                writer.Add(candle, "ATS_MA_60", 60, ma60);
            }

            // Z-scores
            if (state.Stats15.Count >= 2)
            {
                var z15 = ComputeZ(atsDouble, state.Stats15);
                if (!double.IsNaN(z15))
                    writer.Add(candle, "ATS_Z_15", 15, (decimal)z15);
            }

            if (state.Stats60.Count >= 2)
            {
                var z60 = ComputeZ(atsDouble, state.Stats60);
                if (!double.IsNaN(z60))
                    writer.Add(candle, "ATS_Z_60", 60, (decimal)z60);
            }
        }

        private static double ComputeZ(double x, RollingWindow w)
        {
            var mean = w.Mean;
            var std = w.StdSample;

            if (double.IsNaN(mean) || double.IsNaN(std) || std <= 0.0001)
                return 0.0;

            return (x - mean) / std;
        }
    }
}
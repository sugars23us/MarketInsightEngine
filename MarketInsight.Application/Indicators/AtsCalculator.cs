using System;
using MarketInsight.Application.Engine;
using MarketInsight.Shared.Utils;

namespace MarketInsight.Application.Indicators
{
    /// <summary>
    /// Computes ATS, ATS_MA_15, ATS_MA_60, ATS_Z_15, ATS_Z_60.
    /// Uses O(1) rolling windows per symbol per session.
    /// </summary>
    public sealed class AtsCalculator : IIndicatorCalculator
    {
        private sealed class State
        {
            public RollingWindow Ma15 = new(15);
            public RollingWindow Stats15 = new(15);
            public RollingWindow Stats60 = new(60);
        }

        private static State GetState(SymbolSession session)
        {
            const string key = "ATS";
            if (!session.State.TryGetValue(key, out var obj) || obj is not State st)
            {
                st = new State();
                session.State[key] = st;
            }
            return st;
        }

        public void OnSessionStarted(SymbolSession session)
        {
            session.State["ATS"] = new State();
        }

        public void OnBar(in MarketBar bar, SymbolSession session, IndicatorWriter writer)
        {
            var state = GetState(session);

            if (bar.TradeCount is null or <= 0)
                return;

            var ats = (decimal)bar.Volume / bar.TradeCount.Value;

            state.Ma15.Add((double)ats);
            state.Stats15.Add((double)ats);
            state.Stats60.Add((double)ats);

            decimal ma15 = ToDecimal(state.Ma15.Mean);
            decimal ma60 = ToDecimal(state.Stats60.Mean);
            decimal z15 = ComputeZ(ats, state.Stats15);
            decimal z60 = ComputeZ(ats, state.Stats60);

            writer.Add(bar, "ATS", 0, ats);
            if (ma15 != 0m) writer.Add(bar, "ATS_MA_15", 15, ma15);
            if (ma60 != 0m) writer.Add(bar, "ATS_MA_60", 60, ma60);
            if (z15 != 0m) writer.Add(bar, "ATS_Z_15", 15, z15);
            if (z60 != 0m) writer.Add(bar, "ATS_Z_60", 60, z60);
        }

        private static decimal ComputeZ(decimal x, RollingWindow w)
        {
            var mean = w.Mean;
            var std = w.StdSample;
            if (double.IsNaN(mean) || double.IsNaN(std) || std <= 0.0)
                return 0m;

            var m = (decimal)mean;
            var s = (decimal)std;
            return s == 0m ? 0m : (x - m) / s;
        }

        private static decimal ToDecimal(double d)
            => double.IsNaN(d) ? 0m : (decimal)d;
    }
}

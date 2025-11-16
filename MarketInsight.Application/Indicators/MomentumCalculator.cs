using System;
using MarketInsight.Application.Engine;

namespace MarketInsight.Application.Indicators
{
    /// <summary>
    /// Computes RSI_14, OBV_D, CVD_1M.
    /// </summary>
    public sealed class MomentumCalculator : IIndicatorCalculator
    {
        private sealed class State
        {
            public Rsi14Helper Rsi = new();
            public decimal PrevClose;
            public decimal Obv;
            public long Cvd;
        }

        private static State GetState(SymbolSession session)
        {
            const string key = "MOM";
            if (!session.State.TryGetValue(key, out var obj) || obj is not State st)
            {
                st = new State();
                session.State[key] = st;
            }
            return st;
        }

        public void OnSessionStarted(SymbolSession session)
        {
            session.State["MOM"] = new State();
        }

        public void OnBar(in MarketBar bar, SymbolSession session, IndicatorWriter writer)
        {
            var st = GetState(session);

            if (st.PrevClose == 0m)
            {
                st.PrevClose = bar.Close;
            }

            var delta = (double)(bar.Close - st.PrevClose);
            st.Rsi.AddDelta(delta);
            var rsi = st.Rsi.GetValue();

            if (bar.Close > st.PrevClose) st.Obv += bar.Volume;
            else if (bar.Close < st.PrevClose) st.Obv -= bar.Volume;

            if (bar.Close >= bar.Open) st.Cvd += bar.Volume;
            else st.Cvd -= bar.Volume;

            writer.Add(bar, "RSI_14", 14, (decimal)rsi);
            writer.Add(bar, "OBV_D", 0, st.Obv);
            writer.Add(bar, "CVD_1M", 1, st.Cvd);

            st.PrevClose = bar.Close;
        }
    }
}

using System;
using MarketInsight.Application.Engine;
using MarketInsight.Shared.DTOs;
using MarketInsight.Shared.Models;

namespace MarketInsight.Application.Indicators
{
    /// <summary>
    /// Computes VWAP_DEV, R (float rotations), RVOL_63, EFF, BACKSIDE.
    /// </summary>
    public sealed class FlowCalculator : IIndicatorCalculator
    {
        private readonly TimeSpan _noNewHighWindow = TimeSpan.FromMinutes(10);

        private sealed class State
        {
            public decimal SessionOpen;
            public decimal SessionHigh;
            public long CumVolume;
            public DateTime LastHodTsUtc;
            public decimal PeakRvol63;
        }

        public void OnSessionStarted(SymbolSession session)
        {
            session.State["FLOW"] = new State
            {
                SessionOpen = 0m,
                SessionHigh = 0m,
                CumVolume = 0L,
                LastHodTsUtc = DateTime.MinValue,
                PeakRvol63 = 0m
            };

            // META is populated by IngestionWorker before first bar; we don't touch it here.
        }

        private static State GetState(SymbolSession session)
        {
            if (!session.State.TryGetValue("FLOW", out var obj) || obj is not State st)
            {
                st = new State();
                session.State["FLOW"] = st;
            }
            return st;
        }

        private static TickerMeta? GetMeta(SymbolSession session)
        {
            session.State.TryGetValue("META", out var obj);
            return obj as TickerMeta;
        }

        public void OnBar(in Engine.MarketBar bar, SymbolSession session, IndicatorWriter writer)
        {
            var st = GetState(session);
            var meta = GetMeta(session);

            if (st.SessionOpen == 0m)
                st.SessionOpen = bar.Open;

            st.CumVolume += bar.Volume;

            if (bar.High > st.SessionHigh)
            {
                st.SessionHigh = bar.High;
                st.LastHodTsUtc = bar.TsUtc;
            }

            decimal vwapDev = 0m;
            if (bar.Vwap.HasValue && bar.Vwap.Value != 0m)
                vwapDev = (bar.Close - bar.Vwap.Value) / bar.Vwap.Value;

            decimal R = 0m;
            if (meta?.FreeFloatShares is > 0)
                R = (decimal)st.CumVolume / meta.FreeFloatShares.Value;

            decimal rvol63 = 0m;
            if (meta?.AvgDailyVolume3M is > 0)
                rvol63 = (decimal)st.CumVolume / meta.AvgDailyVolume3M.Value;

            decimal eff = 0m;
            if (st.SessionOpen != 0m && R > 0m)
            {
                var pctMove = (bar.Close / st.SessionOpen) - 1m;
                eff = pctMove / R;
            }

            bool noNewHigh = (bar.TsUtc - st.LastHodTsUtc) >= _noNewHighWindow;
            bool belowVwap = bar.Vwap.HasValue && bar.Close < bar.Vwap.Value;
            bool backside = (R >= 3m) && belowVwap && noNewHigh && (rvol63 < st.PeakRvol63);

            st.PeakRvol63 = Math.Max(st.PeakRvol63, rvol63);

            writer.Add(bar, "VWAP_DEV", 0, vwapDev);
            writer.Add(bar, "R", 0, R);
            writer.Add(bar, "RVOL_63", 63, rvol63);
            writer.Add(bar, "EFF", 0, eff);
            writer.Add(bar, "BACKSIDE", 0, backside ? 1m : 0m);
        }
    }
}

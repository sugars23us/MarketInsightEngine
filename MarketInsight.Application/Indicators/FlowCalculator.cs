// MarketInsight.Application/Indicators/FlowCalculator.cs
using System;
using MarketInsight.Application.Engine;
using MarketInsight.Application.Interfaces;
using MarketInsight.Shared.DTOs;

namespace MarketInsight.Application.Indicators
{
    /// <summary>
    /// Computes VWAP_DEV, R (float rotations), RVOL_63, EFF, BACKSIDE.
    /// Uses session VWAP ("a" field) and ATS ("z" field) from Polygon.
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
        }

        private static State GetState(SymbolSession session)
        {
            if (!session.State.TryGetValue("FLOW", out var obj) || obj is not State state)
            {
                state = new State();
                session.State["FLOW"] = state;
            }
            return state;
        }

        private static Equity? GetEquity(SymbolSession session)
        {
            session.State.TryGetValue("EQUITY", out var obj);
            return obj as Equity;
        }

        public void OnCandle(in EquityCandle candle, SymbolSession session, IndicatorWriter writer)
        {
            var state = GetState(session);
            var equity = GetEquity(session);

            // First bar of session — capture open
            if (state.SessionOpen == 0m)
                state.SessionOpen = candle.Open;

            state.CumVolume += candle.Volume;

            // Track session high
            if (candle.High > state.SessionHigh)
            {
                state.SessionHigh = candle.High;
                state.LastHodTsUtc = candle.TsUtc;
            }

            // VWAP_DEV — using session VWAP ("a" field)
            decimal vwapDev = 0m;
            if (candle.Vwap.HasValue && candle.Vwap.Value != 0m)
                vwapDev = (candle.Close - candle.Vwap.Value) / candle.Vwap.Value;

            // R — float rotations
            decimal r = 0m;
            if (equity?.FloatShares is > 0)
                r = (decimal)state.CumVolume / equity.FloatShares.Value;

            // RVOL_63 — relative volume
            decimal rvol63 = 0m;
            if (equity?.AvgVolume3M is > 0)
                rvol63 = (decimal)state.CumVolume / equity.AvgVolume3M.Value;

            // EFF — efficiency
            decimal eff = 0m;
            if (state.SessionOpen != 0m && r > 0m)
            {
                var pctMove = (candle.Close / state.SessionOpen) - 1m;
                eff = pctMove / r;
            }

            // Backside detection
            bool noNewHigh = (candle.TsUtc - state.LastHodTsUtc) >= _noNewHighWindow;
            bool belowVwap = candle.Vwap.HasValue && candle.Close < candle.Vwap.Value;
            bool backside = r >= 3m && belowVwap && noNewHigh && rvol63 < state.PeakRvol63;

            state.PeakRvol63 = Math.Max(state.PeakRvol63, rvol63);

            writer.Add(candle, "VWAP_DEV", 0, vwapDev);
            writer.Add(candle, "R", 0, r);
            writer.Add(candle, "RVOL_63", 63, rvol63);
            writer.Add(candle, "EFF", 0, eff);
            writer.Add(candle, "BACKSIDE", 0, backside ? 1m : 0m);
        }
    }
}
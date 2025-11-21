// MarketInsight.Application/Indicators/MomentumCalculator.cs
using System;
using MarketInsight.Application.Engine;
using MarketInsight.Application.Interfaces;

namespace MarketInsight.Application.Indicators
{
    /// <summary>
    /// Computes RSI_14, OBV_D (session), CVD_1M (cumulative volume delta per minute).
    /// </summary>
    public sealed class MomentumCalculator : IIndicatorCalculator
    {
        private sealed class State
        {
            public Rsi14Helper Rsi { get; } = new();
            public decimal PrevClose;
            public decimal Obv;     // On-Balance Volume (session cumulative)
            public long Cvd;        // Cumulative Volume Delta (signed by direction)
        }

        private static State GetState(SymbolSession session)
        {
            const string key = "MOM";
            if (!session.State.TryGetValue(key, out var obj) || obj is not State state)
            {
                state = new State();
                session.State[key] = state;
            }
            return state;
        }

        public void OnSessionStarted(SymbolSession session)
        {
            session.State["MOM"] = new State();
        }

        public void OnCandle(in EquityCandle candle, SymbolSession session, IndicatorWriter writer)
        {
            var state = GetState(session);

            // First candle of session — initialize
            if (state.PrevClose == 0m)
            {
                state.PrevClose = candle.Close;
                return; // RSI needs at least one delta
            }

            // RSI_14
            var delta = (double)(candle.Close - state.PrevClose);
            state.Rsi.AddDelta(delta);
            var rsi = state.Rsi.GetValue();

            // OBV_D — session On-Balance Volume
            if (candle.Close > state.PrevClose)
                state.Obv += candle.Volume;
            else if (candle.Close < state.PrevClose)
                state.Obv -= candle.Volume;
            // unchanged = no change

            // CVD_1M — Cumulative Volume Delta (signed by close vs open)
            if (candle.Close >= candle.Open)
                state.Cvd += candle.Volume;
            else
                state.Cvd -= candle.Volume;

            // Emit indicators
            writer.Add(candle, "RSI_14", 14, (decimal)rsi);
            writer.Add(candle, "OBV_D", 0, state.Obv);
            writer.Add(candle, "CVD_1M", 1, state.Cvd);

            // Update for next bar
            state.PrevClose = candle.Close;
        }
    }
}
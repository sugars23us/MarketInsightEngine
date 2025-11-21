using MarketInsight.Application.Engine;

namespace MarketInsight.Application.Interfaces
{
    /// <summary>
    /// Pluggable calculator: consumes candles, emits indicator values.
    /// Must be pure CPU (no I/O) and non-blocking.
    /// </summary>
    public interface IIndicatorCalculator
    {
        void OnSessionStarted(SymbolSession session);
        void OnCandle(in EquityCandle candle, SymbolSession session, IndicatorWriter writer);
    }
}

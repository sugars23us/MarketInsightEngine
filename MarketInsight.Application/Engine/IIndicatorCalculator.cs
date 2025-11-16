namespace MarketInsight.Application.Engine
{
    /// <summary>
    /// Pluggable calculator: consumes bars, emits indicator values.
    /// Must be pure CPU (no I/O) and non-blocking.
    /// </summary>
    public interface IIndicatorCalculator
    {
        void OnSessionStarted(SymbolSession session);
        void OnBar(in MarketBar bar, SymbolSession session, IndicatorWriter writer);
    }
}

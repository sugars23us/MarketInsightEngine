using System.Collections.Generic;

namespace MarketInsight.Application.Engine
{
    /// <summary>
    /// Buffer for collecting indicator values from calculators.
    /// </summary>
    public sealed class IndicatorWriter
    {
        private readonly List<IndicatorValue> _buffer = new();

        public int Count => _buffer.Count;

        public void Add(IndicatorValue value) => _buffer.Add(value);

        public void Add(in MarketBar bar, string metricCode, short period, decimal value, string? paramsJson = null)
        {
            _buffer.Add(new IndicatorValue(
                bar.StockId,
                bar.TimeframeId,
                bar.TsUtc,
                metricCode,
                period,
                value,
                paramsJson));
        }

        public IReadOnlyList<IndicatorValue> Flush()
        {
            var arr = _buffer.ToArray();
            _buffer.Clear();
            return arr;
        }
    }
}

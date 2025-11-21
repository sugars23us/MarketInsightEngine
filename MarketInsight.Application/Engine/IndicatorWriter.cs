// MarketInsight.Application/Engine/IndicatorWriter.cs
using System.Collections.Generic;

namespace MarketInsight.Application.Engine
{
    /// <summary>
    /// Buffer for collecting indicator values from calculators.
    /// </summary>
    public sealed class IndicatorWriter
    {
        private readonly List<EquityIndicator> _buffer = new();

        public int Count => _buffer.Count;

        public void Add(EquityIndicator indicator) => _buffer.Add(indicator);

        public void Add(in EquityCandle candle, string metricCode, short period, decimal value, string? paramsJson = null)
        {
            _buffer.Add(new EquityIndicator(
                EquityId: candle.EquityId,
                TimeframeId: candle.TimeframeId,
                TsUtc: candle.TsUtc,
                MetricCode: metricCode,
                Period: period,
                Value: value,
                ParamsJson: paramsJson));
        }

        public IReadOnlyList<EquityIndicator> Flush()
        {
            var result = _buffer.ToArray();
            _buffer.Clear();
            return result;
        }
    }
}
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MarketInsight.Application.Engine;

namespace MarketInsight.Application.Interfaces
{
    /// <summary>
    /// Writes EquityCandle records to persistent storage (e.g., SQL)
    /// </summary>
    public interface IEquityCandleSink
    {
        Task UpsertAsync(IReadOnlyCollection<EquityCandle> candles, CancellationToken ct = default);
    }
}
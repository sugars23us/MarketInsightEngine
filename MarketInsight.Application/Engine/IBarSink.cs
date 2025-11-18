using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarketInsight.Application.Engine
{
    /// <summary>
    /// Persists raw bars (OHLCV) to the BAR table.
    /// Implemented in Infrastructure (e.g. SqlBarWriter).
    /// </summary>
    public interface IBarSink
    {
        Task UpsertAsync(IReadOnlyCollection<MarketBar> bars,
            CancellationToken cancellationToken = default);
    }
}

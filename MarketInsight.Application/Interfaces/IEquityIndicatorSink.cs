using MarketInsight.Application.Engine;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarketInsight.Application.Interfaces
{
    /// <summary>
    /// Abstraction for persisting computed indicator values.
    /// Implemented in Infrastructure (e.g., SQL writer).
    /// </summary>
    public interface IEquityIndicatorSink
    {
        Task UpsertAsync(
            IReadOnlyCollection<EquityIndicator> batch,
            CancellationToken cancellationToken = default);
    }
}

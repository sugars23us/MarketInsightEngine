using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarketInsight.Application.Engine
{
    /// <summary>
    /// Abstraction for persisting computed indicator values.
    /// Implemented in Infrastructure (e.g., SQL writer).
    /// </summary>
    public interface IIndicatorSink
    {
        Task UpsertAsync(
            IReadOnlyCollection<IndicatorValue> batch,
            CancellationToken cancellationToken = default);
    }
}

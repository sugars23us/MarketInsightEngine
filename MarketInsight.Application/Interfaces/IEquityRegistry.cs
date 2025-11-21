// MarketInsight.Application/Services/IEquityRegistry.cs
using System.Threading;
using System.Threading.Tasks;
using MarketInsight.Shared.DTOs;

namespace MarketInsight.Application.Interfaces
{
    /// <summary>
    /// Async abstraction to retrieve equity metadata (float shares, ADV, market cap, etc.).
    /// Implemented in Infrastructure (e.g., EF Core or cached lookup).
    /// </summary>
    public interface IEquityRegistry
    {
        /// <summary>
        /// Get equity metadata by primary key.
        /// </summary>
        Task<Equity?> GetEquityAsync(int equityId, CancellationToken ct = default);

        /// <summary>
        /// Get equity metadata by ticker symbol.
        /// </summary>
        Task<Equity?> GetEquityByTickerAsync(string ticker, CancellationToken ct = default);

        /// <summary>
        /// Get metadata by ticker — creates the equity record if it doesn't exist.
        /// Used during real-time ingestion for unknown symbols.
        /// </summary>
        Task<Equity> GetOrCreateEquityAsync(string ticker, CancellationToken ct = default);
    }
}
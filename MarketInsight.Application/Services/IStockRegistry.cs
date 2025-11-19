using System.Threading;
using System.Threading.Tasks;
using MarketInsight.Shared.DTOs;
using MarketInsight.Shared.Models;

namespace MarketInsight.Application.Services
{
    /// <summary>
    /// Async abstraction to retrieve ticker metadata (float, ADV, etc.).
    /// Implemented in Infrastructure (e.g. EF Core or ADO.NET).
    /// </summary>
    public interface IStockRegistry
    {
        Task<TickerMeta?> GetTickerMetaAsync(int stockId, CancellationToken ct = default);
        Task<TickerMeta?> GetTickerMetaAsync(string ticker, CancellationToken ct = default);
        Task<TickerMeta> CreateTickerMetaIfMissingAsync(string ticker, CancellationToken ct = default);
    }
}

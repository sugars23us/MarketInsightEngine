// MarketInsight.Application/Engine/IndicatorEngine.cs
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using MarketInsight.Shared.DTOs;

namespace MarketInsight.Application.Engine
{
    public sealed class IndicatorEngine
    {
        private readonly IReadOnlyList<IIndicatorCalculator> _calculators;
        private readonly ConcurrentDictionary<(int, byte), SymbolSession> _sessions = new();
        private readonly TimeZoneInfo _marketTz;

        public IndicatorEngine(IEnumerable<IIndicatorCalculator> calculators,
                               TimeZoneInfo? marketTimeZone = null)
        {
            _calculators = new List<IIndicatorCalculator>(calculators ?? Array.Empty<IIndicatorCalculator>());
            _marketTz = marketTimeZone ?? TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        }

        /// <summary>
        /// Process a single bar, producing zero or more indicator values.
        /// The optional <paramref name="tickerMetadata"/> is attached to the session and used by FLOW calculator.
        /// </summary>
        public IReadOnlyList<IndicatorValue> ProcessBar(MarketBar bar, TickerMeta? tickerMetadata)
        {
            var nyLocal = TimeZoneInfo.ConvertTimeFromUtc(bar.TsUtc, _marketTz);
            var sessionDay = DateOnly.FromDateTime(nyLocal.Date);
            var key = (bar.StockId, bar.TimeframeId);

            var session = _sessions.AddOrUpdate(
                key,
                _ =>
                {
                    var symbolSession = new SymbolSession(bar.StockId, bar.TimeframeId, sessionDay);
                    if (tickerMetadata != null) symbolSession.State["META"] = tickerMetadata;
                    foreach (var c in _calculators)
                        c.OnSessionStarted(symbolSession);
                    return symbolSession;
                },
                (_, existing) =>
                {
                    if (existing.SessionDate != sessionDay)
                    {
                        existing.Reset(sessionDay);
                        if (tickerMetadata != null) existing.State["META"] = tickerMetadata;
                        foreach (var c in _calculators)
                            c.OnSessionStarted(existing);
                    }
                    else if (tickerMetadata != null)
                    {
                        // update meta if it changed (e.g. on daily refresh)
                        existing.State["META"] = tickerMetadata;
                    }

                    return existing;
                });

            var writer = new IndicatorWriter();
            foreach (var calc in _calculators)
            {
                try
                {
                    calc.OnBar(bar, session, writer);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[IndicatorEngine] {calc.GetType().Name} error: {ex}");
                }
            }

            return writer.Flush();
        }
    }
}

// MarketInsight.Application/Engine/IndicatorEngine.cs
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using MarketInsight.Application.Interfaces;
using MarketInsight.Shared.DTOs;

namespace MarketInsight.Application.Engine
{
    public sealed class IndicatorEngine
    {
        private readonly IReadOnlyList<IIndicatorCalculator> _calculators;
        private readonly ConcurrentDictionary<(int EquityId, byte TimeframeId), SymbolSession> _sessions = new();
        private readonly TimeZoneInfo _marketTz;

        public IndicatorEngine(
            IEnumerable<IIndicatorCalculator> calculators,
            TimeZoneInfo? marketTimeZone = null)
        {
            _calculators = new List<IIndicatorCalculator>(calculators ?? Array.Empty<IIndicatorCalculator>());
            _marketTz = marketTimeZone ?? TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        }

        /// <summary>
        /// Process a single equity candle, producing zero or more indicator values.
        /// The optional <paramref name="equity"/> is attached to the session (used by FlowCalculator, etc.).
        /// </summary>
        public IReadOnlyList<EquityIndicator> ProcessCandle(EquityCandle candle, Equity? equity = null)
        {
            var nyLocal = TimeZoneInfo.ConvertTimeFromUtc(candle.TsUtc, _marketTz);
            var sessionDay = DateOnly.FromDateTime(nyLocal.Date);
            var key = (candle.EquityId, candle.TimeframeId);

            var session = _sessions.AddOrUpdate(
                key,
                _ =>
                {
                    var symbolSession = new SymbolSession(candle.EquityId, candle.TimeframeId, sessionDay);
                    if (equity != null)
                        symbolSession.State["EQUITY"] = equity;

                    foreach (var c in _calculators)
                        c.OnSessionStarted(symbolSession);

                    return symbolSession;
                },
                (_, existing) =>
                {
                    if (existing.SessionDate != sessionDay)
                    {
                        existing.Reset(sessionDay);
                        if (equity != null)
                            existing.State["EQUITY"] = equity;

                        foreach (var c in _calculators)
                            c.OnSessionStarted(existing);
                    }
                    else if (equity != null)
                    {
                        // Update metadata if changed (e.g., daily refresh)
                        existing.State["EQUITY"] = equity;
                    }

                    return existing;
                });

            var writer = new IndicatorWriter();

            foreach (var calc in _calculators)
            {
                try
                {
                    calc.OnCandle(candle, session, writer);
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
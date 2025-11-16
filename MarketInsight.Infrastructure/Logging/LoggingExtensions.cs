using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MarketInsight.Infrastructure.Logging
{
    /// <summary>
    /// Opinionated logging setup using Microsoft.Extensions.Logging.
    /// No external deps. Adds Console + Debug + scopes + UTC timestamps.
    /// </summary>
    public static class LoggingExtensions
    {
        /// <summary>
        /// Configure logging with Console + Debug providers and useful options.
        /// Example call in Program.cs:
        ///   builder.Services.AddMarketInsightLogging(builder.Configuration);
        /// </summary>
        public static IServiceCollection AddMarketInsightLogging(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddLogging(builder =>
            {
                builder.ClearProviders();

                builder.AddDebug();

                builder.AddConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
                    options.UseUtcTimestamp = true;
                });

                // Respect log levels from config (appsettings.json: Logging:LogLevel:Default)
                builder.AddConfiguration(configuration.GetSection("Logging"));
            });

            return services;
        }

        /// <summary>
        /// Helper to create a named scope with (key,value) — reduces boilerplate.
        /// Usage:
        /// using var _ = logger.BeginKeyScope(("Ticker","TSLA"), ("Tf","1m"));
        /// </summary>
        public static IDisposable BeginKeyScope(this ILogger logger, params (string Key, object? Value)[] kvs)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (kvs == null || kvs.Length == 0) return NullScope.Instance;

            var state = new System.Collections.Generic.Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var (k, v) in kvs) state[k] = v ?? "";
            return logger.BeginScope(state);
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
